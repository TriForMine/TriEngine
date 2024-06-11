using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using TriEngineEditor.Components;
using TriEngineEditor.GameProject;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.Editors
{
    public class NullableBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool?)value == true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value;
        }
    }
    /// <summary>
    /// Interaction logic for GameEntityView.xaml
    /// </summary>
    public partial class GameEntityView : UserControl
    {
        private Action _undoAction;
        private string _propertyName;
        public static GameEntityView Instance { get; private set; }

        public GameEntityView()
        {
            InitializeComponent();
            DataContext = null;
            Instance = this;
            DataContextChanged += (_, _) =>
            {
                if (DataContext != null)
                {
                    ((MSEntity)DataContext).PropertyChanged += (s, e) => _propertyName = e.PropertyName;
                }
            };
        }

        private Action GetRenameAction()
        {
            var vm = DataContext as MSEntity;
            var selection = vm.SelectedEntities.Select(entity => (entity, entity.Name)).ToList();
            return new Action(
               () =>
               {
                   selection.ForEach(pair => pair.entity.Name = pair.Item2);
                   ((MSEntity)DataContext).Refresh();
               }
            );
        }

        private Action GetIsEnabledAction()
        {
            var vm = DataContext as MSEntity;
            var selection = vm.SelectedEntities.Select(entity => (entity, entity.IsEnabled)).ToList();
            return new Action(
               () =>
               {
                   selection.ForEach(pair => pair.entity.IsEnabled = pair.Item2);
                   ((MSEntity)DataContext).Refresh();
               }
            );
        }


        private void OnName_TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _propertyName = string.Empty;
            _undoAction = GetRenameAction();
        }

        private void OnName_TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_propertyName == nameof(MSEntity.Name) && _undoAction != null)
            {
                var redoAction = GetRenameAction();
                Project.UndoRedo.Add(new UndoRedoAction(_undoAction, redoAction, "Change Entity Name"));
                _propertyName = null;
            }
            _undoAction = null;
        }

        private void OnIsEnabled_Checkbox_Click(object sender, RoutedEventArgs e)
        {
            var undoAction = GetIsEnabledAction();
            var vm = DataContext as MSEntity;
            vm.IsEnabled = ((CheckBox)sender).IsChecked;
            var redoAction = GetIsEnabledAction();
            Project.UndoRedo.Add(new UndoRedoAction(undoAction, redoAction, vm.IsEnabled == true ? "Enable Entity" : "Disable Entity"));
        }

        private void OnAddComponent_Button_PreviewMouse_LBD(object sender, MouseButtonEventArgs e)
        {
            var menu = FindResource("addComponentMenu") as ContextMenu;
            var btn = sender as ToggleButton;
            btn.IsChecked = true;
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = btn;
            menu.MinWidth = btn.ActualWidth;
            menu.IsOpen = true;
        }
        
        private void AddComponent(ComponentType componentType, string componentName)
        {
            var creationFunction = ComponentFactory.GetCreateFunction(componentType);
            var chandedEntities = new List<(GameEntity entity, Component component)>();
            var vm = DataContext as MSEntity;
            foreach (var entity in vm.SelectedEntities)
            {
                var component = creationFunction(entity, componentName);
                if (entity.AddComponent(component))
                {
                    chandedEntities.Add((entity, component));
                }
            }

            if (chandedEntities.Count > 0)
            {
                vm.Refresh();

                var undoAction = new Action(() =>
                {
                    chandedEntities.ForEach(pair => pair.entity.RemoveComponent(pair.component));
                    (DataContext as MSEntity).Refresh();
                });

                var redoAction = new Action(() =>
                {
                    chandedEntities.ForEach(pair => pair.entity.AddComponent(pair.component));
                    (DataContext as MSEntity).Refresh();
                });

                Project.UndoRedo.Add(new UndoRedoAction(undoAction, redoAction, $"Add {componentName} Component"));
            }
        }

        private void OnAddScriptComponent(object sender, RoutedEventArgs e)
        {
            AddComponent(ComponentType.Script, (sender as MenuItem).Header.ToString());
        }
    }
}
