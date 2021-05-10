/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
namespace Slim
{
  /// <summary>
  /// When set on a parameterless constructor, instructs the Slim serializer not to invoke
  ///  the ctor() on deserialization
  /// </summary>
  [AttributeUsage(AttributeTargets.Constructor)]
  public sealed class SlimDeserializationCtorSkipAttribute : Attribute { }


  /// <summary>
  /// When set fails an attempt to serialize the decorated type
  /// </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
  public sealed class SlimSerializationProhibitedAttribute : Attribute { }

}
