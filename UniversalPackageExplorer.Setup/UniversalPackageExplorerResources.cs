using System;
using System.Windows;
using System.Windows.Media;

namespace Inedo.UniversalPackageExplorer.Setup
{
    internal static class UniversalPackageExplorerResources
    {
        private static readonly Lazy<Brush> upackDarkGrayBrush = new Lazy<Brush>(() => Freeze(new SolidColorBrush(Color.FromRgb(0x59, 0x5C, 0x73)))); //595C73

        public static Brush UPackDarkGrayBrush => upackDarkGrayBrush.Value;

        private static T Freeze<T>(T instance)
            where T : Freezable
        {
            instance.Freeze();
            return instance;
        }
    }
}
