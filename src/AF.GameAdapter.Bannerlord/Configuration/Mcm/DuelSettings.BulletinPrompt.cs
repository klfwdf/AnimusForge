using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    private const string WorldBulletinWritingRequirementsJsonFileName = "WorldBulletinWritingRequirements.json";
    private const string DefaultWorldBulletinWritingRequirements = WorldBulletinPolicy.DefaultWritingRequirements;
    private string _worldBulletinWritingRequirements = LoadWorldBulletinWritingRequirementsFromDiskOrDefault();

    public string WorldBulletinWritingRequirements
    {
        get => _worldBulletinWritingRequirements;
        set => _worldBulletinWritingRequirements = NormalizeWorldBulletinWritingRequirementsText(value);
    }

    [SettingPropertyButton("快报写作要求", -1, true, "", Content = "打开编辑器", Order = 1,
        RequireRestart = false, HintText = "独立编辑快报写作要求。默认允许合理补写动作、短对白和报刊轶事，保留真实主体与重大结果，避免战事数字对账。保存后下次快报请求生效；清空则不追加这段要求。输出格式与结果边界始终保留。")]
    [SettingPropertyGroup(NewsSettingsGroup + "/快报", GroupOrder = 1)]
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
            ? DefaultWorldBulletinWritingRequirements : input;

    private void OpenWorldBulletinWritingRequirementsEditor()
    {
        DevTextEditorHelper.ShowLongTextEditor("编辑快报写作要求", "只编辑即时快报的写作要求，不修改周报。",
            "保存后用于下一次快报请求；清空后不追加写作要求。主体与重大结果边界、输出格式和现有篇幅规则仍由内置提示词控制。",
            WorldBulletinWritingRequirements ?? "", SaveWorldBulletinWritingRequirementsFromEditor, null, "保存", "返回");
    }

    private void SaveWorldBulletinWritingRequirementsFromEditor(string input)
    {
        string text = NormalizeWorldBulletinWritingRequirementsText(input);
        if (!TryPersistCustomPromptTextFile(WorldBulletinWritingRequirementsJsonFileName, text))
        {
            InformationManager.DisplayMessage(new InformationMessage("快报写作要求保存失败，原文保留；请查看日志。", Colors.Red));
            return;
        }
        WorldBulletinWritingRequirements = text;
        var current = GetSettings();
        if (current != null) current.WorldBulletinWritingRequirements = text;
        InformationManager.DisplayMessage(new InformationMessage("快报写作要求已保存，下次请求生效。", Colors.Green));
    }
}
