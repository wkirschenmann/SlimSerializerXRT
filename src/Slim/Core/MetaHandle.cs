/*<FILE_LICENSE>
* See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Globalization;


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
    internal struct MetaHandle : IEquatable<MetaHandle>
    {
        internal const int InlinedStringHandle = 0;
        internal const int InlinedValueTypeHandle = 1;
        internal const int InlinedRefTypeHandle = 2;
        internal const int InlinedTypeValHandle = 3;
        internal const int HandleOffset = 4;


        internal int HandleRawValue { get; }
        private VarIntStr? m_Metadata;


        /// <summary>
        /// Returns handle value. This value is invalid if special conditions such as inlining are true
        /// </summary>
        public int Handle => HandleRawValue - HandleOffset;

        /// <summary>
        /// Indicates whether a string instance is inlined in Metadata property
        /// </summary>
        public bool IsInlinedString => HandleRawValue == InlinedStringHandle;

        /// <summary>
        /// Indicates whether a struct (value type) instance is inlined right after this handle and Metadata property contains type
        /// </summary>
        public bool IsInlinedValueType => HandleRawValue == InlinedValueTypeHandle;

        /// <summary>
        /// Indicates whether a reference (reference type) instance is inlined right after this handle and Metadata property contains type.
        /// This is used for handling of ref types that are natively supported by streamers
        /// </summary>
        public bool IsInlinedRefType => HandleRawValue == InlinedRefTypeHandle;

        /// <summary>
        /// Indicates whether a reference to TYPE is inlined - that is a Metadata parameter points to the value of type (reference to Type)
        /// </summary>
        public bool IsInlinedTypeValue => HandleRawValue == InlinedTypeValHandle;


        public VarIntStr? Metadata => m_Metadata;


        public MetaHandle(int handle, bool raw = false) : this(handle, null, raw)
        { }

        public MetaHandle(bool serializer, int handle)
        {
            HandleRawValue = handle + (serializer ? 0 : HandleOffset);
            m_Metadata = null;
        }

        public MetaHandle(int handle, VarIntStr? metadata, bool raw = false)
        {
            HandleRawValue = handle + (raw ? 0 : HandleOffset);
            m_Metadata = metadata;
        }

        internal MetaHandle(bool serializer, int handle, VarIntStr? metadata)
        {
            HandleRawValue = handle + (serializer ? 0 : HandleOffset);
            m_Metadata = metadata;
        }

        /// <summary>
        /// Inlines string instance instead of pointer handle
        /// </summary>
        public static MetaHandle InlineString(string inlinedString)
        {
            var result = new MetaHandle(InlinedStringHandle, true)
            {
                m_Metadata = new VarIntStr(inlinedString)
            };
            return result;
        }


        /// <summary>
        /// Inlines value type instead of pointer handle
        /// </summary>
        public static MetaHandle InlineValueType(VarIntStr? inlinedValueType)
        {
            var result = new MetaHandle(InlinedValueTypeHandle, true)
            {
                m_Metadata = inlinedValueType
            };
            return result;
        }

        /// <summary>
        /// Inlines ref type instead of pointer handle
        /// </summary>
        public static MetaHandle InlineRefType(VarIntStr? inlinedRefType)
        {
            var result = new MetaHandle(InlinedRefTypeHandle, true)
            {
                m_Metadata = inlinedRefType
            };
            return result;
        }

        /// <summary>
        /// Inlines type value instead of pointer handle
        /// </summary>
        public static MetaHandle InlineTypeValue(VarIntStr? inlinedTypeValue)
        {
            var result = new MetaHandle(InlinedTypeValHandle, true)
            {
                m_Metadata = inlinedTypeValue
            };
            return result;
        }

        public override string ToString()
        {
            return
              $"[{(IsInlinedString ? "string" : IsInlinedValueType ? "struct" : IsInlinedRefType ? "refobj" : Handle.ToString(CultureInfo.InvariantCulture))}] {Metadata}";
        }

        public override int GetHashCode()
        {
            return HandleRawValue.GetHashCode();
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

            return HandleRawValue == other.HandleRawValue &&
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
