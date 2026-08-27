using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using Credfeto.ChangeLog.Services;
using NSubstitute;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

[SuppressMessage(
    category: "Microsoft.Reliability",
    checkId: "CA2012:UseValueTasksCorrectly",
    Justification = "NSubstitute .Returns() for ValueTask is idiomatic test pattern"
)]
internal static class ChangeLogStorageMockHelper
{
    internal static void SetupLoad(IChangeLogStorage storage, ChangeLogDocument document)
    {
        storage.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(document));
    }

    internal static void SetupSave(IChangeLogStorage storage)
    {
        storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<ChangeLogDocument>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
    }
}
