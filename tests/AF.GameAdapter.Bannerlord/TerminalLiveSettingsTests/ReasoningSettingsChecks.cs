using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

static class ReasoningSettingsChecks
{
    internal static void Run(Assembly af, object settings, Action<bool, string> check)
    {
        Type type = settings.GetType();
        string[] efforts = { "low", "medium", "high", "xhigh", "max" };
        var normalize = type.GetMethod("NormalizeReasoningEffortSelection");
        check((string)normalize.Invoke(null, new object[] { " MINIMAL " }) == "low", "legacy string migrates to low");
        var registry = af.GetType("AnimusForge.TerminalSettingsRegistry");
        foreach (string owner in new[] { "Main", "Auxiliary", "ActionPostprocess", "EventAndRebellion" })
        {
            string id = owner + "ApiReasoningEffortDropdown";
            var property = type.GetProperty(id);
            var effortProperty = type.GetProperty(owner + "ApiReasoningEffort");
            var dropdownType = property.PropertyType;
            foreach (int index in Enumerable.Range(0, 6))
            {
                object dropdown = Activator.CreateInstance(dropdownType, new object[] { efforts.Concat(new[] { "minimal" }).ToList(), index });
                property.SetValue(settings, dropdown);
                object actual = property.GetValue(settings);
                int expected = index == 5 ? 0 : index;
                check((int)dropdownType.GetProperty("SelectedIndex").GetValue(actual) == expected, owner + " old index " + index);
                check((int)dropdownType.GetProperty("Count").GetValue(actual) == 5, owner + " five options");
                check((string)effortProperty.GetValue(settings) == efforts[expected], owner + " string selection");
            }
            type.GetMethod("Set" + owner + "ApiReasoningEffortForExternal").Invoke(settings, new object[] { "minimal" });
            check((string)effortProperty.GetValue(settings) == "low", owner + " external legacy string");
            object definition = registry.GetMethod("GetById").Invoke(null, new object[] { id });
            var setter = (Delegate)definition.GetType().GetProperty("Setter").GetValue(definition);
            var getter = (Delegate)definition.GetType().GetProperty("Getter").GetValue(definition);
            string hint = (string)definition.GetType().GetProperty("HintText").GetValue(definition);
            check(hint.Contains("low/medium/high/xhigh/max") && !hint.Contains("minimal"), owner + " localized terminal hint");
            foreach (int index in Enumerable.Range(0, 5))
            {
                setter.DynamicInvoke(settings, index);
                check((string)getter.DynamicInvoke(settings) == efforts[index], owner + " terminal setter " + index);
                check((string)effortProperty.GetValue(settings) == efforts[index], owner + " same MCM owner");
            }
        }
        var apply = type.GetMethod("ApplyThinkingControls");
        foreach (string model in new[] { "gemini-3.7-flash-high", "gemini-3.8-flash-high", "gemini-4-argon" })
        {
            foreach (string effort in efforts.Concat(new[] { "minimal", " MINIMAL " }))
            {
                var payload = new JObject { ["thinking"] = new JObject(), ["output_config"] = new JObject() };
                apply.Invoke(null, new object[] { payload, "https://www.shenlanqaq.com/v1", model, true, effort, null });
                string expected = effort.Trim().Equals("minimal", StringComparison.OrdinalIgnoreCase) ? "low" : effort;
                check((string)payload["reasoning_effort"] == expected, model + "/" + effort + " request effort");
                check(payload["thinking"] == null && payload["output_config"] == null, "no conflicting YJ controls");
            }
            foreach (string effort in new[] { "none", "low", "minimal" })
            {
                var payload = new JObject { ["thinking"] = new JObject(), ["output_config"] = new JObject(), ["reasoning_effort"] = "high" };
                apply.Invoke(null, new object[] { payload, "https://www.shenlanqaq.com/v1", model, false, effort, null });
                check(payload["thinking"] == null && payload["output_config"] == null, "disabled YJ controls removed");
                check(model == "gemini-4-argon" ? (string)payload["reasoning_effort"] == "none" : payload["reasoning_effort"] == null, model + " disabled request");
            }
        }
        foreach (string url in new[] { "https://api.openai.com/v1", "https://www.shenlanqaq.com.evil.test/v1" })
        {
            var payload = new JObject();
            apply.Invoke(null, new object[] { payload, url, "gemini-3.8-flash-high", true, "minimal", null });
            check(payload.Count == 0, "unrelated endpoints remain untouched");
        }
    }
}
