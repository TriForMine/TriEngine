#pragma once

#include "..\Components\ComponentsCommon.h"
#include "TransformComponent.h"
#include "ScriptComponent.h"

namespace triengine {

	namespace game_entity {

		DEFINE_TYPED_ID(item_id);

		class entity {
		public:
			constexpr entity(item_id id) : _id{ id } {}
			constexpr entity() : _id{ id::invalid_id } {}
			constexpr item_id get_id() const { return _id; }
			constexpr bool is_valid() const { return id::is_valid(_id); }

			transform::component transform() const;
			script::component script() const;
		private:
			item_id _id;
		};

	}; // namespace game_entity

	namespace script
	{
		class entity_script : public game_entity::entity
		{
		public:
			virtual ~entity_script() = default;
			virtual void begin_play() {}
			virtual void update(float) {};
		protected:
			constexpr explicit entity_script(game_entity::entity entity) : game_entity::entity{ entity.get_id() } {};
		};

		namespace detail {
			using script_ptr = std::unique_ptr<script::entity_script>;
			using script_creator = script_ptr(*)(game_entity::entity entity);
			using string_hash = std::hash<std::string>;

			u8 register_script(size_t, script_creator);
#ifdef USE_WITH_EDITOR
			extern "C" __declspec(dllexport)
#endif
			script_creator get_script_creator(size_t tag);

			template<class script_class> script_ptr create_script(game_entity::entity entity)
			{
				assert(entity.is_valid());
				return std::make_unique<script_class>(entity);
			}

			u8 add_script_name(const char* name);

#ifdef USE_WITH_EDITOR
#define REGISTER_SCRIPT(TYPE)                                                                                                                                                                                      \
			namespace {																																															   \
				static u8 _reg_##TYPE{ triengine::script::detail::register_script(triengine::script::detail::string_hash()(#TYPE), &triengine::script::detail::create_script<TYPE>) };                             \
			} 																																																	   \
			static u8 _name_##TYPE{ triengine::script::detail::add_script_name(#TYPE) };
#else

#define REGISTER_SCRIPT(TYPE)                                                                                                                                                                                      \
			namespace {																																															   \
				static u8 _reg_##TYPE{ triengine::script::detail::register_script(triengine::script::detail::string_hash()(#TYPE), &triengine::script::detail::create_script<TYPE>) };                             \
			}
#endif
		} // namespace detail
	} // namespace script
} // namespace triengine