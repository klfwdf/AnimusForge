using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_TERMINAL_UI_EnableNpcPersonaEventEvolution_Title}Event-driven NPC persona evolution", Order = 2, RequireRestart = false,
        HintText = "{=AF_TERMINAL_UI_EnableNpcPersonaEventEvolution_Hint}Default: off. New confirmed bulletin events can change NPC personas. Uses the auxiliary API with merged, rate-limited requests. Turning off discards pending updates but keeps existing changes. Requires bulletin mode; auto-publishing is not required.")]
    [SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Bulletin}Bulletin", GroupOrder = 1)]
    public bool EnableNpcPersonaEventEvolution
    {
        get => _enableNpcPersonaEventEvolution;
        set
        {
            if (_enableNpcPersonaEventEvolution == value) return;
            _enableNpcPersonaEventEvolution = value;
            MyBehavior.NotifyPersonaEvolutionSettingChanged(this);
        }
    }
    private bool _enableNpcPersonaEventEvolution;

    private const string WorldBulletinWritingRequirementsJsonFileName = "WorldBulletinWritingRequirements.json";
    private static readonly string DefaultWorldBulletinWritingRequirements = WorldBulletinPolicy.DefaultWritingRequirements;
    private string _worldBulletinWritingRequirements = LoadWorldBulletinWritingRequirementsFromDiskOrDefault();

    public string WorldBulletinWritingRequirements
    {
        get => _worldBulletinWritingRequirements;
        set => _worldBulletinWritingRequirements = NormalizeWorldBulletinWritingRequirementsText(value);
    }

    [SettingPropertyButton("{=AF_MCM_News_WritingRequirements}Bulletin writing requirements", -1, true, "", Content = "{=AF_MCM_Common_OpenEditor}Open editor", Order = 1,
        RequireRestart = false, HintText = "{=AF_MCM_News_WritingRequirements_Hint}Edit bulletin writing requirements separately. Changes apply to the next bulletin request; clearing the field removes the extra requirements.")]
    [SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Bulletin}Bulletin", GroupOrder = 1)]
    public Action EditWorldBulletinWritingRequirements { get; set; }

    private static string LoadWorldBulletinWritingRequirementsFromDiskOrDefault()
        => TryReadCustomPromptTextStore(out CustomPromptTextStoreJson store)
            ? store.WorldBulletinWritingRequirements ?? DefaultWorldBulletinWritingRequirements
            : DefaultWorldBulletinWritingRequirements;

    private static string NormalizeWorldBulletinWritingRequirementsText(string input)
        => LimitCustomPromptText((input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim(), WorldBulletinWritingRequirementsJsonFileName);

    // Load boundary only: an exact shipped default can advance; custom/empty text stays owned by the user.
    private static string MigrateLegacyWorldBulletinWritingRequirements(string input)
        => string.Equals(input, WorldBulletinPolicy.LegacyDefaultWritingRequirements, StringComparison.Ordinal)
            || string.Equals(input, WorldBulletinPolicy.LegacyNarrativeWritingRequirements, StringComparison.Ordinal)
            || string.Equals(input, WorldBulletinPolicy.LegacyCurrentWritingRequirements, StringComparison.Ordinal)
            || string.Equals(input, WorldBulletinPolicy.LegacyConciseWritingRequirementsCn, StringComparison.Ordinal)
            || string.Equals(input, WorldBulletinPolicy.LegacyConciseWritingRequirementsEn, StringComparison.Ordinal)
            ? DefaultWorldBulletinWritingRequirements : input;

    private void OpenWorldBulletinWritingRequirementsEditor()
    {
        DevTextEditorHelper.ShowLongTextEditor(
            DuelSettingsUiLocalization.Text("AF_MCM_News_EditBulletinTitle", "Edit bulletin writing requirements"),
            DuelSettingsUiLocalization.Text("AF_MCM_News_EditBulletinSubtitle", "Edit bulletin requirements only; weekly reports are not changed."),
            DuelSettingsUiLocalization.Text("AF_MCM_News_EditBulletinBody", "Changes apply to the next bulletin request. Clearing the field removes the extra requirements. Built-in prompts still control subject boundaries, output format, and length rules."),
            WorldBulletinWritingRequirements ?? "", SaveWorldBulletinWritingRequirementsFromEditor, null,
            DuelSettingsUiLocalization.Text("AF_MCM_Common_Save", "Save"),
            DuelSettingsUiLocalization.Text("AF_MCM_Common_Back", "Back"));
    }

    private void SaveWorldBulletinWritingRequirementsFromEditor(string input)
    {
        string text = NormalizeWorldBulletinWritingRequirementsText(input);
        if (!TryPersistCustomPromptTextFile(WorldBulletinWritingRequirementsJsonFileName, text))
        {
            InformationManager.DisplayMessage(new InformationMessage(DuelSettingsUiLocalization.Text("AF_MCM_News_SaveFailed", "Bulletin writing requirements could not be saved; the original text was kept. Check the log."), Colors.Red));
            return;
        }
        WorldBulletinWritingRequirements = text;
        var current = GetSettings();
        if (current != null) current.WorldBulletinWritingRequirements = text;
        InformationManager.DisplayMessage(new InformationMessage(DuelSettingsUiLocalization.Text("AF_MCM_News_Saved", "Bulletin writing requirements saved; they apply to the next request."), Colors.Green));
    }
}
