/*<FILE_LICENSE>
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
  public sealed class SlimException : Exception
  {
    public const string FromFldName = "DAE-F";
    
    public SlimException() { }
    public SlimException(string message) : base(message) { }
    public SlimException(string message, Exception inner) : base(message, inner) { }

    public SlimException(string message, string from = null) : base(message) { m_From = from; }
    private SlimException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      m_From = info.GetString(FromFldName);
    }
    public SlimException(Type type)
      : base(StringConsts.SerProhibitError.Args(type != null ? type.FullName : "<null>",
                                                        nameof(SlimSerializationProhibitedAttribute)))
    { }
    private readonly string m_From;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof(info),  $"{GetType().Name}.{nameof(GetObjectData)}({nameof(info)}=null)");
      info.AddValue(FromFldName, m_From);
      base.GetObjectData(info, context);
    }

  }
}