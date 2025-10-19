using System;
using WindowsDesktop;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            // Get the virtual desktop that was active at program start
            var current = VirtualDesktop.Current;
            if (current == null)
            {
                Console.WriteLine("unknown");
                return 1;
            } else {
                Console.WriteLine("current");
                Console.WriteLine(current);
            }

            var desktops = VirtualDesktop.GetDesktops();
            if (desktops == null)
            {
                Console.WriteLine("no desktops");
            }
            else
            {
                Console.WriteLine("desktops");
                int i = 1;
                foreach (var d in desktops)
                {
                    var mark = d.Id == current.Id ? " *" : string.Empty;
                    Console.WriteLine($"{i}\t{d.Id}{mark}");
                    i++;
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"vdtime: failed to get virtual desktop: {ex.Message}");
            return 1;
        }
    }
}
