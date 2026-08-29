using System.Threading;
using System.Threading.Tasks;

namespace Credfeto.ChangeLog;

public interface IChangeLogReader
{
    ValueTask<string> ExtractReleaseNotesFromFileAsync(
        string changeLogFileName,
        string version,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );

    ValueTask<int?> FindFirstReleaseVersionPositionAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );
}
