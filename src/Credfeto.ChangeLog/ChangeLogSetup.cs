using System;
using Credfeto.ChangeLog.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Credfeto.ChangeLog;

public static class ChangeLogSetup
{
    public static IServiceCollection AddChangeLog(this IServiceCollection services)
    {
        // TryAdd: a host that already registered its own TimeProvider (e.g. a FakeTimeProvider in
        // tests) keeps that registration instead of this library silently overriding it.
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddSingleton<IChangeLogParser, ChangeLogParser>()
            .AddSingleton<IChangeLogSerialiser, ChangeLogSerialiser>()
            .AddSingleton<IChangeLogLanguageFactory, ChangeLogLanguageFactory>()
            .AddSingleton<IChangeLogStorage, FileSystemChangeLogStorage>()
            .AddSingleton<IChangeLogReader, ChangeLogReader>()
            .AddSingleton<IChangeLogLinter, ChangeLogLinter>()
            .AddSingleton<IChangeLogFixer, ChangeLogFixer>()
            .AddSingleton<IChangeLogUpdater, ChangeLogUpdater>()
            .AddSingleton<IChangeLogChecker, ChangeLogChecker>()
            .AddSingleton<IChangeLogDetector, ChangeLogDetector>();
    }
}
