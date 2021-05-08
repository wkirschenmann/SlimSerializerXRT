/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
[assembly:CLSCompliant(true)]
namespace Slim
{
  /// <summary>
  /// When set on a parameterless constructor, instructs the Slim serializer not to invoke
  ///  the ctor() on deserialization
  /// </summary>
  [AttributeUsage(AttributeTargets.Constructor)]
  public sealed class SlimDeserializationCtorSkipAttribute : Attribute {}


  /// <summary>
  /// When set fails an attempt to serialize the decorated type
  /// </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
  public sealed class SlimSerializationProhibitedAttribute : Attribute { }

}
