using System.Numerics;
using System.Runtime.InteropServices;
using TriEngineEditor.Components;
using TriEngineEditor.EngineAPIStructs;
using TriEngineEditor.GameProject;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.EngineAPIStructs
{
    [StructLayout(LayoutKind.Sequential)]
    class TransformComponent
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;
    }

    [StructLayout(LayoutKind.Sequential)]
    class ScriptComponent
    {
        public IntPtr ScriptCreator;
    }

    [StructLayout(LayoutKind.Sequential)]
    class GameEntityDescriptor
    {
        public TransformComponent Transform = new TransformComponent();
        public ScriptComponent Script = new ScriptComponent();
    }
}

namespace TriEngineEditor.DllWrappers
{
    static class EngineAPI
    {
        private const string _engineDll = "EngineDll.dll";

        [DllImport(_engineDll, CharSet = CharSet.Ansi)]
        public static extern int LoadGameCodeDll(string dllPath);

        [DllImport(_engineDll)]
        public static extern int UnloadGameCodeDll();
        [DllImport(_engineDll)]
        public static extern IntPtr GetScriptCreator(string name);
        [DllImport(_engineDll)]
        [return: MarshalAs(UnmanagedType.SafeArray)]
        public static extern string[] GetScriptNames();
        [DllImport(_engineDll)]
        public static extern int CreateRenderSurface(IntPtr host, int width, int height);
        [DllImport(_engineDll)]
        public static extern int RemoveRenderSurface(int surfaceId);
        [DllImport(_engineDll)]
        public static extern IntPtr GetWindowHandle(int surfaceId);
        [DllImport(_engineDll)]
        public static extern void ResizeRenderSurface(int surfaceId);

        internal static class EntityAPI
        {
            [DllImport(_engineDll)]
            private static extern int CreateGameEntity(GameEntityDescriptor desc);

            public static int CreateGameEntity(GameEntity entity)
            {
                GameEntityDescriptor desc = new GameEntityDescriptor();

                // transform
                {
                    var c = entity.GetComponent<Transform>();
                    desc.Transform.Position = c.Position;
                    desc.Transform.Rotation = c.Rotation;
                    desc.Transform.Scale = c.Scale;
                }

                // script
                {
                    var c = entity.GetComponent<Script>();
                    if (c != null && Project.Current != null)
                    {
                        if (Project.Current.AvailableScripts.Contains(c.Name)){
                            desc.Script.ScriptCreator = EngineAPI.GetScriptCreator(c.Name);
                        }
                        else
                        {
                            Logger.Log(MessageType.Error, $"Unable to find script {c.Name}. Game entity {entity.Name} will not have a script component.");
                        }
                    }
                   
                }

                return CreateGameEntity(desc);
            }

            [DllImport(_engineDll)]
            private static extern void RemoveGameEntity(int id);

            public static void RemoveGameEntity(GameEntity entity)
            {
                RemoveGameEntity(entity.EntityId);
            }
        }
    }
}
