using System;
using System.IO;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Common;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
    // Full-width slash is intentional: MCM uses ASCII '/' to create subgroups.
    internal const string NewsSettingsGroup = "12. 周报／快报";
    private Dropdown<string> _newsMode = new Dropdown<string>(new[] { "周报", "快报" }, 1);
    private bool _newsModeLoaded;
    private bool? _autoGenerateWorldBulletins;

    [SettingPropertyDropdown("主模式", Order = 0, RequireRestart = false,
        HintText = "周报：每周汇总世界与各国事件。快报：发生大事后收集近期消息，再发布一期快报。两种模式互斥，默认快报；切换保留已有档案。")]
    [SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
    public Dropdown<string> NewsMode
    {
        get => _newsMode;
        set
        {
            int index = value?.SelectedIndex ?? 1;
            _newsMode = new Dropdown<string>(new[] { "周报", "快报" }, index == 0 ? 0 : 1);
            _newsModeLoaded = true;
        }
    }

    // Retain the old runtime API; its former JSON key is migrated after MCM loads.
    public bool UseWorldBulletin
    {
        get => _newsMode.SelectedIndex != 0;
        set => _newsMode.SelectedIndex = value ? 1 : 0;
    }

    [SettingPropertyBool("自动发布快报", Order = 0, RequireRestart = false,
        HintText = "仅主模式为快报时生效。关闭后不再发起新的快报生成，仍记录近期事件供 NPC 知情；已有档案保留。关闭前已发出的请求可能继续完成，但不会发布。与周报自动生成开关独立。")]
    [SettingPropertyGroup(NewsSettingsGroup + "/快报", GroupOrder = 1)]
    public bool AutoGenerateWorldBulletins
    {
        get => _autoGenerateWorldBulletins ?? AutoGenerateWeeklyReports;
        set => _autoGenerateWorldBulletins = value;
    }

    public override void OnPropertyChanged(string propertyName = null)
    {
        if (propertyName == "LOADING_COMPLETE")
        {
            // Read only on the settings-load boundary. No disk access on event/tick paths.
            if (!_newsModeLoaded)
            {
                try
                {
                    var path = new PlatformFilePath(new PlatformDirectoryPath(PlatformFileType.User,
                        "Configs/ModSettings/Global/AnimusForge"), Id + ".json");
                    if (File.Exists(path.FileFullPath))
                    {
                        var stored = JObject.Parse(File.ReadAllText(path.FileFullPath));
                        ApplyLegacyNewsMode(stored);
                    }
                }
                catch (Exception ex) { Logger.Log("DuelSettings", "[WARN] news mode migration: " + ex.Message); }
            }
            _autoGenerateWorldBulletins ??= AutoGenerateWeeklyReports;
        }
        if (propertyName == "SAVE_TRIGGERED") PreserveLegacyNewsModeKey();
        base.OnPropertyChanged(propertyName);
    }

    // MCM serializes attributed UI fields only. Preserve the old boolean alias in
    // its completed JSON file so earlier installations and tools can still read it.
    private void PreserveLegacyNewsModeKey()
    {
        string temporary = null;
        try
        {
            var file = new PlatformFilePath(new PlatformDirectoryPath(PlatformFileType.User,
                "Configs/ModSettings/Global/AnimusForge"), Id + ".json");
            string path = file.FileFullPath;
            if (!File.Exists(path)) return;
            var stored = JObject.Parse(File.ReadAllText(path));
            stored["UseWorldBulletin"] = UseWorldBulletin;
            temporary = path + ".news-" + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, stored.ToString(), new System.Text.UTF8Encoding(false));
            File.Replace(temporary, path, null);
        }
        catch (Exception ex) { Logger.Log("DuelSettings", "[WARN] legacy news mode alias: " + ex.Message); }
        finally { if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
    }

    internal void ApplyLegacyNewsMode(JObject stored)
    {
        if (!_newsModeLoaded && stored?["NewsMode"] == null && stored?["UseWorldBulletin"]?.Type == JTokenType.Boolean)
            UseWorldBulletin = stored.Value<bool>("UseWorldBulletin");
    }
}
