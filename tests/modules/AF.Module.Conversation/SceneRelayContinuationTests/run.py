"""Replay current production relay boundaries, with detached network/agent fixtures."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment, resolve_dotnet

HERE = Path(__file__).resolve().parent
SCENE = "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs"
OWNER = "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs"

def declaration(text, signature):
    start = text.index(signature)
    opening = text.index("{", start)
    depth = 0
    for m in re.finditer(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/|[{}]', text[opening:]):
        if m.group() == "{": depth += 1
        elif m.group() == "}":
            depth -= 1
            if not depth: return text[start:opening + m.end()]
    raise ValueError("Unterminated declaration: " + signature)

def source(path, ref):
    if ref:
        return subprocess.check_output(["git", "show", ref + ":" + path], cwd=ROOT).decode("utf-8-sig")
    return (ROOT / path).read_text(encoding="utf-8-sig")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dotnet")
    parser.add_argument("--source-ref")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    scene = source(SCENE, args.source_ref)
    owner = source(OWNER, args.source_ref)
    method = declaration(scene, "internal async Task HandleGroupResponsePerHeroIndependent(")
    setup_begin = method.index("bool firstTurn = true;")
    loop_open = method.index("{", method.index("while (currentSpeaker != null", setup_begin))
    admission_begin = method.index("if (!IsSceneConversationEpochCurrent(conversationEpoch))", loop_open)
    admission_end = method.index("engagedAgentIndices.Add(currentSpeaker.AgentIndex);", admission_begin)
    eligibility = declaration(method, "if (!suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0)")
    eligibility_end = method.index(";", method.index("relayPostprocessSelected = !suppressBattleSpeechFollowups", admission_end)) + 1
    candidate_begin = method.index("if (!suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0)")
    selection_begin = method.index("if (relayPostprocessSelected)", method.index("Task<ScenePostprocessOutcome> postprocessTask"))
    tail_begin = method.index("if (!string.IsNullOrWhiteSpace(cleaned) && relayRequested)", selection_begin)
    tail_end = method.index("firstTurn = false;", tail_begin) + len("firstTurn = false;")
    snippets = {
        "SETUP": method[setup_begin:loop_open + 1],
        "ADMISSION": method[admission_begin:admission_end],
        "CANDIDATES": method[candidate_begin:eligibility_end],
        "SELECTION": declaration(method[selection_begin:], "if (relayPostprocessSelected)"),
        "TAIL": method[tail_begin:tail_end],
        "NORMALIZER": declaration(owner, "internal static string NormalizeAutoGroupRelayPostprocessTagsForScene("),
        "PROMPT": declaration(owner, "internal static string BuildSceneRelayTargetListForPostprocess("),
        "RULES": declaration(owner, "internal static List<PostprocessRuleEntry> BuildAutoGroupRelayPostprocessRulesForScene("),
        "CONSTANT": re.search(r"internal const int AUTO_GROUP_CHAT_MAX_LINES = \d+;", source("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs", args.source_ref)).group(),
    }
    output = new_run_root(ROOT, "scene-relay-continuation", args.run_root)
    harness = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
    for key, value in snippets.items(): harness = harness.replace("@@" + key + "@@", value)
    if "@@" in harness: raise ValueError("Unexpanded harness placeholder")
    (output / "Program.cs").write_text(harness, encoding="utf-8")
    linked = "".join('<Compile Include="' + str(ROOT / path).replace("\\", "/") + '" />' for path in (
        "AnimusForge.SiegeAftermathIntervention/SiegeNpcResponseEventBudget.cs",
        "AnimusForge.SiegeAftermathIntervention/SiegeNpcResponseLimitProfile.cs"))
    (output / "Relay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + linked + '</ItemGroup></Project>', encoding="utf-8")
    (output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
    fingerprints = {key: hashlib.sha256(value.encode()).hexdigest() for key, value in snippets.items()}
    fingerprints.update(source_ref=args.source_ref or "working-tree", boundary_scope="production snippets; synthetic network/agent context; real GCCZ budget")
    (output / "source-fingerprints.json").write_text(json.dumps(fingerprints, indent=2), encoding="utf-8")
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "Relay.csproj"), "-c", "Release"], cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    log = result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(log)
    return result.returncode

if __name__ == "__main__": raise SystemExit(main())
