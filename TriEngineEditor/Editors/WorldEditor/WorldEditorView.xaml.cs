using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using TriEngineEditor.Content;
using TriEngineEditor.GameDev;
using TriEngineEditor.GameProject;

namespace TriEngineEditor.Editors
{
    /// <summary>
    /// Interaction logic for WorldEditorView.xaml
    /// </summary>
    public partial class WorldEditorView : UserControl
    {
        public WorldEditorView()
        {
            InitializeComponent();
            Loaded += OnWorldEditorViewLoader;
        }

        private void OnWorldEditorViewLoader(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWorldEditorViewLoader;
            Focus();
        }

        private void OnNewScript_Button_Click(object sender, RoutedEventArgs e)
        {
            new NewScriptDialog().ShowDialog();
        }

        private void OnCreatePrimitiveMesh_Button_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PrimitiveMeshDialog();
            dlg.ShowDialog();
        }

        private void OnNewProject(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ProjectBrowserDialog.GotoNewProjectTab = true;
            Project.Current?.Unload();
            Application.Current.MainWindow.DataContext = null;
            Application.Current.MainWindow.Close();
        }

        private void OnOpenProject(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Project.Current?.Unload();
            Application.Current.MainWindow.DataContext = null;
            Application.Current.MainWindow.Close();
        }

        private void OnEditorClose(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }
    }
}
