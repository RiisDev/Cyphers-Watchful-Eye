using System.Windows;
using System.Windows.Media;

namespace CyphersWatchfulEye.InternalLogic;

public static class VisualTreeHelperExtensions
{
    public static IEnumerable<UIElement> FindVisualChildren(DependencyObject depObj)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

            if (child is UIElement element)
            {
                yield return element;
            }

            foreach (UIElement childOfChild in FindVisualChildren(child))
            {
                yield return childOfChild;
            }
        }
    }
}

public static class WpfWindowHelper
{
    public static IEnumerable<UIElement?> GetAllControls(Window window)
    {
        return VisualTreeHelperExtensions.FindVisualChildren(window).ToList();
    }
}