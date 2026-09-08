"""Execute extracted production cutover blocks against deterministic boundary stubs."""
from __future__ import annotations

import argparse
import hashlib
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SCENE_LIFECYCLE_DISPATCHES = {
    "SCENE_PRIMARY_RELEASE_DELEGATE": "scene_primary_first_release",
    "SCENE_HOLD_PARTICIPANTS_DELEGATE": "scene_relay_hold_participants",
    "SCENE_BATTLE_SUPPRESSION_DELEGATE": "battle_speech_followup_suppression",
    "SCENE_IDLE_TIMEOUT_DELEGATE": "scene_relay_idle_timeout",
    "SCENE_FAILURE_RELEASE_DELEGATE": "scene_relay_failure_release",
}


def source(path: str, ref: str | None) -> str:
    if ref:
        return subprocess.check_output(
            ["git", "show", f"{ref}:{path}"], cwd=ROOT
        ).decode("utf-8-sig").replace("\r\n", "\n")
    return (ROOT / path).read_text(encoding="utf-8-sig")


def declaration(text: str, signature: str, optional: bool = False) -> str:
    """Extract a declaration by brace depth, ignoring strings and comments."""
    start = text.find(signature)
    if start < 0:
        if optional:
            return ""
        raise ValueError(f"Missing source declaration: {signature}")
    opening = text.index("{", start)
    tokens = re.compile(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/|[{}]')
    depth = 0
    for match in tokens.finditer(text, opening):
        token = match.group()
        if token == "{":
            depth += 1
        elif token == "}":
            depth -= 1
            if depth == 0:
                return text[start:match.end()]
    raise ValueError(f"Unterminated source declaration: {signature}")


def extract(ref: str | None) -> dict[str, str]:
    scene = source("ShoutBehavior.cs", ref)
    anchor = scene.index('"[MemoryPerf] group_turn_prompt_ready')
    begin = scene.index('string output = "";', anchor)
    end = scene.index("apiSw.Stop();", begin)
    courier = source("CourierDeliveryBehavior.cs", ref)
    method = declaration(courier, "private async Task PrepareAndGenerateCourierReplyOffMainThreadAsync(")
    begin_c = method.index("if (IsCourierBridgeEnabled()")
    # The same await also appears inside the fallback lambda; select the last.
    last_await = "await GenerateNpcReplyAsync(request).ConfigureAwait(false);"
    end_c = method.rindex(last_await) + len(last_await)
    contracts = source("Refactor/Contracts/InteractionContracts.cs", ref)
    tail_begin = scene.index("relayPostprocessSelected = !suppressBattleSpeechFollowups", end)
    tail_end = scene.index(";", scene.index("bool flag11 =", tail_begin)) + 1
    tail_decisions = scene[tail_begin:tail_end]
    selected_names = sorted(set(re.findall(r"\b\w+PostprocessSelected\b", tail_decisions)) - {"relayPostprocessSelected"})
    speech_begin = scene.index(";", scene.index("bool flag10 = endRequested", end)) + 1
    speech_end = scene.index("if (!string.IsNullOrWhiteSpace(cleaned))", speech_begin)
    prompt_contracts = [
        "public enum InteractionChannel", "public sealed class InteractionIdentity",
        "public sealed class TraceContext", "public sealed class InteractionCandidate",
        "public sealed class GameInteractionSnapshot", "public sealed class InteractionEnvelope",
        "public sealed class DetachedPromptSections", "public sealed class DetachedPostprocessPromptSections",
        "public sealed class RuleSelection", "public sealed class CapabilitySet",
        "public sealed class PromptMessage", "public sealed class PromptPackage",
        "public sealed class PostprocessContext", "public sealed class ActionRequest",
        "public sealed class ActionPlan", "public sealed class FactRecord", "public sealed class InteractionResult",
        "public interface IPromptPackageComposer",
        "public interface IActionPostprocessor", "internal static class ContractGuard",
        "internal static class ContractCollections",
    ]
    blocks = {
        "SCENE_BLOCK": scene[begin:end],
        "COURIER_BLOCK": method[begin_c:end_c],
        "FAIL_METHOD": declaration(courier, "private void FailCourierReplyGenerationOnMainThread("),
        "FINALIZE_METHOD": declaration(courier, "private void FinalizeCourierReplyGenerationOnMainThread("),
        "DETACHED_FAIL_METHOD": declaration(courier, "private void FailDetachedCourierReplyOnMainThread(", optional=True),
        "STATUS_ENUM": declaration(source("Refactor/Contracts/InteractionContracts.cs", ref), "public enum InteractionStatus"),
        "RESULT_TYPE": declaration(source("Refactor/Runtime/DetachedInteractionHost.cs", ref), "public sealed class DetachedInteractionHostResult"),
        "PROMPT_CONTRACTS": "\n\n".join(declaration(contracts, item) for item in prompt_contracts),
        "POSTPROCESS_INTERFACE": declaration(source("Refactor/Contracts/LlmContracts.cs", ref), "public interface IPostprocessPromptComposer"),
        "PORTS_TYPE": declaration(source("Refactor/Adapters/LegacyInteractionPipelineComposition.cs", ref), "public sealed class LegacyInteractionPipelinePorts"),
        "MAIN_COMPOSER": declaration(source("Refactor/Adapters/LegacyDetachedPromptComposer.cs", ref), "public sealed class LegacyDetachedPromptComposer"),
        "POSTPROCESS_COMPOSER": declaration(source("Refactor/Adapters/LegacyDetachedPostprocessPromptComposer.cs", ref), "public sealed class LegacyDetachedPostprocessPromptComposer"),
        "LEGACY_PROMPT_ADAPTER": declaration(source("Refactor/Adapters/LegacyPromptPackageAdapter.cs", ref), "public static class LegacyPromptPackageAdapter"),
        "ACTION_PARSER": declaration(source("Refactor/Adapters/LegacyActionTagParser.cs", ref), "public sealed class LegacyActionTagParser"),
        "BUILD_PROMPT": declaration(source("Refactor/Adapters/LegacyConfiguredChatGateway.cs", ref), "internal static PromptPackage BuildPromptPackage("),
        "CREATE_MESSAGE": declaration(scene, "private static object CreateChatMessage("),
        "PUBLIC_SCENE_FACTORY": declaration(scene, "public static LegacyInteractionPipelinePorts CreateSceneShoutDetachedPortsForExternal("),
        "PRIVATE_SCENE_FACTORY": declaration(scene, "private static LegacyInteractionPipelinePorts CreateSceneShoutDetachedPorts(", optional=True),
        "MAIN_REPLY_FACTORY": declaration(scene, "private static LegacyInteractionPipelinePorts CreateSceneShoutMainReplyPorts(", optional=True),
        "MAIN_REPLY_METHOD": declaration(scene, "private async Task<DetachedInteractionHostResult> GenerateSceneShoutMainReplyAsync(", optional=True),
        "MAIN_FALLBACK_METHOD": declaration(scene, "private static async Task<DetachedInteractionHostResult> RunDetachedRefactorFallbackAsync("),
        "SCENE_HISTORY_METHOD": declaration(scene, "private Task<bool> RecordSceneReplyHistoryOnMainThreadAsync(", optional=True),
        "SCENE_TAIL_DECISIONS": tail_decisions,
        # Synthetic fixture inputs only; the decision expressions above remain verbatim production.
        "SCENE_TAIL_LOCALS": "\n".join(f'bool {name} = ruleName == "{name}";' for name in selected_names),
        "SCENE_DIRECT_REPLY_ASSIGNMENT": re.search(r"bool replyIsDirectPlayerResponse = firstTurn;", scene[end:]).group(),
        "BATTLE_QUEUE_METHOD": declaration(source("extensions/AnimusForge.XihaiAction/src/CoreProject/BattleSpeechFrameworkV2.cs", ref),
                                           "public static bool ShouldQueueOrdinaryScenePostprocess("),
        "SCENE_MAIN_SPEECH_SANITIZER": declaration(scene, "private static string PrepareSceneMainReplySpeechText(", optional=True),
        "SCENE_MAIN_SPEECH_QUEUE": declaration(scene, "private Task<bool> QueueSceneMainReplyOnMainThreadAsync(", optional=True),
        "SCENE_SPEECH_SANITIZATION_BLOCK": scene[speech_begin:speech_end],
        "SCENE_SPEECH_STRIPPERS": "\n\n".join(declaration(scene, signature) for signature in (
            "private static bool ContainsAutoGroupEndSignal(", "private static string StripAutoGroupStopSignal(",
            "private static string StripAutoGroupRelaySignal(", "private static string StripActionTagsForSceneSpeech(")),
        "GIVE_ASSET_CODEC": "\n\n".join(declaration(source("GiveAssetTagCodec.cs", ref), signature) for signature in (
            "internal readonly struct GiveAssetTag", "internal static class GiveAssetTagCodec")),
    }
    group_method = declaration(scene, "private async Task HandleGroupResponsePerHeroIndependent(")
    for name, operation in SCENE_LIFECYCLE_DISPATCHES.items():
        dispatch_start = group_method.index('RunNativeConversationMainThreadFuncAsync("' + operation + '"')
        delegate_start = group_method.index("delegate", dispatch_start)
        blocks[name] = declaration(group_method[delegate_start:], "delegate")
    return blocks


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-ref", help="Read immutable Git source instead of the working tree.")
    parser.add_argument("--dotnet", help="Path to dotnet; otherwise use DOTNET_ROOT or PATH.")
    parser.add_argument("--output-name", default="current", help="A single name under .tmp/channel-cutover-boundary.")
    parser.add_argument("--newtonsoft", type=Path, default=ROOT / ".tmp/nuget-packages/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll",
                        help="Existing Newtonsoft.Json assembly for the production anonymous-message adapter; no package download.")
    args = parser.parse_args()
    dotnet_root = os.environ.get("DOTNET_ROOT")
    root_dotnet = Path(dotnet_root) / ("dotnet.exe" if os.name == "nt" else "dotnet") if dotnet_root else None
    dotnet = args.dotnet or (str(root_dotnet) if root_dotnet and root_dotnet.is_file() else shutil.which("dotnet"))
    if not dotnet or not Path(dotnet).is_file():
        parser.error("Could not find the .NET SDK launcher; pass --dotnet <full-path-to-dotnet>.")
    dotnet = str(Path(dotnet).resolve())
    if not re.fullmatch(r"[A-Za-z0-9_-]+", args.output_name):
        parser.error("--output-name must contain only letters, digits, underscore or hyphen")
    if not args.newtonsoft.is_file():
        parser.error("Pass --newtonsoft <path-to-existing-Newtonsoft.Json.dll>.")
    blocks = extract(args.source_ref)
    template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
    for name, block in blocks.items():
        placeholder = "@@" + name + "@@"
        if template.count(placeholder) != 1:
            raise ValueError(f"Expected exactly one {placeholder}")
        template = template.replace(placeholder, block)
    output = ROOT / ".tmp" / "channel-cutover-boundary" / args.output_name
    output.mkdir(parents=True, exist_ok=True)
    (output / "Program.cs").write_text(template, encoding="utf-8")
    shutil.copyfile(args.newtonsoft, output / "Newtonsoft.Json.dll")
    (output / "Boundary.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
        '<Nullable>disable</Nullable>'
        '<RestoreSources></RestoreSources></PropertyGroup>'
        '<ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>Newtonsoft.Json.dll</HintPath></Reference></ItemGroup>'
        '</Project>', encoding="utf-8"
    )
    (output / "NuGet.Config").write_text(
        '<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8"
    )
    fingerprints = [f"{key} sha256={hashlib.sha256(value.encode()).hexdigest()}" for key, value in blocks.items()]
    metadata = "source=" + (args.source_ref or "working-tree") + "\n" + "\n".join(fingerprints)
    (output / "source-fingerprints.txt").write_text(metadata + "\n", encoding="utf-8")
    print(metadata, flush=True)
    env = os.environ.copy()
    env["DOTNET_ROOT"] = str(Path(dotnet).parent)
    env["DOTNET_CLI_HOME"] = str(ROOT / ".tmp" / "dotnet-cli")
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["DOTNET_NOLOGO"] = "1"
    env["DOTNET_CLI_UI_LANGUAGE"] = "en"
    result = subprocess.run(
        [dotnet, "run", "--project", str(output / "Boundary.csproj"), "--configuration", "Release"],
        cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120
    )
    log = metadata + "\n" + result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    print(result.stdout, end="")
    print(result.stderr, end="", file=sys.stderr)
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
