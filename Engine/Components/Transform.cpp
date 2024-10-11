#include "Transform.h"
#include "Entity.h"

namespace triengine::transform {
	namespace {
		utl::vector<math::m4x4> to_world;
		utl::vector<math::m4x4> inv_world;
		utl::vector<math::v3> positions;
		utl::vector<math::v3> orientations;
		utl::vector<math::v4> rotations;
		utl::vector<math::v3> scales;
		utl::vector<u8> has_transform;

		void calculate_transform_matrices(id::id_type index)
		{
			assert(rotations.size() >= index);
			assert(positions.size() >= index);
			assert(scales.size() >= index);

			using namespace DirectX;
			XMVECTOR r{ XMLoadFloat4(&rotations[index]) };
			XMVECTOR p{ XMLoadFloat3(&positions[index]) };
			XMVECTOR s{ XMLoadFloat3(&scales[index]) };

			XMMATRIX world{ XMMatrixAffineTransformation(s, XMQuaternionIdentity(), r, p) };
			DirectX::XMStoreFloat4x4(&to_world[index], world);

			world.r[3] = XMVectorSet(0.0f, 0.0f, 0.0f, 1.0f);
			XMMATRIX inverse_world{ XMMatrixInverse(nullptr, world) };
			DirectX::XMStoreFloat4x4(&inv_world[index], inverse_world);

			has_transform[index] = 1;
		}

		math::v3 calculate_orientation(math::v4 rotation)
		{
			using namespace DirectX;
			XMVECTOR rotation_quat{ XMLoadFloat4(&rotation) };
			XMVECTOR front{ XMVectorSet(0.0f, 0.0f, 1.0f, 0.0f) };
			math::v3 orientation;
			XMStoreFloat3(&orientation, XMVector3Rotate(front, rotation_quat));
			return orientation;
		}
	}

	component create(init_info info, game_entity::entity entity)
	{
		assert(entity.is_valid());
		const id::id_type entity_index{ id::index(entity.get_id()) };

		if (positions.size() > entity_index)
		{
			math::v4 rotation{ info.rotation };
			rotations[entity_index] = rotation;
			orientations[entity_index] = calculate_orientation(rotation);
			positions[entity_index] = math::v3{ info.position };
			scales[entity_index] = math::v3{ info.scale };
			has_transform[entity_index] = 0;
		}
		else
		{
			assert(positions.size() == entity_index);
			to_world.emplace_back();
			inv_world.emplace_back();
			rotations.emplace_back(info.rotation);
			orientations.emplace_back(calculate_orientation(math::v4{ info.rotation }));
			positions.emplace_back(info.position);
			scales.emplace_back(info.scale);
			has_transform.emplace_back((u8)0);
		}

		// NOTE: each entity has a transform component. Therefor, id's for transform components
		//       are exactly the same as entity ids.
		return component{ transform_id{ entity.get_id() } };
	}

	void remove([[maybe_unused]] component c)
	{
		assert(c.is_valid());
	}

	void get_transform_matrices(const game_entity::item_id id, math::m4x4& world, math::m4x4& inverse_world)
	{
		assert(game_entity::entity{ id }.is_valid());

		const id::id_type entity_index{ id::index(id) };
		if (!has_transform[entity_index])
		{
			calculate_transform_matrices(entity_index);
		}

		world = to_world[entity_index];
		inverse_world = inv_world[entity_index];
	}

	math::v4 component::rotation() const {
		assert(is_valid());
		return rotations[id::index(_id)];
	}

	math::v3 component::orientation() const {
		assert(is_valid());
		return orientations[id::index(_id)];
	}

	math::v3 component::position() const {
		assert(is_valid());
		return positions[id::index(_id)];
	}

	math::v3 component::scale() const {
		assert(is_valid());
		return scales[id::index(_id)];
	}
}