#pragma once
#include "CommonHeaders.h"
#include "..\Platform\Window.h"

namespace triengine::graphics {

	class surface {};

	struct render_surface {
		platform::window window{};
		surface surface{};
	};

	enum class graphics_platform {
		direct3d12 = 0,
	};

	bool initialize(graphics_platform platform);
	void shutdown();
	void render();
}