// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System;
using System.Diagnostics.CodeAnalysis;

[assembly:CLSCompliant(true)]
[assembly: SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.TypeSchema.Serialize(Slim.Core.SlimWriter,Slim.Core.TypeRegistry,Slim.Core.RefPool,System.Object,System.Runtime.Serialization.StreamingContext,System.Boolean,System.Type)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.TypeSchema.DeserializeRootOrInner(Slim.Core.SlimReader,Slim.Core.TypeRegistry,Slim.Core.RefPool,System.Runtime.Serialization.StreamingContext,System.Boolean,System.Type)~System.Object")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.Arrays.DescriptorToArray(System.String,System.Type)~System.Array")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.TypeRegistry.GetTypeHandle(System.Type,System.Boolean)~Slim.Core.VarIntStr")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.RefPool.HandleToReference(Slim.Core.MetaHandle,Slim.Core.TypeRegistry,Slim.Core.SlimFormat,Slim.Core.SlimReader)~System.Object")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "Fix not compatible with netFW451", Scope = "member", Target = "~M:Slim.Core.VarIntStr.GetHashCode~System.Int32")]
