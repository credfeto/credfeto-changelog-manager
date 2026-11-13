using System;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog.Mcp;

public static class Program
{
    public static Task<int> Main() => ServerHost.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
}
