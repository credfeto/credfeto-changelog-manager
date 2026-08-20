using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Could not find a common ancestor (merge base) between HEAD and the origin branch.")]
public sealed partial class MergeBaseNotFoundException : Exception;
