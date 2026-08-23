using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ActionProgramStepV3
    {
        public ActionProgramStepV3(IEnumerable<string> intentKeys)
        {
            string[] keys = (intentKeys ?? Enumerable.Empty<string>()).ToArray();
            if (keys.Length == 0 || keys.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "A V3 program step must contain non-empty actions.",
                    nameof(intentKeys));
            }
            IntentKeys = Array.AsReadOnly(keys);
        }

        public IReadOnlyList<string> IntentKeys { get; }
        public bool IsSimultaneous => IntentKeys.Count > 1;
    }

    public sealed class ActionProgramV3
    {
        public const int MaximumActionCount = 4;

        public ActionProgramV3(IEnumerable<ActionProgramStepV3> steps)
        {
            ActionProgramStepV3[] materialized =
                (steps ?? Enumerable.Empty<ActionProgramStepV3>()).ToArray();
            if (materialized.Length == 0 || materialized.Any(step => step == null))
            {
                throw new ArgumentException(
                    "A V3 action program must contain non-null steps.",
                    nameof(steps));
            }
            int count = materialized.Sum(step => step.IntentKeys.Count);
            if (count < 1 || count > MaximumActionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps),
                    "A V3 action program must contain between one and four logical actions.");
            }
            foreach (string key in materialized.SelectMany(step => step.IntentKeys))
            {
                if (!SceneActionFrameworkV3.IsLogicalIntent(key))
                {
                    throw new ArgumentException(
                        "Program intent is outside SceneActionFrameworkV3: " + key,
                        nameof(steps));
                }
            }
            Steps = new ReadOnlyCollection<ActionProgramStepV3>(materialized);
            ActionCount = count;
        }

        public IReadOnlyList<ActionProgramStepV3> Steps { get; }
        public int ActionCount { get; }
        public bool IsSingleAction => Steps.Count == 1 && Steps[0].IntentKeys.Count == 1;
        public string SingleIntentKey => IsSingleAction ? Steps[0].IntentKeys[0] : null;
        public string ProtocolExpression => string.Join(
            ">",
            Steps.Select(step => string.Join("+", step.IntentKeys)));

        public static ActionProgramV3 FromSingle(string intentKey)
        {
            return new ActionProgramV3(new[]
            {
                new ActionProgramStepV3(new[] { intentKey })
            });
        }

        public static ActionProgramV3 FromV2(ActionProgramV2 program)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            return new ActionProgramV3(program.Steps.Select(step =>
                new ActionProgramStepV3(step.IntentKeys)));
        }

        public bool TryToV2(out ActionProgramV2 program)
        {
            program = null;
            if (Steps.SelectMany(step => step.IntentKeys)
                .Any(key => !SceneActionFrameworkV2.IsLogicalIntent(key)))
            {
                return false;
            }
            program = new ActionProgramV2(Steps.Select(step =>
                new ActionProgramStepV2(step.IntentKeys)));
            return true;
        }

        public static bool TryParseExpression(
            string expression,
            out ActionProgramV3 program,
            out string error)
        {
            program = null;
            error = null;
            if (string.IsNullOrEmpty(expression) ||
                expression.Any(char.IsWhiteSpace))
            {
                error = "Program expression must be non-empty and contain no whitespace.";
                return false;
            }
            string[] rawSteps = expression.Split('>');
            List<ActionProgramStepV3> steps = new List<ActionProgramStepV3>();
            int actionCount = 0;
            foreach (string rawStep in rawSteps)
            {
                string[] keys = rawStep.Split('+');
                if (keys.Length == 0 || keys.Any(string.IsNullOrEmpty))
                {
                    error = "Program contains an empty action or step.";
                    return false;
                }
                foreach (string key in keys)
                {
                    if (!SceneActionFrameworkV3.IsLogicalIntent(key))
                    {
                        error = "Program selected a non-whitelisted V3 intent: " + key;
                        return false;
                    }
                }
                actionCount += keys.Length;
                if (actionCount > MaximumActionCount)
                {
                    error = "Program exceeds the four-action limit.";
                    return false;
                }
                steps.Add(new ActionProgramStepV3(keys));
            }
            try
            {
                program = new ActionProgramV3(steps);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryNormalizeForExecution(
            out ActionProgramV3 normalized,
            out string error)
        {
            normalized = null;
            error = null;
            List<ActionProgramStepV3> result = new List<ActionProgramStepV3>();
            foreach (ActionProgramStepV3 step in Steps)
            {
                if (!step.IsSimultaneous)
                {
                    result.Add(step);
                    continue;
                }
                bool containsKneel = step.IntentKeys.Contains(
                    SceneActionFrameworkV3.Kneel,
                    StringComparer.Ordinal);
                string[] overlays = step.IntentKeys
                    .Where(key => !string.Equals(
                        key,
                        SceneActionFrameworkV3.Kneel,
                        StringComparison.Ordinal))
                    .ToArray();
                if (containsKneel && overlays.Length > 0 &&
                    overlays.All(SceneActionFrameworkV3.CanOverlayKneel))
                {
                    foreach (string overlay in overlays)
                    {
                        result.Add(new ActionProgramStepV3(new[]
                        {
                            SceneActionFrameworkV3.Kneel,
                            overlay
                        }));
                    }
                    continue;
                }
                foreach (string key in step.IntentKeys)
                {
                    result.Add(new ActionProgramStepV3(new[] { key }));
                }
            }
            int normalizedCount = result.Sum(step => step.IntentKeys.Count);
            if (normalizedCount > MaximumActionCount)
            {
                error = "Controlled kneel layering would exceed the four-action execution limit.";
                return false;
            }
            normalized = new ActionProgramV3(result);
            return true;
        }

        public ActionProgramV3 ToSequentialProgram()
        {
            return new ActionProgramV3(
                Steps.SelectMany(step => step.IntentKeys)
                    .Select(key => new ActionProgramStepV3(new[] { key })));
        }

        public override string ToString()
        {
            return ProtocolExpression;
        }
    }
}
