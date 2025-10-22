using System;
using Microsoft.Win32;
using WindowsDesktop;
using WinRT;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;

using System.Text.Json;
using System.IO.Pipes;
using System.Text;


public class Program
{
  [STAThread]
  public static int Main(string[] args)
  {
    Option<int?> portOption = new("--port")
    {
      Description = "Port to listen on",
    };
    Option<string?> pipeNameOption = new("--pipe")
    {
      Description = "Named pipe name to listen on",
    };
    RootCommand rootCommand = new("Sample CLI for VDTime Core");
    rootCommand.Options.Add(portOption);
    rootCommand.Options.Add(pipeNameOption);
    ParseResult parseResult = rootCommand.Parse(args);
    if (parseResult.Errors.Count != 0)
    {
      foreach (ParseError parseError in parseResult.Errors)
      {
        Console.Error.WriteLine(parseError.Message);
      }
      return -1;
    }

    var stateManager = new StateManager();
    stateManager.listen();

    if (parseResult.GetValue(portOption) is int port)
    {
      RestAdaptor.createRestAdaptor(stateManager, port);
    }
    else if (parseResult.GetValue(pipeNameOption) is string pipeName)
    {
      NamedPipeAdaptor.createNamedPipeAdaptor(stateManager, pipeName);
    }
    
    // Keep the main thread alive to allow the named pipe server to run
    if (Console.IsInputRedirected)
    {
      // If input is redirected (like in tests), just wait indefinitely
      Thread.Sleep(Timeout.Infinite);
    }
    else
    {
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }
    return 0;
  }
}
