using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

// Tests production initialization data only. These are real managed game types with
// deliberately incomplete fixture owners; no Game/Campaign/model/PartyVM is created.
internal static class SelectionRegression
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private sealed class Callbacks
    {
        internal int CompletedCount;
        internal int CancelledCount;
        internal object LastRoster;
        public void Completed(object roster) { CompletedCount++; LastRoster = roster; }
        public void Cancelled() { CancelledCount++; }
    }

    internal static void Run(Assembly coup, Action<string> write)
    {
        write("SELECTION_SCOPE production datafactory fixture only; no native selection UI, Campaign or game models.");
        Assembly campaign = Assembly.Load("TaleWorlds.CampaignSystem");
        Type partyType = campaign.GetType("TaleWorlds.CampaignSystem.Party.PartyBase", true);
        Type rosterType = campaign.GetType("TaleWorlds.CampaignSystem.Roster.TroopRoster", true);
        Type itemRosterType = campaign.GetType("TaleWorlds.CampaignSystem.Roster.ItemRoster", true);
        Type characterType = campaign.GetType("TaleWorlds.CampaignSystem.CharacterObject", true);
        Type selection = coup.GetType("AnimusForge.CoupSystem.CoupTroopSelection", true);
        MethodInfo factory = selection.GetMethod("CreateInitializationData", All)
            ?? throw new MissingMethodException(selection.FullName, "CreateInitializationData");

        // PartyBase's real constructor requires Campaign.Current.GeneratePartyId. Bypass
        // that constructor explicitly for this data-binding fixture, not for game behavior.
        object owner = FormatterServices.GetUninitializedObject(partyType);
        object inventory = Activator.CreateInstance(itemRosterType);
        object realMembers = Activator.CreateInstance(rosterType, new[] { owner });
        object realPrisoners = Activator.CreateInstance(rosterType, new[] { owner });
        SetProperty(owner, "ItemRoster", inventory);
        SetProperty(owner, "MemberRoster", realMembers);
        SetProperty(owner, "PrisonRoster", realPrisoners);
        object available = rosterType.GetMethod("CreateDummyTroopRoster", All).Invoke(null, null);
        // Native TroopRoster.AddToCounts only needs IsHero=false for a detached regular
        // soldier. Do not synthesize counts or invoke any character upgrade/model code.
        object soldier = FormatterServices.GetUninitializedObject(characterType);
        int checks = 0;
        void Assert(bool result, string message)
        {
            if (!result) throw new InvalidOperationException("Selection regression failed: " + message);
            checks++;
            write("SELECTION_PASS " + message);
        }
        Assert(!(bool)GetProperty(soldier, "IsHero"), "regular CharacterObject fixture is non-hero");
        AddCount(available, soldier, 61);
        Assert(Count(available) == 61, "native detached roster AddToCounts supplies boundary counts");

        var callbacks = new Callbacks();
        Type completedType = typeof(Action<>).MakeGenericType(rosterType);
        Delegate completed = Delegate.CreateDelegate(completedType, callbacks, typeof(Callbacks).GetMethod(nameof(Callbacks.Completed)));
        Action cancelled = callbacks.Cancelled;
        object[] Arguments(object party, object roster, int limit) => new[] { party, roster, (object)limit, "selection regression", completed, cancelled };
        object data = Invoke(factory, Arguments(owner, available, 60));
        object Field(string name) => data.GetType().GetField(name, All).GetValue(data);

        Assert(ReferenceEquals(Field("RightOwnerParty"), owner), "right owner preserves supplied party context");
        Assert(ReferenceEquals(GetProperty(Field("RightOwnerParty"), "ItemRoster"), inventory), "right owner ItemRoster can be dereferenced");
        Assert(ReferenceEquals(Field("RightLeaderHero"), GetProperty(owner, "LeaderHero")), "right leader metadata matches owner (null in fixture)");
        Assert(Field("LeftOwnerParty") == null, "left side has no real owner");
        string[] rosterFields = { "LeftMemberRoster", "RightMemberRoster", "LeftPrisonerRoster", "RightPrisonerRoster" };
        object[] editingRosters = rosterFields.Select(Field).ToArray();
        Assert(editingRosters.All(r => r != null), "all four editing rosters exist");
        Assert(editingRosters.Distinct().Count() == 4, "all four editing rosters are independent");
        Assert(editingRosters.All(r => !ReferenceEquals(r, realMembers) && !ReferenceEquals(r, realPrisoners)), "editing rosters never alias owner member/prisoner rosters");
        Assert(editingRosters.All(r => GetProperty(r, "OwnerParty") == null), "editing rosters remain native dummy rosters");
        Assert(ReferenceEquals(Field("LeftMemberRoster"), available), "available dummy roster is preserved");
        Assert((bool)Field("IsTroopUpgradesDisabled") && (bool)Field("DoNotApplyGoldTransactions"), "upgrade and gold transactions are disabled");
        Assert(Field("CanTalkToTroopDelegate") == null && !(bool)Field("ShowProgressBar"), "no troop dialogue or quest progress owner is introduced");
        Assert(Field("MemberTransferState").ToString() == "Transferable"
            && Field("PrisonerTransferState").ToString() == "NotTransferable"
            && Field("AccompanyingTransferState").ToString() == "NotTransferable", "only regular member selection is transferable");

        void Reject(object[] arguments, Type exceptionType, string message)
        {
            Exception caught = null;
            try { Invoke(factory, arguments); } catch (Exception ex) { caught = ex; }
            Assert(caught != null && exceptionType.IsInstanceOfType(caught), message);
        }
        Reject(Arguments(null, available, 60), typeof(ArgumentException), "null owner rejected");
        Reject(Arguments(FormatterServices.GetUninitializedObject(partyType), available, 60), typeof(ArgumentException), "owner without ItemRoster rejected");
        Reject(Arguments(owner, null, 60), typeof(ArgumentException), "null available roster rejected");
        Reject(Arguments(owner, realMembers, 60), typeof(ArgumentException), "real owner member roster rejected");
        Reject(Arguments(owner, realPrisoners, 60), typeof(ArgumentException), "real owner prisoner roster rejected");
        Reject(Arguments(owner, available, 0), typeof(ArgumentOutOfRangeException), "zero limit rejected");
        Reject(Arguments(owner, available, -1), typeof(ArgumentOutOfRangeException), "negative limit rejected");

        object selected = Field("RightMemberRoster");
        var condition = (Delegate)Field("PartyPresentationDoneButtonConditionDelegate");
        bool CanComplete()
        {
            object result = condition.DynamicInvoke(Field("LeftMemberRoster"), Field("LeftPrisonerRoster"), selected,
                Field("RightPrisonerRoster"), 61, 60);
            return (bool)GetProperty(result, "Item1");
        }
        Assert(Count(selected) == 0 && !CanComplete(), "Done rejects empty selection");
        AddCount(selected, soldier, 60);
        Assert(CanComplete(), "Done accepts exactly the selection limit");
        AddCount(selected, soldier, 1);
        Assert(!CanComplete(), "Done rejects limit plus one");
        AddCount(selected, soldier, -1);

        var done = (Delegate)Field("PartyPresentationDoneButtonDelegate");
        object resultDone = done.DynamicInvoke(available, Field("LeftPrisonerRoster"), selected,
            Field("RightPrisonerRoster"), null, null, false, null, owner);
        Assert((bool)resultDone && Count(realMembers) == 0 && Count(realPrisoners) == 0
            && Count(available) == 61 && Count(selected) == 60, "Done callback approves without transferring real or dummy troops");
        Assert(callbacks.CompletedCount == 0 && callbacks.CancelledCount == 0, "Done callback does not dispatch closed-result early");

        var closed = (Delegate)Field("PartyScreenClosedDelegate");
        closed.DynamicInvoke(null, available, Field("LeftPrisonerRoster"), owner, selected, Field("RightPrisonerRoster"), false);
        Assert(callbacks.CompletedCount == 1 && callbacks.CancelledCount == 0 && ReferenceEquals(callbacks.LastRoster, selected), "normal close returns selected dummy roster");
        closed.DynamicInvoke(null, available, Field("LeftPrisonerRoster"), owner, selected, Field("RightPrisonerRoster"), true);
        Assert(callbacks.CompletedCount == 1 && callbacks.CancelledCount == 1, "cancel close dispatches only cancellation");
        Assert(Count(realMembers) == 0 && Count(realPrisoners) == 0 && ReferenceEquals(GetProperty(owner, "ItemRoster"), inventory), "callbacks preserve owner roster and inventory references");
        write("PASS selection datafactory regression assertions=" + checks);
        write("SELECTION_LIMITATION native PartyVM.InitializeUpgrades/transfer arrows and reset UI were not executed; requires in-game verification.");
    }

    private static object GetProperty(object target, string property) => target.GetType().GetProperty(property, All).GetValue(target);
    private static void SetProperty(object target, string property, object value) => target.GetType().GetProperty(property, All).GetSetMethod(true).Invoke(target, new[] { value });
    private static int Count(object roster) => (int)GetProperty(roster, "TotalManCount");
    private static void AddCount(object roster, object character, int count) => roster.GetType().GetMethod("AddToCounts", All)
        .Invoke(roster, new object[] { character, count, false, 0, 0, true, -1 });
    private static object Invoke(MethodInfo factory, object[] arguments)
    {
        try { return factory.Invoke(null, arguments); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }
}
