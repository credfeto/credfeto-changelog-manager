using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.ChangeLog.Extensions;
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

        string lineEnding = await DetectExistingLineEndingAsync(
            changeLogFileName: changeLogFileName,
            cancellationToken: cancellationToken
        );

        await File.WriteAllTextAsync(
            path: changeLogFileName,
            contents: content.WithLineEnding(lineEnding),
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );
    }

    private static async ValueTask<string> DetectExistingLineEndingAsync(
        string changeLogFileName,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(changeLogFileName))
        {
            return Environment.NewLine;
        }

        string existing = await File.ReadAllTextAsync(
            path: changeLogFileName,
            encoding: Encoding.UTF8,
            cancellationToken: cancellationToken
        );

        return existing.DetectLineEnding();
    }
}
