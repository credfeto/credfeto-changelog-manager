using System;
using System.Collections.Generic;
using System.IO;
using Credfeto.ChangeLog.Extensions;

namespace Credfeto.ChangeLog.Helpers;

// git reports the physical working folder, and .NET has no realpath. ResolveLinkTarget joins a relative link target
// to the folder the link was reached through rather than the folder it sits in, so each link is followed here from
// its already-resolved parent instead. Path.GetFullPath collapses ".." as text before any link is followed, so only
// the root is made absolute with it and every name after the root is walked in order, the way the OS does.
public static class PhysicalPath
{
    private const int MAX_LINKS_FOLLOWED = 40;

    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static string Resolve(string path)
    {
        ReadOnlySpan<char> root = Path.GetPathRoot(path.AsSpan());
        string start = root.IsEmpty ? Directory.GetCurrentDirectory() : Path.GetFullPath(root.ToString());
        string resolved = Path.GetPathRoot(start.AsSpan()).ToString();

        Stack<string> pending = new();
        PushNames(pending: pending, path: path[root.Length..]);
        PushNames(pending: pending, path: start[resolved.Length..]);

        int linksFollowed = 0;

        while (pending.TryPop(out string? name))
        {
            if (name.EqualsOrdinal("."))
            {
                continue;
            }

            if (name.EqualsOrdinal(".."))
            {
                resolved = Path.GetDirectoryName(resolved) ?? resolved;

                continue;
            }

            string candidate = Path.Combine(path1: resolved, path2: name);
            string? target = new FileInfo(candidate).LinkTarget;

            if (target is null)
            {
                resolved = candidate;

                continue;
            }

            if (++linksFollowed > MAX_LINKS_FOLLOWED)
            {
                throw new IOException("Too many levels of symbolic links: \"" + candidate + "\"");
            }

            resolved = FollowLink(parent: resolved, target: target, pending: pending);
        }

        return resolved;
    }

    // A relative target continues from the link's own physical parent; an absolute one starts again from its root
    private static string FollowLink(string parent, string target, Stack<string> pending)
    {
        if (!Path.IsPathRooted(target))
        {
            PushNames(pending: pending, path: target);

            return parent;
        }

        PushNames(pending: pending, path: target[Path.GetPathRoot(target.AsSpan()).Length..]);

        return Path.GetPathRoot(Path.GetFullPath(path: target, basePath: parent).AsSpan()).ToString();
    }

    // Names are pushed last first so they pop in path order, ahead of whatever followed the link
    private static void PushNames(Stack<string> pending, string path)
    {
        string[] names = path.Split(separator: Separators, options: StringSplitOptions.RemoveEmptyEntries);

        for (int index = names.Length - 1; index >= 0; --index)
        {
            pending.Push(names[index]);
        }
    }
}
