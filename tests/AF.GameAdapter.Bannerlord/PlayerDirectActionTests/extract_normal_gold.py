"""Link the complete normal reward gold method, not the EconomyReplay subset."""
from pathlib import Path
import importlib.util
import sys

root = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("declaration", root / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (root / "src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "internal int TransferGold(Hero giver, Hero receiver, int amount, bool forceComplete)")
output = Path(sys.argv[1])
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("using System; using TaleWorlds.CampaignSystem; namespace AnimusForge { public partial class RewardSystemBehavior {\n" + method + "\n}}", encoding="utf-8")
