using System;
using System.Globalization;

namespace Slim.Core
{
  /// <summary>
  /// Holds either an integer or a string value.
  /// This is useful for metadata, i.e. types, if type is known an integer is sent, otherwise a full type name is sent
  /// </summary>
  [Serializable]
  public struct VarIntStr : IEquatable<VarIntStr>
  {
    public VarIntStr(uint value) { IntValue = value; StringValue = null; }
    public VarIntStr(string value) { IntValue = 0; StringValue = value; }

    public string StringValue { get; }

    public uint IntValue { get; }

    public override string ToString()
    {
      return StringValue ?? IntValue.ToString(CultureInfo.InvariantCulture);
    }

    public override bool Equals(object obj)
    {
      return base.Equals((VarIntStr)obj);
    }

    public bool Equals(VarIntStr other)
    {
      return this.IntValue == other.IntValue && this.StringValue == other.StringValue;
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