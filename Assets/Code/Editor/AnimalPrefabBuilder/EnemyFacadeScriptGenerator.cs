#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class EnemyFacadeScriptGenerator
    {
        public static bool TryGenerate(AnimalRecipe recipe, out string createdPath)
        {
            createdPath = null;
            if (recipe.Side != AnimalSideProfile.Enemy) return false;

            var className = $"{recipe.Type}Facade";
            var path = $"{AnimalPrefabPaths.FacadesFolder}/{className}.cs";

            if (File.Exists(path)) return false;

            var source =
                "namespace Code.Animals.Facades\n" +
                "{\n" +
                $"    public class {className} : EnemyAnimalFacade\n" +
                "    {\n" +
                "        public override void InitBehaviours()\n" +
                "        {\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path);
            createdPath = path;

            Debug.Log(
                $"[AnimalPrefabTool] Created {path}. Wait for Unity to finish compiling, assign it to the " +
                "recipe FacadeScript field, then run the build again.");

            return true;
        }
    }
}
#endif
