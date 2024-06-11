using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace TriEngineEditor
{
    /// <summary>
    /// Interaction logic for EnginePathDialog.xaml
    /// </summary>
    public partial class EnginePathDialog : Window
    {
        public string TriEnginePath { get; private set; }

        public EnginePathDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void OnOk_Buttn_Click(object sender, RoutedEventArgs e)
        {
            var path = pathTextBox.Text;
            messageTextBlock.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                messageTextBlock.Text = "Please enter a valid path.";
                return;
            }
            else if (path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) != -1)
            {
                messageTextBlock.Text = "Invalid character(s) used in path.";
                return;
            }
            else if (!System.IO.Directory.Exists(System.IO.Path.Combine(path, @"Engine\EngineAPI")))
            {
                messageTextBlock.Text = "Unable to find the engine at the specified path.";
                return;
            }

            if (string.IsNullOrEmpty(messageTextBlock.Text))
            {
                if (!Path.EndsInDirectorySeparator(path)) path += @"\";
                TriEnginePath = path;
                DialogResult = true;
                Close();
            }
        }
    }
}
