using System.Windows.Input;

namespace UniversalPackageExplorer
{
    public static class Commands
    {
        #region File Menu
        public static RoutedUICommand New { get; } = new RoutedUICommand("_New", nameof(New), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control) });
        public static RoutedUICommand Open { get; } = new RoutedUICommand("_Open…", nameof(Open), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control) });
        public static RoutedUICommand OpenFromFeed { get; } = new RoutedUICommand("Open from F_eed…", nameof(OpenFromFeed), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });

        public static RoutedUICommand Close { get; } = new RoutedUICommand("_Close", nameof(Close), typeof(Commands));

        public static RoutedUICommand Save { get; } = new RoutedUICommand("_Save", nameof(Save), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });
        public static RoutedUICommand SaveAs { get; } = new RoutedUICommand("Save _As…", nameof(SaveAs), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) });

        public static RoutedUICommand Publish { get; } = new RoutedUICommand("_Publish…", nameof(Publish), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control) });
        public static RoutedCommand OpenRecentFile { get; } = new RoutedCommand(nameof(OpenRecentFile), typeof(Commands));
        public static RoutedUICommand Exit { get; } = new RoutedUICommand("E_xit", nameof(Exit), typeof(Commands));
        #endregion

        #region Content Menu
        public static RoutedUICommand ViewFileContent { get; } = new RoutedUICommand("View File _Content", nameof(ViewFileContent), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.Enter) });
        public static RoutedUICommand OpenFileInWindowsShell { get; } = new RoutedUICommand("Open file in Window_s shell…", nameof(OpenFileInWindowsShell), typeof(Commands));
        public static RoutedUICommand SaveFileAs { get; } = new RoutedUICommand("Save _As…", nameof(SaveFileAs), typeof(Commands));

        #region Add Submenu
        public static RoutedUICommand NewFile { get; } = new RoutedUICommand("_New File…", nameof(NewFile), typeof(Commands));
        public static RoutedUICommand ExistingFile { get; } = new RoutedUICommand("_Existing File…", nameof(ExistingFile), typeof(Commands));
        public static RoutedUICommand NewFolder { get; } = new RoutedUICommand("New _Folder…", nameof(NewFolder), typeof(Commands));
        public static RoutedUICommand ExistingFolder { get; } = new RoutedUICommand("E_xisting Folder…", nameof(ExistingFolder), typeof(Commands));
        #endregion

        public static RoutedUICommand Rename { get; } = new RoutedUICommand("_Rename…", nameof(Rename), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.F2) });
        public static RoutedUICommand Delete { get; } = new RoutedUICommand("_Delete", nameof(Delete), typeof(Commands), new InputGestureCollection { new KeyGesture(Key.Delete) });
        #endregion

        #region Help Menu
        public static RoutedUICommand ProjectHome { get; } = new RoutedUICommand("_UPE Project Home", nameof(ProjectHome), typeof(Commands));
        public static RoutedUICommand FileReference { get; } = new RoutedUICommand("UPack File _Reference", nameof(FileReference), typeof(Commands));
        public static RoutedUICommand About { get; } = new RoutedUICommand("_About…", nameof(About), typeof(Commands));
        #endregion

        public static RoutedUICommand AssociateWithUPack { get; } = new RoutedUICommand("Associate with .upack files", nameof(AssociateWithUPack), typeof(Commands));
    }
}
