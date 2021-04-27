/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using SlimSerializer.Core;

using System;
using System.Runtime.Serialization;

namespace SlimSerializer
{
  /// <summary>
  /// Base exception thrown by the Slim serialization format
  /// </summary>
  [Serializable]
  public class SlimException : Exception
  {
    public const string CODE_FLD_NAME = "AE-C";
    public const string FROM_FLD_NAME = "DAE-F";
    public SlimException() { }
    public SlimException(string message) : base(message) { }
    public SlimException(string message, Exception inner) : base(message, inner) { }

    public SlimException(string message, string from = null) : base(message) { m_From = from; }
    protected SlimException(SerializationInfo info, StreamingContext context) : base(info, context) {
      m_From = info.GetString(FROM_FLD_NAME);
    }
    public SlimException(Type type)
      : base(StringConsts.SLIM_SER_PROHIBIT_ERROR.Args(type != null ? type.FullName : CoreConsts.NULL_STRING,
                                                        typeof(SlimSerializationProhibitedAttribute).Name))
    { }

    /// <summary>
    /// Provides general-purpose error code
    /// </summary>
    public int Code { get; set; }
    private readonly string m_From;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException("info", GetType().Name + ".GetObjectData(info=null)");
      info.AddValue(CODE_FLD_NAME, Code);
      info.AddValue(FROM_FLD_NAME, m_From);
      base.GetObjectData(info, context);
    }
  }
}