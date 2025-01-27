#pragma once
#include "CommonHeaders.h"
#include "Platform/Window.h"
#include "EngineAPI/Camera.h"

namespace triengine::graphics {
	struct frame_info
	{
		id::id_type* render_item_ids{ nullptr };
		f32* thresholds{ nullptr };
		u32 render_item_count{ 0 };
		camera_id camera_id{ id::invalid_id };
	};

	DEFINE_TYPED_ID(surface_id);

	class surface {
	public:
		constexpr surface(surface_id id) : _id{ id } {}
		constexpr surface() = default;
		constexpr surface_id get_id() const { return _id; }
		constexpr bool is_valid() const { return id::is_valid(_id); }

		void resize(u32 width, u32 height) const;
		u32 width() const;
		u32 height() const;
		void render(frame_info info) const;
	private:
		surface_id _id{ id::invalid_id };
	};

	struct render_surface {
		platform::window window{};
		triengine::graphics::surface surface{};
	};

	struct camera_parameter {
		enum parameter : u32 {
			up_vector,
			field_of_view,
			aspect_ratio,
			view_width,
			view_height,
			near_z,
			far_z,
			view,
			projection,
			inverse_projection,
			view_projection,
			inverse_view_projection,
			type,
			item_id,

			count
		};
	};

	struct camera_init_info
	{
		id::id_type item_id{ id::invalid_id };
		camera::type type{};
		math::v3 up;
		union {
			f32 field_of_view;
			f32 view_width;
		};

		union {
			f32 aspect_ratio;
			f32 view_height;
		};

		f32 near_z;
		f32 far_z;
	};

	struct perspective_camera_init_info : public camera_init_info
	{
		explicit perspective_camera_init_info(id::id_type id)
		{
			assert(id::is_valid(id));
			item_id = id;
			type = camera::perspective;
			up = math::v3{ 0.f, 1.f, 0.f };
			field_of_view = 0.25f;
			aspect_ratio = 16.f / 9.f;
			near_z = 0.01f;
			far_z = 1000.f;
		}
	};

	struct orthographic_camera_init_info : public camera_init_info
	{
		explicit orthographic_camera_init_info(id::id_type id)
		{
			assert(id::is_valid(id));
			item_id = id;
			type = camera::orthographic;
			up = math::v3{ 0.f, 1.f, 0.f };
			view_width = 1920;
			view_height = 1080;
			near_z = 0.01f;
			far_z = 1000.f;
		}
	};

	struct shader_flags {
		enum flags : u32 {
			none = 0x0,
			vertex = 0x01,
			hull = 0x02,
			domain = 0x04,
			geometry = 0x08,
			pixel = 0x010,
			compute = 0x20,
			amplification = 0x40,
			mesh = 0x80,
		};
	};

	struct shader_type {
		enum type : u32 {
			vertex = 0,
			hull,
			domain,
			geometry,
			pixel,
			compute,
			amplification,
			mesh,

			count
		};
	};

	struct material_type {
		enum type : u32 {
			opaque,

			count
		};
	};

	struct material_init_info
	{
		material_type::type type;
		u32 texture_count;
		id::id_type shader_ids[shader_type::count]{ id::invalid_id, id::invalid_id, id::invalid_id, id::invalid_id, id::invalid_id, id::invalid_id, id::invalid_id, id::invalid_id };
		id::id_type* texture_ids;
	};

	struct primitive_topology {
		enum type : u32 {
			point_list = 1,
			line_list,
			line_strip,
			triangle_list,
			triangle_strip,

			count
		};
	};

#ifndef PRIMAL_PLUS
	enum class graphics_platform {
		direct3d12 = 0,
	};
#else
#include "Graphics/GraphicsPlatform.h"
#endif

	bool initialize(graphics_platform platform);
	void shutdown();

	const char* get_engine_shaders_path();
	const char* get_engine_shaders_path(graphics_platform platform);

	surface create_surface(platform::window window);
	void remove_surface(surface_id id);

	camera create_camera(camera_init_info info);
	void remove_camera(camera_id id);

	id::id_type add_submesh(const u8*& data);
	void remove_submesh(id::id_type id);

	id::id_type add_material(const material_init_info& info);
	void remove_material(id::id_type id);

	id::id_type add_render_item(id::id_type item_id, id::id_type geometry_content_id, u32 material_count, const id::id_type* const material_ids);
	void remove_render_item(id::id_type id);
}