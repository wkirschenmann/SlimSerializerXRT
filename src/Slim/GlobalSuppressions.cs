// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System;
using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(true)]
[assembly: SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:Slim.Core")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:Slim.Core")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:Slim.Core")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:Slim.Core")]
