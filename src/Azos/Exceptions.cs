/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

namespace Azos
{
  /// <summary>
  /// Base exception thrown by the framework
  /// </summary>
  [Serializable]
  public class AzosException : Exception
  {
    public const string CODE_FLD_NAME = "AE-C";

    public AzosException() {}
    public AzosException(string message) : base(message) {}
    public AzosException(string message, Exception inner) : base(message, inner) {}
    protected AzosException(SerializationInfo info, StreamingContext context) : base(info, context) { Code = info.GetInt32(CODE_FLD_NAME); }

    /// <summary>
    /// Provides general-purpose error code
    /// </summary>
    public int Code { get; set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info", GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(CODE_FLD_NAME, Code);
      base.GetObjectData(info, context);
    }
  }


  /// <summary>
  /// Thrown by Debug class to indicate assertion failures
  /// </summary>
  [Serializable]
  public sealed class DebugAssertionException : AzosException
  {
    public const string FROM_FLD_NAME = "DAE-F";

    public DebugAssertionException(string from = null) { m_From = from; }
    public DebugAssertionException(string message, string from = null) : base(message) { m_From = from; }
    private DebugAssertionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FROM_FLD_NAME);
    }

    private string m_From;

    public string From { get { return m_From ?? string.Empty; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(FROM_FLD_NAME, m_From);
      base.GetObjectData(info, context);
    }
  }


  /// <summary>
  /// Thrown by Aver class to indicate averment failures
  /// </summary>
  [Serializable]
  public sealed class AvermentException : AzosException
  {
    public const string FROM_FLD_NAME = "AE-F";

    public AvermentException(string message = null) : this(message, null, null) {}

    public AvermentException(string message, string from = null) : this(message, from, null) {}

    public AvermentException(string message, string from, Exception inner) : base((from.IsNullOrWhiteSpace() ? "" : "from '{0}' ".Args(from)) + message, inner)
    {
      m_From = from;
    }

    private AvermentException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FROM_FLD_NAME);
    }

    private string m_From;

    public string From { get { return m_From ?? string.Empty; } }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new AzosException(StringConsts.ARGUMENT_ERROR + GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(FROM_FLD_NAME, m_From);
      base.GetObjectData(info, context);
    }
  }

}