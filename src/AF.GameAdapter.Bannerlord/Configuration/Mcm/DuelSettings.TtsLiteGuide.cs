using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideDownload}L guide 1 - Download and version", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -19, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideDownload { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(0);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideInstall}L guide 2 - Installation", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -18, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideInstall { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(1);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideUse}L guide 3 - Enable and assign voices", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -17, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideUse { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(2);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideHardware}L guide 4 - VRAM and performance", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -16, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideHardware { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(3);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideRecordings}L guide 5 - Custom recordings", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -15, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideRecordings { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(4);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideManifest}L guide 6 - Folders and manifest", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -14, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideManifest { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(5);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideLanguage}L guide 7 - Language and emotion", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -13, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideLanguage { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(6);

    [SettingPropertyButton("{=AF_GPT_UI_LiteGuideTroubleshoot}L guide 8 - Troubleshooting and updates", -1, true, "", Content = "{=AF_GPT_UI_GuideRead}Read guide", Order = -12, RequireRestart = false,
        HintText = "{=AF_GPT_UI_LiteGuideHint}AF-GPTSOVITS-L tutorial. Reading does not download, start a model, or change settings.")]
    [SettingPropertyGroup("{=AF_MCM_Tts_Group}7. Speech synthesis")]
    public Action TtsLiteGuideTroubleshoot { get; set; } = () => GptSoVitsLiteGuide.OpenChapter(7);

}
