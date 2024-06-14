#pragma once
#include "CommonHeaders.h"
#include "Renderer.h"

namespace triengine::graphics {
	struct platform_interface
	{
		bool (*initialize)(void);
		void (*shutdown)(void);
		void (*render)(void);
	};
}