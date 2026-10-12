"""Static key/consumer closure evidence. C# lifecycle execution is deferred."""
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
helper = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Kingdom/KingdomStabilityUiText.cs"
source = helper.read_text(encoding="utf-8-sig")
assert not re.search(r"[\u3400-\u9fff]", source)
assert "KingdomStabilityPolicy.GetKingdomStabilityTier(value)" in source
english, chinese = {}, {}
for path in (ROOT / "content/modules").rglob("*.xml"):
    if "Languages" not in path.parts:
        continue
    target = chinese if "CNs" in path.parts else english
    target.update({n.get("id"): n.get("text") for n in ET.parse(path).iter("string")})
keys = re.findall(r'\{=([A-Za-z0-9_]+)\}([^"\n]+)', source)
assert len(keys) == 7
for key, fallback in keys:
    assert english[key] == fallback and chinese[key], key
lab = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors/KingdomStabilityLabController.cs").read_text(encoding="utf-8-sig")
assert lab.count("KingdomStabilityUiText.Tier(") == 4
assert "_port.GetKingdomStabilityTierText(" not in lab
for filename in ("KingdomRebellionEditorController.cs", "EventEditorProjection.cs"):
    assert "KingdomStabilityUiText.Tier(" in (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Editors" / filename).read_text(encoding="utf-8-sig")
for project in (
    "tests/modules/AF.Module.Weekly/EventEditorLifecycleTests/EventEditorLifecycleTests.csproj",
    "tests/AF.Persistence/PlayerExports/PlayerExportsTests.csproj",
):
    for include in ET.parse(ROOT / project).iter("Compile"):
        path = (ROOT / project).parent / include.get("Include")
        assert path.exists(), path
    assert "KingdomStabilityUiText.cs" in (ROOT / project).read_text(encoding="utf-8-sig")
print("PASS stability editor: 7 registered tier keys, 6 UI consumers, fixture source closures; C# execution NOT-RUN")
