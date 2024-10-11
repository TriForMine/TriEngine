#pragma once

#include "CommonHeaders.h"
#include "MathTypes.h"

namespace triengine::math {
	template<typename T> constexpr T clamp(T value, T min, T max) {
		return value < min ? min : value > max ? max : value;
	}

	template<u32 bits>
	[[nodiscard]] constexpr u32 pack_unit_float(f32 f)
	{
		static_assert(bits <= sizeof(u32) * 8);
		assert(f >= 0.f && f <= 1.f);
		constexpr f32 interval{ (f32)(((u32)1 << bits) - 1) };
		return (u32)(f * interval + 0.5f);
	}

	template<u32 bits>
	[[nodiscard]] constexpr f32 unpack_to_unit_float(u32 i)
	{
		static_assert(bits <= sizeof(u32) * 8);
		assert(i <= (u32)((u32)1 << bits));
		constexpr f32 interval{ (f32)(((u32)1 << bits) - 1) };
		return (f32)i / interval;
	}

	template<u32 bits>
	[[nodiscard]] constexpr u32 pack_float(f32 f, f32 min, f32 max)
	{
		assert(min < max);
		assert(f >= min && f <= max);
		const f32 distance{ (f - min) / (max - min) };
		return pack_unit_float<bits>(distance);
	}

	template<u32 bits>
	[[nodiscard]] constexpr f32 unpack_float(u32 i, f32 min, f32 max)
	{
		assert(min < max);
		assert(i <= (u32)((u32)1 << bits));
		const f32 distance{ unpack_to_unit_float<bits>(i) };
		return min + distance * (max - min);
	}

	template<u64 alignment>
	[[nodiscard]] constexpr u64 align_size_up(u64 size)
	{
		static_assert(alignment > 0 && (alignment & (alignment - 1)) == 0);
		return (size + alignment - 1) & ~(alignment - 1);
	}

	template<u64 alignment>
	[[nodiscard]] constexpr u64 align_size_down(u64 size)
	{
		static_assert(alignment > 0 && (alignment & (alignment - 1)) == 0);
		return size & ~(alignment - 1);
	}

	[[nodiscard]] constexpr u64 align_size_up(u64 size, u64 alignment)
	{
		assert(alignment > 0 && (alignment & (alignment - 1)) == 0);
		return (size + alignment - 1) & ~(alignment - 1);
	}

	[[nodiscard]] constexpr u64 align_size_down(u64 size, u64 alignment)
	{
		assert(alignment > 0 && (alignment & (alignment - 1)) == 0);
		return size & ~(alignment - 1);
	}

	[[nodiscard]] constexpr u64 calc_crc32_u64(const u8* const data, u64 size)
	{
		assert(size >= sizeof(u64));
		u64 crc{ 0 };
		const u8* at{ data };
		const u8* end{ data + align_size_down<sizeof(u64)>(size) };
		while (at < end)
		{
			crc = _mm_crc32_u64(crc, *(const u64*)at);
			at += sizeof(u64);
		}

		return crc;
	}
}