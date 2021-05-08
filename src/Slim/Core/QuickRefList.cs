using System;

namespace Slim.Core
{
  internal struct QuickRefList
  {
    public QuickRefList(int capacity)
    {
      m_InitialCapacity = capacity;
      m_Data = new object[ capacity ];
      Count = 1;//the "zeros" element is always NULL
    }

    private readonly int m_InitialCapacity;
    private object[] m_Data;


    public int Count { get; private set; }

    public object this[int i] => m_Data[i];

    public void Clear()
    {
      var trimAt = RefPool.LargeTrimThreshold;

      if (Count>trimAt) //We want to get rid of excess data when too much
      {                           //otherwise the array will get stuck in pool cache for a long time
        m_Data = new object[ m_InitialCapacity ];
      } 

      Count = 1;//[0]==null, don't clear //notice: no Array.Clear... for normal memory modes
    }

    public void Add(object reference)
    {
      var len = m_Data.Length;
      if (Count==len)
      {
        var newData = new object[2 * len];
        Array.Copy(m_Data, 0, newData, 0, len);
        m_Data = newData;
      }

      m_Data[Count] = reference;
      Count++;
    }

  }
}