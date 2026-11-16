using System;
using System.Linq;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;
using NSubstitute;
using NSubstitute.Extensions;

namespace Credfeto.ChangeLog.Tests.TestHelpers;

internal static class ChangeLogStorageMock
{
    // Configured via ReturnsForAll rather than storage.LoadAsync(...).Returns(...): the latter calls a
    // ValueTask-returning member purely to use its result as a setup receiver, which CA2012 flags.
    // LoadAsync is the only IChangeLogStorage member that returns ValueTask<ChangeLogDocument>.
    public static void MockLoad(IChangeLogStorage storage, ChangeLogDocument document)
    {
        storage.ReturnsForAll<ValueTask<ChangeLogDocument>>(_ => ValueTask.FromResult(document));
    }

    // SaveAsync needs no setup: an unconfigured substitute returns default(ValueTask), which is
    // already completed. The saved document is read back from the substitute's received calls.
    public static ChangeLogDocument? LastSavedDocument(IChangeLogStorage storage)
    {
        return storage
            .ReceivedCalls()
            .Where(call =>
                StringComparer.Ordinal.Equals(x: call.GetMethodInfo().Name, y: nameof(IChangeLogStorage.SaveAsync))
            )
            .Select(call => call.GetArguments()[1])
            .OfType<ChangeLogDocument>()
            .LastOrDefault();
    }
}
