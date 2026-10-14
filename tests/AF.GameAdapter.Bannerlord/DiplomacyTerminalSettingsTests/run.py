"""Removed-field compatibility through the actual MCM JSON format, without game IO."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
parser.add_argument("--mcm", type=Path, default=ROOT / ".tmp/build_check/1.4/MCMv5.dll")
args = parser.parse_args()
out = new_run_root(ROOT, "diplomacy-terminal-settings", args.run_root)
dotnet = resolve_dotnet(ROOT)
source = (ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs").read_text(encoding="utf-8-sig")
constants = []
for name in ("DefaultWorldDiplomacyDeclarationMinCharacters", "DefaultWorldDiplomacyDeclarationMaxCharacters",
             "WorldDiplomacyDeclarationCharactersMin", "WorldDiplomacyDeclarationCharactersMax"):
    constants.append(re.search(r"public const int " + name + r"\s*=\s*\d+;", source)[0])
properties = []
for name in ("WorldDiplomacyDeclarationMinCharacters", "WorldDiplomacyDeclarationMaxCharacters"):
    end = source.index("public int " + name)
    start = source.rfind("[SettingPropertyInteger", 0, end)
    end = source.index("\n", end)
    properties.append(source[start:end])
retired = ("EnableWorldDiplomacyForcedWar", "WorldDiplomacyWarPressureThreshold",
           "WorldDiplomacyPeacePressureThreshold", "WorldDiplomacyNativeIntentInfluencePercent",
           "WorldDiplomacyDocumentInfluencePercent", "WorldDiplomacyCompressionThresholdTenThousands")
assert all(not re.search(r"\b" + name + r"\b", source) for name in retired)

# Inspect the real UI resources and consumers. Keep identities/ranges/setter routes.
registry = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs").read_text(encoding="utf-8-sig")
fallback = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsLocalization.cs").read_text(encoding="utf-8-sig")
base = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages"
cn = {n.get("id"): n.get("text") for n in ET.parse(base / "CNs/af_ui_strings-zh-CN.xml").iter("string")}
en = {n.get("id"): n.get("text") for n in ET.parse(base / "af_ui_strings.xml").iter("string")}
for prop in ("WorldDiplomacyDeclarationMinCharacters", "WorldDiplomacyDeclarationMaxCharacters",
             "EditWorldDiplomacyPrompt", "RestoreDefaultWorldDiplomacyPrompt", "EventAndRebellionApiTemperature"):
    line = next(line for line in registry.splitlines() if 'new TerminalSettingDef("' + prop + '"' in line)
    literals = [json.loads(x) for x in re.findall(r'"(?:\\.|[^"\\])*"', line)]
    for suffix, value in (("Title", literals[2]), ("Hint", literals[3])):
        key = "AF_TERMINAL_UI_" + prop + "_" + suffix
        assert en[key] == value and cn[key], key
        assert json.dumps(value, ensure_ascii=False) in fallback, key + ": English fallback"
    if "Characters" in prop:
        assert "1f, 1000f" in line and "s => s." + prop in line
        assert "s." + prop + " = Convert.ToInt32(v)" in line
assert "300" in en["AF_TERMINAL_UI_WorldDiplomacyDeclarationMaxCharacters_Hint"]
assert "generation capacity" in en["AF_TERMINAL_UI_WorldDiplomacyDeclarationMaxCharacters_Hint"]
assert "主API温度" in cn["AF_TERMINAL_UI_EventAndRebellionApiTemperature_Hint"]
assert "main API temperature" in en["AF_TERMINAL_UI_EventAndRebellionApiTemperature_Hint"]
assert "起草或重写" in cn["AF_TERMINAL_UI_EditWorldDiplomacyPrompt_Hint"]
assert "drafting or rewriting" in en["AF_TERMINAL_UI_EditWorldDiplomacyPrompt_Hint"]
ui_keys = {"AF_TERMINAL_UI_" + prop + "_" + suffix
           for prop in ("WorldDiplomacyDeclarationMinCharacters", "WorldDiplomacyDeclarationMaxCharacters",
                        "EditWorldDiplomacyPrompt", "RestoreDefaultWorldDiplomacyPrompt", "EventAndRebellionApiTemperature")
           for suffix in ("Title", "Hint")}
ui_keys.update("AF_MCM_Diplomacy_" + suffix for suffix in (
    "DeclarationMin", "DeclarationMin_Hint", "DeclarationMax", "DeclarationMax_Hint",
    "Edit", "Edit_Hint", "Restore", "Restore_Hint"))
ui_keys.add("AF_MCM_EventRebellionApi_Temperature_Hint")
ui_keys.update("AF_MCM_Editor_Diplomacy_" + suffix for suffix in ("Title", "Description", "Hint", "RestoreTitle"))
(out / "expected-ui.json").write_text(json.dumps({"en": {k: en[k] for k in sorted(ui_keys)},
                                                "cn": {k: cn[k] for k in sorted(ui_keys)}}, ensure_ascii=False), encoding="utf-8")

derived = "\n".join(constants + properties)
settings = '''using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
namespace AnimusForge {
internal sealed class DuelSettings : AttributeGlobalSettings<DuelSettings> {
public override string Id => "AnimusForge_settings_replay";
public override string DisplayName => "Settings replay";
public override string FolderName => "Replay";
public override string FormatType => "json";
[SettingPropertyText("Fixture model")]
[SettingPropertyGroup("Fixture")]
public string ModelName { get; set; } = "default-model";
[SettingPropertyFloatingInteger("Fixture temperature", 0, 2)]
[SettingPropertyGroup("Fixture")]
public float MainApiTemperature { get; set; } = 0.8f;
[SettingPropertyBool("Fixture diplomacy")]
[SettingPropertyGroup("Fixture")]
public bool EnableWorldDiplomacy { get; set; } = true;
[SettingPropertyText("Fixture preference")]
[SettingPropertyGroup("Fixture")]
public string WorldDiplomacyPrompt { get; set; } = "";
''' + derived + "\n}}\n"
(out / "Settings.cs").write_text(settings, encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
refs = [args.mcm.resolve()] + [args.mcm.parent / name for name in (
    "Newtonsoft.Json.dll", "0Harmony.dll", "TaleWorlds.Localization.dll", "TaleWorlds.Library.dll")]
assert all(path.is_file() for path in refs), "MCM reference overlay is unavailable"
references = "".join('<Reference Include="' + path.stem + '"><HintPath>' + escape(str(path.resolve()))
                     + '</HintPath></Reference>' for path in refs)
(out / "Test.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><WarningLevel>0</WarningLevel>'
    '</PropertyGroup><ItemGroup>' + references + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(dotnet, out)
env.pop("HOME", None)
for command, name in (([str(dotnet), "build", str(out / "Test.csproj"), "--nologo", "-v:q",
                        "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], "build.log"),
                      ([str(dotnet), str(out / "bin/Debug/net8.0/Test.dll"), str(base), str(out / "expected-ui.json")], "run.log")):
    result = subprocess.run(command, cwd=out, env=env, capture_output=True, text=True,
                            encoding="utf-8", errors="replace", timeout=120)
    (out / name).write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
(out / "receipt.json").write_text(json.dumps({"production_span_sha256": hashlib.sha256(derived.encode()).hexdigest(),
    "mcm_sha256": hashlib.sha256(args.mcm.read_bytes()).hexdigest(), "exit_code": 0,
    "scope": "actual MCM JSON format/converters; source-linked defaults/properties; game provider and UI not started"}, indent=2), encoding="utf-8")
print("OUTPUT", out)
