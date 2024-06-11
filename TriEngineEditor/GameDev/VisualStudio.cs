using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriEngineEditor.Utilities;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.IO;
using TriEngineEditor.GameProject;

namespace TriEngineEditor.GameDev
{
    static class VisualStudio
    {
        public static bool BuildSucceded { get; private set; } = true;
        public static bool BuildDone { get; private set; } = true;

        private static EnvDTE80.DTE2 _vsInstance = null;
        private static readonly string _progID = "VisualStudio.DTE.17.0";

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable pprot);

        public static void OpenVisualStudio(string solutionPath)
        {
            IRunningObjectTable rot = null;
            IEnumMoniker monikerTable = null;
            IBindCtx bindCtx = null;

            try {
                if (_vsInstance == null)
                {
                    // Find and open 
                    var hResult = GetRunningObjectTable(0, out rot);
                    if (hResult < 0 || rot == null) throw new COMException($"GetRunningObjectTable failed with hResult: {hResult:X8}");

                    rot.EnumRunning(out monikerTable);
                    monikerTable.Reset();

                    hResult = CreateBindCtx(0, out bindCtx);
                    if (hResult < 0 || bindCtx == null) throw new COMException($"CreateBindCtx failed with hResult: {hResult:X8}");

                    IMoniker[] currentMoniker = new IMoniker[1];

                    while (monikerTable.Next(1, currentMoniker, IntPtr.Zero) == 0)
                    {
                        currentMoniker[0].GetDisplayName(bindCtx, null, out string name);
                        if (name.Contains(_progID))
                        {
                            hResult = rot.GetObject(currentMoniker[0], out object obj);
                            if (hResult < 0) throw new COMException($"GetObject failed with hResult: {hResult:X8}");

                            EnvDTE80.DTE2 dte = obj as EnvDTE80.DTE2;
                            var solutionName = dte.Solution.FullName;
                            if (solutionName == solutionPath)
                            {
                                _vsInstance = dte;
                                break;
                            }
                        }
                    }

                    if (_vsInstance == null)
                    {
                        Type visualStudioType = Type.GetTypeFromProgID(_progID, true);
                        _vsInstance = Activator.CreateInstance(visualStudioType) as EnvDTE80.DTE2;
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Logger.Log(MessageType.Error, "Failed to open Visual Studio. Make sure you have Visual Studio installed and the version is compatible with the editor.");
            }
            finally
            {
                if (monikerTable != null) Marshal.ReleaseComObject(monikerTable);
                if (rot != null) Marshal.ReleaseComObject(rot);
                if (bindCtx != null) Marshal.ReleaseComObject(bindCtx);
            }
        }

        public static void CloseVisualStudio()
        {
            if (_vsInstance?.Solution.IsOpen == true)
            {
                _vsInstance.ExecuteCommand("File.SaveAll");
                _vsInstance.Solution.Close(true);
            }
            _vsInstance?.Quit();
        }

        internal static bool AddFilesToSolution(string solution, string projectName, string[] files)
        {
            Debug.Assert(files?.Length > 0, "No files to add to solution.");
            OpenVisualStudio(solution);
            try
            {
                if (_vsInstance != null)
                {
                    if (!_vsInstance.Solution.IsOpen) _vsInstance.Solution.Open(solution);
                    else _vsInstance.ExecuteCommand("File.SaveAll");

                    foreach (EnvDTE.Project project in _vsInstance.Solution.Projects)
                    {
                        if (project.UniqueName.Contains(projectName))
                        {
                            foreach (string file in files)
                            {
                                if (!System.IO.File.Exists(file)) continue;
                                project.ProjectItems.AddFromFile(file);
                            }
                        }
                    }

                    var cpp = files.FirstOrDefault(f => Path.GetExtension(f) == ".cpp");
                    if (!string.IsNullOrEmpty(cpp))
                    {
                        _vsInstance.ItemOperations.OpenFile(cpp, "{7651A703-06E5-11D1-8EBD-00A0C90F26EA}").Visible = true;
                    }
                    _vsInstance.MainWindow.Activate();
                    _vsInstance.MainWindow.Visible = true;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Logger.Log(MessageType.Error, "Failed to add files to solution.");
                return false;
            }

            return true;
        }
        private static void OnBuildSolutionBegin(string project, string projectConfig, string platform, string solutionConfig)
        {
            if (BuildDone) return;
            Logger.Log(MessageType.Info, $"Building solution: {project}, {projectConfig}, {platform}, {solutionConfig}");
        }

        private static void OnBuildSolutionDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (BuildDone) return;

            if (success) Logger.Log(MessageType.Info, $"Building {projectConfig} configuration succeeded.");
            else Logger.Log(MessageType.Error, $"Building {projectConfig} configuration failed.");

            BuildDone = true;
            BuildSucceded = success;
        }

        public static bool IsDebugging()
        {
            bool result = false;
            bool tryAgain = true;

            for (int i = 0; i < 3 && tryAgain; i++)
            {
                try
                {
                    result = _vsInstance != null && (_vsInstance.Debugger?.CurrentProgram != null || _vsInstance.Debugger.CurrentMode == EnvDTE.dbgDebugMode.dbgRunMode);
                    tryAgain = false;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Thread.Sleep(1000);
                }
            }

            return result;
        }

        internal static void BuildSolution(Project project, string configName, bool showWindow = true)
        {
            if (IsDebugging())
            {
                Logger.Log(MessageType.Error, "Visual Studio is currently debugging. Please stop debugging before building the solution.");
            }

            OpenVisualStudio(project.Solution);
            BuildDone = BuildSucceded = false;

            for (int i = 0; i < 3 && !BuildDone; i++)
            {
                try
                {
                    if (!_vsInstance.Solution.IsOpen) _vsInstance.Solution.Open(project.Solution);
                    _vsInstance.MainWindow.Visible = showWindow;

                    _vsInstance.Events.BuildEvents.OnBuildProjConfigBegin += OnBuildSolutionBegin;
                    _vsInstance.Events.BuildEvents.OnBuildProjConfigDone += OnBuildSolutionDone;

                    try {
                        foreach (var pdbFile in Directory.GetFiles(Path.Combine($"{project.Path}", $@"x64\{configName}"), "*.pdb"))
                        {
                            File.Delete(pdbFile);
                        }
                    } catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }

                    _vsInstance.Solution.SolutionBuild.SolutionConfigurations.Item(configName).Activate();
                    _vsInstance.Solution.SolutionBuild.Build(true);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine($"Attempt {i} failed to build solution.");
                    Logger.Log(MessageType.Error, $"Failed to build solution: {e.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        public static void Run(Project project, string configName, bool debug)
        {
            if (_vsInstance != null && !IsDebugging() && BuildDone && BuildSucceded)
            {
                _vsInstance.ExecuteCommand(debug ? "Debug.Start" : "Debug.StartWithoutDebugging");
            }
        }

        public static void Stop()
        {
            if (_vsInstance != null && IsDebugging())
            {
                _vsInstance.Debugger.Stop(true);
            }
        }
    }
}
