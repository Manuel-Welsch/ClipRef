using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ClipRefTests")]

namespace ClipRef;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
