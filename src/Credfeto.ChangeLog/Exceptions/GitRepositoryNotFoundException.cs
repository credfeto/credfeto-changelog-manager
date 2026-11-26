using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Could not find a usable git repository.")]
public sealed partial class GitRepositoryNotFoundException : Exception;
