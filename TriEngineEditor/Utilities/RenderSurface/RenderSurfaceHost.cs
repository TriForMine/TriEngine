using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using TriEngineEditor.DllWrappers;

namespace TriEngineEditor.Utilities.RenderSurface
{
    class RenderSurfaceHost : HwndHost
    {
        private IntPtr _renderWindowHandle = IntPtr.Zero;
        private readonly int _width = 800;
        private readonly int _height = 600;
        private DelayEventTimer _resizeTimer;

        public int SurfaceId { get; private set; } = ID.INVALID_ID;

        public void Resize()
        {
            _resizeTimer.Trigger();
        }

        private void Resize(object sender, DelayEventTimerArgs e)
        {
            e.RepeatEvent = Mouse.LeftButton == MouseButtonState.Pressed;

            if (!e.RepeatEvent){
                EngineAPI.ResizeRenderSurface(SurfaceId);
            }
        }

        public RenderSurfaceHost(double width, double height)
        {
            width = (int)width;
            height = (int)height;
            _resizeTimer = new DelayEventTimer(TimeSpan.FromMilliseconds(250.0));
            _resizeTimer.Triggered += Resize;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            SurfaceId = EngineAPI.CreateRenderSurface(hwndParent.Handle, _width, _height);
            Debug.Assert(ID.IsValid(SurfaceId), "Failed to create render surface");
            _renderWindowHandle = EngineAPI.GetWindowHandle(SurfaceId);
            Debug.Assert(_renderWindowHandle != IntPtr.Zero, "Failed to get render window handle");

            return new HandleRef(this, _renderWindowHandle);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            EngineAPI.RemoveRenderSurface(SurfaceId);
            SurfaceId = ID.INVALID_ID;
            _renderWindowHandle = IntPtr.Zero;
        }
    }
}
