using AnimusForge;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
void Reset()
{
    Game.Current = new Game();
    Campaign.Current = new Campaign();
    Hero.MainHero = new Hero();
    Mission.Current = null;
    ScreenProbe.Reset();
    TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ThrowAfterPayment = false;
}
var village = new Settlement();
village.Village.VillageType.Productions.Add(Tuple.Create(new ItemObject("grain"), 60f));
Reset();
var batch = new SetsBattleRewardBatch();
var troop = new CharacterObject();
var agent = new Agent { Index = 9, Character = troop };
batch.RecordDefeatedReserve(agent, AgentState.Unconscious, true);
batch.RecordDefeatedReserve(agent, AgentState.Unconscious, true);
Check(batch.Prisoners.TotalManCount == 1 && batch.Items.Count == 1, "duplicate casualty cannot duplicate prisoners/equipment");
Check(!batch.TrySettle(village) && ScreenProbe.PartyOpens == 1, "prisoner interface opens first");
Check(!batch.TrySettle(village) && ScreenProbe.InventoryOpens == 0, "inventory cannot open behind prisoner interface");
Game.Current.GameStateManager.ActiveState = new MapState();
Check(!batch.TrySettle(village) && ScreenProbe.InventoryOpens == 1, "inventory follows closed prisoner interface");
Check(!batch.TrySettle(village), "aftermath waits for closed inventory");
Game.Current.GameStateManager.ActiveState = new MapState();
Check(batch.TrySettle(village) && batch.TrySettle(village), "completed batch permits handoff without another reward screen");
Check(ScreenProbe.PartyOpens == 1 && ScreenProbe.InventoryOpens == 1, "both interfaces open exactly once");

Reset();
batch = new SetsBattleRewardBatch();
batch.RecordDefeatedReserve(agent, AgentState.Killed, true);
Check(batch.Prisoners.TotalManCount == 0 && batch.Items.Count == 1, "killed reserve never becomes a prisoner");
ScreenProbe.FailBeforePush = true;
Check(!batch.TrySettle(village) && !batch.Ledger.ItemsPresented, "failed UI open retains pending reward");
ScreenProbe.FailBeforePush = false;
ScreenProbe.FailAfterPush = true;
Check(!batch.TrySettle(village) && batch.Ledger.ItemsPresented, "exception after actual UI push cannot reopen rewards");
ScreenProbe.FailAfterPush = false;
Game.Current.GameStateManager.ActiveState = new MapState();
Check(batch.TrySettle(village) && ScreenProbe.InventoryOpens == 1, "post-push failure resumes after closed UI without duplicate");

Reset();
batch = new SetsBattleRewardBatch();
batch.RecordDefeatedReserve(agent, AgentState.Killed, false);
batch.RecordDefeatedReserve(new Agent { Index = 1, Character = new CharacterObject { IsHero = true } }, AgentState.Unconscious, true);
Check(batch.Items.Count == 0 && batch.Prisoners.TotalManCount == 0, "unrelated troops and hero lifecycle do not generate rewards");
Check(batch.TrySettle(village) && ScreenProbe.PartyOpens == 0 && ScreenProbe.InventoryOpens == 0, "empty rewards do not show empty interfaces");

Reset();
batch = new SetsBattleRewardBatch();
TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ThrowAfterPayment = true;
try { batch.TryPrepareVillageSupplies(village); } catch (InvalidOperationException) { }
Check(Hero.MainHero.Gold == 240 && batch.VillageGoldPaid == 240, "payment receipt survives notification exception");
TaleWorlds.CampaignSystem.Actions.GiveGoldAction.ThrowAfterPayment = false;
Check(batch.TryPrepareVillageSupplies(village), "partial supply settlement can resume");
Check(Hero.MainHero.Gold == 240 && batch.Items.TotalCount == 60, "resumed supply settlement does not repay/regenerate goods");
float hitPoints = village.SettlementHitPoints;
Check(batch.TryPrepareVillageSupplies(village) && village.SettlementHitPoints == hitPoints, "village damage is not repeated");
Check(Math.Abs(hitPoints - 0.2f) < 0.0001f, "original village damage ratio is preserved");
var restored = new SetsBattleRewardBatch { Items = batch.Items, Prisoners = batch.Prisoners };
restored.Ledger.RestorePresentation(true, true);
Check(restored.TrySettle(village) && ScreenProbe.InventoryOpens == 0, "restored presented rewards do not replay inventory");
Console.WriteLine($"PASS {checks}: actual production reward batch with native UI/model substitutes; not game acceptance.");
