/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Runtime.Serialization;

namespace Azos.Serialization.Slim
{
  /// <summary>
  /// Base exception thrown by the Slim serialization format
  /// </summary>
  [Serializable]
  public class SlimException : AzosSerializationException
  {
    public SlimException() { }
    public SlimException(string message) : base(message) { }
    public SlimException(string message, Exception inner) : base(message, inner) { }
    protected SlimException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Base exception thrown by the Slim when serializing objects
  /// </summary>
  [Serializable]
  public class SlimSerializationException : SlimException
  {
    public SlimSerializationException() { }
    public SlimSerializationException(string message) : base(message) { }
    public SlimSerializationException(string message, Exception inner) : base(message, inner) { }
    protected SlimSerializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Base exception thrown by the Slim when deserializing objects
  /// </summary>
  [Serializable]
  public class SlimDeserializationException : SlimException
  {
    public SlimDeserializationException() { }
    public SlimDeserializationException(string message) : base(message) { }
    public SlimDeserializationException(string message, Exception inner) : base(message, inner) { }
    protected SlimDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Thrown by type registry when supplied type handle is invalid/not found
  /// </summary>
  [Serializable]
  public class SlimInvalidTypeHandleException : SlimException
  {
    public SlimInvalidTypeHandleException() { }
    public SlimInvalidTypeHandleException(string message) : base(message) { }
    public SlimInvalidTypeHandleException(string message, Exception inner) : base(message, inner) { }
    protected SlimInvalidTypeHandleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }

  /// <summary>
  /// Thrown when a type is decoreted with SlimSerializationProhibitedAttribute
  /// </summary>
  [Serializable]
  public class SlimSerializationProhibitedException : SlimException
  {
    protected SlimSerializationProhibitedException() { }
    public SlimSerializationProhibitedException(Type type)
      : base(StringConsts.SLIM_SER_PROHIBIT_ERROR.Args(type != null ? type.FullName : CoreConsts.NULL_STRING,
                                                        typeof(SlimSerializationProhibitedAttribute).Name)) { }

    protected SlimSerializationProhibitedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}