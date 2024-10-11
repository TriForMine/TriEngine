#pragma once
#include "CommonHeaders.h"
#include "Graphics\Renderer.h"
#include "Platform\Window.h"

// Skip defintion of min/max macros in Windows.h
#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <dxgi1_6.h>
#include <d3d12.h>
#include <wrl.h>

#pragma comment(lib, "d3d12.lib")
#pragma comment(lib, "dxgi.lib")

namespace triengine::graphics::d3d12 {
	constexpr u32 frame_buffer_count{ 3 };
	using id3d12_device = ID3D12Device10;
	using id3d12_graphics_command_list = ID3D12GraphicsCommandList7;
}

#ifdef _DEBUG
#ifndef DXCall

#define DXCall(x)									\
if (FAILED(x)) {									\
	char line_number[32];							\
    sprintf_s(line_number, "%u", __LINE__);			\
    OutputDebugStringA("DXCall failed in: ");		\
	OutputDebugStringA(__FILE__);					\
	OutputDebugStringA("\nLine: ");				    \
	OutputDebugStringA(line_number);				\
	OutputDebugStringA("\n");						\
	OutputDebugStringA(#x); 						\
	OutputDebugStringA("\n");						\
	__debugbreak();									\
}

#endif // !DXCall
#else
#ifndef DXCall
#define DXCall(x) x
#endif // !DXCall
#endif // !_DEBUG

#ifdef _DEBUG
#define NAME_D3D12_OBJECT(x, name) x->SetName(name); OutputDebugString(L"::D3D12 Object Created: "); OutputDebugString(name); OutputDebugString(L"\n");

#define NAME_D3D12_OBJECT_INDEXED(x, index, name) { wchar_t buffer[128]; swprintf_s(buffer, L"%s[%llu]", name, (u64)index); x->SetName(buffer); OutputDebugString(L"::D3D12 Object Created: "); OutputDebugString(buffer); OutputDebugString(L"\n"); }
#else
#define NAME_D3D12_OBJECT(x, name)

#define NAME_D3D12_OBJECT_INDEXED(x, index, name)
#endif

#include "D3D12Helpers.h"
#include "D3D12Resources.h"