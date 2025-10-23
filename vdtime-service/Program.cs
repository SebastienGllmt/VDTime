using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting.WindowsServices;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            // Map simple flags to configuration keys so you can pass --pipe / --port
            .ConfigureAppConfiguration((_, config) =>
            {
                var switches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["--mode"] = "Service:Mode",
                    ["--port"] = "Service:Port",
                    ["--pipe"] = "Service:Pipe",
                    ["--servicename"] = "Service:ServiceName",
                };
                config.AddCommandLine(args, switches);
            })
            // Ensure content root is exe folder when running as a service
            .UseContentRoot(AppContext.BaseDirectory)
            .UseWindowsService()
            .ConfigureServices((ctx, services) =>
            {
                // Configure service name from config
                services.Configure<WindowsServiceLifetimeOptions>(options =>
                {
                    options.ServiceName = ctx.Configuration.GetValue<string>("Service:ServiceName") ?? "vdtime-core";
                });

                services.Configure<ServiceOptions>(ctx.Configuration.GetSection("Service"));
                services.AddHostedService<VdtimeWorker>();

                services.AddLogging(builder =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddEventLog();
                });
            });
}

public class ServiceOptions
{
    public string Mode { get; set; } = "pipe"; // "rest" or "pipe"
    public int? Port { get; set; }
    public string? Pipe { get; set; }
    public string ServiceName { get; set; } = "vdtime-core";
}
