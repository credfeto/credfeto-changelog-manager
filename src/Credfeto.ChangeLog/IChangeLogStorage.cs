using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogStorage
{
    ValueTask<ChangeLogDocument> LoadAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );

    ValueTask SaveAsync(
        string changeLogFileName,
        ChangeLogDocument document,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );
}
