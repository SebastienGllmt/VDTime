# Virtual Desktop Time (VDTime)

Virtual Desktop Time is a Windows app that tracks how much time you spend on different virtual desktops

This project is a monorepo that contains two components:

## VDTime Core

VDTime Core tracks how much time you spend on each virtual desktop as a minimal program that simply exposes an API that other applications can query.
This allows you to query the time-per-desktop statistics easily to compose them with any productivity tracking application you use (or any you develop).

### Explicitly unsupported features

These features will never be supported by design:

- **No historical information**: VDTime Core does NOT store historical information anywhere (on disk or any remote server), and all information is lost when the application closes. It is the user's responsibility to persist the information anywhere if they are interested in doing so.
- **Only active tracking**: VDTime Core does not track when your computer is asleep, turned off, logged off, or in the lock screen. It only actively tracks when you're using the computer.
- **Older versions of Windows**: The code is only tested to work on Windows 11. Although it should work on Windows 10, no guarantee is given

### API

Command line args:

- `--port`: port to use for REST API

Rest endpoints:

- `get_desktops(): <Guid, name>[]` - returns all the virtual desktops the user has
- `curr_desktop(): <Guid, name>` - returns the current virtual desktop the user is on
- `time_on(nameOrGuid): string` - returns the time spent on a specific desktop by name or Guid
- `time_all(): <guid, <name, string>>` - returns the time spent on all desktops

Durations will be represented using seconds as a number

## Architecture

The core of this project is built using [IVirtualDesktopManager](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ivirtualdesktopmanager?redirectedfrom=MSDN) on top of the [C# bindings](https://github.com/Slion/VirtualDesktop)

### TODOs

- [ ] (medium) reset timers
- [ ] (medium) counter since last switch
- [ ] (low) option to start application when Windows boots

## VDTime GUI

VDTime GUI is a simple application (built with WinUI) that simply shows the current time you've spent on the current virtual desktop in a very small window.
The goal is that you can easily pin this window somewhere on your screen you can glance to get an idea how long you've spent on the window, without it being too large to be distracting.

### TODOs

- [x] (critical) Create a VDtime GUI project in my monorepo
- [x] (critical) Create a starter VDTime GUI program that can open and close (and does nothing else)
- [ ] (medium) Placeholder graphics that have two text boxes: one for the desktop name, one for the time spent
- [ ] (medium) Uses an API call to show the currently selected desktop
- [ ] (medium) Uses an API call to show the time spent on the currently selected desktop

## Build / Run

1. Run `dotnet restore`

### VDTime Core only

- Run in-memory: `dotnet run --project vdtime-core`
- Run tests: `dotnet test vdtime-core.Tests/vdtime-core.Tests.csproj`

### VDTime GUI only

- Project path: `vdtime-winui/vdtime-gui.csproj`
- Requirements: Visual Studio 2022 (17.7+) with Windows App SDK/WinUI 3 workload installed.
- Run steps:
  - Start Core API: run `vdtime-core` (defaults to `http://localhost:5055`).
  - Launch GUI: set startup project to `vdtime-winui` and run.
  - Optional: set `PORT` environment variable to point the GUI at a different Core API port.

Notes: The project is created as an Unpackaged WinUI 3 app for simpler local runs. It polls `/get_desktops` once per second and displays the list with Name, Id, and TimeSpent (seconds). Styling and compact UI can be refined next.

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
