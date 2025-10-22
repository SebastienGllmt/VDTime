using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

// Shared fixture that starts the CLI with named pipe support once for all tests and stops it afterwards
public class NamedPipeFixture : IAsyncLifetime
{
    public string PipeName { get; private set; } = "vdtime-pipe";
    private Process? _proc;

    public async Task InitializeAsync()
    {
        _proc = StartServerWithNamedPipe(PipeName);
        
        // Wait a bit for the server to start up
        await Task.Delay(2000);
        
        // The server should be ready now. We don't test the connection here
        // because the named pipe server closes after each command, and we don't
        // want to consume the first connection in the fixture.
    }

    public Task DisposeAsync()
    {
        if (_proc != null)
        {
            TryKill(_proc);
            _proc = null;
        }
        return Task.CompletedTask;
    }

    private static Process StartServerWithNamedPipe(string pipeName)
    {
        // Resolve absolute path to the app project to avoid cwd issues
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var projectPath = Path.Combine(repoRoot, "vdtime-core", "vdtime-core.csproj");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -- --pipe={pipeName}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = true,
            CreateNoWindow = false
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

    public async Task<string> SendCommandAsync(string command)
    {
        using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipeClient.ConnectAsync(5000);
        
        using var writer = new StreamWriter(pipeClient, Encoding.UTF8, 1024, leaveOpen: true) {
            AutoFlush = true
        };
        using var reader = new StreamReader(pipeClient, Encoding.UTF8, false, 1024, leaveOpen: true);
        await writer.WriteLineAsync(command);
        
        var response = await reader.ReadLineAsync();
        return response ?? "";
    }
}

[CollectionDefinition("NamedPipe")]
public class NamedPipeCollection : ICollectionFixture<NamedPipeFixture> { }
