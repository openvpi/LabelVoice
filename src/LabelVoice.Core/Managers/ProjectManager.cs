using LabelVoice.Models;
using LabelVoice.Core.Utils;

namespace LabelVoice.Core.Managers
{
    public class ProjectManager : SingletonBase<ProjectManager>
    {

        #region Properties

        public ProjectModel Project { get; private set; } = new ();

        public HexCodeGenerator HexCodeGenerator { get; } = new();

        public string? ProjectFilePath { get; set; }

        #endregion Properties

        #region Methods

        public void NewProject()
        {
            Project = new();
            ProjectFilePath = null;
        }

        public void NewProject(bool initWithData)
        {
            NewProject();
            if (initWithData)
                Project = new ProjectModel
                {
                    Languages = new()
                    { 
                        new()
                        {
                            Id = HexCodeGenerator.Generate(4),
                            Name = "Lang 1"
                        }
                    },
                    Speakers = new()
                    {
                        new()
                        {
                            Id = HexCodeGenerator.Generate(4),
                            Name = "Speaker 1"
                        }
                    }
                };
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
            ProjectFilePath = filePath;
        }

        #endregion Methods
    }
}