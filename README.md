# Changelog manager .net tool

Changelog manager is a .net tool that works on all supported .net platforms (windows/linux/mac).

## Build Status

| Branch  | Status                                                                                                                                                                                                                                                    |
|---------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| main    | [![Build: Pre-Release](https://github.com/credfeto/credfeto-changelog-manager/actions/workflows/build-and-publish-pre-release.yml/badge.svg)](https://github.com/credfeto/credfeto-changelog-manager/actions/workflows/build-and-publish-pre-release.yml) |
| release | [![Build: Release](https://github.com/credfeto/credfeto-changelog-manager/actions/workflows/build-and-publish-release.yml/badge.svg)](https://github.com/credfeto/credfeto-changelog-manager/actions/workflows/build-and-publish-release.yml)             |

## Release Notes

See [CHANGELOG.md](CHANGELOG.md) managed by this tool.

## Installation

### Install as a global tool

```shell
dotnet tool install Credfeto.ChangeLog.Cmd
```

To update to latest released version

```shell
dotnet tool update Credfeto.ChangeLog.Cmd
```

### Install as a local tool

```shell
dotnet new tool-manifest
dotnet tool install Credfeto.ChangeLog.Cmd --local
```

To update to latest released version

```shell
dotnet tool update Credfeto.ChangeLog.Cmd --local
```

## Usage

Common notes

- The parameter `-changelog CHANGELOG.md` is optional and the changelog will be searched for in the current git repo.  Its is given explicitly in the examples assuming CHANGELOG.md is in the current directory.
- If CHANGELOG.md doesn't exist calling `-add` with and when using `-changelog CHANGELOG.md` will create a file at that location.  It will error if `-changelog CHANGELOG.md` is not used.

### Extracting Release Notes

#### Extract the release notes for a pre-release build

```shell
dotnet changelog --changelog CHANGELOG.md --extract RELEASE_NOTES.md --version 1.0.1.27-master
```

#### Extract the release notes for a release build

```shell
dotnet changelog --changelog CHANGELOG.md --extract RELEASE_NOTES.md --version 1.0.2.77
```

### Add a new entry to the [Unreleased] section

Note the value of the `--add` parameter matches exactly the heading section in the [Unreleased] section.  It will not create/update a new section if it that section does not exist.

```shell
dotnet changelog --changelog CHANGELOG.md --add Added --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Changed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Fixed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add Removed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --add "Deployment Changes" --message "Change description"
```

### Remove an entry from the [Unreleased] section

Note the value of the `--add` parameter matches exactly the heading section in the [Unreleased] section. It will not
create/update a new section if it that section does not exist.

```shell
dotnet changelog --changelog CHANGELOG.md --remove Added --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Changed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Fixed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove Removed --message "Change description"
```

```shell
dotnet changelog --changelog CHANGELOG.md --remove "Deployment Changes" --message "Change description"
```

### Create a release

This pulls out all the changes in the [Unreleased] section and adds them to to the release given by the version number
and sets the release date to be today.

```shell
dotnet changelog -changelog CHANGELOG.md --create-release 1.2.3
```

This pulls out all the changes in the [Unreleased] section and adds them to to the release given by the version number
and sets the release date to pending.

```shell
dotnet changelog -changelog CHANGELOG.md --create-release 1.2.3 --pending
```

Notes:

- If the specified version already exists then an error will occur.
- If the specified version is older than the latest release then an error will occur.

### Print Unreleased Content to Console

Prints the current [Unreleased] section to stdout.

```shell
dotnet changelog --changelog CHANGELOG.md --un-released
```

### Lint the Changelog

Validates the changelog format and reports any errors.

```shell
dotnet changelog --changelog CHANGELOG.md --lint
```

Lint with additional allowed sections (comma-separated):

```shell
dotnet changelog --changelog CHANGELOG.md --lint --additional-sections "Security,Performance"
```

Lint and automatically fix formatting issues (rewrites the file):

```shell
dotnet changelog --changelog CHANGELOG.md --lint --fix
```

Lint with additional sections and auto-fix:

```shell
dotnet changelog --changelog CHANGELOG.md --lint --additional-sections "Security,Performance" --fix
```

Notes:

- `--lint` exits with a non-zero exit code if any errors are found.
- `--fix` rewrites the changelog to correct ordering and missing sections in `[Unreleased]`; it then re-lints to confirm the file is valid.

### Check insert position of changes are all in [Unreleased] section

Where `origin/target` is the name of the branch a PR is to be merged into.

```shell
dotnet changelog --changelog CHANGELOG.md --check-insert origin/target
```

Notes

- Assumes that the branch to be merged into is up-to-date with the latest changes.
- Assumes that all entries have been committed to the local repo.
- All changes in non-committed (staged and unstaged) files are ignored.

### Self-test

Checks that the tool and all of its dependencies load correctly, without reading or writing any file, running git or accessing the network.
Useful as a container or install health check.

```shell
dotnet changelog --self-test
```

Notes:

- Exits with `0` and prints `Self-test passed` when everything loads.
- Exits with a non-zero exit code and writes a single `SELF-TEST FAILED: <reason>` line to stderr if anything fails to load.

## MCP Server

The same changelog operations are available to AI assistants through a Model Context Protocol (MCP) server that communicates over stdio.
It is shipped as a separate dotnet tool.

### Install the MCP server

```shell
dotnet tool install --global Credfeto.ChangeLog.Mcp
```

To update to latest released version

```shell
dotnet tool update --global Credfeto.ChangeLog.Mcp
```

### Configure an MCP client

Set the `CHANGELOG_MCP_REPO_ROOT` environment variable to the root of the repository whose changelog the server should manage.
This lets the server be started from any folder, for example your home folder.
For example, in a client that reads an `mcpServers` configuration:

```json
{
  "mcpServers": {
    "changelog": {
      "command": "changelog-mcp",
      "env": {
        "CHANGELOG_MCP_REPO_ROOT": "/path/to/repository"
      }
    }
  }
}
```

When `CHANGELOG_MCP_REPO_ROOT` is set it must be an absolute path to an existing folder.
Otherwise the server writes an error to stderr and exits with a non-zero exit code without starting.
The folder is used as the repository root as given: the server does not search above it, and it does not need to contain `.git`, to start or to resolve changelog paths.
`changelog_check_insert` compares the changelog with git history, so it needs the changelog to be inside a git working tree; it finds the git repository by searching upwards from the changelog's folder.
Set `CHANGELOG_MCP_REPO_ROOT` to the root of that git repository so the paths the tools report agree with the paths git uses.

When `CHANGELOG_MCP_REPO_ROOT` is not set, or is empty or only whitespace, start the server from inside the repository instead.
The server then uses the nearest folder containing `.git` at or above the folder it was started in as the repository root.

### Tools

| Tool                       | Description                                                                         |
|----------------------------|-------------------------------------------------------------------------------------|
| `changelog_add`            | Adds an entry to a section of the [Unreleased] section.                             |
| `changelog_remove`         | Removes an entry from a section of the [Unreleased] section.                        |
| `changelog_unreleased`     | Returns the content of the [Unreleased] section.                                    |
| `changelog_extract`        | Returns the release notes for a version as text.                                    |
| `changelog_create_release` | Moves the [Unreleased] changes into a new release, optionally with a pending date.  |
| `changelog_check_insert`   | Checks that all changes since a branch are only in the [Unreleased] section.        |
| `changelog_lint`           | Validates the changelog format and can optionally fix the problems it finds.        |

Notes:

- Every tool takes an optional `changelog` parameter. A relative path is resolved against `CHANGELOG_MCP_REPO_ROOT` when it is set, otherwise against the folder the server was started in.
- When `changelog` is omitted and `CHANGELOG_MCP_REPO_ROOT` is set, only `CHANGELOG.md` at that root is used; pass `changelog` for a changelog anywhere else.
  When `CHANGELOG_MCP_REPO_ROOT` is not set, the changelog is searched for in the current git repo, as the command line tool does.
- Every tool takes an optional `additionalSections` parameter: a comma-separated list of extra allowed section names.
- The changelog must be a file named `CHANGELOG.md` inside the repository; any other path is rejected.
- Results are returned as JSON, apart from `changelog_extract`, which returns the release notes as plain text.
- Failures are returned as tool errors rather than ending the server.

## Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->
