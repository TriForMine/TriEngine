using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace TriEngineEditor
{
    static class VisualExtensions
    {
        public static T? FindVisualParent<T>(this DependencyObject depObject) where T : DependencyObject
        {
            if (!(depObject is Visual)) return null;

            var parent = VisualTreeHelper.GetParent(depObject);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }
    }

    public static class ContentHelper
    {
        public static string GetRandomString(int length = 8)
        {
            if (length <= 0) length = 8;
            var n = length / 11;
            var sb = new StringBuilder();
            for (var i = 0; i < n; i++)
            {
                sb.Append(Path.GetRandomFileName().Replace(".", ""));
            }

            return sb.ToString(0, length);
        }
    }
}
