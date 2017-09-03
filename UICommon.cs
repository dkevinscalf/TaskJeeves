using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TaskJeeves
{
    public static class UICommon
    {
        public static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        public static void AddToProperties(string key, object value)
        {
            if (Application.Current.Properties.Contains(key))
            {
                Application.Current.Properties[key] = value;
            }
            else
            {
                Application.Current.Properties.Add(key, value);
            }
        }

        public static object GetProperty(string fieldName)
        {
            return Application.Current.Properties[fieldName];
        }

        public static void AddOnUI<T>(this ICollection<T> collection, T item)
        {
            Action<T> addMethod = collection.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }

        public static void RemoveAtOnUI<T>(this ObservableCollection<T> collection, int index)
        {
            Action<int> removeMethod = collection.RemoveAt;
            Application.Current.Dispatcher.BeginInvoke(removeMethod, index);
        }
    }
}
