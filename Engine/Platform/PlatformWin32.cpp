#include "Platform.h"
#include "PlatformTypes.h"

namespace triengine::platform {
#ifdef _WIN64
	namespace {
		struct window_info
		{
			HWND hwnd{ nullptr };
			RECT client_area{ 0, 0, 1920, 1080 };
			RECT fullscreen_area{};
			POINT top_left{ 0,0 };
			DWORD style{ WS_VISIBLE };
			bool is_fullscreen{ false };
			bool is_closed{ false };
		};

		utl::free_list<window_info> windows;

		window_info& get_from_id(window_id id) {
			assert(windows[id].hwnd);
			return windows[id];
		}

		window_info& get_from_handle(window_handle handle) {
			const window_id id{ (id::id_type)GetWindowLongPtr(handle, GWLP_USERDATA) };
			return get_from_id(id);
		}

		bool resized{ false };

		LRESULT CALLBACK internal_window_proc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam) {
			window_info* info{ nullptr };
			switch (msg)
			{
			case WM_NCCREATE:
			{
				DEBUG_OP(SetLastError(0));
				const window_id id{ windows.add() };
				windows[id].hwnd = hwnd;
				SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)id);
				assert(GetLastError() == 0);
			}
			case WM_DESTROY:
				get_from_handle(hwnd).is_closed = true;
				break;
			case WM_EXITSIZEMOVE:
				info = &get_from_handle(hwnd);
				break;
			case WM_SIZE:
				resized = (wparam != SIZE_MINIMIZED);
				break;
			default:
				break;
			}

			if (resized && GetAsyncKeyState(VK_LBUTTON) >= 0)
			{
				window_info& info{ get_from_handle(hwnd) };
				assert(info.hwnd);
				GetClientRect(info.hwnd, info.is_fullscreen ? &info.fullscreen_area : &info.client_area);
				resized = false;
			}

			LONG_PTR long_ptr{ GetWindowLongPtr(hwnd, 0) };
			return long_ptr ? reinterpret_cast<window_proc>(long_ptr)(hwnd, msg, wparam, lparam) : DefWindowProc(hwnd, msg, wparam, lparam);
		}

		void resize_window(const window_info& info, const RECT& area) {
			RECT window_rect{ area };
			AdjustWindowRect(&window_rect, info.style, FALSE);

			const s32 width{ window_rect.right - window_rect.left };
			const s32 height{ window_rect.bottom - window_rect.top };

			MoveWindow(info.hwnd, info.top_left.x, info.top_left.y, width, height, TRUE);
		}

		void resize_window(window_id id, u32 width, u32 height) {
			window_info& info{ get_from_id(id) };

			if (info.style & WS_CHILD)
			{
				GetClientRect(info.hwnd, &info.client_area);
			}
			else
			{
				RECT& area{ info.is_fullscreen ? info.fullscreen_area : info.client_area };

				area.right = area.left + width;
				area.bottom = area.top + height;

				resize_window(info, area);
			}
		}

		void set_window_fullscreen(window_id id, bool is_fullscreen)
		{
			window_info& info{ get_from_id(id) };
			if (info.is_fullscreen == is_fullscreen) return;

			info.is_fullscreen = is_fullscreen;

			if (is_fullscreen)
			{
				GetClientRect(info.hwnd, &info.client_area);
				RECT rect;
				GetWindowRect(info.hwnd, &rect);
				info.top_left.x = rect.left;
				info.top_left.y = rect.top;
				SetWindowLongPtr(info.hwnd, GWL_STYLE, 0);
				ShowWindow(info.hwnd, SW_MAXIMIZE);
			}
			else
			{
				SetWindowLongPtr(info.hwnd, GWL_STYLE, info.style);
				resize_window(info, info.client_area);
				ShowWindow(info.hwnd, SW_SHOWNORMAL);
			}
		}

		bool is_window_fullscreen(window_id id)
		{
			const window_info& info{ get_from_id(id) };
			return info.is_fullscreen;
		}

		window_handle get_window_handle(window_id id)
		{
			const window_info& info{ get_from_id(id) };
			return info.hwnd;
		}

		void set_window_caption(window_id id, const wchar_t* caption)
		{
			window_info& info{ get_from_id(id) };
			SetWindowText(info.hwnd, caption);
		}

		math::u32v4 get_window_size(window_id id)
		{
			const window_info& info{ get_from_id(id) };
			RECT area{ info.is_fullscreen ? info.fullscreen_area : info.client_area };
			return math::u32v4{ (u32)area.left, (u32)area.top, (u32)(area.right), (u32)(area.bottom) };
		}

		bool is_window_closed(window_id id)
		{
			return get_from_id(id).is_closed;
		}
	} // anonymous namespace

	window create_window(const window_init_info* init_info) {
		window_proc callback{ init_info ? init_info->callback : nullptr };
		window_handle parent{ init_info ? init_info->parent : nullptr };

		// Setup a window class
		WNDCLASSEX wc;
		ZeroMemory(&wc, sizeof(WNDCLASSEX));
		wc.cbSize = sizeof(WNDCLASSEX);
		wc.style = CS_HREDRAW | CS_VREDRAW;
		wc.lpfnWndProc = internal_window_proc;
		wc.cbClsExtra = 0;
		wc.cbWndExtra = callback ? sizeof(callback) : 0;
		wc.hInstance = 0;
		wc.hIcon = LoadIcon(nullptr, IDI_APPLICATION);
		wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
		wc.hbrBackground = CreateSolidBrush(RGB(26, 48, 76));
		wc.lpszMenuName = nullptr;
		wc.lpszClassName = L"TriEngineWindow";
		wc.hIconSm = LoadIcon(nullptr, IDI_APPLICATION);

		// Register the window class
		RegisterClassEx(&wc);

		window_info info{};
		info.client_area.right = (init_info && init_info->width) ? info.client_area.left + init_info->width : info.client_area.right;
		info.client_area.bottom = (init_info && init_info->height) ? info.client_area.top + init_info->height : info.client_area.bottom;
		info.style |= parent ? WS_CHILD : WS_OVERLAPPEDWINDOW;

		RECT rect{ info.client_area };

		AdjustWindowRect(&rect, info.style, FALSE);

		const wchar_t* caption{ (init_info && init_info->caption) ? init_info->caption : L"TriEngine Game" };
		const s32 left{ init_info ? init_info->left : info.top_left.x };
		const s32 top{ init_info ? init_info->top : info.top_left.y };
		const s32 width{ rect.right - rect.left };
		const s32 height{ rect.bottom - rect.top };

		// Create the window
		info.hwnd = CreateWindowEx(0, wc.lpszClassName, caption, info.style, left, top, width, height, parent, nullptr, nullptr, nullptr);

		if (info.hwnd)
		{
			DEBUG_OP(SetLastError(0));
			if (callback) SetWindowLongPtr(info.hwnd, 0, reinterpret_cast<LONG_PTR>(callback));
			assert(GetLastError() == 0);

			ShowWindow(info.hwnd, SW_SHOWNORMAL);
			UpdateWindow(info.hwnd);

			window_id id{ (id::id_type)GetWindowLongPtr(info.hwnd, GWLP_USERDATA) };
			windows[id] = info;

			return window{ id };
		}

		return {};
	}

	void remove_window(window_id id) {
		window_info& info{ get_from_id(id) };
		DestroyWindow(info.hwnd);
		windows.remove(id);
	}
}

#include "IncludeWindowCpp.h"

#endif 