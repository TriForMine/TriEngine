using System.ComponentModel;
using System.IO;
using System.Windows;
using TriEngineEditor.GameProject;

namespace TriEngineEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string TriEnginePath { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnMainWindowLoaded;
            Closing += OnMainWindowClosing;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnMainWindowLoaded;
            GetEnginePath();
            OpenProjectBrowserDialog();
        }

        private void GetEnginePath()
        {
            var enginePath = Environment.GetEnvironmentVariable("TRIENGINE_ENGINE", EnvironmentVariableTarget.User);

            if (enginePath == null || !Directory.Exists(Path.Combine(enginePath, @"Engine\EngineAPI")))
            {
                var dlg = new EnginePathDialog();
                if (dlg.ShowDialog() == true)
                {
                    TriEnginePath = dlg.TriEnginePath;
                    Environment.SetEnvironmentVariable("TRIENGINE_ENGINE", TriEnginePath.ToUpper(), EnvironmentVariableTarget.User);
                } else
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                TriEnginePath = enginePath;
            }
        }

        private void OnMainWindowClosing(object? sender, CancelEventArgs e)
        {
            Closing -= OnMainWindowClosing;
            Project.Current?.Unload();
        }

        private void OpenProjectBrowserDialog()
        {
            var projectBrowserDialog = new GameProject.ProjectBrowserDialog();
            projectBrowserDialog.Owner = this;
            if (projectBrowserDialog.ShowDialog() == false || projectBrowserDialog.DataContext == null)
            {
                Application.Current.Shutdown();
            }
            else
            {
                Project.Current?.Unload();
                DataContext = projectBrowserDialog.DataContext;
            }
        }
    }
}