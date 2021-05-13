using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RTSC.Devices.Helpers
{
    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
        public static IList<T> MoveFirstItemToEnd<T>(this IList<T> listToResort)
        {
            if (listToResort.Count <= 1)
            {
                return listToResort;
            }
            else
            {
                T temp = listToResort[0];
                listToResort.Remove(listToResort[0]);
                listToResort.Insert(listToResort.Count, temp);
                return listToResort;
            }

        }
        public static void MutateVerbose<TField>(this INotifyPropertyChanged instance, ref TField field,
            TField newValue, Action<PropertyChangedEventArgs> raise, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TField>.Default.Equals(field, newValue)) return;
            field = newValue;
            raise?.Invoke(new PropertyChangedEventArgs(propertyName));
        }
    }
}
