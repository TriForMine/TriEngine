using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TriEngineEditor.GameProject
{
    /// <summary>
    /// Interaction logic for OpenProjectView.xaml
    /// </summary>
    public partial class OpenProjectView : UserControl
    {
        public OpenProjectView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                var item = projectsListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                item?.Focus();
            };
        }

        private void OnOpen_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedProject();
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedProject();
        }

        private void OpenSelectedProject()
        {
            var project = OpenProject.Open((ProjectData)projectsListBox.SelectedItem);
            bool dialogResult = false;
            var win = Window.GetWindow(this);

            if (project != null)
            {
                dialogResult = true;
                win.DataContext = project;
            }

            win.DialogResult = dialogResult;
            win.Close();
        }
    }
}
