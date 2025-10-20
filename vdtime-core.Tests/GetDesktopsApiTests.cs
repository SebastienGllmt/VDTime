using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;

[Collection("AppServer")]
public class GetDesktopsApiTests
{
    private readonly AppServerFixture _fx;
    public GetDesktopsApiTests(AppServerFixture fx) => _fx = fx;

    [Fact]
    public async Task Desktops()
    {
        var desktops = await callREST<DesktopInfo[]>($"{_fx.BaseUrl}/get_desktops", JsonValueKind.Array);
        var currDesktop = await callREST<DesktopInfo>($"{_fx.BaseUrl}/curr_desktop", JsonValueKind.Object);

        Thread.Sleep(TimeSpan.FromSeconds(2));

        var currDesktopTimeByGuid = await callREST<UInt64>($"{_fx.BaseUrl}/time_on?guid={currDesktop.Id}", JsonValueKind.Number);
        var currDesktopTimeByName = await callREST<UInt64>($"{_fx.BaseUrl}/time_on?name={currDesktop.Name}", JsonValueKind.Number);

        Assert.Equal(currDesktopTimeByGuid, currDesktopTimeByName);
        Assert.True(currDesktopTimeByGuid > 0);
    }

    private async Task<T> callREST<T>(string path, JsonValueKind kind)
    {
        var resp = await _fx.Http.GetAsync(path);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(kind, doc.RootElement.ValueKind);
        return JsonSerializer.Deserialize<T>(doc);
    }
}

