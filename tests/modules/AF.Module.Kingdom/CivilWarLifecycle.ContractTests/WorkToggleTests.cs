using System.Reflection;
using AnimusForge;
using TaleWorlds.CampaignSystem.Actions;

internal static class WorkToggleTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Drain(KingdomCivilWarOwner owner) { for (int i = 0; i < 100; i++) owner.ProcessPending(); }

        var f = new Fixture();
        var work = ((IEnumerable<bool>)typeof(KingdomCivilWarOwner).GetMethod("EvaluatePoliticalKingdom", flags)!
            .Invoke(f.Owner, new object[] { f.Home.StringId, new HashSet<string> { "daily" } })!).GetEnumerator();
        Check(work.MoveNext(), "production political iterator suspended after first record");
        var field = typeof(KingdomCivilWarOwner).GetField("_politicalWork", flags)!;
        field.SetValue(f.Owner, work);
        DuelSettings.Enabled = false;
        Drain(f.Owner);
        Check(f.State.Factions.Count == 1 && ChangeKingdomAction.Moves == 0, "disabled active job cannot settle or transfer clans");
        Check(f.Owner.LastBatchRecords == 0 && ReferenceEquals(field.GetValue(f.Owner), work), "disabled job preserves iterator without polling or disposing it");
        DuelSettings.Enabled = true;
        Drain(f.Owner);
        Check(f.State.Factions.Count == 0 && ChangeKingdomAction.Moves == 3, "reenabling resumes the same settlement exactly once");
        Drain(f.Owner);
        Check(ChangeKingdomAction.Moves == 3, "resumed settlement never repeats");

        f = new Fixture();
        f.Owner.NotifyPoliticalChange(f.Home, "daily");
        DuelSettings.Enabled = false;
        Drain(f.Owner);
        Check(f.State.Factions.Count == 1 && ChangeKingdomAction.Moves == 0, "disabled queued work performs no political effects");
        var queue = (System.Collections.ICollection)typeof(KingdomCivilWarOwner).GetField("_politicalQueue", flags)!.GetValue(f.Owner)!;
        Check(queue.Count == 1, "disabled queue retains its event");
        DuelSettings.Enabled = true;
        Drain(f.Owner);
        Check(f.State.Factions.Count == 0, "retained event completes after reenabling");
        return checks;
    }
}
