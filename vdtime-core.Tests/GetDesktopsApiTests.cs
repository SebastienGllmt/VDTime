using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net.Http;
using Xunit;

[Collection("AppServer")]
public class GetDesktopsApiTests
{
    private readonly AppServerFixture _fx;
    public GetDesktopsApiTests(AppServerFixture fx) => _fx = fx;

    [Fact]
    public async Task Desktops()
    {
        var desktops = await callParseREST<DesktopInfo[]>($"{_fx.BaseUrl}/get_desktops", JsonValueKind.Array);
        var currDesktop = await callParseREST<DesktopAndTime>($"{_fx.BaseUrl}/curr_desktop", JsonValueKind.Object);

        // wait 2 seconds so that time accumulates on a monitor
        // Careful: this test may fail if you switch monitors while it's waiting
        Thread.Sleep(TimeSpan.FromSeconds(2));

        // both ways of getting current time are equivalent
        var currDesktopTimeByGuid = await callParseREST<UInt64>($"{_fx.BaseUrl}/time_on?guid={currDesktop.Desktop.Id}", JsonValueKind.Number);
        var currDesktopTimeByName = await callParseREST<UInt64>($"{_fx.BaseUrl}/time_on?name={currDesktop.Desktop.Name}", JsonValueKind.Number);

        Assert.Equal(currDesktopTimeByGuid, currDesktopTimeByName);
        Assert.True(currDesktopTimeByGuid > 0);

        // current desktop included in time_all request
        var allDesktops = await callParseREST<DesktopAndTime[]>($"{_fx.BaseUrl}/time_all", JsonValueKind.Array);
        Assert.True(allDesktops.Length > 0);
        Assert.Equal(Array.Find(allDesktops, desktop => desktop.Time.Current > 0)!.Time.Current, currDesktopTimeByGuid);

        // reset properly set the times back to 0
        await callREST($"{_fx.BaseUrl}/reset");
        var currDesktopTimeByGuid = await callParseREST<UInt64>($"{_fx.BaseUrl}/time_on?guid={currDesktop.Desktop.Id}", JsonValueKind.Number);
        Assert.Equal(currDesktopTimeByGuid, 0);
    }

    private async Task<HttpResponseMessage> callREST(string path)
    {
        var resp = await _fx.Http.GetAsync(path);
        resp.EnsureSuccessStatusCode();
        return resp;
    }

    private async Task<T> callParseREST<T>(string path, JsonValueKind kind)
    {
        var resp = await callREST(path);
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(kind, doc.RootElement.ValueKind);
        return JsonSerializer.Deserialize<T>(doc);
    }
}

