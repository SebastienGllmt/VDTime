# Test script to verify the named pipe fix
Write-Host "Starting vdtime-core with named pipe..."
$process = Start-Process -FilePath "dotnet" -ArgumentList "run", "--pipe", "vdtime-pipe" -WorkingDirectory "vdtime-core" -PassThru -NoNewWindow

# Wait a moment for the server to start
Start-Sleep -Seconds 2

Write-Host "Testing named pipe connection..."
try {
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "vdtime-pipe", [System.IO.Pipes.PipeDirection]::InOut)
    $pipe.Connect(5000) # 5 second timeout
    
    $writer = New-Object System.IO.StreamWriter($pipe)
    $reader = New-Object System.IO.StreamReader($pipe)
    
    Write-Host "Testing get_desktops command..."
    $writer.WriteLine("get_desktops")
    $writer.Flush()
    $response = $reader.ReadLine()
    Write-Host "Response: $response"
    
    $pipe.Close()
    Write-Host "Test completed successfully!"
}
catch {
    Write-Host "Error: $($_.Exception.Message)"
}
finally {
    Write-Host "Stopping vdtime-core process..."
    $process.Kill()
    $process.WaitForExit()
    Write-Host "Process stopped."
}
