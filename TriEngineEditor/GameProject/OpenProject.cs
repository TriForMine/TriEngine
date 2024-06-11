using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.GameProject
{
    [DataContract]
    public class ProjectData
    {
        [DataMember]
        public string ProjectName { get; set; }
        [DataMember]
        public string ProjectPath { get; set; }
        [DataMember]
        public DateTime Date { get; set; }

        public string FullPath => $@"{ProjectPath}{ProjectName}{Project.Extension}";
        public byte[] Icon { get; set; }
        public byte[] Screenshot { get; set; }
    }

    [DataContract]
    public class ProjectDataList
    {
        [DataMember]
        public List<ProjectData> Projects { get; set; }
    }


    class OpenProject
    {
        private static readonly string _applicationDataPath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\TriEngineEditor\";
        private static readonly string _projectDataPath;
        private static readonly ObservableCollection<ProjectData> _projects = new ObservableCollection<ProjectData>();
        public static ReadOnlyObservableCollection<ProjectData> Projects { get; }

        private static void ReadProjectData()
        {
            if (File.Exists(_projectDataPath))
            {
                var projectDataList = Serializer.FromFile<ProjectDataList>(_projectDataPath);
                _projects.Clear();
                foreach (var project in projectDataList.Projects)
                {
                    if (File.Exists(project.FullPath))
                    {
                        project.Icon = File.ReadAllBytes($@"{project.ProjectPath}\.TriEngine\Icon.png");
                        project.Screenshot = File.ReadAllBytes($@"{project.ProjectPath}\.TriEngine\Screenshot.png");
                        _projects.Add(project);
                    }
                }
            }
        }

        private static void WriteProjectData()
        {
            var projects = _projects.OrderByDescending(p => p.Date).ToList();
            Serializer.ToFile(new ProjectDataList { Projects = projects }, _projectDataPath);
        }

        public static Project Open(ProjectData data)
        {
            ReadProjectData();
            var project = _projects.FirstOrDefault(p => p.FullPath == data.FullPath);

            if (project != null)
            {
                project.Date = DateTime.Now;
            }
            else
            {
                project = data;
                project.Date = DateTime.Now;
                _projects.Add(project);
            }
            WriteProjectData();

            return Project.Load(project.FullPath);
        }

        static OpenProject()
        {
            try
            {
                if (!Directory.Exists(_applicationDataPath))
                {
                    Directory.CreateDirectory(_applicationDataPath);
                }

                _projectDataPath = $@"{_applicationDataPath}ProjectData.xml";
                Projects = new ReadOnlyObservableCollection<ProjectData>(_projects);
                ReadProjectData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Logger.Log(MessageType.Error, $"Failed to initialize project data: {ex.Message}");
                throw;
            }
        }
    }
}
