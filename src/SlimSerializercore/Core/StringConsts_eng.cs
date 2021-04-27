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
    internal const string ARGUMENT_ERROR = "Argument error: ";
    internal const string BUILD_INFO_READ_ERROR =
            "Error reading BUILD_INFO resource: ";
    internal const string SLIM_STREAM_CORRUPTED_ERROR = "Slim data stream is corrupted: ";
    internal const string STREAM_READ_EOF_ERROR =
        "Stream EOF before operation could complete: ";


    internal const string SLIM_READ_X_ARRAY_MAX_SIZE_ERROR =
        "Slim reader could not read requested array of {0} {1} as it exceeds the maximum limit of {2} bytes'";

    internal const string SLIM_WRITE_X_ARRAY_MAX_SIZE_ERROR =
        "Slim writer could not write requested array of {0} {1} as it exceeds the maximum limit of {2} bytes'";



    internal const string SLIM_SERIALIZATION_EXCEPTION_ERROR =
        "Exception in SlimSerializer.Serialize():  ";

    internal const string SLIM_DESERIALIZATION_EXCEPTION_ERROR =
        "Exception in SlimSerializer.Deserialize():  ";


    internal const string SLIM_DESERIALIZE_CALLBACK_ERROR =
        "Exception leaked from OnDeserializationCallback() invoked by SlimSerializer. Error:  ";

    internal const string SLIM_ISERIALIZABLE_MISSING_CTOR_ERROR =
        "ISerializable object does not implement .ctor(SerializationInfo, StreamingContext): ";

    internal const string SLIM_BAD_HEADER_ERROR =
        "Bad SLIM format header";

    internal const string SLIM_TREG_COUNT_ERROR =
        "Slim type registry count mismatch";

    internal const string SLIM_TREG_CSUM_ERROR =
        "Slim type registry CSUM mismatch";

    public const string SLIM_HNDLTOREF_MISSING_TYPE_NAME_ERROR =
        "HandleToReference(). Missing type name: ";

    internal const string SLIM_ARRAYS_TYPE_NOT_ARRAY_ERROR =
        "DescriptorToArray(). Type is not array : ";

    internal const string SLIM_ARRAYS_MISSING_ARRAY_DIMS_ERROR =
        "DescriptorToArray(). Missing array dimensions: ";

    internal const string SLIM_ARRAYS_OVER_MAX_DIMS_ERROR =
        "Slim does not support an array with {0} dimensions. Only up to {1} maximum array dimensions supported";

    internal const string SLIM_ARRAYS_OVER_MAX_ELM_ERROR =
        "Slim does not support an array with {0} elements. Only up to {1} maximum array elements supported";

    internal const string SLIM_ARRAYS_WRONG_ARRAY_DIMS_ERROR =
        "DescriptorToArray(). Wrong array dimensions: ";

    internal const string SLIM_ARRAYS_ARRAY_INSTANCE_ERROR =
        "DescriptorToArray(). Error instantiating array '";

    internal const string SLIM_SER_PROHIBIT_ERROR =
        "Slim can not process type '{0}' as it is marked with [{1}] attribute";
  }
}
