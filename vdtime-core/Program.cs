using System;
using Microsoft.Win32;
using WindowsDesktop;
using WinRT;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

public record State(
  DateTime LastUpdate,
  Guid? currentDesktop,
  DesktopInfo[] knownDesktops,
  Dictionary<Guid, UInt64> timeMap
)
{
  public State() : this(DateTime.Now, null, [], new Dictionary<Guid, ulong> { })
  {
    var list = new List<DesktopInfo>();
    foreach (var d in VirtualDesktop.GetDesktops())
    {
      var desktop = new DesktopInfo
      {
        Id = d.Id,
        Name = d.Name,
      };
      list.Add(desktop);
      timeMap.Add(desktop.Id, 0);
    }
    this.knownDesktops = list.ToArray();
    this.currentDesktop = VirtualDesktop.Current.Id;
  }

  public State applyTime()
  {
    var newTime = DateTime.Now;
    return this with
    {
      timeMap = (currentDesktop is Guid desktopId) ? new Dictionary<Guid, ulong>(timeMap)
      {
        [desktopId] = timeMap[desktopId] + this.timeSince(newTime)
      } : timeMap,
      LastUpdate = newTime,
    };
  }

  public UInt64 timeSince(DateTime newTime)
  {
    return (UInt64)((newTime - LastUpdate).TotalSeconds);
  }

  public DesktopInfo? FindDesktop(string nameOrGuid)
  {
    if (Guid.TryParse(nameOrGuid, out var gid))
    {
      var byId = Array.Find(this.knownDesktops, d => d.Id == gid);
      if (byId != null && !byId.Equals(default(DesktopInfo))) return byId;
    }
    var byName = Array.Find(this.knownDesktops, d => string.Equals(d.Name, nameOrGuid, StringComparison.OrdinalIgnoreCase));
    if (byName != null && !byName.Equals(default(DesktopInfo))) return byName;
    return null;
  }
}

public abstract record Action;
public record SetDesktop(Guid targetDesktop) : Action;
public record NewDesktop(DesktopInfo newDesktop) : Action;
public record UpdateDesktop(DesktopInfo updatedDesktop) : Action;
public record RemoveDesktop(Guid target) : Action;
public record SessionSwitch(SessionSwitchReason reason) : Action;
public record ResetTime() : Action;

public static class Reducer
{
  public static State Reduce(State s, Action a)
  {
    try
    {
      switch (a)
      {
        case SetDesktop x:
          {
            var newState = s.applyTime();
            return newState with { currentDesktop = x.targetDesktop };
          }
        case NewDesktop x:
          {
            var newState = s.applyTime();
            return newState with
            {
              knownDesktops = [.. newState.knownDesktops, x.newDesktop],
              timeMap = new Dictionary<Guid, ulong>(newState.timeMap)
              {
                [x.newDesktop.Id] = 0
              }
            };
          }
        case UpdateDesktop x:
          {
            var newState = s.applyTime();
            return newState with { knownDesktops = [.. newState.knownDesktops.Where(desktop => desktop.Id != x.updatedDesktop.Id), x.updatedDesktop] };
          }
        case RemoveDesktop x:
          {
            var newState = s.applyTime();
            return newState with
            {
              knownDesktops = newState.knownDesktops.Where(desktop => desktop.Id != x.target).ToArray(),
              timeMap = newState.timeMap.Where(kv => kv.Key != x.target)
                              .ToDictionary(kv => kv.Key, kv => kv.Value)
            };
          }
        case SessionSwitch x:
          {
            if (x.reason == SessionSwitchReason.SessionLock || x.reason == SessionSwitchReason.SessionLogoff)
            {
              var newState = s.applyTime();
              return newState with { currentDesktop = null };
            }
            if (x.reason == SessionSwitchReason.SessionUnlock || x.reason == SessionSwitchReason.SessionLogon)
            {
              return s with { LastUpdate = DateTime.Now, currentDesktop = VirtualDesktop.Current.Id };
            }
            else
            {
              return s;
            }
          }
        case ResetTime x:
          {
            var newState = s.applyTime();
            return s with
            {
              timeMap = newState.knownDesktops.ToDictionary(
                  d => d.Id,
                  d => (UInt64)0
              )
            };
          }
        default:
          throw new ArgumentException($"Unhandled action {a}");
      }
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      return s;
    }
  }
}

public class Program
{
  [STAThread]
  public static int Main(string[] args)
  {
    Option<int> portOption = new("--port")
    {
      Description = "Port to listen on",
      DefaultValueFactory = (_) => 5055
    };
    RootCommand rootCommand = new("Sample CLI for VDTime Core");
    rootCommand.Options.Add(portOption);
    ParseResult parseResult = rootCommand.Parse(args);
    if (parseResult.Errors.Count != 0)
    {
      foreach (ParseError parseError in parseResult.Errors)
      {
        Console.Error.WriteLine(parseError.Message);
      }
      return -1;
    }


    var state = new State();
    Console.WriteLine(state);

    VirtualDesktop.CurrentChanged += (_, __) =>
    {
      state = Reducer.Reduce(state, new SetDesktop(VirtualDesktop.Current.Id));
    };
    VirtualDesktop.Created += (_, e) =>
    {
      state = Reducer.Reduce(state, new NewDesktop(new DesktopInfo { Id = e.Id, Name = e.Name }));
      Console.WriteLine(state);
    };
    VirtualDesktop.Destroyed += (_, e) =>
    {
      state = Reducer.Reduce(state, new RemoveDesktop(e.Destroyed.Id));
      Console.WriteLine(state);
    };
    VirtualDesktop.Renamed += (_, e) =>
    {
      state = Reducer.Reduce(state, new UpdateDesktop(new DesktopInfo { Id = e.Desktop.Id, Name = e.Name }));
      Console.WriteLine(state);
    };
    SystemEvents.SessionSwitch += (_, e) =>
    {
      state = Reducer.Reduce(state, new SessionSwitch(e.Reason));
      Console.WriteLine(state);
    };

    if (parseResult.GetValue(portOption) is int port)
    {
      var builder = WebApplication.CreateBuilder(args);
      builder.WebHost.UseUrls($"http://localhost:{port}");
      var app = builder.Build();

      app.MapGet("/healthz", () => Results.Ok("ok"));
      app.MapGet("/get_desktops", () =>
      {
        var stateCopy = state;
        return Results.Json(stateCopy.knownDesktops, new JsonSerializerOptions { WriteIndented = false });
      });
      app.MapGet("/curr_desktop", () =>
      {
        var stateCopy = state;
        var desktop = Array.Find(stateCopy.knownDesktops, desktop => desktop.Id == stateCopy.currentDesktop)!;
        var timeSince = stateCopy.timeSince(DateTime.Now);
        var timeInfo = new TimeInfo
        {
          Current = timeSince,
          Total = stateCopy.timeMap[desktop.Id] + timeSince,
        };
        var fullInfo = new DesktopAndTime
        {
          Desktop = desktop,
          Time = timeInfo,
        };
        return Results.Json(fullInfo, new JsonSerializerOptions { WriteIndented = false });
      });
      app.MapGet("/time_on", (HttpRequest req) =>
      {
        var stateCopy = state;
        var name = req.Query["name"].ToString();
        var guid = req.Query["guid"].ToString();
        DesktopInfo? desktop = null;

        if (!string.IsNullOrWhiteSpace(name))
        {
          desktop = stateCopy.FindDesktop(name);
        }
        else if (!string.IsNullOrWhiteSpace(guid))
        {
          desktop = stateCopy.FindDesktop(guid);
        }
        else
        {
          return Results.BadRequest("missing name or guid");
        }
        if (desktop == null) return Results.NotFound("desktop not found");
        var timeSince = stateCopy.timeSince(DateTime.Now);
        return Results.Json(stateCopy.timeMap[desktop.Id]! + timeSince);
      });
      app.MapGet("/time_all", () =>
      {
        var stateCopy = state;
        var timeSince = stateCopy.timeSince(DateTime.Now);
        var desktops = stateCopy.knownDesktops.Select(desktop => new DesktopAndTime
        {
          Desktop = desktop,
          Time = new TimeInfo
          {
            Current = desktop.Id == stateCopy.currentDesktop ? timeSince : 0,
            Total = desktop.Id == stateCopy.currentDesktop ? timeSince + stateCopy.timeMap[desktop.Id] : stateCopy.timeMap[desktop.Id],
          },
        });
        return Results.Json(desktops, new JsonSerializerOptions { WriteIndented = false });
      });
      app.MapGet("/reset", () =>
      {
        state = Reducer.Reduce(state, new ResetTime());
        return Results.Accepted();
      });
      Console.WriteLine($"vdtime-core listening on http://localhost:{port}");
      app.Run();
    }
    return 1;
  }
}

public class DesktopAndTime
{
  public required DesktopInfo Desktop { get; set; }
  public required TimeInfo Time { get; set; }

  public override string ToString() => $"{Desktop.Name}({Desktop.Id}) = {Time.Total}s";
}

public class TimeInfo
{
  public required UInt64 Current { get; set; }
  public required UInt64 Total { get; set; }
}

public class DesktopInfo
{
  public required string Name { get; set; }

  public required Guid Id { get; set; }
}
