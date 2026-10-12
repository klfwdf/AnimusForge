using LocalizedTextObject = TaleWorlds.Localization.TextObject;

namespace AnimusForge;

// Resolve only when the existing Courier main-thread acceptance opens a notice.
internal static class CourierFailureUiText
{
    internal static string ReplyFailedTitle => new LocalizedTextObject("{=AF_CONV_UI_Courier_ReplyFailedTitle}Courier reply generation failed").ToString();
    internal static string LetterFailedTitle => new LocalizedTextObject("{=AF_CONV_UI_Courier_LetterFailedTitle}Courier letter generation failed").ToString();
    internal static string Retry => new LocalizedTextObject("{=AF_UI_Retry_Retry}Retry").ToString();
    internal static string Abort => new LocalizedTextObject("{=AF_UI_Retry_Abort}Abort").ToString();
    internal static string ReplyStoppedTitle => new LocalizedTextObject("{=AF_CONV_UI_Courier_ReplyStoppedTitle}Courier reply stopped").ToString();
    internal static string ReplyStoppedDetail => new LocalizedTextObject("{=AF_CONV_UI_Courier_ReplyStoppedDetail}This reply could not be completed safely. The system will not automatically retry actions that may already have taken effect. Check the log and verify the actual outcome.").ToString();
    internal static string InboundAborted => new LocalizedTextObject("{=AF_CONV_UI_Courier_InboundAborted}Courier letter processing failed. The courier was safely stopped and the letter was not delivered.").ToString();

    internal static string ReplyFailedDetail(string error)
        => FailureDetail("{=AF_CONV_UI_Courier_ReplyFailedDetail}Courier reply generation failed:\n\n{DETAIL}", error);

    internal static string LetterFailedDetail(string error)
        => FailureDetail("{=AF_CONV_UI_Courier_LetterFailedDetail}Courier letter generation failed:\n\n{DETAIL}", error);

    private static string FailureDetail(string template, string error)
    {
        var text = new LocalizedTextObject(template);
        text.SetTextVariable("DETAIL", "AFCOURIERLITERALEND");
        return text.ToString().Replace("AFCOURIERLITERALEND", LlmRetryPrompt.ProjectKnownErrorForUi(error) ?? new LocalizedTextObject("{=AF_UI_Retry_UnknownError}Unknown error").ToString());
    }
}
