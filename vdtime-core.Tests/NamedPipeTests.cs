using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

[Collection("NamedPipe")]
public class NamedPipeTests
{
    private readonly NamedPipeFixture _fx;
    public NamedPipeTests(NamedPipeFixture fx) => _fx = fx;

    [Fact]
    public async Task AllEndpoints()
    {
        var response = await _fx.SendCommandAsync("get_desktops");
        var desktops = JsonSerializer.Deserialize<DesktopInfo[]>(response);
        // Get current desktop
        var currDesktopResponse = await _fx.SendCommandAsync("curr_desktop");
        var currDesktop = JsonSerializer.Deserialize<DesktopAndTime>(currDesktopResponse);
        
        // Wait 2 seconds so that time accumulates
        Thread.Sleep(TimeSpan.FromSeconds(2));
        
         // Get time by name
        var timeByName = await _fx.SendCommandAsync($"time_on name={currDesktop!.Desktop.Name}");
        var currDesktopTimeByName = UInt64.Parse(timeByName);
        // Get time by GUID
        var timeByGuid = await _fx.SendCommandAsync($"time_on guid={currDesktop!.Desktop.Id}");
        var currDesktopTimeByGuid = UInt64.Parse(timeByGuid);

        Assert.True(isClose(currDesktopTimeByGuid, currDesktopTimeByName));
        Assert.True(currDesktopTimeByGuid > 0);
        
        // Get all desktops with time
        var timeAllResponse = await _fx.SendCommandAsync("time_all");
        var allDesktops = JsonSerializer.Deserialize<DesktopAndTime[]>(timeAllResponse);
        Assert.True(allDesktops.Length > 0);
        Assert.True(isClose(Array.Find(allDesktops, desktop => desktop.Time.Current > 0)!.Time.Current, currDesktopTimeByGuid));
        
        // reset
        await _fx.SendCommandAsync("reset");

        // Find current desktop in the time_all results
        var timeByGuidAfterReset = await _fx.SendCommandAsync($"time_on guid={currDesktop!.Desktop.Id}");
        var timeByGuidValueAfterReset = UInt64.Parse(timeByGuidAfterReset);
        Assert.True(isClose(timeByGuidValueAfterReset, (UInt64)0));
    }

    private static bool isClose(UInt64 a, UInt64 b)
    {
        return Math.Abs((Int64)(a) - (Int64)(b)) <= 1;
    }
}
