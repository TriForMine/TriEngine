#pragma once
#include "CommonHeaders.h"

namespace triengine::utl {

	// NOTE: (Important) This utility class is intended for local use only (i.e. within one function).
	// Do not keep instances around as member variables.
	class blob_stream_reader {
	public:
		DISABLE_COPY_AND_MOVE(blob_stream_reader);
		explicit blob_stream_reader(const u8* buffer)
			: _buffer(buffer)
			, _position(buffer)
		{
			assert(buffer);
		}

		template<typename T>
		[[nodiscard]] T read()
		{
			static_assert(std::is_arithmetic_v<T> || std::is_enum_v<T>, "T must be an arithmetic type or an enum.");
			T value{ *reinterpret_cast<const T*>(_position) };
			_position += sizeof(T);
			return value;
		}

		// reads 'length' bytes into 'buffer'. The caller is responsible for ensuring that 'buffer' is large enough.
		void read(u8* buffer, size_t length)
		{
			memcpy(buffer, _position, length);
			_position += length;
		}

		void skip(size_t offset)
		{
			_position += offset;
		}

		[[nodiscard]] constexpr const u8* const buffer_start() const { return _buffer;  }
		[[nodiscard]] constexpr const u8* const position() const { return _position;  }
		[[nodiscard]] constexpr size_t const offset() const { return _position - _buffer;  }
	private:
		const u8* _buffer;
		const u8* _position;
	};

	// NOTE: (Important) This utility class is intended for local use only (i.e. within one function).
	// Do not keep instances around as member variables.
	class blob_stream_writer
	{
	public:
		DISABLE_COPY_AND_MOVE(blob_stream_writer);
		explicit blob_stream_writer(u8* buffer, size_t buffer_size)
			: _buffer(buffer)
			, _position(buffer)
			, _buffer_size(buffer_size)
		{
			assert(buffer && buffer_size);
		}

		template<typename T>
		void write(T value)
		{
			static_assert(std::is_arithmetic_v<T> || std::is_enum_v<T>, "T must be an arithmetic type or an enum.");
			assert(&_position[sizeof(T)] <= &_buffer[_buffer_size]);
			*reinterpret_cast<T*>(_position) = value;
			_position += sizeof(T);
		}

		// writes 'length' chars into 'buffer'.
		void write(const char* buffer, size_t length)
		{
			assert(&_position[length] <= &_buffer[_buffer_size]);
			memcpy(_position, buffer, length);
			_position += length;
		}

		// writes 'length' chars into 'buffer'.
		void write(const u8* buffer, size_t length)
		{
			assert(&_position[length] <= &_buffer[_buffer_size]);
			memcpy(_position, buffer, length);
			_position += length;
		}

		void skip(size_t offset)
		{
			assert(&_position[offset] <= &_buffer[_buffer_size]);
			_position += offset;
		}

		[[nodiscard]] constexpr const u8* const buffer_start() const { return _buffer; }
		[[nodiscard]] constexpr const u8* const buffer_end() const { return &_buffer[_buffer_size]; }
		[[nodiscard]] constexpr const u8* const position() const { return _position; }
		[[nodiscard]] constexpr size_t const offset() const { return _position - _buffer; }
	private:
		u8* const _buffer;
		u8* _position;
		size_t _buffer_size;
	};
}