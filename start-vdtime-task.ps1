# Build/publish
dotnet publish .\vdtime-core\vdtime-core.csproj -c Release -r win-x64

# Paths
$publishDir = Resolve-Path 'vdtime-core\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish'
$exe        = Join-Path $publishDir 'vdtime-core.exe'
$taskArgs   = '--pipe vdtime-pipe'
$launcher   = Join-Path $publishDir 'run-vdtime-core.ps1'

# Create a tiny launcher that starts the app hidden
$launcherContent = @"
Start-Process -FilePath "$exe" -ArgumentList "$taskArgs" -WindowStyle Hidden -WorkingDirectory "$publishDir"
"@
Set-Content -Path $launcher -Value $launcherContent -Encoding UTF8

# Action points to PowerShell which runs the launcher hidden
$action  = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$launcher`"" -WorkingDirectory $publishDir
$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"

Register-ScheduledTask -TaskName 'vdtime-core' -Action $action -Trigger $trigger -Force
