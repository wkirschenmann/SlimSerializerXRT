/*<FILE_LICENSE>
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Slim.Core
{
  /// <summary>
  /// Checks for reference equality
  /// </summary>
  public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
  {
    public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

    private ReferenceEqualityComparer() { }

    public bool Equals(T x, T y) => ReferenceEquals(x, y);
    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);

    bool IEqualityComparer.Equals(object x, object y) => ReferenceEquals(x, y);
    int IEqualityComparer.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
  }
}

