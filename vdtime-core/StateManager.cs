// Facade
using Microsoft.Win32;
using WindowsDesktop;

// State manager Facade
public class StateManager {
  private State state;

  public StateManager() {
    this.state = new State();
  }

  public void listen() {
    VirtualDesktop.CurrentChanged += (_, __) =>
    {
      state = Reducer.Reduce(state, new SetDesktop(VirtualDesktop.Current.Id));
    };
    VirtualDesktop.Created += (_, e) =>
    {
      state = Reducer.Reduce(state, new NewDesktop(new DesktopInfo { Id = e.Id, Name = e.Name }));
    };
    VirtualDesktop.Destroyed += (_, e) =>
    {
      state = Reducer.Reduce(state, new RemoveDesktop(e.Destroyed.Id));
    };
    VirtualDesktop.Renamed += (_, e) =>
    {
      state = Reducer.Reduce(state, new UpdateDesktop(new DesktopInfo { Id = e.Desktop.Id, Name = e.Name }));
    };
    SystemEvents.SessionSwitch += (_, e) =>
    {
      state = Reducer.Reduce(state, new SessionSwitch(e.Reason));
    };
  }

  // Below are the API endpoints shared by different adaptors

  public DesktopInfo[] getDesktops() {
    var stateCopy = state;
    return stateCopy.knownDesktops;
  }

  public DesktopAndTime getCurrentDesktop() {
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
    return fullInfo;
  }

  public UInt64 getTimeOn(
    string? name,
    string? guid
  ) {
    var stateCopy = state;
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
      throw new Exception("missing name or guid");
    }
    if (desktop == null) throw new Exception("desktop not found");
    var timeSince = stateCopy.timeSince(DateTime.Now);
    return stateCopy.timeMap[desktop.Id]! + timeSince;
  }

  public DesktopAndTime[] getTimeAll(
  ) {
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

    return desktops.ToArray();
  }

  public void reset(
  ) {
    this.state = Reducer.Reduce(state, new ResetTime());
  }
}