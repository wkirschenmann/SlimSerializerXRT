/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Text;

namespace Slim.Core
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
              (typeHandle.IntValue < TypeRegistry.StrHndlPool.Length ?
                        TypeRegistry.StrHndlPool[typeHandle.IntValue] :
                        $"${typeHandle.IntValue}"
              );

      var ar = array.Rank;
      if (ar > MaxDimCount)
        throw new SlimException(StringConsts.ArraysOverMaxDimsError.Args(ar, MaxDimCount));


      var descr = new StringBuilder();
      descr.Append(th);
      descr.Append('|');//separator char

      for (int i = 0; i < ar; i++)
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
      var i = descr.IndexOf('|');

      if (i < 2)
      {
        throw new SlimException(StringConsts.ArraysMissingArrayDimsError + descr.ToString());
      }

      if (i == 2 && descr[1] == '2' && descr[0] == '$')//object[] case: $2|len
      {
        i++;//over |
        var total = QuickParseInt(descr, ref i, descr.Length);
        if (total > MaxElmCount)
          throw new SlimException(StringConsts.ArraysOverMaxElmError.Args(total, MaxElmCount));

        return new object[total];
      }



      Array instance = null;

      if (!type.IsArray)
        throw new SlimException(StringConsts.ArraysTypeNotArrayError + type.FullName);

      i++;//over |
      var len = descr.Length;
      //descr = $0|0~12,1~100
      //           ^

      try
      {
        var dimCount = 1;
        for (var j = i; j < len - 1; j++) if (descr[j] == ',') dimCount++;

        if (dimCount > MaxDimCount)
          throw new SlimException(StringConsts.ArraysOverMaxDimsError.Args(dimCount, MaxDimCount));

        int[] lengths = new int[dimCount];
        int[] lowerBounds = new int[dimCount];

        long total = 0;
        for (int dim = 0; dim < dimCount; dim++)
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

        instance = Array.CreateInstance(type.GetElementType(), lengths, lowerBounds);
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.ArraysArrayInstanceError + descr.ToString() + "': " + error.Message, error);
      }

      return instance;
    }

    private static int QuickParseInt(string str, ref int i, int len)
    {
      int result = 0;
      bool pos = true;
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
