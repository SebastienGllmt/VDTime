using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using WindowsDesktop;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var port = GetPort(args);

            // Capture desktops at startup
            var current = VirtualDesktop.Current;
            var vdList = VirtualDesktop.GetDesktops();
            var desktops = new List<DesktopInfo>();
            foreach (var d in vdList)
            {
                desktops.Add(new DesktopInfo
                {
                    Id = d.Id,
                    Name = current.Name,
                });
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://localhost:{port}");
            var app = builder.Build();

            app.MapGet("/healthz", () => Results.Ok("ok"));
            app.MapGet("/get_desktops", () => Results.Json(desktops, new JsonSerializerOptions
            {
                WriteIndented = false
            }));

            Console.WriteLine($"vdtime-core listening on http://localhost:{port}");
            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"vdtime: failed to start API: {ex.Message}");
            return 1;
        }
    }

    private static int GetPort(string[] args)
    {
        const int defaultPort = 5055;
        foreach (var a in args)
        {
            if (a.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
            {
                var s = a.Substring("--port=".Length);
                if (int.TryParse(s, out var p) && p > 0 && p < 65536)
                    return p;
            }
        }
        var env = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrEmpty(env) && int.TryParse(env, out var pe) && pe > 0 && pe < 65536)
            return pe;
        return defaultPort;
    }
}

public struct DesktopInfo
{
    public string Name { get; set; }

    public Guid Id { get; set; }
}
