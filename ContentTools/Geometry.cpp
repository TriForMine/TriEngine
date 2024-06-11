#include "Geometry.h"

namespace triengine::tools {
	namespace {
		using namespace math;
		using namespace DirectX;

		void recalculate_normals(mesh& m)
		{
			const u32 num_indices{ (u32)m.raw_indices.size() };
			m.normals.reserve(num_indices);

			for (u32 i{ 0 }; i < num_indices; ++i)
			{
				const u32 i0{ m.raw_indices[i] };
				const u32 i1{ m.raw_indices[++i] };
				const u32 i2{ m.raw_indices[++i] };

				XMVECTOR v0{ XMLoadFloat3(&m.positions[i0]) };
				XMVECTOR v1{ XMLoadFloat3(&m.positions[i1]) };
				XMVECTOR v2{ XMLoadFloat3(&m.positions[i2]) };

				XMVECTOR e0{ v1 - v0 };
				XMVECTOR e1{ v2 - v0 };

				XMVECTOR n{ XMVector3Normalize(XMVector3Cross(e0, e1)) };

				XMStoreFloat3(&m.normals[i], n);
				m.normals[i - 1] = m.normals[i];
				m.normals[i - 2] = m.normals[i];
			}
		}

		void process_normals(mesh& m, f32 smoothing_angle)
		{
			const f32 cos_angle{ XMScalarCos(pi - smoothing_angle * pi / 180.f) };
			const bool is_hard_edge{ XMScalarNearEqual(smoothing_angle, 180.f, epsilon) };
			const bool is_soft_edge{ XMScalarNearEqual(smoothing_angle, 0.f, epsilon) };
			const u32 num_indices{(u32)m.raw_indices.size() };
			const u32 num_vertices{ (u32)m.positions.size() };
			assert(num_indices && num_vertices);

			m.indices.resize(num_indices);
		}
	}

	void process_vertices(mesh& m, const geometry_import_settings& settings) {
		assert((m.raw_indices.size() % 3) == 0);

		if (settings.calculate_normals || m.normals.empty())
		{
			recalculate_normals(m);
		}

		process_normals(m, settings.smoothing_angle);

		if (!m.uv_sets.empty())
		{
			process_uvs(m);
		}

		pack_vertices_static(m);
	}

	void process_scene(scene& scene, const geometry_import_settings& settings) {
		for(auto& lod: scene.lod_groups)
			for (auto& m : lod.meshes)
			{
				process_vertices(m, settings);
			}
	}

	void pack_data(const scene& scene, const scene_data& data)
	{

	}
}