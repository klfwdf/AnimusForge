"""Read-only C# literal inventory for the six C4 localization scopes."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "8a29ce5f49a76230006cb508fd511d00a135bd8a"
sys.path.insert(0, str(ROOT / "tools/localization"))
from inventory_untranslated import ESCAPE, HAN, LEX, decode

MODULE_FILES = {
    "Persona": [
        "src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs",
        "src/modules/AF.Module.Persona/Import/UnnamedPersonaImportValidationOwner.cs",
        "src/modules/AF.Module.Persona/Generation/NpcPersonaReadinessSnapshot.cs",
    ],
    "Taunt": [
        "src/modules/AF.Module.Taunt/Host/SceneTauntBehavior.cs",
    ],
    "Llm": [
        "src/modules/AF.Module.Llm/Application/ConfiguredChatApplicationAdapter.cs",
        "src/modules/AF.Module.Llm/Application/NativeConversationLlmApplicationAdapter.cs",
        "src/modules/AF.Module.Llm/ShoutNetwork.cs",
        "src/modules/AF.Module.Llm/Tts/VoiceMapper.cs",
        "src/modules/AF.Module.Llm/Tts/TtsEngine.cs",
        "src/modules/AF.Module.Llm/Ambient/TownAmbientAiClient.cs",
        "src/modules/AF.Module.Llm/Transport/LegacyShoutNetworkGateway.cs",
        "src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs",
        "src/modules/AF.Module.Llm/ModelCatalog/LegacyModelCatalogGateway.cs",
    ],
    "Memory": [
        "src/modules/AF.Module.Memory/Summary/MemorySummaryRules.cs",
        "src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs",
        "src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs",
        "src/modules/AF.Module.Memory/Records/NpcActionRecordOwner.cs",
        "src/modules/AF.Module.Memory/Records/NpcActionLedger.cs",
        "src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs",
        "src/modules/AF.Module.Memory/Summary/MemorySummaryQueueRunRuntime.cs",
        "src/modules/AF.Module.Memory/ImportExport/CompressedMemoryExportBundleReader.cs",
        "src/modules/AF.Module.Memory/Host/MyBehavior.DiplomacyMemory.cs",
        "src/modules/AF.Module.Memory/Domain/DiplomacyMemoryMaterial.cs",
        "src/modules/AF.Module.Memory/Recovery/MemoryRecoverySeedRules.cs",
        "src/modules/AF.Module.Memory/Records/ExecutionTranscriptStore.cs",
        "src/modules/AF.Module.Memory/Recall/HistoryArchiveRecallOwner.cs",
        "src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.Daily.cs",
        "src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs",
        "src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs",
    ],
    "KingdomCivilWar": [
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarModuleAdapter.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Coup.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Events.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.Actions.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarUiText.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.PlayerControl.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/RestoreCoupDynastyEffect.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarEffects.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarCampaignBehavior.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarCatalog.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.DialogueTags.cs",
        "src/modules/AF.Module.Kingdom/CivilWar/CivilWarWorld.cs",
        "src/modules/AF.Module.Kingdom/Rebellion/RebellionNamingRules.cs",
    ],
    "Knowledge": [
        "src/modules/AF.Module.Knowledge/Entities/WorldEntityRetrievalService.cs",
        "src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs",
        "src/modules/AF.Module.Knowledge/Semantic/OnnxCrossEncoderReranker.cs",
        "src/modules/AF.Module.Knowledge/Semantic/OnnxEmbeddingEngine.cs",
        "src/modules/AF.Module.Knowledge/Entities/EntityInjectionAllocator.cs",
        "src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs",
    ],
}


def decode_char(raw):
    value = raw[1:-1]
    controls = {"n": "\n", "r": "\r", "t": "\t", "0": "\0"}
    return ESCAPE.sub(lambda m: chr(int(next(x for x in m.groups()[:3] if x), 16))
                      if any(m.groups()[:3]) else controls.get(m[4], m[4]), value)


def baseline_source(relative):
    return subprocess.check_output(["git", "show", f"{BASELINE}:{relative}"], cwd=ROOT).decode("utf-8")


def literal_tokens(source):
    found = []
    for match in LEX.finditer(source):
        if match.lastgroup == "string":
            kind, value = "string", decode(match[0])
        elif match.lastgroup == "char":
            kind, value = "char", decode_char(match[0])
        else:
            continue
        found.append((match.start(), kind, value))
    return found


def has_cjk_or_fullwidth(value):
    return any(
        0x3000 <= ord(char) <= 0x303F
        or 0x3400 <= ord(char) <= 0x9FFF
        or 0xF900 <= ord(char) <= 0xFAFF
        or 0xFE30 <= ord(char) <= 0xFE4F
        or 0xFF00 <= ord(char) <= 0xFFEF
        or 0x20000 <= ord(char) <= 0x2FA1F
        for char in value
    )


EXPECTED_LINES = {"Persona": 32, "Taunt": 73, "Llm": 151, "Memory": 177, "KingdomCivilWar": 197, "Knowledge": 292}
EXPECTED_HAN_LITERALS = {"Persona": 39, "Taunt": 119, "Llm": 168, "Memory": 256, "KingdomCivilWar": 250, "Knowledge": 417}
EXPECTED_PUNCTUATION_ONLY = {"Persona": 1, "Taunt": 8, "Llm": 21, "Memory": 15, "KingdomCivilWar": 22, "Knowledge": 90}
EXPECTED_ALL_CJK = {"Persona": 40, "Taunt": 127, "Llm": 189, "Memory": 271, "KingdomCivilWar": 272, "Knowledge": 507}
module_totals = {}
for module, files in MODULE_FILES.items():
    total_han_literals = 0
    total_han_lines = 0
    total_cjk_tokens = 0
    module_strings = 0
    module_chars = 0
    punctuation_only = 0
    print(f"{module}:")
    for relative in files:
        source = baseline_source(relative)
        tokens = literal_tokens(source)
        han_literals = [(offset, kind, value) for offset, kind, value in tokens if HAN.search(value)]
        extra_cjk = [(offset, kind, value) for offset, kind, value in tokens if not HAN.search(value) and has_cjk_or_fullwidth(value)]
        lines = {source.count("\n", 0, offset) + 1 for offset, _, _ in han_literals}
        strings = sum(kind == "string" for _, kind, _ in han_literals)
        chars = sum(kind == "char" for _, kind, _ in han_literals)
        extra_strings = sum(kind == "string" for _, kind, _ in extra_cjk)
        extra_chars = sum(kind == "char" for _, kind, _ in extra_cjk)
        total_han_literals += len(han_literals)
        total_han_lines += len(lines)
        total_cjk_tokens += len(han_literals) + len(extra_cjk)
        module_strings += strings
        module_chars += chars
        punctuation_only += len(extra_cjk)
        print(f"  {relative}: han_literals={len(han_literals)} strings={strings} chars={chars} han_lines={len(lines)} cjk_punct_only={len(extra_cjk)} (strings={extra_strings} chars={extra_chars})")
    module_totals[module] = total_han_literals
    assert total_han_literals == EXPECTED_HAN_LITERALS[module], (module, total_han_literals)
    assert total_han_lines == EXPECTED_LINES[module], (module, total_han_lines)
    assert punctuation_only == EXPECTED_PUNCTUATION_ONLY[module], (module, punctuation_only)
    assert total_cjk_tokens == EXPECTED_ALL_CJK[module], (module, total_cjk_tokens)
    print(f"  total: han_literals={total_han_literals} strings={module_strings} chars={module_chars} han_lines={total_han_lines} punctuation_only_literals={punctuation_only} all_cjk_tokens={total_cjk_tokens}")

assert len(MODULE_FILES) == 6 and sum(map(len, MODULE_FILES.values())) == 49
assert all(total > 0 for total in module_totals.values())
print(f"PASS C4 baseline C# literal inventory baseline={BASELINE} files=49 modules=6 comments=excluded punctuation_and_fullwidth_counted_separately")
