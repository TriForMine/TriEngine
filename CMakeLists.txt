cmake_minimum_required(VERSION 3.29)
project(TriEngine)

set(CMAKE_CXX_STANDARD 20)

include_directories(ContentTools)
include_directories(Engine/Common)
include_directories(Engine/Components)
include_directories(Engine/Content)
include_directories(Engine/EngineAPI)
include_directories(Engine/Graphics)
include_directories(Engine/Platform)
include_directories(Engine/Utilities)
include_directories(EngineDLL)
include_directories(EngineTest)

add_executable(TriEngine
        ContentTools/Geometry.cpp
        ContentTools/Geometry.h
        ContentTools/Main.cpp
        ContentTools/PrimitiveMesh.cpp
        ContentTools/PrimitiveMesh.h
        ContentTools/ToolsCommon.h
        Engine/Common/CommonHeaders.h
        Engine/Common/Id.h
        Engine/Common/PrimitiveTypes.h
        Engine/Components/ComponentsCommon.h
        Engine/Components/Entity.cpp
        Engine/Components/Entity.h
        Engine/Components/Script.cpp
        Engine/Components/Script.h
        Engine/Components/Transform.cpp
        Engine/Components/Transform.h
        Engine/Content/ContentLoader.cpp
        Engine/Content/ContentLoader.h
        Engine/Core/Engine.cpp
        Engine/Core/Main.cpp
        Engine/EngineAPI/GameEntity.h
        Engine/EngineAPI/ScriptComponent.h
        Engine/EngineAPI/TransformComponent.h
        Engine/Graphics/Renderer.h
        Engine/Platform/Platform.cpp
        Engine/Platform/Platform.h
        Engine/Platform/PlatformTypes.h
        Engine/Platform/Window.h
        Engine/Utilities/Math.h
        Engine/Utilities/MathTypes.h
        Engine/Utilities/Utilities.h
        EngineDLL/Common.h
        EngineDLL/DllMain.cpp
        EngineDLL/EngineAPI.cpp
        EngineDLL/EntityAPI.cpp
        EngineTest/Main.cpp
        EngineTest/Test.h
        EngineTest/TestEntityComponents.h
        EngineTest/TestWindow.h)
