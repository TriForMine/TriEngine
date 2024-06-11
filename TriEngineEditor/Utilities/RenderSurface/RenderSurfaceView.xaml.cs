using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using TriEngineEditor.Utilities.RenderSurface;

namespace TriEngineEditor.Utilities
{
    /// <summary>
    /// Interaction logic for RenderSurfaceView.xaml
    /// </summary>
    public partial class RenderSurfaceView : UserControl, IDisposable
    {
        private enum Win32Msg
        {
            WM_SIZING = 0x0214,
            WM_ENTERSIZEMOVE = 0x0231,
            WM_EXITSIZEMOVE = 0x0232,
            WM_SIZE = 0x0005,
        }
        private RenderSurfaceHost? _host = null;
        private bool _canResize = true;
        private bool _moved = false;

        public RenderSurfaceView()
        {
            InitializeComponent();
            Loaded += OnRenderSurfaceViewLoaded;
        }

        private void OnRenderSurfaceViewLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnRenderSurfaceViewLoaded;

            _host = new RenderSurfaceHost(ActualWidth, ActualHeight);
            _host.MessageHook += new HwndSourceHook(HostMsgFilter);
            Content = _host;

            var window = this.FindVisualParent<Window>();
            Debug.Assert(window != null);

            var helper = new WindowInteropHelper(window);

            HwndSource.FromHwnd(helper.Handle).AddHook(HwndMsgFilter);
        }

        private IntPtr HwndMsgFilter(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((Win32Msg)msg)
            {
                case Win32Msg.WM_SIZING:
                    _canResize = false;
                    _moved = false;
                    break;
                case Win32Msg.WM_ENTERSIZEMOVE:
                    _moved = true;
                    break;
                case Win32Msg.WM_EXITSIZEMOVE:
                    _canResize = true;
                    if (!_moved)
                        _host?.Resize();
                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        private IntPtr HostMsgFilter(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((Win32Msg)msg)
            {
                case Win32Msg.WM_SIZING: throw new Exception();
                case Win32Msg.WM_ENTERSIZEMOVE: throw new Exception();
                case Win32Msg.WM_EXITSIZEMOVE: throw new Exception();
                case Win32Msg.WM_SIZE:
                    if (_canResize)
                        _host?.Resize();
                    break;
            }

            return IntPtr.Zero;
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _host.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
