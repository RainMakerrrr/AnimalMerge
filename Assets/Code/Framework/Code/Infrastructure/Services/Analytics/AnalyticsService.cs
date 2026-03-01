using System.Collections.Generic;
using UnityEngine;

namespace Framework.Code.Infrastructure.Services.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        const string LEVEL_LOAD = "level_load";
        const string LEVEL_START = "level_start";
        const string LEVEL_COMPLETE = "level_complete";
        const string LEVEL_RESTART = "level_restart";
        const string LEVEL_FAIL = "level_fail";
        const string TIME_SPENT = "time_spent";

        public void LevelLoaded(string levelId, int userLevel)
        {
            string stringUserLevel = LevelToString(userLevel);

            TinySauce.OnGameStarted(stringUserLevel);
            TinySauce.TrackCustomEvent($"{LEVEL_LOAD}:{stringUserLevel}");
            Debug.Log($"[Analytics] LevelLoaded: levelId={levelId}, userLevel={stringUserLevel}");
        }

        public void LevelStarted(string levelId, int userLevel)
        {
            string stringUserLevel = LevelToString(userLevel);
            TinySauce.TrackCustomEvent($"{LEVEL_START}:{stringUserLevel}");
            Debug.Log($"[Analytics] LevelStarted: levelId={levelId}, userLevel={stringUserLevel}");
        }

        public void LevelRestarted(string levelId, float timeSpent)
        {
            var eventName = $"{LEVEL_RESTART}:{levelId}";
            var eventProperties = new Dictionary<string, object> {{TIME_SPENT, timeSpent}};

            TinySauce.TrackCustomEvent(eventName, eventProperties);
        }

        public void LevelCompleted(string levelId, int userLevel, bool isFinished, int coinsCollected,
            float timeSpent)
        {
            string stringUserLevel = LevelToString(userLevel);
            string condition = isFinished ? LEVEL_COMPLETE : LEVEL_FAIL;

            var eventName = $"{condition}:{stringUserLevel}";
            var eventProperties = new Dictionary<string, object> {{TIME_SPENT, timeSpent}};

            TinySauce.OnGameFinished(isFinished, coinsCollected, stringUserLevel);
            TinySauce.TrackCustomEvent(eventName, eventProperties);

            Debug.Log($"[Analytics] LevelCompleted: levelId={levelId}, userLevel={stringUserLevel}, " +
                      $"timeSpent={timeSpent}, isFinished={isFinished}");
        }

        string LevelToString(int level)
        {
            var zeros = "00000000";
            var levelString = level.ToString();
            zeros = zeros.Remove(0, levelString.Length);
            return zeros + levelString;
        }
    }
}