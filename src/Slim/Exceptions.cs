/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

using Slim.Core;

namespace Slim
{
  /// <summary>
  /// Base exception thrown by the Slim serialization format
  /// </summary>
  [Serializable]
  public class SlimException : Exception
  {
    public const string CodeFldName = "AE-C";
    public const string FromFldName = "DAE-F";
    public SlimException() { }
    public SlimException(string message) : base(message) { }
    public SlimException(string message, Exception inner) : base(message, inner) { }

    public SlimException(string message, string from = null) : base(message) { m_From = from; }
    protected SlimException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FromFldName);
    }
    public SlimException(Type type)
      : base(StringConsts.SerProhibitError.Args(type != null ? type.FullName : CoreConsts.NullString,
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
      info.AddValue(CodeFldName, Code);
      info.AddValue(FromFldName, m_From);
      base.GetObjectData(info, context);
    }
  }
}