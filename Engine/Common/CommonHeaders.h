#pragma once

#ifdef _WIN64
#pragma warning(disable: 4530) // disable warnings about exceptions in C++/CLI
#endif

// C/C++
#include <cstdint>
#include <assert.h>
#include <typeinfo>
#include <memory>
#include <unordered_map>
#include <string>
#include <mutex>
#include <cstring>

#if defined(_WIN64)
#include <DirectXMath.h>
#endif

#ifndef DISABLE_COPY
#define DISABLE_COPY(T) \
	T(const T&) = delete; \
	T& operator=(const T&) = delete;
#endif

#ifndef DISABLE_MOVE
#define DISABLE_MOVE(T) \
	T(T&&) = delete; \
	T& operator=(T&&) = delete;
#endif

#ifndef DISABLE_COPY_AND_MOVE
#define DISABLE_COPY_AND_MOVE(T) \
	DISABLE_COPY(T) \
	DISABLE_MOVE(T)
#endif

#if _DEBUG
#define DEBUG_OP(x) x
#else
#define DEBUG_OP(x)
#endif

// common headers
#include "PrimitiveTypes.h"
#include "..\Utilities\Math.h"
#include "..\Utilities\Utilities.h"
#include "..\Utilities\MathTypes.h"
#include "PrimitiveTypes.h"
#include "Id.h"