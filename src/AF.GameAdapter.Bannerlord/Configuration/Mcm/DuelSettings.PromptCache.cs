using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_MCM_Cache_Enabled}Cache-friendly conversation prompts", Order = 20, RequireRestart = false,
        HintText = "{=AF_MCM_Cache_Enabled_Hint}Use a stable prefix and append-only transcript for free conversation and scene shout. Only accepted replies enter the cache. Provider cache hits and prices remain provider-dependent. Applies to the next request.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_MCM_Cache_Group}Conversation prompt cache", GroupOrder = -290)]
    public bool ConversationPromptCacheEnabled { get; set; } = true;

    [SettingPropertyInteger("{=AF_MCM_Cache_MaxChars}Maximum cached transcript characters", 20000, 1000000, "0", Order = 21, RequireRestart = false,
        HintText = "{=AF_MCM_Cache_MaxChars_Hint}Counts the complete cached request, including system, references and current input. Rebuilds a complete snapshot before this limit; an oversized snapshot uses the original request subject to the final context budget. This is a cache-storage limit, not the model token limit. Default: 150000.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_MCM_Cache_Group}Conversation prompt cache", GroupOrder = -290)]
    public int ConversationPromptCacheMaxChars { get; set; } = 150000;

    [SettingPropertyInteger("{=AF_MCM_Cache_Ttl}Cache lifetime in minutes", 1, 60, "0", Order = 22, RequireRestart = false,
        HintText = "{=AF_MCM_Cache_Ttl_Hint}Reuse within the same runtime, channel and scene or native conversation identity. Expired sessions restart with a complete snapshot. No Tick scanning or save data. Default: 10 minutes.")]
    [SettingPropertyGroup("{=AF_MCM_AiCore_Group}1. AI core configuration/{=AF_MCM_Cache_Group}Conversation prompt cache", GroupOrder = -290)]
    public int ConversationPromptCacheTtlMinutes { get; set; } = 10;
}
