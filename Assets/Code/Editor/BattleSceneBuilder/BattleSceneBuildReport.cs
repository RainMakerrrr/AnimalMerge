#if UNITY_EDITOR
using System.Collections.Generic;

namespace Code.Editor.BattleSceneBuilder
{
    internal class BattleSceneBuildReport
    {
        private readonly List<string> _messages = new List<string>();

        public int GridCells { get; set; }
        public int Allies { get; set; }
        public int Enemies { get; set; }

        public IReadOnlyList<string> Messages => _messages;

        public void AddInfo(string message)
        {
            _messages.Add($"[Info] {message}");
        }

        public void AddWarning(string message)
        {
            _messages.Add($"[Warning] {message}");
        }

        public void AddError(string message)
        {
            _messages.Add($"[Error] {message}");
        }
    }
}
#endif
