using System;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace SlimSerializer.Core
{
  /// <summary>
  /// Holds either an integer or a string value.
  /// This is useful for metadata, i.e. types, if type is known an integer is sent, otherwise a full type name is sent
  /// </summary>
  [Serializable]
  public struct VarIntStr : IEquatable<VarIntStr>
  {
    public VarIntStr(int value) { IntValue = value; StringValue = null; }
    public VarIntStr(string value) { IntValue = 0; StringValue = value; }

    public int IntValue { get; }
    public string StringValue { get; }

    public override string ToString() => StringValue ?? IntValue.ToString(CultureInfo.InvariantCulture);

    public override bool Equals(object obj)
    {
      Contract.Requires(!(obj is null), $"{nameof(obj)} is not null");
      return base.Equals((VarIntStr)obj);
    }

    public bool Equals(VarIntStr other)
    {
      return IntValue == other.IntValue && StringValue == other.StringValue;
    }

    public override int GetHashCode()
    {
      return IntValue.GetHashCode() + (StringValue != null ? StringValue.GetHashCode() : 0);
    }

    public static bool operator ==(VarIntStr left, VarIntStr right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(VarIntStr left, VarIntStr right)
    {
      return !(left == right);
    }
  }
}