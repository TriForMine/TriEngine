#pragma once
#include "ComponentsCommon.h"

namespace triengine {

#define INIT_INFO(component) namespace component { struct init_info; }

	INIT_INFO(transform);
	INIT_INFO(script);

#undef INIT_INFO

	namespace game_entity {
		struct entity_info
		{
			transform::init_info* transform{nullptr};
			script::init_info* script{nullptr};
		};

		entity create(entity_info info);
		void remove(item_id e);
		bool is_alive(item_id e);
	}
}