﻿using System.Windows;
using System.Windows.Controls;

namespace TriEngineEditor.GameProject
{
    /// <summary>
    /// Interaction logic for NewProjectView.xaml
    /// </summary>
    public partial class NewProjectView : UserControl
    {
        public NewProjectView()
        {
            InitializeComponent();
        }

        private void OnCreate_Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as NewProject;
            var projectPath = vm?.CreateProject((ProjectTemplate)templateListBox.SelectedItem);
            bool dialogResult = false;
            var win = Window.GetWindow(this);

            if (projectPath != null)
            {
                dialogResult = true;
                var project = OpenProject.Open(new ProjectData
                {
                    ProjectName = vm.ProjectName,
                    ProjectPath = projectPath
                });

                win.DataContext = project;
            }
            win.DialogResult = dialogResult;
            win.Close();
        }
    }
}
