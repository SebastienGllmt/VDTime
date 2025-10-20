Param(
    [int]$Port = 5055,
    [switch]$Build,
    [switch]$ShowBackendWindow
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

if ($Build) {
    Write-Host "Building solution (Debug, default platform)..." -ForegroundColor Cyan
    # Do not force Platform here; the solution only defines Any CPU.
    # The vdtime-winui project itself targets x64 via PlatformTarget/RuntimeIdentifier.
    dotnet build -c Debug | Out-Host
}

Write-Host "Starting vdtime-core on port $Port..." -ForegroundColor Cyan
$backendArgs = "run --project `"vdtime-core`" -- --port=$Port"
try {
    $backend = Start-Process -FilePath dotnet -ArgumentList $backendArgs -WorkingDirectory $root -PassThru -NoNewWindow:(!$ShowBackendWindow) -WindowStyle Hidden
} catch {
    Write-Error "Failed to start vdtime-core: $($_.Exception.Message)"
    exit 1
}

# Wait for health endpoint
$healthUrl = "http://localhost:$Port/healthz"
$healthy = $false
for ($i = 0; $i -lt 50; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) { $healthy = $true; break }
    } catch {
        Start-Sleep -Milliseconds 200
    }
}
if ($healthy) {
    Write-Host "vdtime-core is healthy at $healthUrl" -ForegroundColor Green
} else {
    Write-Warning "vdtime-core did not report healthy; continuing anyway."
}

try {
    # Run GUI with PORT environment variable
    $env:PORT = "$Port"
    Write-Host "Launching vdtime-gui (GUI)..." -ForegroundColor Cyan
    # Do not force Platform; project already targets x64 via PlatformTarget.
    dotnet run --project "vdtime-gui" -c Debug | Out-Host
} finally {
    # Cleanup backend when GUI exits
    if ($backend -and -not $backend.HasExited) {
        Write-Host "Stopping vdtime-core (pid $($backend.Id))..." -ForegroundColor Yellow
        try { Stop-Process -Id $backend.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
}
