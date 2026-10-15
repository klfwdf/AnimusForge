"""Replay production clan-join rule assembly/normalization with CN/EN catalogs."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--token-log", type=Path)
    args = parser.parse_args()
    out = new_run_root(ROOT, "clan-join-postprocess", args.out)
    spec = importlib.util.spec_from_file_location(
        "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)
    config = (ROOT / "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs").read_text("utf-8-sig")
    owner = (ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs").read_text("utf-8-sig")
    methods = "\n".join(extract.declaration(config, marker) for marker in [
        "public static bool IsPlayerPartyTradeLimitedTarget(",
        "private static bool IsPlayerClanLordTarget(",
        "public static List<PostprocessRuleEntry> BuildRuntimeHeroJoinPartyPostprocessRules(",
        "private static List<PostprocessRuleEntry> BuildRuntimeClanJoinKingdomPostprocessRules(",
        "private static string ResolveClanJoinKingdomStateKey(",
        "private static List<Kingdom> ResolveRetrievedKingdomsForClanJoin(",
        "private static bool IsClanJoinKingdomServicePostprocessTag(",
        "private static bool IsPersonalHeroJoinPartyPostprocessTag(",
    ])
    postprocess = "\n".join(extract.declaration(owner, marker) for marker in [
        "internal static string BuildPostprocessRuleTextForScene(",
        "internal static string NormalizeHeroJoinPartyPostprocessTagsForScene(",
    ])
    program = (HERE / "Harness.cs.txt").read_text("utf-8").replace("@@CONFIG@@", methods).replace("@@POSTPROCESS@@", postprocess)
    (out / "Program.cs").write_text(program, encoding="utf-8")
    dto = ROOT / "src/modules/AF.Module.Prompt/Configuration/PostprocessRuleEntry.cs"
    newtonsoft = ROOT / ".tmp/build_check/1.4/Newtonsoft.Json.dll"
    assert newtonsoft.is_file(), newtonsoft
    (out / "Test.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
        '<NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Compile Include="'
        + escape(str(dto)) + '" /><Reference Include="Newtonsoft.Json"><HintPath>'
        + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    contexts = []
    if args.token_log:
        text = args.token_log.read_text("utf-8-sig")
        headers = list(re.finditer(r"(?m)^\[(\d\d:\d\d:\d\d)\] in=\d+ out=\d+ mode=(\S+)", text))
        for index, header in enumerate(headers):
            if header[1] not in ("16:50:23", "16:51:38") or header[2] != "action_postprocess_gateway":
                continue
            block = text[header.end():headers[index + 1].start() if index + 1 < len(headers) else len(text)]
            context = block.split("OUTPUT:", 1)[0]
            assert "[Kingdoms]" in context and "ID: new_kingdom1" in context
            contexts.append(context)
        assert len(contexts) == 2, "Expected both reported clan allegiance failures"
    (out / "log-contexts.json").write_text(json.dumps(contexts, ensure_ascii=False), encoding="utf-8")
    (out / "receipt.json").write_text(json.dumps({
        "methodsSha256": hashlib.sha256((methods + postprocess).encode()).hexdigest(),
        "compatibilitySha256": hashlib.sha256((ROOT / "content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json").read_bytes()).hexdigest(),
        "privateLogSha256": hashlib.sha256(args.token_log.read_bytes()).hexdigest() if args.token_log else None,
        "scope": "Production method bodies and DTO; game state is stubbed. No provider/game/action execution."
    }, indent=2), encoding="utf-8")
    sdk = resolve_dotnet(ROOT)
    env = minimal_test_environment(sdk, out)
    result = subprocess.run([str(sdk), "run", "--project", str(out / "Test.csproj"), "--", str(ROOT), str(out / "log-contexts.json")],
                            cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    print(result.stdout + result.stderr)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
