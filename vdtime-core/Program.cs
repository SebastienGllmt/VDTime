using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using WindowsDesktop;
using Microsoft.Win32;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var port = GetPort(args);

            // Current state. Should only ever be updated with the `syncLock`
            var desktops = new List<DesktopInfo>();
            VirtualDesktop? cur = VirtualDesktop.Current;

            var syncLock = new object();

            // Initialize tracked desktops
            {
                var vdList = VirtualDesktop.GetDesktops();
                foreach (var d in vdList)
                {
                    desktops.Add(new DesktopInfo
                    {
                        Id = d.Id,
                        Name = d.Name,
                        TimeSpent = 0,
                    });
                }
            }

            // Simple second-based timer to track active desktop time
            var timer = new System.Threading.Timer(_ =>
            {
                try
                {
                    lock (syncLock)
                    {
                        if (cur == null) return;
                        var idx = desktops.FindIndex(x => x.Id == cur.Id);
                        if (idx >= 0)
                        {
                            var entry = desktops[idx];
                            entry.TimeSpent += 1; // seconds
                            desktops[idx] = entry;
                            Console.WriteLine(desktops[idx]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"vdtime: timer error: {ex.Message}");
                }
            }, null, dueTime: 1000, period: 1000);

            // Listen to virtual desktop changes
            VirtualDesktop.CurrentChanged += (_, __) =>
            {
                try
                {
                    var newCurr = VirtualDesktop.Current;
                    lock (syncLock)
                    {
                        cur = newCurr;
                    }
                    Console.WriteLine($"Current desktop changed: {newCurr.Id} \"{newCurr.Name}\"");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"vdtime: CurrentChanged handler error: {ex.Message}");
                }
            };

            VirtualDesktop.Created += (_, e) =>
            {
                try
                {
                    lock (syncLock)
                    {
                        desktops.Add(new DesktopInfo { Id = e.Id, Name = e.Name, TimeSpent = 0 });
                    }
                    Console.WriteLine($"Desktop created: {e.Id} \"{e.Name}\"");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"vdtime: Created handler error: {ex.Message}");
                }
            };

            SystemEvents.SessionSwitch += (_, e) =>
            {
                if (e.Reason == SessionSwitchReason.SessionLock || e.Reason == SessionSwitchReason.SessionLogoff)
                {
                    lock (syncLock)
                    {

                        cur = null;
                    }
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock || e.Reason == SessionSwitchReason.SessionLogon)
                {
                    lock (syncLock)
                    {
                        cur = VirtualDesktop.Current;
                    }
                }
            };

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://localhost:{port}");
            var app = builder.Build();

            app.MapGet("/healthz", () => Results.Ok("ok"));
            app.MapGet("/get_desktops", () =>
            {
                lock (syncLock)
                {
                    return Results.Json(desktops, new JsonSerializerOptions { WriteIndented = false });
                }
            });
            app.MapGet("/curr_desktop", () =>
            {
                var cur = VirtualDesktop.Current;
                lock (syncLock)
                {
                    return Results.Json(desktops.Find(desktop => desktop.Id == cur.Id), new JsonSerializerOptions { WriteIndented = false });
                }
            });
            // Support both /time_on/{nameOrGuid} and /time_on?nameOrGuid=...
            app.MapGet("/time_on/{nameOrGuid}", (string nameOrGuid) =>
            {
                lock (syncLock)
                {
                    var di = FindDesktop(desktops, nameOrGuid);
                    if (di == null) return Results.NotFound("desktop not found");
                    return Results.Text(di.Value.TimeSpent.ToString());
                }
            });
            app.MapGet("/time_on", (HttpRequest req) =>
            {
                var name = req.Query["name"].ToString();
                var guid = req.Query["guid"].ToString();
                lock (syncLock)
                {
                    DesktopInfo? desktop = null;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        desktop = FindDesktop(desktops, name);
                    }
                    else if (!string.IsNullOrWhiteSpace(guid))
                    {
                        desktop = FindDesktop(desktops, guid);
                    }
                    else
                    {
                        return Results.BadRequest("missing name or guid");
                    }
                    if (desktop == null) return Results.NotFound("desktop not found");
                    return Results.Text(desktop.Value.TimeSpent.ToString());
                }
            });
            app.MapGet("/time_all", () =>
            {
                lock (syncLock)
                {
                    var payload = desktops.ToDictionary(d => d.Id, d => new { name = d.Name, time = d.TimeSpent.ToString() });
                    return Results.Json(payload, new JsonSerializerOptions { WriteIndented = false });
                }
            });

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

    private static DesktopInfo? FindDesktop(List<DesktopInfo> desktops, string nameOrGuid)
    {
        if (Guid.TryParse(nameOrGuid, out var gid))
        {
            var byId = desktops.Find(d => d.Id == gid);
            if (!byId.Equals(default(DesktopInfo))) return byId;
        }
        var byName = desktops.Find(d => string.Equals(d.Name, nameOrGuid, StringComparison.OrdinalIgnoreCase));
        if (!byName.Equals(default(DesktopInfo))) return byName;
        return null;
    }
}

public struct DesktopInfo
{
    public string Name { get; set; }

    public Guid Id { get; set; }

    public UInt64 TimeSpent { get; set; }

    public override string ToString() => $"{Name}({Id}) = {TimeSpent}s";
}
