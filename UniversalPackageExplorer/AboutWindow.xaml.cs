using System.Windows;

namespace UniversalPackageExplorer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        public string VersionText
        {
            get
            {
                var version = typeof(AboutWindow).Assembly.GetName().Version;
                return $"{version.ToString(3)} (build {version.Build})";
            }
        }
    }
}
