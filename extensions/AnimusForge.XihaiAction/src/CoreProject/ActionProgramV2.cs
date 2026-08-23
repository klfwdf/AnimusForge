using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ActionProgramStepV2
    {
        public ActionProgramStepV2(IEnumerable<string> intentKeys)
        {
            string[] keys = (intentKeys ?? Enumerable.Empty<string>()).ToArray();
            if (keys.Length == 0)
            {
                throw new ArgumentException("A program step must contain at least one action.", nameof(intentKeys));
            }
            if (keys.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Program intent keys must not be empty.", nameof(intentKeys));
            }
            IntentKeys = Array.AsReadOnly(keys);
        }

        public IReadOnlyList<string> IntentKeys { get; }
        public bool IsSimultaneous => IntentKeys.Count > 1;
    }

    /// <summary>
    /// Immutable V2 closed-set action program. '>' separates ordered steps and '+'
    /// separates actions requested in the same step. The input program never contains
    /// more than four logical actions.
    /// </summary>
    public sealed class ActionProgramV2
    {
        public const int MaximumActionCount = 4;

        public ActionProgramV2(IEnumerable<ActionProgramStepV2> steps)
        {
            ActionProgramStepV2[] materialized =
                (steps ?? Enumerable.Empty<ActionProgramStepV2>()).ToArray();
            if (materialized.Length == 0 || materialized.Any(step => step == null))
            {
                throw new ArgumentException("An action program must contain non-null steps.", nameof(steps));
            }

            int count = materialized.Sum(step => step.IntentKeys.Count);
            if (count < 1 || count > MaximumActionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps),
                    "An action program must contain between one and four logical actions.");
            }
            foreach (string key in materialized.SelectMany(step => step.IntentKeys))
            {
                if (!SceneActionFrameworkV2.IsLogicalIntent(key))
                {
                    throw new ArgumentException(
                        "Program intent is outside SceneActionFrameworkV2: " + key,
                        nameof(steps));
                }
            }

            Steps = new ReadOnlyCollection<ActionProgramStepV2>(materialized);
            ActionCount = count;
        }

        public IReadOnlyList<ActionProgramStepV2> Steps { get; }
        public int ActionCount { get; }
        public bool IsSingleAction => Steps.Count == 1 && Steps[0].IntentKeys.Count == 1;
        public string SingleIntentKey => IsSingleAction ? Steps[0].IntentKeys[0] : null;

        public string ProtocolExpression => string.Join(
            ">",
            Steps.Select(step => string.Join("+", step.IntentKeys)));

        public static ActionProgramV2 FromSingle(string intentKey)
        {
            return new ActionProgramV2(new[]
            {
                new ActionProgramStepV2(new[] { intentKey })
            });
        }

        public static bool TryParseExpression(
            string expression,
            out ActionProgramV2 program,
            out string error)
        {
            program = null;
            error = null;
            if (string.IsNullOrEmpty(expression) ||
                expression.IndexOf(' ') >= 0 ||
                expression.IndexOf('\t') >= 0 ||
                expression.IndexOf('\r') >= 0 ||
                expression.IndexOf('\n') >= 0)
            {
                error = "Program expression must be non-empty and contain no whitespace.";
                return false;
            }

            string[] rawSteps = expression.Split('>');
            List<ActionProgramStepV2> steps = new List<ActionProgramStepV2>();
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
                    if (!SceneActionFrameworkV2.IsLogicalIntent(key))
                    {
                        error = "Program selected a non-whitelisted intent: " + key;
                        return false;
                    }
                }
                actionCount += keys.Length;
                if (actionCount > MaximumActionCount)
                {
                    error = "Program exceeds the four-action limit.";
                    return false;
                }
                steps.Add(new ActionProgramStepV2(keys));
            }

            try
            {
                program = new ActionProgramV2(steps);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryNormalizeForExecution(
            out ActionProgramV2 normalized,
            out string error)
        {
            normalized = null;
            error = null;
            List<ActionProgramStepV2> result = new List<ActionProgramStepV2>();
            foreach (ActionProgramStepV2 step in Steps)
            {
                if (!step.IsSimultaneous)
                {
                    result.Add(step);
                    continue;
                }

                bool containsKneel = step.IntentKeys.Contains(
                    SceneActionFrameworkV2.Kneel,
                    StringComparer.Ordinal);
                string[] overlays = step.IntentKeys
                    .Where(key => !string.Equals(
                        key,
                        SceneActionFrameworkV2.Kneel,
                        StringComparison.Ordinal))
                    .ToArray();
                if (containsKneel &&
                    overlays.Length > 0 &&
                    overlays.All(SceneActionFrameworkV2.CanOverlayKneel))
                {
                    foreach (string overlay in overlays)
                    {
                        result.Add(new ActionProgramStepV2(new[]
                        {
                            SceneActionFrameworkV2.Kneel,
                            overlay
                        }));
                    }
                    continue;
                }

                // Unsupported simultaneous combinations are made deterministic and safe:
                // preserve their declared order, but execute them one at a time.
                foreach (string key in step.IntentKeys)
                {
                    result.Add(new ActionProgramStepV2(new[] { key }));
                }
            }

            int normalizedCount = result.Sum(step => step.IntentKeys.Count);
            if (normalizedCount > MaximumActionCount)
            {
                error = "Controlled kneel layering would exceed the four-action execution limit.";
                return false;
            }
            normalized = new ActionProgramV2(result);
            return true;
        }

        public ActionProgramV2 ToSequentialProgram()
        {
            return new ActionProgramV2(
                Steps.SelectMany(step => step.IntentKeys)
                    .Select(key => new ActionProgramStepV2(new[] { key })));
        }

        public override string ToString()
        {
            return ProtocolExpression;
        }
    }
}
