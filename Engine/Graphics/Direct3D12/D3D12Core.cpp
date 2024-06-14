#include "D3D12Core.h"
#include "D3D12Resources.h"

using namespace Microsoft::WRL;

namespace triengine::graphics::d3d12::core {
	namespace {
		class d3d12_command
		{
		public:
			d3d12_command() = default;
			DISABLE_COPY_AND_MOVE(d3d12_command);
			explicit d3d12_command(ID3D12Device10* const device, D3D12_COMMAND_LIST_TYPE type)
			{
				HRESULT hr{ S_OK };
				D3D12_COMMAND_QUEUE_DESC desc{};
				desc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
				desc.NodeMask = 0;
				desc.Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL;
				desc.Type = type;
				DXCall(hr = device->CreateCommandQueue(&desc, IID_PPV_ARGS(&_cmd_queue)));
				if (FAILED(hr)) goto _error;
				NAME_D3D12_OBJECT(_cmd_queue, type == D3D12_COMMAND_LIST_TYPE_DIRECT ? L"GFX COMMAND QUEUE" : type == D3D12_COMMAND_LIST_TYPE_COMPUTE ? L"COMPUTE COMMAND QUEUE" : L"COMMAND QUEUE");

				for (u32 i{ 0 }; i < frame_buffer_count; ++i)
				{
					command_frame& frame{ _cmd_frames[i] };
					DXCall(hr = device->CreateCommandAllocator(type, IID_PPV_ARGS(&frame.cmd_allocator)));
					if (FAILED(hr)) goto _error;
					NAME_D3D12_OBJECT_INDEXED(frame.cmd_allocator, i, type == D3D12_COMMAND_LIST_TYPE_DIRECT ? L"GFX COMMAND ALLOCATOR" : type == D3D12_COMMAND_LIST_TYPE_COMPUTE ? L"COMPUTE COMMAND ALLOCATOR" : L"COMMAND ALLOCATOR");
				}

				DXCall(hr = device->CreateCommandList(0, type, _cmd_frames[0].cmd_allocator, nullptr, IID_PPV_ARGS(&_cmd_list)));
				if (FAILED(hr)) goto _error;
				DXCall(_cmd_list->Close());

				NAME_D3D12_OBJECT(_cmd_list, type == D3D12_COMMAND_LIST_TYPE_DIRECT ? L"GFX COMMAND LIST" : type == D3D12_COMMAND_LIST_TYPE_COMPUTE ? L"COMPUTE COMMAND LIST" : L"COMMAND LIST");

				DXCall(hr = device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&_fence)));
				if (FAILED(hr)) goto _error;
				NAME_D3D12_OBJECT(_fence, L"D3D12 FENCE");

				_fence_event = CreateEventEx(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
				assert(_fence_event);

				return;

			_error:
				release();
			}

			~d3d12_command()
			{
				assert(!_cmd_queue && !_cmd_list && !_fence && !_fence_event);
			}

			void begin_frame()
			{
				command_frame& frame{ _cmd_frames[_frame_index] };
				frame.wait(_fence_event, _fence);
				DXCall(frame.cmd_allocator->Reset());
				DXCall(_cmd_list->Reset(frame.cmd_allocator, nullptr));
			}

			void end_frame()
			{
				DXCall(_cmd_list->Close());
				ID3D12CommandList* cmd_lists[]{ _cmd_list };
				_cmd_queue->ExecuteCommandLists(_countof(cmd_lists), &cmd_lists[0]);

				u64& fence_value{ _fence_value };
				++fence_value;
				command_frame& frame{ _cmd_frames[_frame_index] };
				frame.fence_value = fence_value;
				_cmd_queue->Signal(_fence, _fence_value);

				_frame_index = (_frame_index + 1) % frame_buffer_count;
			}

			void flush()
			{
				for (u32 i{ 0 }; i < frame_buffer_count; ++i)
				{
					_cmd_frames[i].wait(_fence_event, _fence);
				}
				_frame_index = 0;
			}

			void release()
			{
				flush();
				core::release(_fence);
				_fence_value = 0;

				CloseHandle(_fence_event);
				_fence_event = nullptr;

				core::release(_cmd_queue);
				core::release(_cmd_list);

				for (u32 i{ 0 }; i < frame_buffer_count; ++i)
				{
					_cmd_frames[i].release();
				}
			}

			constexpr ID3D12CommandQueue* const command_queue() const noexcept { return _cmd_queue; }
			constexpr ID3D12GraphicsCommandList7* const command_list() const noexcept { return _cmd_list; }
			constexpr u32 frame_index() const noexcept { return _frame_index; }
		private:
			struct command_frame
			{
				ID3D12CommandAllocator* cmd_allocator{ nullptr };
				u64 fence_value{ 0 };

				void wait(HANDLE fence_event, ID3D12Fence1* fence)
				{
					assert(fence && fence_event);

					if (fence->GetCompletedValue() < fence_value)
					{
						DXCall(fence->SetEventOnCompletion(fence_value, fence_event));
						WaitForSingleObject(fence_event, INFINITE);
					}
				}

				void release()
				{
					core::release(cmd_allocator);
					fence_value = 0;
				}
			};

			ID3D12CommandQueue* _cmd_queue{ nullptr };
			ID3D12GraphicsCommandList7* _cmd_list{ nullptr };
			ID3D12Fence1* _fence{ nullptr };
			u64 _fence_value{ 0 };
			HANDLE _fence_event{ nullptr };
			command_frame _cmd_frames[frame_buffer_count]{};
			u32 _frame_index{ 0 };
		};

		ID3D12Device10* main_device{ nullptr };
		IDXGIFactory7* dxgi_factory{ nullptr };
		d3d12_command gfx_command;
		descriptor_heap rtv_desc_heap{ D3D12_DESCRIPTOR_HEAP_TYPE_RTV };
		descriptor_heap dsv_desc_heap{ D3D12_DESCRIPTOR_HEAP_TYPE_DSV };
		descriptor_heap srv_desc_heap{ D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV };
		descriptor_heap uav_desc_heap{ D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV };

		utl::vector<IUnknown*> deferred_releases[frame_buffer_count]{};
		u32 deferred_releases_flag[frame_buffer_count]{};
		std::mutex deferred_releases_mutex{};

		constexpr D3D_FEATURE_LEVEL minimum_feature_level{ D3D_FEATURE_LEVEL_11_0 };

		bool failed_init()
		{
			shutdown();
			return false;
		}

		IDXGIAdapter4* determine_main_adapter()
		{
			IDXGIAdapter4* adapter{ nullptr };

			for (u32 i{ 0 }; dxgi_factory->EnumAdapterByGpuPreference(i, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, IID_PPV_ARGS(&adapter)) != DXGI_ERROR_NOT_FOUND; ++i)
			{
				if (SUCCEEDED(D3D12CreateDevice(adapter, minimum_feature_level, __uuidof(ID3D12Device10), nullptr))) {
					return adapter;
				}
				release(adapter);
			}

			return nullptr;
		}

		D3D_FEATURE_LEVEL get_max_feature_level(IDXGIAdapter4* adapter)
		{
			constexpr D3D_FEATURE_LEVEL feature_levels[]
			{
				D3D_FEATURE_LEVEL_11_0,
				D3D_FEATURE_LEVEL_11_1,
				D3D_FEATURE_LEVEL_12_0,
				D3D_FEATURE_LEVEL_12_1,
				D3D_FEATURE_LEVEL_12_2
			};

			D3D12_FEATURE_DATA_FEATURE_LEVELS features_level_info{};
			features_level_info.NumFeatureLevels = _countof(feature_levels);
			features_level_info.pFeatureLevelsRequested = feature_levels;

			ComPtr<ID3D12Device> device;
			DXCall(D3D12CreateDevice(adapter, minimum_feature_level, IID_PPV_ARGS(&device)));
			DXCall(device->CheckFeatureSupport(D3D12_FEATURE_FEATURE_LEVELS, &features_level_info, sizeof(features_level_info)));
			return features_level_info.MaxSupportedFeatureLevel;
		}

		void __declspec(noinline) process_deferred_releases(u32 frame_idx)
		{
			std::lock_guard lock{ deferred_releases_mutex };

			deferred_releases_flag[frame_idx] = 0;

			rtv_desc_heap.process_defered_free(frame_idx);
			dsv_desc_heap.process_defered_free(frame_idx);
			srv_desc_heap.process_defered_free(frame_idx);
			uav_desc_heap.process_defered_free(frame_idx);
			
			utl::vector<IUnknown*>& resources{ deferred_releases[frame_idx] };
			if (!resources.empty())
			{
				for (IUnknown* resource : resources) release(resource);
				resources.clear();
			}
		}
	} // anonymous namespace 

	namespace detail {
		void deferred_release(IUnknown* resource)
		{
			const u32 frame_idx{ current_frame_index() };
			std::lock_guard lock{ deferred_releases_mutex };
			deferred_releases[frame_idx].push_back(resource);
			set_deferred_releases_flag();
		}
	}

	bool initialize()
	{
		if (main_device) shutdown();

		u32 dxgi_factory_flags = 0;
#ifdef _DEBUG
		// Enable debug layer.
		{
			ComPtr<ID3D12Debug> debug_interface;
			if (SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debug_interface))))
			{
				debug_interface->EnableDebugLayer();
			}
			else
			{
				OutputDebugStringA("Warning: D3D12 Debug Layer is not available. Verify that Graphics Tools optional feature is installed in Windows Settings.\n");
			}
			dxgi_factory_flags |= DXGI_CREATE_FACTORY_DEBUG;
		}
#endif

		HRESULT hr{ S_OK };
		DXCall(hr = CreateDXGIFactory2(dxgi_factory_flags, IID_PPV_ARGS(&dxgi_factory)));
		if (FAILED(hr)) return failed_init();

		ComPtr<IDXGIAdapter4> main_adapter;
		main_adapter.Attach(determine_main_adapter());
		if (!main_adapter) return failed_init();

		D3D_FEATURE_LEVEL max_feature_level{ get_max_feature_level(main_adapter.Get()) };
		assert(max_feature_level >= minimum_feature_level);
		if (max_feature_level < minimum_feature_level) return failed_init();

		DXCall(hr = D3D12CreateDevice(main_adapter.Get(), max_feature_level, IID_PPV_ARGS(&main_device)));
		if (FAILED(hr)) return failed_init();

		bool result{ true };
		result &= rtv_desc_heap.initialize(512, false);
		result &= dsv_desc_heap.initialize(512, false);
		result &= srv_desc_heap.initialize(4096, true);
		result &= uav_desc_heap.initialize(512, false);
		if (!result) return failed_init();

#ifdef _DEBUG
		{
			ComPtr<ID3D12InfoQueue> info_queue;
			DXCall(main_device->QueryInterface(IID_PPV_ARGS(&info_queue)));

			info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, true);
			info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_WARNING, true);
			info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, true);
		}
#endif

		new (&gfx_command) d3d12_command(main_device, D3D12_COMMAND_LIST_TYPE_DIRECT);
		if (!gfx_command.command_queue()) return failed_init();

		NAME_D3D12_OBJECT(main_device, L"MAIN DEVICE");
		NAME_D3D12_OBJECT(rtv_desc_heap.heap(), L"RTV DESCRIPTOR HEAP");
		NAME_D3D12_OBJECT(dsv_desc_heap.heap(), L"DSV DESCRIPTOR HEAP");
		NAME_D3D12_OBJECT(srv_desc_heap.heap(), L"SRV DESCRIPTOR HEAP");
		NAME_D3D12_OBJECT(uav_desc_heap.heap(), L"UAV DESCRIPTOR HEAP");

		return true;
	}

	void shutdown()
	{
		gfx_command.release();

		for (u32 i{ 0 }; i < frame_buffer_count; ++i)
		{
			process_deferred_releases(i);
		}

		release(dxgi_factory);

		rtv_desc_heap.release();
		dsv_desc_heap.release();
		srv_desc_heap.release();
		uav_desc_heap.release();

		process_deferred_releases(0);

#ifdef _DEBUG
		{
			{
				ComPtr<ID3D12InfoQueue> info_queue;
				DXCall(main_device->QueryInterface(IID_PPV_ARGS(&info_queue)));

				info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, false);
				info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_WARNING, false);
				info_queue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, false);
			}

			ComPtr<ID3D12DebugDevice> debug_device;
			DXCall(main_device->QueryInterface(IID_PPV_ARGS(&debug_device)));
			release(main_device);
			DXCall(debug_device->ReportLiveDeviceObjects(D3D12_RLDO_DETAIL | D3D12_RLDO_SUMMARY | D3D12_RLDO_IGNORE_INTERNAL));
		}
#endif

		release(main_device);
	}

	void render()
	{
		gfx_command.begin_frame();
		ID3D12GraphicsCommandList7* cmd_list{ gfx_command.command_list() };

		const u32 frame_idx{ current_frame_index() };
		if (deferred_releases_flag[frame_idx])
		{
			process_deferred_releases(frame_idx);
		}


		gfx_command.end_frame();
	}

	ID3D12Device10* const device()
	{
		return main_device;
	}

	u32 current_frame_index() { return gfx_command.frame_index(); }

	void set_deferred_releases_flag() { deferred_releases_flag[current_frame_index()] = 1; }
}