"""Source/resource contract; this does not execute Gauntlet or TaleWorlds native UI."""
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
UI = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Conversation"
page = (UI / "AnimusForgeConversationHistoryLogVM.cs").read_text(encoding="utf-8-sig")
item = (UI / "AnimusForgeConversationHistoryLogItemVM.cs").read_text(encoding="utf-8-sig")
memory = (ROOT / "src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs").read_text(encoding="utf-8-sig")
edit = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.DialogueHistoryEdit.cs").read_text(encoding="utf-8-sig")
reader = (ROOT / "src/modules/AF.Module.Prompt/Runtime/PromptComposerTexts.cs").read_text(encoding="utf-8-sig")
raw = json.loads((ROOT / "content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json").read_text(encoding="utf-8-sig"))


def body(source, signature):
    start = source.index("\n\t{", source.index(signature))
    end = source.index("\n\t}", start) + len("\n\t}")
    return source[start:end]


resource_paths = [
    "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml",
    "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml",
]
languages = []
for path in resource_paths:
    rows = list(ET.parse(ROOT / path).iter("string"))
    values = {row.get("id"): row.get("text") for row in rows}
    assert len(rows) == len(values), (path, "duplicate native IDs")
    languages.append(values)
en, cn = languages
expected_labels = {
    "Edit": ("Edit", "编辑"),
    "Delete": ("Delete", "删除"),
    "SpeakerFallback": ("Record", "记录"),
    "PlayerAction": ("Player action", "玩家行为"),
    "NpcAction": ("NPC action", "NPC行为"),
}
for suffix, expected in expected_labels.items():
    key = "AF_UI_ConversationHistory_" + suffix
    assert (en[key], cn[key]) == expected, key

# Header TextWidget resolves against the page History datasource, not an ItemVM.
prefab = ET.parse(ROOT / "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml")
header = [node for node in prefab.iter("Widget") if node.get("DataSource") == "{History}"]
assert header
for command, property_name, key in [
    ("ToggleEditMode", "EditButtonText", "Edit"),
    ("ToggleDeleteMode", "DeleteText", "Delete"),
]:
    buttons = [node for widget in header for node in widget.iter("ButtonWidget")
               if node.get("Command.Click") == command]
    assert len(buttons) == 1, command
    assert any(node.get("Text") == "@" + property_name for node in buttons[0].iter("TextWidget"))
    pattern = (r'\[DataSourceProperty\]\s+public string ' + property_name
               + r' => Localize\("AF_UI_ConversationHistory_' + key + r'", "' + en["AF_UI_ConversationHistory_" + key] + r'"\);')
    assert re.search(pattern, page), property_name
host = (ROOT / "extensions/AnimusForge.DialogueUI/src/Native/DialogueAuxiliaryVM.cs").read_text(encoding="utf-8-sig")
assert "[DataSourceProperty] public AnimusForgeConversationHistoryLogVM History { get; private set; }" in host
assert "History = new AnimusForgeConversationHistoryLogVM(" in host

# The projection is intentionally restricted to two trusted role labels. An unknown
# speaker cannot be translated merely because its text resembles an AFEF role/name.
helper = body(item, "internal static string LocalizeSpeakerForDisplay(")
expected_helper = '''
	{
		string value = string.IsNullOrWhiteSpace(speaker) ? Localize("AF_UI_ConversationHistory_SpeakerFallback", "Record") : speaker.Trim();
		if (string.Equals(kind, "afef_player", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(value, PromptComposerTexts.Raw("weekly_memory_compat_d26ab0225ce9"), StringComparison.Ordinal))
		{
			return Localize("AF_UI_ConversationHistory_PlayerAction", "Player action");
		}
		if (string.Equals(kind, "afef_npc", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(value, PromptComposerTexts.Raw("weekly_memory_compat_d0d9d4e1d4c0"), StringComparison.Ordinal))
		{
			return Localize("AF_UI_ConversationHistory_NpcAction", "NPC action");
		}
		return value;
	}'''
assert helper == expected_helper
assert 'internal static string Raw(string key) => Compatibility.Value[key];' in reader
assert 'Lazy<Dictionary<string, string>> Compatibility' in reader
assert 'GetManifestResourceStream("AnimusForge.Prompt.Compatibility.json")' in reader
assert raw["weekly_memory_compat_d26ab0225ce9"] == "AFEF 玩家行为"
assert raw["weekly_memory_compat_d0d9d4e1d4c0"] == "AFEF NPC行为"
assert raw["weekly_memory_compat_6d7b96ecf184"] == "[AFEF玩家行为补充]"
assert raw["weekly_memory_compat_c231f39bb72c"] == "[AFEF NPC行为补充]"
classifier = body(memory, "internal static void ClassifyDialogueHistoryLine(")
for prefix, role, kind in [
    ("weekly_memory_compat_6d7b96ecf184", "weekly_memory_compat_d26ab0225ce9", "afef_player"),
    ("weekly_memory_compat_c231f39bb72c", "weekly_memory_compat_d0d9d4e1d4c0", "afef_npc"),
]:
    branch = classifier.split('value.StartsWith(AnimusForge.PromptComposerTexts.Raw("' + prefix + '")', 1)[1].split("\n\t\t}", 1)[0]
    assert 'speaker = AnimusForge.PromptComposerTexts.Raw("' + role + '");' in branch
    assert 'kind = "' + kind + '";' in branch
    assert 'text = value.Substring(AnimusForge.PromptComposerTexts.Raw("' + prefix + '").Length).Trim();' in branch

constructor = body(item, "internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string text, string kind,")
assert "ChatSpeaker = LocalizeSpeakerForDisplay(speaker, kind);" in constructor
assert constructor.index("LocalizeSpeakerForDisplay") < constructor.index("rawDisplayText") < constructor.index("linkDisplaySession?.Format")
cached = body(page, "private CachedHistoryDisplayEntry GetOrCreateFormattedEntry(")
assert "string speaker = AnimusForgeConversationHistoryLogItemVM.LocalizeSpeakerForDisplay(entry.Speaker, entry.Kind);" in cached
assert cached.index("_formattedEntriesByIndex.TryGetValue") < cached.index("LocalizeSpeakerForDisplay") < cached.index("rawDisplayText") < cached.index("_linkDisplaySession?.Format")
assert not re.search(r"entry\.[A-Za-z]+\s*=(?!=)", cached)
assert '(entry.Text ?? string.Empty)' in cached
assert '_formattedEntriesByIndex.Add(entryIndex, cachedEntry);' in cached
paged_constructor = body(item, "internal AnimusForgeConversationHistoryLogItemVM(string time, string speaker, string formattedText,")
assert "LocalizeSpeakerForDisplay" not in paged_constructor and "ChatText = formattedText" in paged_constructor
assert item.count("LocalizeSpeakerForDisplay(") == 2  # Declaration plus only original constructor.
assert page.count("LocalizeSpeakerForDisplay(") == 1
assert "private const int HistoryPageSize = 50;" in page
assert "private const int CacheWarmEntriesPerTick = 3;" in page

# Persisted entry identity and edit input remain the original source. UI labels
# do not feed the editor, daily-memory synchronization or stable AFEF prefix.
assert "item.EnableEdit(entry.Text, StartEdit, text => SaveEdit(item, entry, text)" in page
assert "item.EnableDelete(ArmDelete, () => DeleteEntry(entry));" in page
assert "_tryEditEntry(entry, text)" in page
assert "item.ApplyEdit(displayEntry.Speaker, displayEntry.FormattedText, displayEntry.FontColor, entry.Text);" in page
assert "EditText = _editSourceText;" in item
assert "!DisplayTextMatches(day.Lines[lineOrdinal], entry.Text)" in edit
assert "string newLine = line.Substring(0, line.Length - oldText.Length) + text;" in edit
assert "day.Lines[lineOrdinal] = tagged ? DialogueHistoryLedger.TagSceneSession(newLine, sceneSessionId) : newLine;" in edit
assert "LocalizeSpeakerForDisplay" not in memory + edit

# Confirm the existing native resource inclusion and registration, shared by CN/EN packages.
mapping = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
for source, target in zip(resource_paths, ["ModuleData/Languages/af_ui_strings.xml", "ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml"]):
    assert len([row for row in mapping if row["source"] == source and row["target"] == target]) == 1
for metadata, native_path in [
    ("content/foundation/AF.Foundation.Localization/ModuleData/Languages/language_data.xml", "af_ui_strings.xml"),
    ("content/foundation/AF.Foundation.Localization/ModuleData/Languages/CNs/language_data.xml", "CNs/af_ui_strings-zh-CN.xml"),
]:
    assert sum(node.get("xml_path") == native_path for node in ET.parse(ROOT / metadata).iter("LanguageFile")) == 1

# Source-derived controlled dispatch illustrates the exact validated helper's
# pure branch logic. This is explicitly not an execution of C# or native TextObject.
branches = re.findall(r'string.Equals\(kind, "([^"]+)", StringComparison.OrdinalIgnoreCase\)\s*&& string.Equals\(value, PromptComposerTexts.Raw\("([^"]+)"\), StringComparison.Ordinal\)\)\s*\{\s*return Localize\("([^"]+)", "([^"]+)"\);', helper)
assert len(branches) == 2


def controlled_dispatch(speaker, kind, language):
    value = speaker.strip() if speaker is not None and speaker.strip() else language["AF_UI_ConversationHistory_SpeakerFallback"]
    for known_kind, raw_key, native_key, _fallback in branches:
        if kind is not None and kind.lower() == known_kind and value == raw[raw_key]:
            return language[native_key]
    return value


case_count = 0
for language in languages:
    for kind, raw_key, native_key, _ in branches:
        for case_kind in (kind, kind.upper()):
            for speaker in (raw[raw_key], " \t" + raw[raw_key] + " \n"):
                assert controlled_dispatch(speaker, case_kind, language) == language[native_key]
                case_count += 1
    for kind in (None, "npc", "system", "afef_unknown", "afef_player", "afef_npc"):
        for speaker in (None, "", " \t", "Vimi", "{SPEAKER} $1", "AFEF Vimi行为", "AFEF 玩家行为 suffix", "AFEF NPC行为 suffix"):
            expected = speaker.strip() if speaker is not None and speaker.strip() else language["AF_UI_ConversationHistory_SpeakerFallback"]
            assert controlled_dispatch(speaker, kind, language) == expected
            case_count += 1
    for speaker in (raw[branches[0][1]], raw[branches[1][1]]):
        for kind in (None, "npc", "player", "system", "afef_unknown"):
            assert controlled_dispatch(speaker, kind, language) == speaker
            case_count += 1
    assert controlled_dispatch(raw[branches[0][1]], "afef_npc", language) == raw[branches[0][1]]
    assert controlled_dispatch(raw[branches[1][1]], "afef_player", language) == raw[branches[1][1]]
    case_count += 2

# The terminal's nested report VM owns its controls. Only root pagination labels
# are also consumed at the parent datasource; the other eight labels move owners.
terminal_dir = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Terminal"
terminal = (terminal_dir / "AnimusForgeTerminalUiModels.cs").read_text(encoding="utf-8-sig")
weekly = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs").read_text(encoding="utf-8-sig")
weekly_root = weekly.split("public sealed partial class TerminalWeeklyReportBrowserPopupVM : ViewModel", 1)[1].split("public sealed class TerminalWeeklyReportCountryItemVM", 1)[0]
terminal_prefab = ET.parse(ROOT / "content/modules/AF.Module.UI/GUI/Prefabs/AnimusForgeTerminalPopup.xml")
nested_reports = [node for node in terminal_prefab.iter("Widget") if node.get("DataSource") == "{WeeklyReportVm}"]
assert len(nested_reports) == 1
nested_keys = {node.get("Text")[1:] for node in nested_reports[0].iter() if node.get("Text", "").startswith("@AF_UI_Terminal_Prefab_")}
expected_keys = {"AF_UI_Terminal_Prefab_" + str(number).zfill(3) + "Text" for number in (3, 4, 8, 9, 10, 11, 12, 13, 14, 15)}
assert nested_keys == expected_keys
for property_name in sorted(nested_keys):
    key = property_name[:-4]
    pattern = (r'\[DataSourceProperty\]\s+public string ' + property_name
               + r' => Localize\("' + key + r'", "' + re.escape(en[key]) + r'"\);')
    assert re.search(pattern, weekly_root), property_name
    assert en[key] and cn[key]
    assert (property_name in terminal) == (property_name in ("AF_UI_Terminal_Prefab_003Text", "AF_UI_Terminal_Prefab_004Text"))

for command, property_name in (("LoadOlderPage", "OlderPageText"), ("LoadNewerPage", "NewerPageText")):
    buttons = [node for node in prefab.iter("ButtonWidget") if node.get("Command.Click") == command]
    assert len(buttons) == 1
    assert any(node.get("Text") == "@" + property_name for node in buttons[0].iter("TextWidget"))
    assert re.search(r'\[DataSourceProperty\]\s+public string ' + property_name + r'\b', page)
assert not any(node.get("Text") in ("@LoadOlderText", "@LoadNewerText") for node in prefab.iter())

# Country and record ItemTemplates require their own SelectedMarkerText getter.
timeline = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/WorldTimeline/WorldMessageTimelineUi.cs").read_text(encoding="utf-8-sig")
timeline_prefab = ET.parse(ROOT / "content/modules/AF.Module.Weekly/GUI/Prefabs/AnimusForgeWorldMessageTimelinePopup.xml")
assert (en["AF_UI_Weekly_Timeline_Selected"], cn["AF_UI_Weekly_Timeline_Selected"]) == ("Selected", "已选")
for collection, class_name in (("CountryItems", "WorldMessageTimelineCountryItemVM"), ("RecordItems", "WorldMessageTimelineRecordItemVM")):
    list_nodes = [node for node in timeline_prefab.iter() if node.get("DataSource") == "{" + collection + "}"]
    assert len(list_nodes) == 1
    assert any(node.get("Text") == "@SelectedMarkerText" for node in list_nodes[0].iter("TextWidget"))
    class_source = timeline.split("public sealed class " + class_name + " : ViewModel", 1)[1].split("public sealed class ", 1)[0]
    assert re.search(r'\[DataSourceProperty\]\s+public string SelectedMarkerText => new TextObject\("\{=AF_UI_Weekly_Timeline_Selected\}Selected"\).ToString\(\);', class_source)

# Locale comparison precedes cache work; all public definition readers run the
# owner initializer. IDs, limits, getters/setters and unknown-fallback rules remain.
registry = (terminal_dir / "TerminalSettingsRegistry.cs").read_text(encoding="utf-8-sig")
initialize = body(registry, "public static void EnsureInitialized(")
assert 'string language = MBTextManager.ActiveTextLanguage ?? string.Empty;' in initialize
guard = 'if (_initialized && string.Equals(_initializedLanguage, language, StringComparison.Ordinal)) return;'
assert guard in initialize
assert initialize.index(guard) < initialize.index('_allDefinitions.Clear();') < initialize.index('RegisterDefinitions();')
for cache in ("_allDefinitions", "_byCategory", "_byId"):
    assert initialize.count(cache + '.Clear();') == 1
assert '_initializedLanguage = language;' in initialize
for signature in ("public static IReadOnlyList<TerminalSettingDef> AllDefinitions", "public static List<TerminalSettingDef> GetByCategory(", "public static TerminalSettingDef GetById("):
    assert "EnsureInitialized();" in body(registry, signature), signature
localization = (terminal_dir / "TerminalSettingsLocalization.cs").read_text(encoding="utf-8-sig")
definition_ids = re.findall(r'new TerminalSettingDef\("([^"]+)"', registry)
localized_ids = set(re.findall(r'"([^"]+)"', localization.split("LocalizedIds", 1)[1].split("};", 1)[0]))
assert len(definition_ids) == 259 and len(set(definition_ids)) == 259
assert set(definition_ids) <= localized_ids
assert 'if (!LocalizedIds.Contains(id)) return fallback ?? "";' in localization
for setting in definition_ids:
    for part in ("Title", "Hint", "Group"):
        key = "AF_TERMINAL_UI_" + setting + "_" + part
        assert key in en and key in cn, key

print("PASS History/terminal/timeline UI source/resource contract: header2, native roles2, nested terminal10, overlay paging2, timeline markers2; raw edit/identity and locale-cache boundaries; "
      + str(case_count) + " source-derived controlled cases, 259 settings × 3 paired native keys. Native TextObject/Gauntlet and dual API build are not executed here.")
