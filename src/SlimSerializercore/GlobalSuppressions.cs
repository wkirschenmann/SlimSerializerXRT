// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System;
using System.Diagnostics.CodeAnalysis;

//CA1307 cannot be corrected in a portable way
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:SlimSerializer.Core.Arrays.DescriptorToArray(System.String,System.Type)~System.Array")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:SlimSerializer.Core.CoreUtils.DisplayNameWithExpandedGenericArgs(System.Type)~System.String")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:SlimSerializer.Core.RefPool.HandleToReference(SlimSerializer.Core.MetaHandle,SlimSerializer.Core.TypeRegistry,SlimSerializer.Core.SlimFormat,SlimSerializer.Core.SlimReader)~System.Object")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:SlimSerializer.Core.TypeSchema.DeserializeRootOrInner(SlimSerializer.Core.SlimReader,SlimSerializer.Core.TypeRegistry,SlimSerializer.Core.RefPool,System.Runtime.Serialization.StreamingContext,System.Boolean,System.Type)~System.Object")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:SlimSerializer.Core.VarIntStr.GetHashCode~System.Int32")]


[assembly:CLSCompliant(true)]