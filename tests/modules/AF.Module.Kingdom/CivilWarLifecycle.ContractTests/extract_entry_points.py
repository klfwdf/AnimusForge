"""Compile exact production entry methods with fake campaign context; never rewrite their bodies."""
from pathlib import Path
import sys
import re

root = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(root / "tests"))
from output_isolation import current_source_path

def method(path, signature):
    text = current_source_path(root, path).read_text(encoding="utf-8-sig")
    start = re.search(r"(?m)^[ \t]*" + re.escape(signature), text).start()
    # These methods end at the class-member indentation. Braces inside strings are immaterial.
    end = text.index("\n\t}\n", start) + len("\n\t}")
    return text[start:end].replace("private ", "internal ", 1)

parts = ["using System; using System.Collections.Generic; using System.Linq; using System.Text.RegularExpressions; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.Election; using AnimusForge.Refactor.Modules; namespace AnimusForge;"]
for cls, path, signature in [
    ("static partial class ShoutBehavior", "ShoutBehavior.cs", "private static string NormalizeKingdomServicePostprocessTagsForScene("),
    ("static partial class ConversationActionPostprocessOwner", "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs", "internal static string NormalizeKingdomServicePostprocessTagsForScene("),
    ("static partial class AIConfigHandler", "AIConfigHandler.cs", "public static List<PostprocessRuleEntry> BuildRuntimeKingdomServicePostprocessRules("),
    ("partial class CivilWarCampaignBehavior", "src/modules/AF.Module.Kingdom/CivilWar/CivilWarCampaignBehavior.cs", "private void OnKingdomDecisionConcluded("),
    ("partial class CivilWarCampaignBehavior", "src/modules/AF.Module.Kingdom/CivilWar/CivilWarCampaignBehavior.cs", "private void OnDailyTick("),
]:
    parts.append(cls + " {\n" + method(path, signature) + "\n}")
target = Path(sys.argv[1])
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text("\n".join(parts), encoding="utf-8")
