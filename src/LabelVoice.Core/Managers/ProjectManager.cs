using LabelVoice.Models;
using LabelVoice.Core.Utils;

namespace LabelVoice.Core.Managers
{
    public class ProjectManager : SingletonBase<ProjectManager>
    {

        #region Properties

        public ProjectModel Project { get; private set; } = new ProjectModel();

        public string? ProjectFilePath { get; set; }

        #endregion Properties

        #region Methods

        public void NewProject()
        {
            Project = new ProjectModel();
        }

        public void LoadProject(string filePath)
        {
            HexCodeGenerator.GlobalRegistry.Reset();
            using TextReader reader = File.OpenText(filePath);
            var project = ProjectModel.LoadFrom(reader);
            Project = project;
            ProjectFilePath = filePath;
        }

        public void SaveProject(string filePath)
        {
            using TextWriter writer = File.CreateText(filePath);
            Project.SaveTo(writer);
        }

        #endregion Methods
    }
}