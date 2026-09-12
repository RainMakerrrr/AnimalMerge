#if UNITY_EDITOR
namespace Code.Editor.LevelSets
{
    public static class LevelSetPaths
    {
        public const string LibraryAsset = "Assets/Resources/LevelSetLibrary.asset";
        public const string SetsRoot = "Assets/Settings/Levels";
        public const string LevelsSubfolder = "Levels";
        public const string StagesSubfolder = "Stages";
        public const string ResourcesFolder = "Assets/Resources/";
        public const string StandardSetName = "Standard";

        public static string SetFolder(string setName) => $"{SetsRoot}/{setName}";

        public static string SetAsset(string setName) => $"{SetsRoot}/{setName}/{setName}LevelSet.asset";

        public static string LevelsFolder(string setName) => $"{SetsRoot}/{setName}/{LevelsSubfolder}";

        public static string StagesFolder(string setName) => $"{SetsRoot}/{setName}/{StagesSubfolder}";
    }
}
#endif
