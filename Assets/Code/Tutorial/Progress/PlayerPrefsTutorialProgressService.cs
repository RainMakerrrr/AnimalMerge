using UnityEngine;

namespace Code.Tutorial.Progress
{
    public class PlayerPrefsTutorialProgressService : ITutorialProgressService
    {
        private const string KeyPrefix = "Tutorial.";
        private const int CompletedValue = 1;

        public bool IsCompleted(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return false;

            return PlayerPrefs.GetInt(BuildKey(stepId), 0) == CompletedValue;
        }

        public void MarkCompleted(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return;

            PlayerPrefs.SetInt(BuildKey(stepId), CompletedValue);
            PlayerPrefs.Save();
        }

        public void Reset(string stepId)
        {
            if (string.IsNullOrEmpty(stepId))
                return;

            PlayerPrefs.DeleteKey(BuildKey(stepId));
            PlayerPrefs.Save();
        }

        private static string BuildKey(string stepId) => KeyPrefix + stepId;
    }
}
