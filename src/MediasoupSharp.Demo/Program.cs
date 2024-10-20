using System.Text.Json;
using System.Text.Json.Serialization;
using MediasoupSharp.Demo.Authorization;
using MediasoupSharp.Demo.Microsoft.AspNetCore.Builder;
using MediasoupSharp.Demo.Microsoft.Extensions;
using MediasoupSharp.Demo.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var builder     = WebApplication.CreateBuilder(args);



builder.Configuration
    .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
    .AddJsonFile("hosting.json", optional: true)
    .AddJsonFile("mediasoupsettings.json", optional: false)
    .AddJsonFile($"mediasoupsettings.{env}.json", optional: true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services
    .AddDistributedRedisCache(options =>
    {
        options.Configuration = "localhost";
        options.InstanceName  = "Meeting:";
    })
    .AddMemoryCache();
builder.Services.AddCors(options => options.AddPolicy("DefaultPolicy",
    policy =>
    {
        var sec = builder.Configuration.GetSection(nameof(CorsSettings)).Get<CorsSettings>();
        if (sec == null) return;
        policy.WithOrigins(sec.Origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    })
);
builder.Services.AddSingleton<ITokenService, TokenService>();
var tokenValidationSettings = builder.Configuration.GetSection(nameof(TokenValidationSettings))
    .Get<TokenValidationSettings>()!;
builder.Services.AddSingleton(tokenValidationSettings);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = tokenValidationSettings.ValidIssuer,
                        ValidateIssuer = true,

                        ValidAudience = tokenValidationSettings.ValidAudience,
                        ValidateAudience = true,

                        IssuerSigningKey = SignatureHelper.GenerateSymmetricSecurityKey(tokenValidationSettings.IssuerSigningKey),
                        ValidateIssuerSigningKey = true,

                        ValidateLifetime = tokenValidationSettings.ValidateLifetime,
                        ClockSkew = TimeSpan.FromSeconds(tokenValidationSettings.ClockSkewSeconds),
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            // If the request is for our hub...
                            var path = context.HttpContext.Request.Path;
                            if(!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            //_logger.LogError($"Authentication Failed(OnAuthenticationFailed): {context.Request.Path} Error: {context.Exception}");
                            if(context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Append("Token-Expired", "true");
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            //_logger.LogError($"Authentication Challenge(OnChallenge): {context.Request.Path}");
                            var body = "{\"code\": 400, \"message\": \"Authentication Challenge\"}"u8.ToArray();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            await context.Response.Body.WriteAsync(body);
                            context.HandleResponse();
                        }
                    };
                });
builder.Services.AddMeetingServer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("DefaultPolicy");

app.MapControllers();
app.UseMeetingServer();


app.MapGet("/api/Health", async context => await context.Response.WriteAsync("ok"));

app.Run();
