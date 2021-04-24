/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System.Runtime.Serialization.Formatters.Binary;

namespace Azos.Serialization
{
  /// <summary>
  /// Implements ISerializer with Ms BinaryFormatter
  /// </summary>
  public class MSBinaryFormatter : ISerializer
    {
      private BinaryFormatter m_Formatter = new BinaryFormatter();


      public void Serialize(System.IO.Stream stream, object root)
      {
          m_Formatter.Serialize(stream, root);
      }

      public object Deserialize(System.IO.Stream stream)
      {
          return m_Formatter.Deserialize(stream);
      }


      public bool IsThreadSafe
      {
        get { return false; }
      }
    }

}
