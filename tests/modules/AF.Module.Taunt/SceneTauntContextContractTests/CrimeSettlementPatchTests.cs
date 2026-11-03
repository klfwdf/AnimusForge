using AnimusForge;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

internal static class CrimeSettlementPatchTests
{
    internal static void Run()
    {
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label);
            checks++;
        }
        var faction = new Faction { StringId = "battania", MainHeroCrimeRating = 25f };
        var ledger = new SceneTauntPenaltyLedgerOwner();
        SceneTauntBehavior.Ledger = ledger;
        Campaign.Current = new Campaign { Models = new Models { CrimeModel = new CrimeModel() } };
        ledger.QueueDeferredCrime("aserai", 17f);
        foreach (var method in new[] { CrimeModel.PaymentMethod.Gold, CrimeModel.PaymentMethod.Influence,
            CrimeModel.PaymentMethod.Punishment, CrimeModel.PaymentMethod.Execution, CrimeModel.PaymentMethod.ExMachina })
        {
            ledger.QueueDeferredCrime("battania", 50f);
            Patch_SceneTaunt_CrimeSettlement.Postfix(faction, method, true);
            Check(ledger.GetDeferredCrime("battania") == 0f, "completed native sentence retained crime: " + method);
            Check(ledger.GetDeferredCrime("aserai") == 17f, "unrelated faction cleared");
        }
        ledger.QueueDeferredCrime("battania", 50f);
        Patch_SceneTaunt_CrimeSettlement.Postfix(faction, CrimeModel.PaymentMethod.Gold, false);
        Check(ledger.GetDeferredCrime("battania") == 50f, "skipped native payment cleared crime");
        foreach (var method in new[] { (CrimeModel.PaymentMethod)0, (CrimeModel.PaymentMethod)0x2000 })
        {
            Patch_SceneTaunt_CrimeSettlement.Postfix(faction, method, true);
            Check(ledger.GetDeferredCrime("battania") == 50f, "unknown payment method cleared crime");
        }
        faction.MainHeroCrimeRating = 100f;
        Patch_SceneTaunt_CrimeSettlement.Postfix(faction, CrimeModel.PaymentMethod.Gold, true);
        Check(ledger.GetDeferredCrime("battania") == 50f, "unsettled crime cleared without completed punishment");
        faction.MainHeroCrimeRating = float.NaN;
        Patch_SceneTaunt_CrimeSettlement.Postfix(faction, CrimeModel.PaymentMethod.Gold, true);
        Check(ledger.GetDeferredCrime("battania") == 50f, "invalid native rating treated as successful payment");
        faction.MainHeroCrimeRating = 25f;
        Campaign.Current = null;
        Patch_SceneTaunt_CrimeSettlement.Postfix(faction, CrimeModel.PaymentMethod.Gold, true);
        Check(ledger.GetDeferredCrime("battania") == 50f, "missing Campaign cleared crime");
        Campaign.Current = new Campaign { Models = new Models { CrimeModel = new CrimeModel() } };
        Patch_SceneTaunt_CrimeSettlement.Postfix(null, CrimeModel.PaymentMethod.Gold, true);
        Check(ledger.GetDeferredCrime("battania") == 50f, "null faction cleared crime");
        Console.WriteLine($"{checks}/{checks} native sentence adapter cases passed; production postfix with explicit game/ledger boundary fixtures; live payment NOT_RUN");
    }
}

// The real postfix and ledger are linked; only Bannerlord/Harmony boundaries are fixtures.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
}
namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
    public sealed class CrimeModel
    {
        [Flags] public enum PaymentMethod : uint { Gold=1, Influence=2, Punishment=4, Execution=8, ExMachina=0x1000 }
        public float GetCrimeRatingAfterPunishment() => 25f;
    }
}
namespace TaleWorlds.CampaignSystem
{
    public interface IFaction { string StringId { get; } float MainHeroCrimeRating { get; } }
    public sealed class Faction : IFaction { public string StringId { get; set; } public float MainHeroCrimeRating { get; set; } }
    public sealed class Campaign { public static Campaign Current { get; set; } public Models Models { get; set; } }
    public sealed class Models { public CrimeModel CrimeModel { get; set; } }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class PayForCrimeAction { public static void Apply(IFaction faction, CrimeModel.PaymentMethod method) { } }
}
namespace AnimusForge
{
    internal static class SceneTauntBehavior
    {
        internal static SceneTauntPenaltyLedgerOwner Ledger;
        internal static float ClearDeferredCrimeForExternal(IFaction faction, string reason) => Ledger.ClearDeferredCrime(faction.StringId);
    }
    internal static class Logger { internal static void Log(string category, string message) { } }
}
