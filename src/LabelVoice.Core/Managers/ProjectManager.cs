using LabelVoice.Core.ProjectModel;
using LabelVoice.Core.Utils;

namespace LabelVoice.Core.Managers
{
    public class ProjectManager : SingletonBase<ProjectManager>
    {
        #region Constructor

        private ProjectManager()
        {
            Project = new LvProject();
        }

        #endregion Constructor

        #region Properties

        public LvProject Project { get; private set; }

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