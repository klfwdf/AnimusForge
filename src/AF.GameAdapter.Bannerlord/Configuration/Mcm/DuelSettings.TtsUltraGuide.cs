using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyButton("{=AF_GPT_UI_UltraGuide}U edition - quality, installation and voices", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -11, RequireRestart = false,
        HintText = "{=AF_GPT_UI_UltraGuideHint}Independent V5 U tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsUltraUserGuide { get; set; } = GptSoVitsUltraGuide.Open;
}
