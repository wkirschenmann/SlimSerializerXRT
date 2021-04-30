/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace SlimSerializer.Core
{
  internal static class Arrays
  {
    /// <summary>
    /// Maximum number of supported array dimensions.
    /// Used for possible stream corruption detection
    /// </summary>
    public const int MaxDimCount = 37;

    /// <summary>
    /// Maximum number of elements in any array.
    /// Used for possible stream corruption detection
    /// </summary>
    public const int MaxElmCount = MaxDimCount * 10 * 1024 * 1024;

    public static string ArrayToDescriptor(Array array, Type type, VarIntStr typeHandle)
    {
      if (array.LongLength > MaxElmCount)
        throw new SlimException(StringConsts.ArraysOverMaxElmError.Args(array.LongLength, MaxElmCount));

      if (type == typeof(object[]))//special case for object[], because this type is very often used in Glue and other places
        return "$2|" + array.Length;


      var th = typeHandle.StringValue ??
              (typeHandle.IntValue < TypeRegistry.StrHandlePool.Length ?
                        TypeRegistry.StrHandlePool[typeHandle.IntValue] :
                        $"${typeHandle.IntValue}"
              );

      var ar = array.Rank;
      if (ar > MaxDimCount)
        throw new SlimException(StringConsts.ArraysOverMaxDimsError.Args(ar, MaxDimCount));


      var descr = new StringBuilder();
      descr.Append(th);
      descr.Append('|');//separator char

      for (var i = 0; i < ar; i++)
      {
        descr.Append(array.GetLowerBound(i));
        descr.Append('~');
        descr.Append(array.GetUpperBound(i));
        if (i < ar - 1)
          descr.Append(',');
      }

      return descr.ToString();
    }

    //20140702 DLat+Dkh parsing speed optimization
    public static Array DescriptorToArray(string descr, Type type)
    {
      Contract.Requires(!(descr is null), $"{nameof(descr)} is not null");
      var i = descr.IndexOf('|');

      if (i < 2)
      {
        throw new SlimException(StringConsts.ArraysMissingArrayDimsError + descr);
      }

      if (i == 2 && descr[1] == '2' && descr[0] == '$')//object[] case: $2|len
      {
        i++;//over |
        var total = QuickParseInt(descr, ref i, descr.Length);
        if (total > MaxElmCount)
          throw new SlimException(StringConsts.ArraysOverMaxElmError.Args(total, MaxElmCount));

        return new object[total];
      }

      if (!type.IsArray)
        throw new SlimException(StringConsts.ArraysTypeNotArrayError + type.FullName);

      i++;//over |
      var len = descr.Length;




      try
      {
        var dimCount = 1;
        for (var j = i; j < len - 1; j++) if (descr[j] == ',') dimCount++;

        if (dimCount > MaxDimCount)
          throw new SlimException(StringConsts.ArraysOverMaxDimsError.Args(dimCount, MaxDimCount));

        var lengths = new int[dimCount];
        var lowerBounds = new int[dimCount];

        long total = 0;
        for (var dim = 0; dim < dimCount; dim++)
        {
          var lb = QuickParseInt(descr, ref i, len);
          var ub = QuickParseInt(descr, ref i, len);

          var onelen = (ub - lb) + 1;
          lengths[dim] = onelen;
          lowerBounds[dim] = lb;
          total += onelen;
        }

        if (total > MaxElmCount)
          throw new SlimException(StringConsts.ArraysOverMaxElmError.Args(total, MaxElmCount));

        return Array.CreateInstance(type.GetElementType(), lengths, lowerBounds);
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.ArraysArrayInstanceError + descr + "': " + error.Message, error);
      }
    }

    private static int QuickParseInt(string str, ref int i, int len)
    {
      var result = 0;
      var pos = true;
      for (; i < len; i++)
      {
        var c = str[i];
        if (c == '-')
        {
          pos = false;
          continue;
        }
        if (c == '~' || c == ',')
        {
          i++;
          return pos ? result : -result;
        }
        var d = c - '0';
        if (d < 0 || d > 9) throw new SlimException(StringConsts.ArraysWrongArrayDimsError + str);
        result *= 10;
        result += d;
      }

      return pos ? result : -result;
    }

  }
}
