// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "No globalization required in test data generators", Scope = "namespaceanddescendants", Target = "~N:SerializedDataGenerator")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Fix not compatible with multi-targeting net45", Scope = "namespaceanddescendants", Target = "~N:SerializedDataGenerator")]
