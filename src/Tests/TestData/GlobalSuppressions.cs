// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System;
using System.Diagnostics.CodeAnalysis;

[assembly: CLSCompliant(true)]
[assembly: SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "No Security Issue since the code is test putpose only", Scope = "namespaceanddescendants", Target = "~N:TestData.DataModel")]
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:TestData.DataModel")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "No globalization required in test data generators", Scope = "namespaceanddescendants", Target = "~N:TestData.DataModel")]
