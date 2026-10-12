namespace TaleWorlds.Library
{
    public class InquiryElement
    {
        public object Identifier; public bool IsEnabled; public string Name;
        public InquiryElement(object id, string name, object image, bool isEnabled, string hint) { Identifier=id; Name=name; IsEnabled=isEnabled; }
    }
    public class MultiSelectionInquiryData
    {
        public List<InquiryElement> Elements; public Action<List<InquiryElement>> AffirmativeAction, NegativeAction;
        public MultiSelectionInquiryData(string title, string text, List<InquiryElement> elements, bool exit, int min, int max,
            string yes, string no, Action<List<InquiryElement>> affirmative, Action<List<InquiryElement>> negative,
            string sound="", bool isSeachAvailable=false) { Elements=elements; AffirmativeAction=affirmative; NegativeAction=negative; }
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
namespace TaleWorlds.Core
{
    public static class MBInformationManager
    {
        public static void ShowMultiSelectionInquiry(TaleWorlds.Library.MultiSelectionInquiryData data, bool pauseGameActiveState=false)
            => TaleWorlds.Library.InformationManager.Popup=data;
    }
}
