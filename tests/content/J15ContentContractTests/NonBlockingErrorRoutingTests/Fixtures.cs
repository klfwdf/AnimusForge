using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TaleWorlds.Core { }

namespace TaleWorlds.Library
{
    public sealed class InquiryData
    {
        public string TitleText, Text, NegativeText;
        public bool IsAffirmativeOptionShown, IsNegativeOptionShown;
        public Action AffirmativeAction, NegativeAction, TimeoutAction;
    }

    public sealed class InformationMessage
    {
        public string Information { get; }
        public InformationMessage(string information, object color) => Information = information;
    }

    public static class InformationManager
    {
        public static string LastMessage;
        public static void DisplayMessage(InformationMessage message) => LastMessage = message.Information;
    }

    public static class Colors { public static readonly object Red = new object(); }
}

namespace TaleWorlds.Localization
{
    public sealed class TextObject
    {
        private readonly string _value;
        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();
        public TextObject(string value) => _value = Regex.Replace(value, @"^\{=[^}]+\}", "");
        public void SetTextVariable(string name, string value) => _variables[name] = value;
        public override string ToString()
        {
            string result = _value;
            foreach (var pair in _variables) result = result.Replace("{" + pair.Key + "}", pair.Value);
            return result;
        }
    }
}

namespace AnimusForge
{
    internal static class Logger
    {
        internal static string Last;
        internal static void Log(string category, string message) => Last = category + ":" + message;
    }

    internal static class AiErrorAnalysisInquiry
    {
        internal static string LastDetail;
        internal static void RememberFailure(string title, string detail) => LastDetail = detail;
        internal static void RequestLatestFailureAnalysis() { }
    }

    internal sealed class DuelSettings
    {
        internal string ApiKey, AuxiliaryApiKey, ActionPostprocessApiKey, EventAndRebellionApiKey;
        internal static DuelSettings GetSettings() => null;
    }
}
