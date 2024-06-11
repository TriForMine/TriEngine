#pragma once
#include "Window.h"

namespace triengine::platform {
	struct window_init_info;

	window create_window(const window_init_info* const init_info = nullptr);
	void remove_window(window_id id);

}