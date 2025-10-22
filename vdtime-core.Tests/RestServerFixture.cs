using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

// Shared fixture that starts the CLI once for all tests and stops it afterwards
public class RestServerFixture : IAsyncLifetime
{
    public int Port { get; private set; } = 5087;
    public string BaseUrl => $"http://localhost:{Port}";
    public HttpClient Http { get; private set; } = new HttpClient();
    private Process? _proc;

    public async Task InitializeAsync()
    {
        _proc = StartServer(Port);
        var ready = await WaitUntilAsync(async () =>
        {
            try
            {
                var r = await Http.GetAsync($"{BaseUrl}/healthz");
                return r.IsSuccessStatusCode;
            }
            catch { return false; }
        }, TimeSpan.FromSeconds(5));

        if (!ready)
        {
            TryKill(_proc);
            throw new InvalidOperationException("Server did not become ready in time");
        }
    }

    public Task DisposeAsync()
    {
        if (_proc != null)
        {
            TryKill(_proc);
            _proc = null;
        }
        Http.Dispose();
        return Task.CompletedTask;
    }

    private static Process StartServer(int port)
    {
        // Resolve absolute path to the app project to avoid cwd issues
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var projectPath = Path.Combine(repoRoot, "vdtime-core", "vdtime-core.csproj");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -- --port={port}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var p = Process.Start(psi)!;
        return p;
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await predicate()) return true;
            await Task.Delay(200);
        }
        return false;
    }

    private static void TryKill(Process p)
    {
        try
        {
            if (!p.HasExited) p.Kill(true);
        }
        catch { }
    }
}

[CollectionDefinition("RestServer")]
public class RestServerCollection : ICollectionFixture<RestServerFixture> { }

