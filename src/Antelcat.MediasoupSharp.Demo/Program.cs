using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Antelcat.AspNetCore.ProtooSharp;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.Demo;
using Antelcat.MediasoupSharp.Demo.Extensions;
using Antelcat.MediasoupSharp.Demo.Lib;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Primitives;
using Room = Antelcat.MediasoupSharp.Demo.Lib.Room;

Environment.SetEnvironmentVariable(MediasoupOptions.MEDIASOUP_WORKER_NUM, "2");
Environment.SetEnvironmentVariable(MediasoupOptions.MEDIASOUP_ANNOUNCED_IP, "192.168.1.105");

List<WorkerImpl<TWorkerAppData>> mediasoupWorkers       = [];
Dictionary<string, Room>         rooms                  = [];
var                              nextMediasoupWorkerIdx = 0;
WebSocketServer                  protooWebSocketServer;
AwaitQueue                       queue    = new();
FileExtensionContentTypeProvider provider = new();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
Logger.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();
var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
};
foreach (var converter in Mediasoup.JsonConverters)
{
    jsonSerializerOptions.Converters.Add(converter);
}

var options = MediasoupOptions<TWorkerAppData>.Default;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseWebSockets();

await RunAsync();

await app.RunAsync();

return;

async Task RunAsync()
{
    // Open the interactive server.
    Interactive.InteractiveServer();

    // Run a mediasoup Worker.
    await RunMediasoupWorkersAsync();

    // Run a protoo WebSocketServer.
    RunProtooWebSocketServer();

    // Create Express app.
    CreateExpressApp();
}

async Task RunMediasoupWorkersAsync()
{
    logger.LogInformation("running {Num} mediasoup Workers...", options.NumWorkers);

    var useWebRtcServer = Environment.GetEnvironmentVariable("MEDIASOUP_USE_WEBRTC_SERVER") != "false";

    Console.WriteLine(new AppSerialization().Serialize(options));
    foreach (var task in Mediasoup.CreateWorkers(options.WorkerSettings.NotNull(), options.NumWorkers.NotNull()))
    {
        var worker = await task;
        worker.On(static x => x.Died, async _ =>
        {
            logger.LogError("mediasoup Worker died, exiting in 2 seconds... [pid:{Pid}]", worker.Pid);

            await Task.Delay(2000);

            Environment.Exit(1);
        });

        mediasoupWorkers.Add(worker);

        if (!useWebRtcServer) continue;

        // Each mediasoup Worker will run its own WebRtcServer, so those cannot
        // share the same listening ports. Hence, we increase the value in config.js
        // for each Worker.
        var webRtcServerOptions = options.WebRtcServerOptions! with { };
        var portIncrement       = mediasoupWorkers.Count - 1;

        foreach (var listenInfo in webRtcServerOptions.ListenInfos)
        {
            listenInfo.Port += (ushort)portIncrement;
        }

        var webRtcServer = await worker.CreateWebRtcServerAsync<TWorkerAppData>(new()
        {
            ListenInfos = webRtcServerOptions.ListenInfos
        });

        worker.AppData["webRtcServer"] = webRtcServer;
    }
}

void CreateExpressApp()
{
    // For every API request, verify that the roomId in the path matches and
    // existing room.
    async ValueTask<object?> RoomFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate @delegate)
    {
        if (!context.HttpContext.Request.RouteValues.TryGetValue("roomId", out var id) || id is not string roomId)
        {
            return await @delegate(context);
        }

        var source = new TaskCompletionSource<object?>();
        queue.Push(async () =>
        {
            context.HttpContext.Items.Add("room", await GetOrCreateRoomAsync(roomId, 0));
            source.SetResult(await @delegate(context));
        }).Catch(exception => { source.SetException(exception ?? new NullReferenceException("No Exception")); });
        return await source.Task;
    }

    // API GET resource that returns the mediasoup Router RTP capabilities of
    // the room.
    app.MapGet("/rooms/{roomId}", (HttpContext context) =>
    {
        var data = context.Room().RouterRtpCapabilities;
        return data;
    }).AddEndpointFilter(RoomFilter);

    // POST API to create a Broadcaster.
    app.MapPost("/rooms/{roomId}/broadcasters", async (HttpContext context, [FromBody] CreateBroadcasterRequest json) =>
    {
        var data = await context.Room().CreateBroadcasterAsync(json);
        return data;
    }).AddEndpointFilter(RoomFilter);

    // DELETE API to delete a Broadcaster.
    app.MapDelete("/rooms/{roomId}/broadcasters/{broadcasterId}",
        async (HttpContext context, [FromRoute] string broadcasterId) =>
        {
            await context.Room().DeleteBroadcasterAsync(broadcasterId);
            return "broadcaster deleted";
        }).AddEndpointFilter(RoomFilter);

    // POST API to create a mediasoup Transport associated to a Broadcaster.
    // It can be a PlainTransport or a WebRtcTransport depending on the
    // type parameters in the body. There are also additional parameters for
    // PlainTransport.
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromBody] CreateBroadcastTransport json) =>
        {
            var data = await context.Room().CreateBroadcasterTransportAsync(json);
            return data;
        }).AddEndpointFilter(RoomFilter);

    // POST API to connect a Transport belonging to a Broadcaster. Not needed
    // for PlainTransport if it was created with comedia option set to true.
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports/{transportId}/connect",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromRoute] string transportId,
               [FromBody] ConnectBroadcasterTransportRequest json) =>
        {
            await context.Room().ConnectBroadcasterTransportAsync(broadcasterId, transportId, json.DtlsParameters);
            return Results.Ok();
        }).AddEndpointFilter(RoomFilter);

    // POST API to create a mediasoup Producer associated to a Broadcaster.
    // The exact Transport in which the Producer must be created is signaled in
    // the URL path. Body parameters include kind and rtpParameters of the
    // Producer.
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports/{transportId}/producers",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromRoute] string transportId,
               [FromBody] CreateBroadcasterProducerRequest json) =>
        {
            var (kind, rtpParameters) = json;
            var data = await context.Room()
                .CreateBroadcasterProducerAsync(broadcasterId, transportId, kind, rtpParameters);
            return data;
        }).AddEndpointFilter(RoomFilter);

    // POST API to create a mediasoup Consumer associated to a Broadcaster.
    // The exact Transport in which the Consumer must be created is signaled in
    // the URL path. Query parameters must include the desired producerId to
    // consume.
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports/{transportId}/consume",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromRoute] string transportId,
               [FromBody] ProducerRequest json) =>
        {
            var data = await context.Room().CreateBroadcasterConsumerAsync(broadcasterId, transportId, json.ProducerId);
            return data;
        }).AddEndpointFilter(RoomFilter);

    // POST API to create a mediasoup DataConsumer associated to a Broadcaster.
    // The exact Transport in which the DataConsumer must be created is signaled in
    // the URL path. Query body must include the desired producerId to
    // consume.
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports/{transportId}/consume/data",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromRoute] string transportId,
               [FromBody] DataProducerRequest json) =>
        {
            var data = await context.Room()
                .CreateBroadcasterDataConsumerAsync(broadcasterId, transportId, json.DataProducerId);
            return data;
        }).AddEndpointFilter(RoomFilter);

    // POST API to create a mediasoup DataProducer associated to a Broadcaster.
    // The exact Transport in which the DataProducer must be created is signaled in
    app.MapPost("/rooms/{roomId}/broadcasters/{broadcasterId}/transports/{transportId}/produce/data",
        async (HttpContext context, [FromRoute] string broadcasterId, [FromRoute] string transportId,
               [FromBody] ProduceDataRequest json) =>
        {
            var (_, sctpStreamParameters, label, protocol, appData) = json;
            var data = await context.Room()
                .CreateBroadcasterDataProducerAsync(broadcasterId,
                    transportId,
                    label,
                    protocol,
                    sctpStreamParameters,
                    appData);
            return data;
        }).AddEndpointFilter(RoomFilter);

    // Error handler.
    app.Use(async (context, func) =>
    {
        try
        {
            await func(context);
        }
        catch (Exception ex)
        {
            return;
        }
    });

    app.Map("/", async (HttpContext context) =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.File(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"),
                MediaTypeNames.Text.Html);
        }

        await protooWebSocketServer.OnRequest(context);
        return Results.Ok();
    }).AddEndpointFilter(RoomFilter);

    app.MapGet("/{**rest}", ([FromRoute] string rest) =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", WebUtility.HtmlDecode(rest));
        return File.Exists(path)
            ? Results.File(path,
                provider.TryGetContentType(rest, out var type) ? type : MediaTypeNames.Text.Plain)
            : Results.NotFound("file not found");
    });
}

//Create a protoo WebSocketServer to allow WebSocket connections from browsers.
void RunProtooWebSocketServer()
{
    Serialization.GlobalSerialization = new AppSerialization();

    logger.LogInformation("running protoo WebSocketServer...");

    // Create the protoo WebSocket server.
    protooWebSocketServer = new WebSocketServer(loggerFactory, new());

    // Handle connections from clients.
    protooWebSocketServer.ConnectionRequest += async (info, accept, reject) =>
    {
        // The client indicates the roomId and peerId in the URL query.
        var u = info.Request;

        var roomId = u.Query["roomId"].ToString();
        var peerId = u.Query["peerId"].ToString();

        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(peerId))
        {
            await reject(400, "Connection request without roomId and/or peerId");
            return;
        }

        var consumerReplicas = int.Parse(u.Query["consumerReplicas"] is var value
                                         && value != StringValues.Empty
                                         && value.ToString() is not "undefined"
            ? value.ToString()
            : "0");


        logger.LogInformation(
            "protoo connection request [roomId:{RoomId}, peerId:{PeerId}, address:{Address}, origin:{Origin}]",
            roomId, peerId, info.Request.HttpContext.Connection.RemoteIpAddress, info.Origin);

        // Serialize this code into the queue to avoid that two peers connecting at
        // the same time with the same roomId create two separate rooms with same
        // roomId.
        queue.Push(async () =>
            {
                var room = await GetOrCreateRoomAsync(roomId, consumerReplicas);

                // Accept the protoo WebSocket connection.
                var protooWebSocketTransport = await accept();

                room.HandleProtooConnection(peerId, false, protooWebSocketTransport!);
            })
            .Catch(async exception =>
            {
                logger.LogError("room creation or room joining failed:{Ex}", exception);

                await reject(500, exception!.Message);
            });
    };
}

//Get next mediasoup Worker.
WorkerImpl<TWorkerAppData> GetMediasoupWorker()
{
    var worker = mediasoupWorkers[nextMediasoupWorkerIdx];

    if (++nextMediasoupWorkerIdx == mediasoupWorkers.Count)
        nextMediasoupWorkerIdx = 0;

    return worker;
}

//Get a Room instance (or create one if it does not exist).
async Task<Room> GetOrCreateRoomAsync(string roomId, int consumerReplicas)
{
    if (rooms.TryGetValue(roomId, out var room)) return room;

    logger.LogInformation("creating a new Room [{RoomId}]", roomId);

    var mediasoupWorker = GetMediasoupWorker();

    room = await Room.CreateAsync(loggerFactory, options,
        mediasoupWorker,
        roomId,
        consumerReplicas);

    rooms.Add(roomId, room);
    room.On("close", () => rooms.Remove(roomId));

    return room;
}


file static class HttpContextExtension
{
    public static Room Room(this HttpContext context) =>
        context.Items["room"] as Room ?? throw new NullReferenceException("room");
}

file class AppSerialization : Serialization
{
    private readonly JsonSerializerOptions options;

    public AppSerialization()
    {
        options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        foreach (var converter in Mediasoup.JsonConverters) options.Converters.Add(converter);
    }

    public override string Serialize<T>(T instance) => JsonSerializer.Serialize(instance, options);

    public override T? Deserialize<T>(string json) where T : default => JsonSerializer.Deserialize<T>(json, options);
}