using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ActionProgramStepV4
    {
        public ActionProgramStepV4(IEnumerable<string> intentKeys)
        {
            string[] keys = (intentKeys ?? Enumerable.Empty<string>()).ToArray();
            if (keys.Length == 0 || keys.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "A V4 program step must contain non-empty actions.",
                    nameof(intentKeys));
            }
            IntentKeys = Array.AsReadOnly(keys);
        }

        public IReadOnlyList<string> IntentKeys { get; }
        public bool IsSimultaneous => IntentKeys.Count > 1;
    }

    public sealed class ActionProgramV4
    {
        public const int MaximumActionCount = 4;

        public ActionProgramV4(IEnumerable<ActionProgramStepV4> steps)
            : this(steps, false)
        {
        }

        private ActionProgramV4(
            IEnumerable<ActionProgramStepV4> steps,
            bool allowRuntimeControls)
        {
            ActionProgramStepV4[] materialized =
                (steps ?? Enumerable.Empty<ActionProgramStepV4>()).ToArray();
            if (materialized.Length == 0 || materialized.Any(step => step == null))
            {
                throw new ArgumentException(
                    "A V4 action program must contain non-null steps.",
                    nameof(steps));
            }
            int count = materialized.Sum(step => step.IntentKeys.Count);
            if (count < 1 || count > MaximumActionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps),
                    "A V4 action program must contain between one and four logical actions.");
            }
            foreach (string key in materialized.SelectMany(step => step.IntentKeys))
            {
                if (!SceneActionFrameworkV4.IsLogicalIntent(key) &&
                    !(allowRuntimeControls &&
                      SceneActionRuntimeControlsV1.IsControlIntent(key)))
                {
                    throw new ArgumentException(
                        "Program intent is outside SceneActionFrameworkV4: " + key,
                        nameof(steps));
                }
            }
            Steps = new ReadOnlyCollection<ActionProgramStepV4>(materialized);
            ActionCount = count;
        }

        public IReadOnlyList<ActionProgramStepV4> Steps { get; }
        public int ActionCount { get; }
        public bool IsSingleAction => Steps.Count == 1 && Steps[0].IntentKeys.Count == 1;
        public string SingleIntentKey => IsSingleAction ? Steps[0].IntentKeys[0] : null;
        public string ProtocolExpression => string.Join(
            ">",
            Steps.Select(step => string.Join("+", step.IntentKeys)));

        public static ActionProgramV4 FromSingle(string intentKey)
        {
            return new ActionProgramV4(new[]
            {
                new ActionProgramStepV4(new[] { intentKey })
            });
        }

        public static ActionProgramV4 FromRuntimeControl(string intentKey)
        {
            if (!SceneActionRuntimeControlsV1.IsControlIntent(intentKey))
            {
                throw new ArgumentException(
                    "Intent is not a registered runtime control: " + intentKey,
                    nameof(intentKey));
            }
            return new ActionProgramV4(new[]
            {
                new ActionProgramStepV4(new[] { intentKey })
            }, true);
        }

        public static ActionProgramV4 FromV2(ActionProgramV2 program)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            return new ActionProgramV4(program.Steps.Select(step =>
                new ActionProgramStepV4(step.IntentKeys)));
        }

        public static ActionProgramV4 FromV3(ActionProgramV3 program)
        {
            if (program == null)
            {
                throw new ArgumentNullException(nameof(program));
            }
            return new ActionProgramV4(program.Steps.Select(step =>
                new ActionProgramStepV4(step.IntentKeys)));
        }

        public bool TryToV3(out ActionProgramV3 program)
        {
            program = null;
            if (Steps.SelectMany(step => step.IntentKeys)
                .Any(key => !SceneActionFrameworkV3.IsLogicalIntent(key)))
            {
                return false;
            }
            program = new ActionProgramV3(Steps.Select(step =>
                new ActionProgramStepV3(step.IntentKeys)));
            return true;
        }

        public static bool TryParseExpression(
            string expression,
            out ActionProgramV4 program,
            out string error)
        {
            program = null;
            error = null;
            if (string.IsNullOrEmpty(expression) || expression.Any(char.IsWhiteSpace))
            {
                error = "Program expression must be non-empty and contain no whitespace.";
                return false;
            }
            string[] rawSteps = expression.Split('>');
            List<ActionProgramStepV4> steps = new List<ActionProgramStepV4>();
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
                    if (!SceneActionFrameworkV4.IsLogicalIntent(key))
                    {
                        error = "Program selected a non-whitelisted V4 intent: " + key;
                        return false;
                    }
                }
                actionCount += keys.Length;
                if (actionCount > MaximumActionCount)
                {
                    error = "Program exceeds the four-action limit.";
                    return false;
                }
                steps.Add(new ActionProgramStepV4(keys));
            }
            try
            {
                program = new ActionProgramV4(steps);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryNormalizeForExecution(
            out ActionProgramV4 normalized,
            out string error)
        {
            normalized = null;
            error = null;
            List<ActionProgramStepV4> result = new List<ActionProgramStepV4>();
            foreach (ActionProgramStepV4 step in Steps)
            {
                if (!step.IsSimultaneous)
                {
                    result.Add(step);
                    continue;
                }
                bool containsKneel = step.IntentKeys.Contains(
                    SceneActionFrameworkV4.Kneel,
                    StringComparer.Ordinal);
                string[] overlays = step.IntentKeys
                    .Where(key => !string.Equals(
                        key,
                        SceneActionFrameworkV4.Kneel,
                        StringComparison.Ordinal))
                    .ToArray();
                if (containsKneel && overlays.Length > 0 &&
                    overlays.All(SceneActionFrameworkV4.CanOverlayKneel))
                {
                    foreach (string overlay in overlays)
                    {
                        result.Add(new ActionProgramStepV4(new[]
                        {
                            SceneActionFrameworkV4.Kneel,
                            overlay
                        }));
                    }
                    continue;
                }
                foreach (string key in step.IntentKeys)
                {
                    result.Add(new ActionProgramStepV4(new[] { key }));
                }
            }
            int normalizedCount = result.Sum(step => step.IntentKeys.Count);
            if (normalizedCount > MaximumActionCount)
            {
                error = "Controlled kneel layering would exceed the four-action execution limit.";
                return false;
            }
            bool containsRuntimeControl = result
                .SelectMany(step => step.IntentKeys)
                .Any(SceneActionRuntimeControlsV1.IsControlIntent);
            normalized = new ActionProgramV4(result, containsRuntimeControl);
            return true;
        }

        public ActionProgramV4 ToSequentialProgram()
        {
            bool containsRuntimeControl = Steps
                .SelectMany(step => step.IntentKeys)
                .Any(SceneActionRuntimeControlsV1.IsControlIntent);
            return new ActionProgramV4(
                Steps.SelectMany(step => step.IntentKeys)
                    .Select(key => new ActionProgramStepV4(new[] { key })),
                containsRuntimeControl);
        }

        public override string ToString()
        {
            return ProtocolExpression;
        }
    }
}
