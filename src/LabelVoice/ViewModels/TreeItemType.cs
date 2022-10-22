namespace LabelVoice.ViewModels
{
    /// <summary>
    /// Item 树状图的项目有两种类型，一种是普通的项目，另一种是文件夹
    /// </summary>
    public enum TreeItemType
    {
        /// <summary>
        /// 普通项目，对应工程文件里的 ItemDefinition
        /// </summary>
        Item,
        /// <summary>
        /// 文件夹。空文件夹对应工程文件里的 Placeholder
        /// </summary>
        Folder
    }
}
