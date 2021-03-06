/*<FILE_LICENSE>
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


      var typeHandleStringValue = typeHandle.StringValue ??
              (typeHandle.IntValue < TypeRegistry.StrHandlePool.Length ?
                        TypeRegistry.StrHandlePool[typeHandle.IntValue] :
                        $"${typeHandle.IntValue}"
              );

      var ar = array.Rank;
      if (ar > MaxDimCount)
        throw new SlimException(StringConsts.ArraysOverMaxDimsError.Args(ar, MaxDimCount));


      var descriptorBuilder = new StringBuilder();
      descriptorBuilder.Append(typeHandleStringValue);
      descriptorBuilder.Append('|');//separator char

      for (var i = 0; i < ar; i++)
      {
        descriptorBuilder.Append(array.GetLowerBound(i));
        descriptorBuilder.Append('~');
        descriptorBuilder.Append(array.GetUpperBound(i));
        if (i < ar - 1)
          descriptorBuilder.Append(',');
      }

      return descriptorBuilder.ToString();
    }

    //20140702 DLat+Dkh parsing speed optimization
    public static Array DescriptorToArray(string descr, Type type)
    {
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



      Array instance;
      //descr = $0|0~12,1~100
      //           ^

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

        instance = Array.CreateInstance(type.GetElementType() ?? throw new InvalidOperationException(), lengths, lowerBounds);
      }
      catch (Exception error)
      {
        throw new SlimException(StringConsts.ArraysArrayInstanceError + descr + "': " + error.Message, error);
      }

      return instance;
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
