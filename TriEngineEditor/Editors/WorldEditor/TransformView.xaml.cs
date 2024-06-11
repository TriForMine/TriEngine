using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TriEngineEditor.Components;
using TriEngineEditor.GameProject;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.Editors
{
    /// <summary>
    /// Interaction logic for TransformView.xaml
    /// </summary>
    public partial class TransformView : UserControl
    {
        private Action _undoAction = null;
        private bool _propertyChanged = false;

        public TransformView()
        {
            InitializeComponent();
            Loaded += OnTransformViewLoaded;
        }

        private void OnTransformViewLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnTransformViewLoaded;
            (DataContext as MSTransform).PropertyChanged += (_,_) => _propertyChanged = true;
        }

        private Action GetAction(Func<Transform, (Transform transform, Vector3)> selector, Action<(Transform transform, Vector3)> forEachAction)
        {
            if (DataContext is not MSTransform vm)
            {
                _undoAction = null;
                _propertyChanged = false;
                return null;
            }

            var selection = vm.SelectedComponents.Select(selector).ToList();
            return new Action(() =>
            {
                selection.ForEach(forEachAction);
                (GameEntityView.Instance.DataContext as MSEntity)?.GetMSComponent<MSTransform>().Refresh();
            });
        }

        private Action GetPositionAction() => GetAction(transform => (transform, transform.Position), (x) => x.transform.Position = x.Item2);
        private Action GetRotationAction() => GetAction(transform => (transform, transform.Rotation), (x) => x.transform.Rotation = x.Item2);
        private Action GetScaleAction() => GetAction(transform => (transform, transform.Scale), (x) => x.transform.Scale = x.Item2);

        private void RecordActions(Action redoAction, string name)
        {
            if (_propertyChanged)
            {
                Debug.Assert(_undoAction != null);
                _propertyChanged = false;
                Project.UndoRedo.Add(new UndoRedoAction(_undoAction, redoAction, name));
            }
        }

        private void OnPosition_VectorBox_PreviewMouse_LBD(object sender, MouseButtonEventArgs e)
        {
            _propertyChanged = false;
            _undoAction = GetPositionAction();
        }

        private void OnPosition_VectorBox_PreviewMouse_LBU(object sender, MouseButtonEventArgs e)
        {
            RecordActions(GetPositionAction(), "Position Changed");
        }

        private void OnPosition_VectorBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_propertyChanged && _undoAction != null)
            {
                OnPosition_VectorBox_PreviewMouse_LBU(sender, null);
            }
        }

        private void OnRotation_VectorBox_PreviewMouse_LBD(object sender, MouseButtonEventArgs e)
        {
            _propertyChanged = false;
            _undoAction = GetRotationAction();
        }

        private void OnRotation_VectorBox_PreviewMouse_LBU(object sender, MouseButtonEventArgs e)
        {
            RecordActions(GetRotationAction(), "Rotation Changed");
        }

        private void OnRotation_VectorBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_propertyChanged && _undoAction != null)
            {
                OnRotation_VectorBox_PreviewMouse_LBU(sender, null);
            }
        }

        private void OnScale_VectorBox_PreviewMouse_LBD(object sender, MouseButtonEventArgs e)
        {
            _propertyChanged = false;
            _undoAction = GetScaleAction();
        }

        private void OnScale_VectorBox_PreviewMouse_LBU(object sender, MouseButtonEventArgs e)
        {
            RecordActions(GetScaleAction(), "Scale Changed");
        }

        private void OnScale_VectorBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_propertyChanged && _undoAction != null)
            {
                OnScale_VectorBox_PreviewMouse_LBU(sender, null);
            }
        }
    }
}
