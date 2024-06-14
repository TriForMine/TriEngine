#pragma once

#include "CommonHeaders.h"
#include "MathTypes.h"

namespace triengine::math {
    template<typename T> constexpr T clamp(T value, T min, T max) {
        return value < min ? min : value > max ? max : value;
    }

    template<u32 bits>
    constexpr u32 pack_unit_float(f32 f)
    {
        static_assert(bits <= sizeof(u32) * 8);
        assert(f >= 0.f && f <= 1.f);
        constexpr f32 interval{ (f32)((1ui32 << bits) - 1) };
        return (u32)(f * interval + 0.5f);
    }

    template<u32 bits>
    constexpr f32 unpack_to_unit_float(u32 i)
    {
        static_assert(bits <= sizeof(u32) * 8);
        assert(i <= (u32)(1ui32 << bits));
        constexpr f32 interval{ (f32)((1ui32 << bits) - 1) };
        return (f32)i / interval;
    }

    template<u32 bits>
    constexpr u32 pack_float(f32 f, f32 min, f32 max)
    {
        assert(min < max);
        assert(f >= min && f <= max);
        const f32 distance{ (f - min) / (max - min) };
        return pack_unit_float<bits>(distance);
    }

    template<u32 bits>
    constexpr f32 unpack_float(u32 i, f32 min, f32 max)
    {
        assert(min < max);
        assert(i <= (u32)(1ui32 << bits));
        const f32 distance{ unpack_to_unit_float<bits>(i) };
        return min + distance * (max - min);
    }
}