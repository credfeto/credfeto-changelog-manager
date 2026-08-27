using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Models;

namespace Credfeto.ChangeLog.Services;

public sealed class FileSystemChangeLogStorage : IChangeLogStorage
{
    private readonly IChangeLogParser _parser;
    private readonly IChangeLogSerialiser _serialiser;

    public FileSystemChangeLogStorage(IChangeLogParser parser, IChangeLogSerialiser serialiser)
    {
        this._parser = parser;
        this._serialiser = serialiser;
    }

    public async ValueTask<ChangeLogDocument> LoadAsync(
        string changeLogFileName,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        string content = await File.ReadAllTextAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );

        return await this._parser.ParseAsync(
            content: content,
            language: language,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask SaveAsync(
        string changeLogFileName,
        ChangeLogDocument document,
        ChangeLogLanguage language,
        CancellationToken cancellationToken
    )
    {
        string content = await this._serialiser.SerialiseAsync(
            document: document,
            language: language,
            cancellationToken: cancellationToken
        );

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: content,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }
}
