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
    internal const string NewsSettingsGroup = "{=AF_MCM_Group_News}Weekly reports / bulletins";
    private Dropdown<string> _newsMode = new Dropdown<string>(new[] { "{=AF_MCM_News_Weekly}Weekly report", "{=AF_MCM_News_Bulletin}Bulletin" }, 1);
    private bool _newsModeLoaded;
    private bool? _autoGenerateWorldBulletins;

    [SettingPropertyDropdown("{=AF_MCM_News_Mode}Primary mode", Order = 0, RequireRestart = false,
        HintText = "{=AF_MCM_News_Mode_Hint}Weekly reports summarize world and kingdom events. Bulletins collect recent news after major events. Switching mode restarts collection and cancels old retries; existing archives are kept.")]
    [SettingPropertyGroup(NewsSettingsGroup, GroupOrder = 120)]
    public Dropdown<string> NewsMode
    {
        get => _newsMode;
        set
        {
            int index = value?.SelectedIndex ?? 1;
            _newsMode = new Dropdown<string>(new[] { "{=AF_MCM_News_Weekly}Weekly report", "{=AF_MCM_News_Bulletin}Bulletin" }, index == 0 ? 0 : 1);
            _newsModeLoaded = true;
        }
    }

    // Retain the old runtime API; its former JSON key is migrated after MCM loads.
    public bool UseWorldBulletin
    {
        get => _newsMode.SelectedIndex != 0;
        set => _newsMode.SelectedIndex = value ? 1 : 0;
    }

    [SettingPropertyBool("{=AF_MCM_News_AutoBulletin}Auto-publish bulletins", Order = 0, RequireRestart = false,
        HintText = "{=AF_MCM_News_AutoBulletin_Hint}Applies only in bulletin mode. When disabled, no new bulletin generation starts, while recent events remain available to NPCs and existing archives are kept.")]
    [SettingPropertyGroup(NewsSettingsGroup + "/{=AF_MCM_News_Bulletin}Bulletin", GroupOrder = 1)]
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

