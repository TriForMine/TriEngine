#pragma once
#include "D3D12CommonHeader.h"

namespace triengine::graphics::d3d12::shaders {

	struct engine_shader {
		enum id : u32 {
			fullscreen_triangle_vs = 0,
			fill_color_ps = 1,
			post_process_ps = 2,

			count
		};
	};

	bool initialize();
	void shutdown();

	D3D12_SHADER_BYTECODE get_engine_shader(engine_shader::id id);
}