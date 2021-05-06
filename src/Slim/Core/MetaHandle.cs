/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;


namespace Slim.Core
{
  /// <summary>
  /// Represents a tuple of an unsigned integer with optional int or string metadata. If metadata is null then integer is stored by itself in an efficient way.
  /// The type is useful for storage of handles/indexes (such as pointer surrogates) with optional description of pointed-to data (such as type information).
  /// A special case is reserved for strings which are immutable yet reference types, in which case a special handle INLINED_STRING_HANDLE is set to indicate that
  ///  "Metadata" really contains string data that this handle should resolve into. Check "IsInlinedString" property to see if string was inlined.
  /// Check "IsInlinedValueType" is set to true when a struct/valuetype is inlined and "Metadata" contains type spec
  /// </summary>
  [Serializable]
  public struct MetaHandle : IEquatable<MetaHandle>
  {
    internal const uint InlinedStringHandle = 0;
    internal const uint InlinedValuetypeHandle = 1;
    internal const uint InlinedReftypeHandle = 2;
    internal const uint InlinedTypevalHandle = 3;
    internal const uint HandleOffset = 4;


    internal uint m_Handle;
    private VarIntStr? m_Metadata;


    /// <summary>
    /// Returns handle value. This value is invalid if special conditions such as inlining are true
    /// </summary>
    public uint Handle { get { unchecked { return m_Handle - HandleOffset; } } }

    /// <summary>
    /// Indicates whether a string instance is inlined in Metadata property
    /// </summary>
    public bool IsInlinedString => m_Handle == InlinedStringHandle;

    /// <summary>
    /// Indicates whether a struct (value type) instance is inlined right after this handle and Metadata property contains type
    /// </summary>
    public bool IsInlinedValueType => m_Handle == InlinedValuetypeHandle;

    /// <summary>
    /// Indicates whether a reference (reference type) instance is inlined right after this handle and Metadata property contains type.
    /// This is used for handling of ref types that are natively supported by streamers
    /// </summary>
    public bool IsInlinedRefType => m_Handle == InlinedReftypeHandle;

    /// <summary>
    /// Indicates whether a reference to TYPE is inlined - that is a Metadata parameter points to the value of type (reference to Type)
    /// </summary>
    public bool IsInlinedTypeValue => m_Handle == InlinedTypevalHandle;


    public VarIntStr? Metadata => m_Metadata;
    public uint IntMetadata => m_Metadata.HasValue ? m_Metadata.Value.IntValue : 0;
    public string StringMetadata => m_Metadata.HasValue ? m_Metadata.Value.StringValue : null;


    public MetaHandle(uint handle)
    {
      m_Handle = handle + HandleOffset;
      m_Metadata = null;
    }

    public MetaHandle(bool serializer, uint handle)
    {
      m_Handle = handle + (serializer ? 0 : HandleOffset);
      m_Metadata = null;
    }

    public MetaHandle(uint handle, VarIntStr? metadata)
    {
      m_Handle = handle + HandleOffset;
      m_Metadata = metadata;
    }

    internal MetaHandle(bool serializer, uint handle, VarIntStr? metadata)
    {
      m_Handle = handle + (serializer ? 0 : HandleOffset);
      m_Metadata = metadata;
    }

    /// <summary>
    /// Inlines string instance instead of pointer handle
    /// </summary>
    public static MetaHandle InlineString(string inlinedString)
    {
      var result = new MetaHandle();
      result.m_Handle = InlinedStringHandle;
      result.m_Metadata = new VarIntStr(inlinedString);
      return result;
    }


    /// <summary>
    /// Inlines value type instead of pointer handle
    /// </summary>
    public static MetaHandle InlineValueType(VarIntStr? inlinedValueType)
    {
      var result = new MetaHandle();
      result.m_Handle = InlinedValuetypeHandle;
      result.m_Metadata = inlinedValueType;
      return result;
    }

    /// <summary>
    /// Inlines ref type instead of pointer handle
    /// </summary>
    public static MetaHandle InlineRefType(VarIntStr? inlinedRefType)
    {
      var result = new MetaHandle();
      result.m_Handle = InlinedReftypeHandle;
      result.m_Metadata = inlinedRefType;
      return result;
    }

    /// <summary>
    /// Inlines type value instead of pointer handle
    /// </summary>
    public static MetaHandle InlineTypeValue(VarIntStr? inlinedTypeValue)
    {
      var result = new MetaHandle();
      result.m_Handle = InlinedTypevalHandle;
      result.m_Metadata = inlinedTypeValue;
      return result;
    }

    public override string ToString()
    {
      return string.Format("[{0}] {1}",
           IsInlinedString ? "string" : IsInlinedValueType ? "struct" : IsInlinedRefType ? "refobj" : Handle.ToString(),
           Metadata);
    }

    public override int GetHashCode()
    {
      return m_Handle.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null) return false;
      var other = (MetaHandle)obj;
      return this.Equals(other);
    }

    public bool Equals(MetaHandle other)
    {
      var h1 = m_Metadata.HasValue;
      var h2 = other.m_Metadata.HasValue;

      return m_Handle == other.m_Handle &&
            ((!h1 && !h2) || (h1 && h2 && m_Metadata.Value.Equals(other.m_Metadata.Value)));
    }

    public static bool operator ==(MetaHandle left, MetaHandle right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(MetaHandle left, MetaHandle right)
    {
      return !(left == right);
    }
  }
}
