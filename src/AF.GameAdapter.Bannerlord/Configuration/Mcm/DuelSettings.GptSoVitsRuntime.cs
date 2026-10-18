using System;
using System.Threading.Tasks;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyButton("{=AF_TTS_Runtime_Refresh}Refresh local runtime status", -1, true, "", Content = "{=AF_TTS_Runtime_RefreshButton}Refresh", Order = 35, RequireRestart = false,
        HintText = "{=AF_TTS_Runtime_RefreshHint}Check installed L/U packages, preferring a verified L. Retries preparation when AF-GPTSOVITS is enabled. With speech off, checks metadata without loading models.")]
    [SettingPropertyGroup(GptVoiceGroup)]
    public Action RefreshTtsGptSoVitsRuntime { get; set; }

    private void InitializeGptSoVitsRuntimeActions()
    {
        RefreshTtsGptSoVitsRuntime = () =>
        {
            GptSoVitsVoiceRuntime.InvalidateCatalog();
            GptSoVitsManagedRuntime.Observe(GetSettings() ?? this);
            _ = GptSoVitsManagedRuntime.RefreshAsync();
        };
    }
}
