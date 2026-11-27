using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Changelog entry is too long.")]
public sealed partial class ChangeLogEntryTooLongException : Exception;
