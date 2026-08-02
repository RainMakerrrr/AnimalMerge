#if UNITY_EDITOR
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public enum FindingSeverity
    {
        Warning,
        Error
    }

    public readonly struct ValidationFinding
    {
        public ValidationFinding(string ruleId, FindingSeverity severity, string message, Object context)
        {
            RuleId = ruleId;
            Severity = severity;
            Message = message;
            Context = context;
        }

        public string RuleId { get; }
        public FindingSeverity Severity { get; }
        public string Message { get; }
        public Object Context { get; }

        public static ValidationFinding Error(string ruleId, string message, Object context) =>
            new ValidationFinding(ruleId, FindingSeverity.Error, message, context);

        public static ValidationFinding Warning(string ruleId, string message, Object context) =>
            new ValidationFinding(ruleId, FindingSeverity.Warning, message, context);

        public override string ToString() => $"[{RuleId}] {Message}";
    }
}
#endif
