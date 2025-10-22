# Virtual Desktop Time (VDTime)

Virtual Desktop Time is a Windows app that tracks how much time you spend on different virtual desktops

This project is a monorepo that contains two components:

## VDTime Core

VDTime Core tracks how much time you spend on each virtual desktop as a minimal program that simply exposes an API that other applications can query.
This allows you to query the time-per-desktop statistics easily to compose them with any productivity tracking application you use (or any you develop).

### Key features

- Flexible backend you can integrate with different frontends
- Keep track of time on the current virtual desktop to integrate into your own routine like the [Pomodoro Techinque](https://en.wikipedia.org/wiki/Pomodoro_Technique)
- See your timer at a glance with win+

### Explicitly unsupported features

These features will never be supported by design:

- **No historical information**: VDTime Core does NOT store historical information anywhere (on disk or any remote server), and all information is lost when the application closes. It is the user's responsibility to persist the information anywhere if they are interested in doing so.
- **Only active tracking**: VDTime Core does not track when your computer is asleep, turned off, logged off, or in the lock screen. It only actively tracks when you're using the computer.
- **Older versions of Windows**: The code is only tested to work on Windows 11. Although it should work on Windows 10, no guarantee is given

### API

Command line args:

- `--port`: port to use for REST API
- `--pipe`: named pipe name to use for IPC communication

JSON structures:

- `DesktopInfo`: `{ name: string, id: guid }`
- `TimeInfo`: `{ current: number, total: number }`
- `DesktopAndTime`: `{ desktop: DesktopInfo, time: TimeInfo }`

### REST API

Rest endpoints:

- `get_desktops(): DesktopInfo[]` - returns all the virtual desktops the user has
- `curr_desktop(): DesktopAndTime` - returns the current virtual desktop the user is on
- `time_on(name | guid): uint64` - returns the time spent on a specific desktop by name or Guid
- `time_all(): DesktopAndTime[]` - returns the time spent on all desktops
- `reset()` - resets all timers

### Named Pipes API

As an alternative to the REST API, VDTime Core can also communicate via named pipes for local inter-process communication. Notably, this is used when running VDTime Core as a service on your machine (so you don't have to guess which port to query)

Named pipe commands (sent as text messages):

- `get_desktops` - returns JSON array of all virtual desktops
- `curr_desktop` - returns JSON object with current desktop and time info
- `time_on name=<desktop_name>` - returns time spent on desktop by name
- `time_on guid=<desktop_guid>` - returns time spent on desktop by GUID
- `time_all` - returns JSON array of all desktops with time info
- `reset` - resets all timers (note: state modification not fully implemented in pipe mode)

Example usage with PowerShell:

```powershell
# Connect to named pipe
$pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "vdtime-pipe", [System.IO.Pipes.PipeDirection]::InOut)
$pipe.Connect()

# Send commands
$writer = New-Object System.IO.StreamWriter($pipe)
$reader = New-Object System.IO.StreamReader($pipe)

$writer.WriteLine("get_desktops")
$writer.Flush()
$response = $reader.ReadLine()
Write-Host $response

$pipe.Close()
```

Durations will be represented using seconds as a number

## Architecture

The core of this project is built using [IVirtualDesktopManager](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ivirtualdesktopmanager?redirectedfrom=MSDN) on top of the [C# bindings](https://github.com/Slion/VirtualDesktop)

Internally, we keep track of a few key actions:

- Logon / Logoff the machine
- Lock / unlock the screen
- Switch virtual desktop
- Create new virtual desktop
- Delete virtual desktop
- Metadata update for virtual desktop

Instead of continuously polling the currently selected desktop (which would require the program to constantly be operational), we instead keep track of the last time since a key action has happened. Whenver a new key action happens, we do the following:

1. apply a state transition corresponding on new the action received
2. reset the timer

The corresponding state transition is as follows:

- Logon / Unlock: set currently selected desktop
- Logoff / Lock: unset currently selected desktop
- Create desktop: add to set of tracked desktops
- Delete desktop: remove from set of tracked desktops
- Metadata update: update name of tracked desktops (if required)

### TODOs

- [ ] (low) option to start application when Windows boots

## VDTime GUI

VDTime GUI is a simple application (built with WinUI) that simply shows the current time you've spent on the current virtual desktop in a very small window.
The goal is that you can easily pin this window somewhere on your screen you can glance to get an idea how long you've spent on the window, without it being too large to be distracting.

### TODOs

- [x] (critical) Create a VDtime GUI project in my monorepo
- [x] (critical) Create a starter VDTime GUI program that can open and close (and does nothing else)
- [ ] (medium) Placeholder graphics that have two text boxes: one for the desktop name, one for the time spent

## Build / Run

1. Run `dotnet restore`

### VDTime Core only

- Run with REST API: `dotnet run --project vdtime-core -- --port 5055`
- Run with named pipes: `dotnet run --project vdtime-core -- --pipe vdtime-pipe`

Run tests:

- Run REST tests: `dotnet test vdtime-core.Tests/vdtime-core.Tests.csproj --filter "FullyQualifiedName=RestApiTests.AllEndpoints"`
- Run Named Pipe tests: `dotnet test vdtime-core.Tests/vdtime-core.Tests.csproj --filter "FullyQualifiedName=NamedPipeTests.AllEndpoints"`

### VDTime GUI only

TODO

### VDTime Core & GUI

- Run: `./run-all.ps1 -Build -ShowBackendWindow`
- Options:
  - `-Port <int>` to change the API port (default 5055)
  - `-Build` to build the solution before running
  - `-ShowBackendWindow` to show the Core console window

Examples:

- `pwsh -f ./run-all.ps1`
- `pwsh -f ./run-all.ps1 -Port 5056 -Build`

## Implementation plan

- [ ] A simple WinUI app that does nothing but opens and can be closed
- [ ] A simple WinUI app that has two placeholder text boxes (current monitor, time spent) just using placeholder text that updates every second based on a mock API call that returns different random data every few seconds
- [ ] A simple WinUI app that shows the currently selected desktop, and the time spent on it (by querying the API)
- [ ] Run as a XBox Widget
