using LabelVoice.Models;
using LabelVoice.Core.Utils;

namespace LabelVoice.Core.Managers
{
    public class ProjectManager : SingletonBase<ProjectManager>
    {
        #region Constructor

        private ProjectManager()
        {
            Project = new ProjectModel();
        }

        #endregion Constructor

        #region Properties

        public ProjectModel Project { get; private set; }

        #endregion Properties

        #region Methods

        public void Load(string filePath)
        {
            // Deserialize the XML document.
        }

        public void Save(string filePath)
        {
            // Serialize this project and save it to filePath.
        }

        #endregion Methods
    }
}