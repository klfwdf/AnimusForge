using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_CONTEXT_Enabled}Automatic conversation context management", Order = 30, RequireRestart = false,
        HintText = "{=AF_CONTEXT_Enabled_Hint}Use recent original speech, intraday segment summaries and relevant earlier quotes across scene shout, free conversation and courier. Originals remain saved. Applies to future requests.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_CONTEXT_Group}Conversation context", GroupOrder = -289)]
    public bool ConversationContextEnabled { get; set; } = true;

    [SettingPropertyInteger("{=AF_CONTEXT_RecentTurns}Recent original dialogue turns", 2, 50, "0", Order = 31, RequireRestart = false,
        HintText = "{=AF_CONTEXT_RecentTurns_Hint}Maximum recent user-led dialogue groups carried as originals, also limited by the character budget. A group includes its replies. Default: 12. Current input and confirmed facts are retained separately.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_CONTEXT_Group}Conversation context", GroupOrder = -289)]
    public int ConversationContextRecentTurns { get; set; } = 12;

    [SettingPropertyInteger("{=AF_CONTEXT_RecentChars}Recent original dialogue character budget", 2000, 100000, "0", Order = 32, RequireRestart = false,
        HintText = "{=AF_CONTEXT_RecentChars_Hint}Select complete recent dialogue groups within this character budget. The current group is never silently cut. Earlier relevant quotes have a separate 6000-character budget. Default: 20000.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_CONTEXT_Group}Conversation context", GroupOrder = -289)]
    public int ConversationContextRecentChars { get; set; } = 20000;

    [SettingPropertyInteger("{=AF_CONTEXT_InputChars}Input character management target", 20000, 1000000, "0", Order = 33, RequireRestart = false,
        HintText = "{=AF_CONTEXT_InputChars_Hint}Automatically reduces older optional dialogue groups and rebuilds the cache toward this target. Required rules, facts and current input remain intact and continue even if they alone exceed the target. No local budget-error interruption. Default: 100000.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_CONTEXT_Group}Conversation context", GroupOrder = -289)]
    public int ConversationContextInputChars { get; set; } = 100000;

    [SettingPropertyInteger("{=AF_CONTEXT_Tokens}Estimated context token target", 16000, 1000000, "0", Order = 34, RequireRestart = false,
        HintText = "{=AF_CONTEXT_Tokens_Hint}Target for estimated input tokens plus the configured output allowance. Automatically reduces optional history; required-only input continues above the target. This estimate is not the provider tokenizer or automatic model-capacity detection. Default: 128000.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_CONTEXT_Group}Conversation context", GroupOrder = -289)]
    public int ConversationContextEstimatedTokens { get; set; } = 128000;
}
