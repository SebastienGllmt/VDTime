using Microsoft.Win32;
using WindowsDesktop;

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

/// <summary>
///  State Reducer Pattern
/// </summary>
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
            return newState with
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
    catch (Exception)
    {
      // Log error but don't use Console.WriteLine to avoid deadlocks in tests
      return s;
    }
  }
}
