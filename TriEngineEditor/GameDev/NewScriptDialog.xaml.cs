using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using TriEngineEditor.GameProject;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.GameDev
{
    /// <summary>
    /// Interaction logic for NewScriptDialog.xaml
    /// </summary>
    public partial class NewScriptDialog : Window
    {
        private static readonly string _cppCode = @"#include ""{0}.h""

namespace {1} {{
	REGISTER_SCRIPT({0});

	void {0}::begin_play()
	{{
		
	}}

	void {0}::update(float dt)
	{{
		
	}}
}} // namespace {1}";

        private static readonly string _hCode = @"#pragma once
#include <string>

namespace {1} {{
	class {0} : public triengine::script::entity_script
	{{
	public:
		constexpr explicit {0}(triengine::game_entity::entity entity) : triengine::script::entity_script{{entity}} {{}}

        void begin_play() override;
		void update(float dt) override;
    private:
	}};
}} // namespace {1}";

        private static readonly string _namespace = GetNamespaceFromProjectName();

        private static string GetNamespaceFromProjectName()
        {
            var projectName = Project.Current?.Name.Trim();
            if (string.IsNullOrEmpty(projectName)) return string.Empty;

            return projectName;
        }

        private bool Validate()
        {
            bool isValid = false;

            var name = scriptName.Text.Trim();
            var path = scriptPath.Text.Trim();
            string errorMsg = string.Empty;
            var nameRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");
            if (string.IsNullOrEmpty(name)) {
                errorMsg = "Script name cannot be empty.";
            } else if (!nameRegex.IsMatch(name)) {
                errorMsg = "Script name contains invalid characters.";
            } else if (string.IsNullOrEmpty(path)) {
                errorMsg = "Script path cannot be empty.";
            } else if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                errorMsg = "Script path contains invalid characters.";
            } else if (!Path.GetFullPath(Path.Combine(Project.Current.Path, path)).Contains(Path.Combine(Project.Current.Path, @"GameCode\")))
            {
                errorMsg = "Script path must be within the GameCode folder.";
            } else if (File.Exists(Path.GetFullPath(Path.Combine(Path.Combine(Project.Current.Path, path), $"{name}.cpp"))) ||
                File.Exists(Path.GetFullPath(Path.Combine(Path.Combine(Project.Current.Path, path), $"{name}.h")))) {
                errorMsg = $"Script with name {name} already exists in the specified path.";
            } else {
                isValid = true;
            }

            if (!isValid)
            {
                messageTextBlock.Foreground = FindResource("Editor.RedBrush") as Brush;
            } else
            {
                messageTextBlock.Foreground = FindResource("Editor.FontBrush") as Brush;
            }

            messageTextBlock.Text = errorMsg;
            return isValid;
        }

        private void OnScriptName_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Validate()) return;
            var name = scriptName.Text.Trim();
            var project = Project.Current;
            messageTextBlock.Text = $"{name}.cpp and {name}.h will be created to {Project.Current.Name}";
        }

        private void OnScriptPath_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private async void OnCreate_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            IsEnabled = false;
            busyAnimation.Opacity = 0;
            busyAnimation.Visibility = Visibility.Visible;
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            busyAnimation.BeginAnimation(OpacityProperty, fadeIn);

            try
            {
                var name = scriptName.Text.Trim();
                var path = Path.GetFullPath(Path.Combine(Project.Current.Path, scriptPath.Text.Trim()));
                var solution = Project.Current.Solution;
                var projectName = Project.Current.Name;
                await Task.Run(() => CreateScript(name, path, solution, projectName));
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Logger.Log(MessageType.Error, $"Failed to create script {scriptName.Text}.");
            }
            finally
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.2)));
                fadeOut.Completed += (s, a) => {
                    busyAnimation.Opacity = 1;
                    busyAnimation.Visibility = Visibility.Collapsed;
                    Close();
                };
                busyAnimation.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void CreateScript(string name, string path, string solution, string projectName)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var cpp = Path.GetFullPath(Path.Combine(path, $"{name}.cpp"));
            var h = Path.GetFullPath(Path.Combine(path, $"{name}.h"));

            using (var sw = File.CreateText(cpp))
            {
                sw.Write(string.Format(_cppCode, name, _namespace));
            }

            using (var sw = File.CreateText(h))
            {
                sw.Write(string.Format(_hCode, name, _namespace));
            }

            string[] files = [cpp, h];

            VisualStudio.AddFilesToSolution(solution, projectName, files);
        }

        public NewScriptDialog()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            scriptPath.Text = @"GameCode\";
        }
    }
}
