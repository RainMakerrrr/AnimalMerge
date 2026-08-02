#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using Code.Animals.Merge;
using Code.Animals.Merge.MergeAttributes;
using Code.Animals.UI;
using Code.Animals.Upgrade;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class AllyPrefabAssembler
    {
        public static void Assemble(
            GameObject root,
            AllyAnimalRecipe recipe,
            AnimalFacade facade,
            Transform mergeChild,
            List<ValidationFinding> findings)
        {
            var upgrade = EnsureComponent<AnimalUpgrade>(root);
            var mergeTarget = EnsureComponent<MergeTarget>(mergeChild.gameObject);
            var mergeView = EnsureComponent<MergeView>(mergeChild.gameObject);
            var mergePopup = EnsureComponent<MergePopupController>(root);

            ApplyMergeTriggerOverride(recipe, mergeChild);

            SerializedFieldWriter.SetObject(upgrade, "_health", root.GetComponent<Code.Animals.Health.AnimalHealth>());
            SerializedFieldWriter.SetObject(upgrade, "_attack", root.GetComponent<Code.Animals.AnimalAttack>());
            SerializedFieldWriter.SetObject(upgrade, "_movement",
                root.GetComponent<Code.Animals.Movement.AnimalMovement>());

            SerializedFieldWriter.SetObject(mergeTarget, "_facade", facade);
            SerializedFieldWriter.SetObject(mergeView, "_target", mergeTarget);

            SerializedFieldWriter.SetObject(facade, "_upgrade", upgrade);
            SerializedFieldWriter.SetObject(facade, "_mergeView", mergeView);

            if (recipe.OverrideMergeSkillMultiplier)
                WriteMergeSkillMultiplier(facade, recipe, findings);

            SerializedFieldWriter.SetObject(mergePopup, "_target", mergeTarget);
            SerializedFieldWriter.SetFloat(mergePopup, "_spawnOffsetY", recipe.MergePopupOffsetY);
            SerializedFieldWriter.SetFloat(mergePopup, "_randomXRange", AnimalPrefabConstants.RandomXRange);

            var popupAsset = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPrefabPaths.MergePopupPrefab);
            if (popupAsset != null)
            {
                var view = popupAsset.GetComponent<MergePopupView>();
                if (view != null) SerializedFieldWriter.SetObject(mergePopup, "_popupPrefab", view);
            }

            EnsureMergeAttribute(root, recipe, findings);
            EnsureVisualSlots(root, recipe, findings);
            VerifyMergeReachable(root, recipe, mergeChild, findings);
        }

        private static void ApplyMergeTriggerOverride(AllyAnimalRecipe recipe, Transform mergeChild)
        {
            if (!recipe.OverrideMergeTriggerShape) return;

            var sphere = mergeChild.GetComponent<SphereCollider>();
            if (sphere == null) return;

            sphere.radius = recipe.MergeTriggerRadius;
            sphere.center = recipe.MergeTriggerCenter;
        }

        private static void WriteMergeSkillMultiplier(
            AnimalFacade facade,
            AllyAnimalRecipe recipe,
            List<ValidationFinding> findings)
        {
            var serialized = new SerializedObject(facade);
            var property = serialized.FindProperty("_mergeSkillMultiplier");

            if (property == null)
            {
                findings.Add(ValidationFinding.Warning(
                    "BUILD",
                    $"{recipe.name}: {facade.GetType().Name} has no _mergeSkillMultiplier field, " +
                    "the override was ignored",
                    recipe));
                return;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = Mathf.RoundToInt(recipe.MergeSkillMultiplier);
            else
                property.floatValue = recipe.MergeSkillMultiplier;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureMergeAttribute(
            GameObject root,
            AllyAnimalRecipe recipe,
            List<ValidationFinding> findings)
        {
            if (recipe.MergeAttributeScript == null) return;

            var type = recipe.MergeAttributeScript.GetClass();
            if (type == null) return;

            var childName = $"{recipe.Type}MergeAttribute";
            var existing = root.transform.Find(childName);

            if (existing != null && existing.GetComponent(type) == null)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }

            if (existing == null)
            {
                var created = new GameObject(childName);
                created.transform.SetParent(root.transform, false);
                created.AddComponent(type);
                existing = created.transform;
            }

            var attribute = existing.GetComponent(type) as VisualMergeAttribute;
            if (attribute == null)
            {
                findings.Add(ValidationFinding.Warning(
                    "BUILD",
                    $"{recipe.name}: {type.Name} is not a VisualMergeAttribute, _type was not written",
                    recipe));
                return;
            }

            SerializedFieldWriter.SetEnum(attribute, "_type", (int)recipe.Type);
        }

        private static void EnsureVisualSlots(
            GameObject root,
            AllyAnimalRecipe recipe,
            List<ValidationFinding> findings)
        {
            foreach (var slot in recipe.VisualSlots)
            {
                if (slot == null || slot.VisualPrefab == null) continue;

                var alreadyPresent = root.GetComponentsInChildren<MergeVisualSlot>(true)
                    .Any(existing => existing.SourceType == slot.SourceType);

                if (alreadyPresent) continue;

                var parent = ResolveBone(root, slot, findings, recipe);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(slot.VisualPrefab, parent);
                instance.transform.localPosition = slot.LocalPosition;
                instance.transform.localEulerAngles = slot.LocalEulerAngles;
                instance.transform.localScale = slot.LocalScale;

                var component = instance.GetComponent<MergeVisualSlot>();
                if (component == null) component = instance.AddComponent<MergeVisualSlot>();

                SerializedFieldWriter.SetEnum(component, "_sourceType", (int)slot.SourceType);
                instance.SetActive(false);
            }
        }

        private static Transform ResolveBone(
            GameObject root,
            MergeVisualSlotRecipe slot,
            List<ValidationFinding> findings,
            AllyAnimalRecipe recipe)
        {
            if (string.IsNullOrEmpty(slot.BoneName)) return root.transform;

            if (slot.BoneName.Contains("/"))
            {
                var byPath = root.transform.Find(slot.BoneName);
                if (byPath != null) return byPath;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            var match = transforms.FirstOrDefault(t => t.name == slot.BoneName);
            if (match != null) return match;

            var closest = transforms
                .Select(t => t.name)
                .Distinct()
                .OrderBy(name => LevenshteinDistance(name, slot.BoneName))
                .Take(10);

            findings.Add(ValidationFinding.Warning(
                "BUILD",
                $"{recipe.name}: bone '{slot.BoneName}' not found for the {slot.SourceType} visual slot. " +
                $"The slot was parented to the prefab root instead. Closest names: {string.Join(", ", closest)}",
                recipe));

            return root.transform;
        }

        private static void VerifyMergeReachable(
            GameObject root,
            AllyAnimalRecipe recipe,
            Transform mergeChild,
            List<ValidationFinding> findings)
        {
            var sphere = mergeChild.GetComponent<SphereCollider>();
            if (sphere == null) return;

            var scale = root.transform.localScale;
            var planarScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var worldRadius = sphere.radius * planarScale;
            var centerX = sphere.center.x * scale.x;
            var centerZ = sphere.center.z * scale.z;

            var probeZ = AnimalPrefabConstants.LargestRaycastOffsetInProject;
            var dz = probeZ - centerZ;

            if (centerX * centerX + dz * dz <= worldRadius * worldRadius) return;

            findings.Add(ValidationFinding.Warning(
                "R22",
                $"{recipe.name}: the merge sphere does not cover the placement probe at world XZ (0, {probeZ}). " +
                "AnimalMovement.TryPlace raycasts down from the dragged animal position plus its " +
                "_raycastOffset, so nothing will be droppable onto this animal. Enable " +
                "OverrideMergeTriggerShape and widen the sphere",
                recipe));
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            var existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];

            for (var j = 0; j <= b.Length; j++) previous[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Mathf.Min(Mathf.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                var swap = previous;
                previous = current;
                current = swap;
            }

            return previous[b.Length];
        }
    }
}
#endif
