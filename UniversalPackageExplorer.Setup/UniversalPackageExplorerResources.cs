using System;
using System.Windows;
using System.Windows.Media;

namespace Inedo.UniversalPackageExplorer.Setup
{
    internal static class UniversalPackageExplorerResources
    {
        private static readonly Lazy<Brush> upackDarkGrayBrush = new Lazy<Brush>(() => Freeze(new SolidColorBrush(Color.FromRgb(0x86, 0x89, 0xA1)))); //8689A1

        public static Brush UPackDarkGrayBrush => upackDarkGrayBrush.Value;

        private static T Freeze<T>(T instance)
            where T : Freezable
        {
            instance.Freeze();
            return instance;
        }
    }
}
