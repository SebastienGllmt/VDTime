using System.Text.Json;
using System.IO.Pipes;
using System.Text;

public static class NamedPipeAdaptor
{
  public static void createNamedPipeAdaptor(StateManager stateManager, string pipeName)
  {
    Console.WriteLine($"vdtime-core listening on named pipe: {pipeName}");
    
    // Start the named pipe server in a separate task to avoid blocking
    Task.Run(async () =>
    {
      while (true)
      {
        try
        {
          using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
          await pipeServer.WaitForConnectionAsync();
          Console.WriteLine("Client connected to named pipe");
          
          using var reader = new StreamReader(pipeServer, Encoding.UTF8);
          using var writer = new StreamWriter(pipeServer, Encoding.UTF8);
          
          while (pipeServer.IsConnected)
          {
            try
            {
              var command = await reader.ReadLineAsync();
              if (string.IsNullOrEmpty(command))
              {
                Console.WriteLine("Client disconnected (empty command)");
                break;
              }
              
              Console.WriteLine($"Received command: {command}");
              var response = await HandleCommand(stateManager, command);
              await writer.WriteLineAsync(response);
              await writer.FlushAsync();
              Console.WriteLine($"Sent response: {response}");
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error handling command: {ex.Message}");
              break;
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Named pipe error: {ex.Message}");
          await Task.Delay(1000); // Wait before retrying
        }
      }
    });
  }

  private static async Task<string> HandleCommand(StateManager stateManager, string command)
  {
    try
    {
      var parts = command.Split(' ', 2);
      var commandName = parts[0].ToLower();
      var args = parts.Length > 1 ? parts[1] : string.Empty;

      switch (commandName)
      {
        case "get_desktops":
          var desktops = stateManager.getDesktops();
          return JsonSerializer.Serialize(desktops, new JsonSerializerOptions { WriteIndented = false });

        case "curr_desktop":
          var currentDesktop = stateManager.getCurrentDesktop();
          return JsonSerializer.Serialize(currentDesktop, new JsonSerializerOptions { WriteIndented = false });

        case "time_on":
          return await HandleTimeOnCommand(stateManager, args);

        case "time_all":
          var timeAll = stateManager.getTimeAll();
          return JsonSerializer.Serialize(timeAll, new JsonSerializerOptions { WriteIndented = false });

        case "reset":
          stateManager.reset();
          return "ok";

        default:
          return JsonSerializer.Serialize(new { error = $"Unknown command: {commandName}" });
      }
    }
    catch (Exception ex)
    {
      return JsonSerializer.Serialize(new { error = ex.Message });
    }
  }

  private static Task<string> HandleTimeOnCommand(StateManager stateManager, string args)
  {
    if (args.StartsWith("name="))
    {
      var name = args.Substring(5);
      var time = stateManager.getTimeOn(name, null);
      return Task.FromResult(time.ToString());
    }
    else if (args.StartsWith("guid="))
    {
      var guid = args.Substring(5);
      var time = stateManager.getTimeOn(null, guid);
      return Task.FromResult(time.ToString());
    }
    else
    {
      return Task.FromResult(JsonSerializer.Serialize(new { error = "time_on requires name= or guid= parameter" }));
    }
  }
}
