using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace Utility
{
    public static class Extenstions
    {

        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new InvalidOperationException("The list is empty or null.");
            }

            int index = Random.Range(0, list.Count);
            return list[index];
        }
    }
}
