/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

namespace SlimSerializer.Core
{
  /// <summary>
  /// A dictionary of framework text messages.
  /// Localization may be done in this class in future
  /// </summary>
  internal static class StringConsts
  {
    internal const string ArgumentError = "Argument error: ";
    internal const string BuildInfoReadError =
            "Error reading BUILD_INFO resource: ";
    internal const string StreamCorruptedError = "Slim data stream is corrupted: ";
    internal const string StreamReadEofError =
        "Stream EOF before operation could complete: ";


    internal const string ReadXArrayMaxSizeError =
        "Slim reader could not read requested array of {0} {1} as it exceeds the maximum limit of {2} bytes'";

    internal const string WriteXArrayMaxSizeError =
        "Slim writer could not write requested array of {0} {1} as it exceeds the maximum limit of {2} bytes'";



    internal const string SerializationExceptionError =
        "Exception in SlimSerializer.Serialize():  ";

    internal const string DeserializationExceptionError =
        "Exception in SlimSerializer.Deserialize():  ";


    internal const string DeserializeCallbackError =
        "Exception leaked from OnDeserializationCallback() invoked by SlimSerializer. Error:  ";

    internal const string IserializableMissingCtorError =
        "ISerializable object does not implement .ctor(SerializationInfo, StreamingContext): ";

    internal const string BadHeaderError =
        "Bad SLIM format header";

    internal const string TregCountError =
        "Slim type registry count mismatch";

    internal const string TregCsumError =
        "Slim type registry CSUM mismatch";

    public const string HndltorefMissingTypeNameError =
        "HandleToReference(). Missing type name: ";

    internal const string ArraysTypeNotArrayError =
        "DescriptorToArray(). Type is not array : ";

    internal const string ArraysMissingArrayDimsError =
        "DescriptorToArray(). Missing array dimensions: ";

    internal const string ArraysOverMaxDimsError =
        "Slim does not support an array with {0} dimensions. Only up to {1} maximum array dimensions supported";

    internal const string ArraysOverMaxElmError =
        "Slim does not support an array with {0} elements. Only up to {1} maximum array elements supported";

    internal const string ArraysWrongArrayDimsError =
        "DescriptorToArray(). Wrong array dimensions: ";

    internal const string ArraysArrayInstanceError =
        "DescriptorToArray(). Error instantiating array '";

    internal const string SerProhibitError =
        "Slim can not process type '{0}' as it is marked with [{1}] attribute";
  }
}
