using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

[Collection("AppServer")]
public class GetDesktopsApiTests
{
    private readonly AppServerFixture _fx;
    public GetDesktopsApiTests(AppServerFixture fx) => _fx = fx;

    [Fact]
    public async Task GetDesktops_Returns_Array_And_200()
    {
        var resp = await _fx.Http.GetAsync($"{_fx.BaseUrl}/get_desktops");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}

