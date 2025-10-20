# Virtual Desktop Time (VDTime)

Virtual Desktop Time is a Windows app that tracks how much time you spend on different virtual desktops

This project is a monorepo that contains two components:

## VDTime Core

VDTime Core tracks how much time you spend on each virtual desktop as a minimal program that simply exposes an API that other applications can query.
This allows you to query the time-per-desktop statistics easily to compose them with any productivity tracking application you use (or any you develop).

### Explicitly unsupported features

These features will never be supported by design:

- **No historical information**: VDTime Core does NOT store historical information anywhere (on disk or any remote server), and all information is lost when the application closes. It is the user's responsibility to persist the information anywhere if they are interested in doing so.
- **Only active tracking**: VDTime Core does not track when your computer is asleep, turned off, or in the lock screen. It only actively tracks when you're using the computer.
- **Older versions of Windows**: We only care about developing for Windows 11, and make absolutely no effort to support older versions of Windows.

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

- [ ] (low) option to start application when Windows boots

## VDTime GUI

VDTime GUI is a simple application (built with WinUI) that simply shows the current time you've spent on the current virtual desktop in a very small window.
The goal is that you can easily pin this window somewhere on your screen you can glance to get an idea how long you've spent on the window, without it being too large to be distracting.

### TODOs

- [ ] (critical) Create a VDtime GUI project in my monorepo
- [ ] (critical) Create a starter VDTime GUI program that can open and close (and does nothing else)
- [ ] (medium) Placeholder graphics that have two text boxes: one for the desktop name, one for the time spent
- [ ] (medium) Uses an API call to show the currently selected desktop
- [ ] (medium) Uses an API call to show the time spent on the currently selected desktop

## Implementation plan

### Before we start building

- [ ] Figure out if there is a way to detect if the user's computer is on the lock screen so that we don't advance the timers during that time

### Once we get building

- [ ] A simple WinUI app that does nothing but opens and can be closed
- [ ] A simple WinUI app that has two placeholder text boxes (current monitor, time spent) just using placeholder text that updates every second based on a mock API call that returns different random data every few seconds
- [ ] A simple WinUI app that shows the currently selected desktop, and the time spent on it (by querying the API)
