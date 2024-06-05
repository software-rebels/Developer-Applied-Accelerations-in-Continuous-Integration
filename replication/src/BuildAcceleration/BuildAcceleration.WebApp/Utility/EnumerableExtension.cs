using System;
using System.Collections.Generic;

namespace ForecastBuildTime.WebApp.Utility
{
    public static class EnumerableExtension
    {
        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
        {
            var array = new T[count];
            int i = 0;
            var random = new Random();
            foreach (var item in source)
            {
                if (i++ < count)
                {
                    array[i - 1] = item;
                }
                else
                {
                    var replaceIndex = random.Next(i);
                    if (replaceIndex < count)
                    {
                        array[replaceIndex] = item;
                    }
                }
            }

            return i >= count ? array : array[0..i];
        }
    }
}