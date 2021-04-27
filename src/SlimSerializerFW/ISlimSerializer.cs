/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.IO;

namespace SlimSerializer
{
  /// <summary>
  /// Denotes ser/deser operations
  /// </summary>
  public enum SerializationOperation
  {
    /// <summary>
    /// Serializing object to stream
    /// </summary>
    Serializing,

    /// <summary>
    /// Deserializing object from stream
    /// </summary>
    Deserializing
  };

  /// <summary>
  /// Denotes modes of handling type registry by Slim serializer
  /// </summary>
  public enum TypeRegistryMode
  {
    /// <summary>
    /// Type registry object is created for every Serialize/Deserialize call only
    /// cloning global types. This is the default mode which is thread-safe(many threads can call Serialize/Deserialize at the same time)
    /// </summary>
    PerCall = 0,

    /// <summary>
    /// Type registry object is cloned from global types only once and it is retained after making calls.
    /// This is not a thread-safe mode, so only one thread may call Serialize/Deserialize at a time.
    /// This mode is beneficial for cases when many object instances of various types need to be transmitted
    /// so repeating their type names in every Ser/Deser is not efficient. In batch mode the type name is
    ///  written/read to/from stream only once, then type handles are transmitted instead thus saving space and
    ///  extra allocations
    /// </summary>
    Batch
  }

  /// <summary>
  /// Marker interface for formats based on Slim algorithm family
  /// </summary>
  public interface ISlimSerializer
  {
    void Serialize(Stream stream, object root);
    object Deserialize(Stream stream);

    /// <summary>
    /// Indicates whether Serialize/Deserialize may be called by multiple threads at the same time
    /// </summary>
    bool IsThreadSafe { get; }

    /// <summary>
    /// Gets/sets how serializer handles type information between calls to Serialize/Deserialize.
    /// This property itself is not thread-safe, that is - it should be only set once by control/initiating thread
    /// </summary>
    TypeRegistryMode TypeMode { get; set; }

    /// <summary>
    /// Resets type registry state to initial state (which is based on global types) for TypeMode = "Batch",
    /// otherwise does nothing
    /// </summary>
    void ResetCallBatch();

    /// <summary>
    /// Returns true if last call to Serialize or Deserialize in batch mode added more types to type registry.
    /// This call is only valid in TypeMode = "Batch" and is inherently not thread-safe
    /// </summary>
    bool BatchTypesAdded { get; }
  }
}
