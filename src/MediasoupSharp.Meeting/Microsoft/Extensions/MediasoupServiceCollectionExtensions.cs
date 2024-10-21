using System.Text.Json;
using System.Text.Json.Serialization;
using MediasoupSharp.Meeting.Settings;
using MediasoupSharp.Meeting.SignalR.Models;
using MediasoupSharp.Meeting.SignalR.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediasoupSharp.Meeting.Microsoft.Extensions;

public static class MeetingServerServiceCollectionExtensions
{
    public static IServiceCollection AddMeetingServer(this IServiceCollection services, Action<MeetingServerOptions>? configure = null) => services.AddMeetingServer(new MeetingServerOptions(), configure);

    public static IServiceCollection AddMeetingServer(this IServiceCollection services, MeetingServerOptions meetingServerOptions, Action<MeetingServerOptions>? configure = null)
    {
        services
            .AddSignalR(options => { options.EnableDetailedErrors = true; })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        services.Replace(ServiceDescriptor.Singleton(typeof(IUserIdProvider), typeof(NameUserIdProvider)));
        return services.AddSingleton(x =>
            {
                var settings = x.GetRequiredService<IConfiguration>().GetSection(nameof(MeetingServerSettings)).Get<MeetingServerSettings>();
                if (settings is null)
                {
                    configure?.Invoke(meetingServerOptions);
                    return meetingServerOptions;
                }
                meetingServerOptions.ServeMode = settings.ServeMode;
                configure?.Invoke(meetingServerOptions);

                return meetingServerOptions;
            }).AddMediasoup()
            .AddSingleton<Scheduler>()
            .AddSingleton<BadDisconnectSocketService>();
    }
}