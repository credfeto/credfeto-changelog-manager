using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("The folder this was started in no longer exists")]
public sealed partial class StartFolderMissingException : Exception;
