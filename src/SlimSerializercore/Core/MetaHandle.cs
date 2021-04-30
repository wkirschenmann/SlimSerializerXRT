/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Globalization;


namespace SlimSerializer.Core
{
  /// <summary>
  /// Represents a tuple of an unsigned integer with optional int or string metadata. If metadata is null then integer is stored by itself in an efficient way.
  /// The type is useful for storage of handles/indexes (such as pointer surrogates) with optional description of pointed-to data (such as type information).
  /// A special case is reserved for strings which are immutable yet reference types, in which case a special HandleValue INLINED_STRING_HANDLE is set to indicate that
  ///  "Metadata" really contains string data that this HandleValue should resolve into. Check "IsInlinedString" property to see if string was inlined.
  /// Check "IsInlinedValueType" is set to true when a struct/valuetype is inlined and "Metadata" contains type spec
  /// </summary>
  [Serializable]
  internal struct MetaHandle : IEquatable<MetaHandle>
  {
    private const int INLINED_STRING_HANDLE = 0;
    private const int INLINED_VALUE_TYPE_HANDLE = 1;
    private const int INLINED_REF_TYPE_HANDLE = 2;
    private const int INLINED_TYPE_VAL_HANDLE = 3;
    private const int HANDLE_OFFSET = 4;


    private int m_HandleValue;
    private VarIntStr? m_Metadata;


    /// <summary>
    /// Returns HandleValue value. This value is invalid if special conditions such as inlining are true
    /// </summary>
    internal int Handle { get { unchecked { return m_HandleValue - HANDLE_OFFSET; } } }

    /// <summary>
    /// Indicates whether a string instance is inlined in Metadata property
    /// </summary>
    internal bool IsInlinedString => m_HandleValue == INLINED_STRING_HANDLE;

    /// <summary>
    /// Indicates whether a struct (value type) instance is inlined right after this HandleValue and Metadata property contains type
    /// </summary>
    internal bool IsInlinedValueType => m_HandleValue == INLINED_VALUE_TYPE_HANDLE;

    /// <summary>
    /// Indicates whether a reference (reference type) instance is inlined right after this HandleValue and Metadata property contains type.
    /// This is used for handling of ref types that are natively supported by streamers
    /// </summary>
    internal bool IsInlinedRefType => m_HandleValue == INLINED_REF_TYPE_HANDLE;

    /// <summary>
    /// Indicates whether a reference to TYPE is inlined - that is a Metadata parameter points to the value of type (reference to Type)
    /// </summary>
    internal bool IsInlinedTypeValue => m_HandleValue == INLINED_TYPE_VAL_HANDLE;


    internal VarIntStr? Metadata => m_Metadata;


    internal MetaHandle(int handle)
    {
      m_HandleValue = handle + HANDLE_OFFSET;
      m_Metadata = null;
    }

    internal MetaHandle(bool serializer, int handle)
    {
      m_HandleValue = handle + (serializer ? 0 : HANDLE_OFFSET);
      m_Metadata = null;
    }

    internal MetaHandle(int handle, VarIntStr? metadata)
    {
      m_HandleValue = handle + HANDLE_OFFSET;
      m_Metadata = metadata;
    }

    internal MetaHandle(bool serializer, int handle, VarIntStr? metadata)
    {
      m_HandleValue = handle + (serializer ? 0 : HANDLE_OFFSET);
      m_Metadata = metadata;
    }

    /// <summary>
    /// Inlines string instance instead of pointer HandleValue
    /// </summary>
    internal static MetaHandle InlineString(string inlinedString)
    {
      var result = new MetaHandle
      {
        m_HandleValue = INLINED_STRING_HANDLE,
        m_Metadata = new VarIntStr(inlinedString)

      };
      return result;
    }


    /// <summary>
    /// Inlines value type instead of pointer HandleValue
    /// </summary>
    internal static MetaHandle InlineValueType(VarIntStr? inlinedValueType)
    {
      var result = new MetaHandle
      {
        m_HandleValue = INLINED_VALUE_TYPE_HANDLE, 
        m_Metadata = inlinedValueType
      };
      return result;
    }

    /// <summary>
    /// Inlines ref type instead of pointer HandleValue
    /// </summary>
    internal static MetaHandle InlineRefType(VarIntStr? inlinedRefType)
    {
      var result = new MetaHandle
      {
        m_HandleValue = INLINED_REF_TYPE_HANDLE, 
        m_Metadata = inlinedRefType
      };
      return result;
    }

    /// <summary>
    /// Inlines type value instead of pointer HandleValue
    /// </summary>
    internal static MetaHandle InlineTypeValue(VarIntStr? inlinedTypeValue)
    {
      var result = new MetaHandle
      {
        m_HandleValue = INLINED_TYPE_VAL_HANDLE, 
        m_Metadata = inlinedTypeValue
      };
      return result;
    }

    public override string ToString()
    {
      // ReSharper disable once UseStringInterpolation
      return string.Format(CultureInfo.InvariantCulture, "[{0}] {1}",
          IsInlinedString ? "string" : 
          IsInlinedValueType ? "struct" : 
          IsInlinedRefType ? @"refobj" : $"{Handle}",
          Metadata);
    }

    public override int GetHashCode()
    {
      return m_HandleValue.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj == null) return false;
      var other = (MetaHandle)obj;
      return Equals(other);
    }

    public bool Equals(MetaHandle other)
    {
      var h1 = m_Metadata.HasValue;
      var h2 = other.m_Metadata.HasValue;

      return m_HandleValue == other.m_HandleValue &&
            ((!h1 && !h2) || (h1 && h2 && m_Metadata.Value.Equals(other.m_Metadata.Value)));
    }

    public static bool operator ==(MetaHandle m1, MetaHandle m2) => m1.Equals((m2));

    public static bool operator !=(MetaHandle m1, MetaHandle m2) => !(m1 == m2);
  }
}
