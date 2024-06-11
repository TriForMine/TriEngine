#pragma once
#pragma once
#include "CommonHeaders.h"

#if !defined(SHIPPING)

namespace triengine::content {
	bool load_game();
	void unload_game();
}

#endif // !SHIPPING