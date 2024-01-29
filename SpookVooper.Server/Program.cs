using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SpookVooper.Server.Config;
using SpookVooper.Server.Workers;

namespace SpookVooper.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting SpookVooper Server...");
        
        // Create builder
        var builder = WebApplication.CreateBuilder(args);

        // Dev on linux will literally explode without this. Took a fun 5 hours to figure out.
        builder.WebHost.UseStaticWebAssets();
        
        // Load configs
        LoadConfigsAsync(builder);

        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            options.Configure(builder.Configuration.GetSection("Kestrel"));
            options.Listen(IPAddress.Any, 5000, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
            });
        }); 

        /*
        builder.WebHost.UseSentry(x =>
        {
            x.Release = typeof(ISharedUser).Assembly.GetName().Version.ToString();
            x.ServerName = NodeConfig.Instance.Name;
        });
        */

        // Set up services
        ConfigureServices(builder);

        // Build web app
        var app = builder.Build();

        // Configure application
        ConfigureApp(app);

        app.MapGet("/api/ping", () => "pong");
        
        await app.RunAsync();
    }

    public static void ConfigureApp(WebApplication app)
    {
        app.UseCors("AllowAll");

        if (app.Environment.IsDevelopment())
        {
            // app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpookVooper API V1");
        });

        app.UseWebSockets();
        // app.UseSentryTracing();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapControllers();

        app.MapFallbackToFile("index.html");

        // app.MapHub<CoreHub>(CoreHub.HubUrl, options => { options.AllowStatefulReconnects = true; });
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {

                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials()
                    .WithOrigins(
                    "https://www.spookvooper.com",
                    "http://www.spookvooper.com",
                    "https://spookvooper.com",
                    "http://spookvooper.com",
                    "https://api.spookvooper.com",
                    "http://api.spookvooper.com",
                    "http://localhost:3000",
                    "https://localhost:3000",
                    "http://localhost:3001",
                    "https://localhost:3001");
            });
        });

        // RateLimitDefs.AddRateLimitDefs(services);

        services.AddSignalR();

        services.AddHttpClient();

        services.Configure<FormOptions>(options =>
        {
            options.MemoryBufferThreshold = 20480000;
            options.MultipartBodyLengthLimit = 20480000;
        });

        /*
        services.AddDbContext<ValourDB>(options =>
        {
            options.UseNpgsql(ValourDB.ConnectionString);
        });
        */
        
        // This probably needs to be customized further but the documentation changed
        services.AddAuthentication().AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        services.AddControllersWithViews().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            //options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services.AddRazorPages();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        
        services.AddHostedService<ValourWorker>();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Valour API", Description = "The official Valour API", Version = "v1.0" });
            c.AddSecurityDefinition("Token", new OpenApiSecurityScheme()
            {
                Description = "The token used for authorizing your account.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Token"
            });
        });
    }

    /// <summary>
    /// Loads the json configs for services
    /// </summary>
    public static void LoadConfigsAsync(WebApplicationBuilder builder)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        
        builder.Configuration.GetSection("ValourConfig").Get<ValourConfig>();
        
    }
}

