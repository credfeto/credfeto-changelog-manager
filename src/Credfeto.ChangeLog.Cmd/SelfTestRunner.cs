using System;
using System.IO;
using Credfeto.ChangeLog.Cmd.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.ChangeLog.Cmd;

public static class SelfTestRunner
{
    public static void Run(Func<ServiceProvider> buildServiceProvider, TextWriter output, TextWriter error)
    {
        try
        {
            using ServiceProvider services = buildServiceProvider();

            ResolveAllServices(services);
        }
        catch (Exception exception)
        {
            string reason = exception.Message.ReplaceLineEndings(" ");
            error.WriteLine($"SELF-TEST FAILED: {reason}");

            throw new SelfTestFailedException(message: reason, innerException: exception);
        }

        output.WriteLine("Self-test passed");
    }

    private static void ResolveAllServices(IServiceProvider services)
    {
        services.GetRequiredService<IChangeLogDetector>();
        services.GetRequiredService<IChangeLogReader>();
        services.GetRequiredService<IChangeLogUpdater>();
        services.GetRequiredService<IChangeLogChecker>();
        services.GetRequiredService<IChangeLogLinter>();
        services.GetRequiredService<IChangeLogFixer>();
        services.GetRequiredService<IChangeLogLanguageFactory>().Get(ChangeLogLanguageFactory.English);
    }
}
