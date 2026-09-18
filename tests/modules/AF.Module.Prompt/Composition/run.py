"""Build and run the Prompt Composition contract against the production owner files.

SDK resolution: AF_DOTNET env var, then repository local/dotnet/8.0.425, then dotnet on PATH.
Exit code is the harness exit code; mutation switches prove the assertions are live.
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent

parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["sticky-limit", "router-excluded"], help="apply a source mutation that must fail")
args = parser.parse_args()


def resolve_dotnet() -> Path:
    candidates = [os.environ.get("AF_DOTNET", ""), str(ROOT / "local/dotnet/8.0.425/dotnet.exe"), shutil.which("dotnet") or ""]
    for candidate in candidates:
        if candidate and Path(candidate).exists():
            return Path(candidate)
    raise SystemExit("NOT-RUN: no dotnet SDK found (set AF_DOTNET)")


dotnet = resolve_dotnet()
output = ROOT / "artifacts/tests/prompt-j04-composition" / (args.mutate or "current")
if output.exists():
    shutil.rmtree(output)
output.mkdir(parents=True)
for name in ("Program.cs", "Stubs.cs", "PromptCompositionTests.csproj"):
    shutil.copy(HERE / name, output / name)
project = (output / "PromptCompositionTests.csproj").read_text(encoding="utf-8").replace("../../../../", (str(ROOT) + "/").replace("\\", "/"))
if args.mutate:
    src_dir = output / "mutated"
    src_dir.mkdir()
    if args.mutate == "sticky-limit":
        text = (ROOT / "src/modules/AF.Module.Prompt/Composition/BuiltInRuleStickyCarry.cs").read_text(encoding="utf-8")
        assert text.count('case "loan":\n\t\t\treturn 3;') == 1
        (src_dir / "BuiltInRuleStickyCarry.cs").write_text(text.replace('case "loan":\n\t\t\treturn 3;', 'case "loan":\n\t\t\treturn 2;'), encoding="utf-8")
        project = project.replace(str(ROOT).replace("\\", "/") + "/src/modules/AF.Module.Prompt/Composition/BuiltInRuleStickyCarry.cs", str(src_dir / "BuiltInRuleStickyCarry.cs").replace("\\", "/"))
    elif args.mutate == "router-excluded":
        text = (ROOT / "src/modules/AF.Module.Prompt/Composition/PromptBuiltInTopicRouter.cs").read_text(encoding="utf-8")
        needle = "if (!allowRulePreprocess || !topicEnabled || PromptRuleIdPolicy.IsExcluded(excludedRuleIds, ruleTag))"
        assert text.count(needle) == 1
        (src_dir / "PromptBuiltInTopicRouter.cs").write_text(text.replace(needle, "if (!allowRulePreprocess || !topicEnabled)"), encoding="utf-8")
        project = project.replace(str(ROOT).replace("\\", "/") + "/src/modules/AF.Module.Prompt/Composition/PromptBuiltInTopicRouter.cs", str(src_dir / "PromptBuiltInTopicRouter.cs").replace("\\", "/"))
(output / "PromptCompositionTests.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1", DOTNET_MULTILEVEL_LOOKUP="0")
build = subprocess.run([str(dotnet), "build", str(output / "PromptCompositionTests.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")], cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode != 0:
    print(build.stdout[-3000:])
    raise SystemExit("build failed: " + str(build.returncode))
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/PromptCompositionTests.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(output / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
print((run.stdout + run.stderr).strip()[-2000:])
sys.exit(run.returncode)
