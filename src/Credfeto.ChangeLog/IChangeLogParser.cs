using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog;

public interface IChangeLogParser
{
    ValueTask<ChangeLogDocument> ParseAsync(
        string content,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    );
}
