namespace TaleWorlds.Library
{
    public class InquiryElement
    {
        public object Identifier; public bool IsEnabled; public string Name;
        public InquiryElement(object id, string name, object image, bool isEnabled, string hint) { Identifier=id; Name=name; IsEnabled=isEnabled; }
    }
    public class MultiSelectionInquiryData
    {
        public string Title, Text; public List<InquiryElement> Elements; public Action<List<InquiryElement>> AffirmativeAction, NegativeAction;
        public MultiSelectionInquiryData(string title, string text, List<InquiryElement> elements, bool exit, int min, int max,
            string yes, string no, Action<List<InquiryElement>> affirmative, Action<List<InquiryElement>> negative,
            string sound="", bool isSeachAvailable=false) { Title=title; Text=text; Elements=elements; AffirmativeAction=affirmative; NegativeAction=negative; }
    }
    public class TextInquiryData
    {
        public Action<string> AffirmativeAction; public Action NegativeAction;
        public TextInquiryData(string title, string text, bool yesShown, bool noShown, string yes, string no,
            Action<string> affirmative, Action negative) { AffirmativeAction=affirmative; NegativeAction=negative; }
    }
    public class InquiryData
    {
        public Action AffirmativeAction, NegativeAction;
        public InquiryData(string title, string text, bool yesShown, bool noShown, string yes, string no,
            Action affirmative, Action negative) { AffirmativeAction=affirmative; NegativeAction=negative; }
    }
    public class InformationMessage { public InformationMessage(string text) { } }
    public static class InformationManager
    {
        public static object Popup;
        public static void ShowTextInquiry(TextInquiryData data, bool pauseGameActiveState=false) => Popup=data;
        public static void ShowInquiry(InquiryData data, bool pauseGameActiveState=false) => Popup=data;
        public static void DisplayMessage(InformationMessage message) { }
    }
}
// Native localization is a boundary substitute; actual production reader and registered XML are linked/read.
namespace TaleWorlds.Localization
{
    public class TextObject
    {
        public static string Language = "English";
        public static Dictionary<string, string> English, Chinese;
        private readonly string _text;
        private readonly Dictionary<string, object> _variables = new();
        public TextObject(string text) { _text = text; }
        public void SetTextVariable(string name, object value) => _variables[name] = value;
        public override string ToString()
        {
            string value = _text;
            if (value.StartsWith("{="))
            {
                int end = value.IndexOf('}'); string id = value.Substring(2, end - 2);
                var catalog = Language == "简体中文" ? Chinese : English;
                value = catalog != null && catalog.TryGetValue(id, out var translated) ? translated : value.Substring(end + 1);
            }
            foreach (var pair in _variables) value = value.Replace("{" + pair.Key + "}", pair.Value?.ToString() ?? "");
            return value;
        }
    }
}
namespace AnimusForge
{
    internal static class LlmRetryPrompt { internal static string ProjectKnownErrorForUi(string value) => value; }
    // Pure notification leaves of the complete source-linked TransferGold; no transfer rules are substituted.
    internal static class EconomyUiTexts
    {
        internal static string UnknownPerson => "unknown person";
        internal static string DenarsReceived(string name, int amount) => name + ": " + amount;
        internal static string DenarsGiven(string name, int amount) => name + ": " + amount;
    }
}
namespace TaleWorlds.Core
{
    public static class MBInformationManager
    {
        public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data, bool pauseGameActiveState=false)
            => TaleWorlds.Library.InformationManager.Popup=data;
    }
}
