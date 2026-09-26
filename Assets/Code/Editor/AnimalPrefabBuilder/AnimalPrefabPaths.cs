#if UNITY_EDITOR
namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AnimalPrefabPaths
    {
        public const string AnimalsFolder = "Assets/Resources/Prefabs/Animals";
        public const string EnemiesFolder = "Assets/Resources/Prefabs/Enemies";
        public const string StatsFolder = "Assets/Settings/Animals/Stats";
        public const string RecipesFolder = "Assets/Settings/Animals/Recipes";
        public const string FacadesFolder = "Assets/Code/Animals/Facades";

        public const string AllyHealthBarPrefab = "Assets/Prefabs/AllyHealthBar.prefab";
        public const string EnemyHealthBarPrefab = "Assets/Prefabs/EnemyHealthBar.prefab";
        public const string DamagePopupPrefab = "Assets/Prefabs/DamagePopupView.prefab";
        public const string MergePopupPrefab = "Assets/Prefabs/MergePopupView.prefab";
        public const string AnimalDatabaseAsset = "Assets/Resources/AnimalDatabase.asset";
    }
}
#endif
