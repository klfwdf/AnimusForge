using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000E8 RID: 232
	public class GameMenuOption
	{
		// Token: 0x170005E7 RID: 1511
		// (get) Token: 0x06001598 RID: 5528 RVA: 0x00061AFD File Offset: 0x0005FCFD
		// (set) Token: 0x06001599 RID: 5529 RVA: 0x00061B05 File Offset: 0x0005FD05
		public GameMenu.MenuAndOptionType Type { get; private set; }

		// Token: 0x170005E8 RID: 1512
		// (get) Token: 0x0600159A RID: 5530 RVA: 0x00061B0E File Offset: 0x0005FD0E
		// (set) Token: 0x0600159B RID: 5531 RVA: 0x00061B16 File Offset: 0x0005FD16
		public GameMenuOption.LeaveType OptionLeaveType { get; set; }

		// Token: 0x170005E9 RID: 1513
		// (get) Token: 0x0600159C RID: 5532 RVA: 0x00061B1F File Offset: 0x0005FD1F
		// (set) Token: 0x0600159D RID: 5533 RVA: 0x00061B27 File Offset: 0x0005FD27
		public GameMenuOption.IssueQuestFlags OptionQuestData { get; set; }

		// Token: 0x170005EA RID: 1514
		// (get) Token: 0x0600159E RID: 5534 RVA: 0x00061B30 File Offset: 0x0005FD30
		// (set) Token: 0x0600159F RID: 5535 RVA: 0x00061B38 File Offset: 0x0005FD38
		public string IdString { get; private set; }

		// Token: 0x170005EB RID: 1515
		// (get) Token: 0x060015A0 RID: 5536 RVA: 0x00061B41 File Offset: 0x0005FD41
		// (set) Token: 0x060015A1 RID: 5537 RVA: 0x00061B49 File Offset: 0x0005FD49
		public TextObject Text { get; private set; }

		// Token: 0x170005EC RID: 1516
		// (get) Token: 0x060015A2 RID: 5538 RVA: 0x00061B52 File Offset: 0x0005FD52
		// (set) Token: 0x060015A3 RID: 5539 RVA: 0x00061B5A File Offset: 0x0005FD5A
		public TextObject Text2 { get; private set; }

		// Token: 0x170005ED RID: 1517
		// (get) Token: 0x060015A4 RID: 5540 RVA: 0x00061B63 File Offset: 0x0005FD63
		// (set) Token: 0x060015A5 RID: 5541 RVA: 0x00061B6B File Offset: 0x0005FD6B
		public TextObject Tooltip { get; private set; }

		// Token: 0x170005EE RID: 1518
		// (get) Token: 0x060015A6 RID: 5542 RVA: 0x00061B74 File Offset: 0x0005FD74
		// (set) Token: 0x060015A7 RID: 5543 RVA: 0x00061B7C File Offset: 0x0005FD7C
		public bool IsLeave { get; private set; }

		// Token: 0x170005EF RID: 1519
		// (get) Token: 0x060015A8 RID: 5544 RVA: 0x00061B85 File Offset: 0x0005FD85
		// (set) Token: 0x060015A9 RID: 5545 RVA: 0x00061B8D File Offset: 0x0005FD8D
		public bool IsRepeatable { get; private set; }

		// Token: 0x170005F0 RID: 1520
		// (get) Token: 0x060015AA RID: 5546 RVA: 0x00061B96 File Offset: 0x0005FD96
		// (set) Token: 0x060015AB RID: 5547 RVA: 0x00061B9E File Offset: 0x0005FD9E
		public bool IsEnabled { get; private set; }

		// Token: 0x170005F1 RID: 1521
		// (get) Token: 0x060015AC RID: 5548 RVA: 0x00061BA7 File Offset: 0x0005FDA7
		// (set) Token: 0x060015AD RID: 5549 RVA: 0x00061BAF File Offset: 0x0005FDAF
		public object RelatedObject { get; private set; }

		// Token: 0x060015AE RID: 5550 RVA: 0x00061BB8 File Offset: 0x0005FDB8
		internal GameMenuOption()
		{
			this.Text = null;
			this.Tooltip = null;
			this.IsEnabled = true;
		}

		// Token: 0x060015AF RID: 5551 RVA: 0x00061BD8 File Offset: 0x0005FDD8
		public GameMenuOption(GameMenu.MenuAndOptionType type, string idString, TextObject text, TextObject text2, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, bool isLeave = false, bool isRepeatable = false, object relatedObject = null)
		{
			this.Type = type;
			this.IdString = idString;
			this.Text = text;
			this.Text2 = text2;
			this.OnCondition = condition;
			this.OnConsequence = consequence;
			this.Tooltip = null;
			this.IsRepeatable = isRepeatable;
			this.IsEnabled = true;
			this.IsLeave = isLeave;
			this.RelatedObject = relatedObject;
		}

		// Token: 0x060015B0 RID: 5552 RVA: 0x00061C40 File Offset: 0x0005FE40
		public bool GetConditionsHold(Game game, MenuContext menuContext)
		{
			if (this.OnCondition != null)
			{
				MenuCallbackArgs menuCallbackArgs = new MenuCallbackArgs(menuContext, this.Text);
				bool result = this.OnCondition(menuCallbackArgs);
				this.IsEnabled = menuCallbackArgs.IsEnabled;
				this.Tooltip = menuCallbackArgs.Tooltip;
				this.OptionQuestData = menuCallbackArgs.OptionQuestData;
				this.OptionLeaveType = menuCallbackArgs.optionLeaveType;
				return result;
			}
			return true;
		}

		// Token: 0x060015B1 RID: 5553 RVA: 0x00061CA0 File Offset: 0x0005FEA0
		public void RunConsequence(MenuContext menuContext)
		{
			if (this.OnConsequence != null)
			{
				MenuCallbackArgs args = new MenuCallbackArgs(menuContext, this.Text);
				this.OnConsequence(args);
			}
			menuContext.OnConsequence(this);
		}

		// Token: 0x060015B2 RID: 5554 RVA: 0x00061CD5 File Offset: 0x0005FED5
		public void SetEnable(bool isEnable)
		{
			this.IsEnabled = isEnable;
		}

		// Token: 0x04000717 RID: 1815
		public static GameMenuOption.IssueQuestFlags[] IssueQuestFlagsValues = (GameMenuOption.IssueQuestFlags[])Enum.GetValues(typeof(GameMenuOption.IssueQuestFlags));

		// Token: 0x0400071F RID: 1823
		public GameMenuOption.OnConditionDelegate OnCondition;

		// Token: 0x04000720 RID: 1824
		public GameMenuOption.OnConsequenceDelegate OnConsequence;

		// Token: 0x02000562 RID: 1378
		// (Invoke) Token: 0x06004CE2 RID: 19682
		public delegate bool OnConditionDelegate(MenuCallbackArgs args);

		// Token: 0x02000563 RID: 1379
		// (Invoke) Token: 0x06004CE6 RID: 19686
		public delegate void OnConsequenceDelegate(MenuCallbackArgs args);

		// Token: 0x02000564 RID: 1380
		public enum LeaveType
		{
			// Token: 0x040016C2 RID: 5826
			Default,
			// Token: 0x040016C3 RID: 5827
			Mission,
			// Token: 0x040016C4 RID: 5828
			Submenu,
			// Token: 0x040016C5 RID: 5829
			BribeAndEscape,
			// Token: 0x040016C6 RID: 5830
			Escape,
			// Token: 0x040016C7 RID: 5831
			Craft,
			// Token: 0x040016C8 RID: 5832
			ForceToGiveGoods,
			// Token: 0x040016C9 RID: 5833
			ForceToGiveTroops,
			// Token: 0x040016CA RID: 5834
			Bribe,
			// Token: 0x040016CB RID: 5835
			LeaveTroopsAndFlee,
			// Token: 0x040016CC RID: 5836
			OrderTroopsToAttack,
			// Token: 0x040016CD RID: 5837
			Raid,
			// Token: 0x040016CE RID: 5838
			HostileAction,
			// Token: 0x040016CF RID: 5839
			Recruit,
			// Token: 0x040016D0 RID: 5840
			Trade,
			// Token: 0x040016D1 RID: 5841
			Wait,
			// Token: 0x040016D2 RID: 5842
			Leave,
			// Token: 0x040016D3 RID: 5843
			Continue,
			// Token: 0x040016D4 RID: 5844
			Manage,
			// Token: 0x040016D5 RID: 5845
			TroopSelection,
			// Token: 0x040016D6 RID: 5846
			WaitQuest,
			// Token: 0x040016D7 RID: 5847
			Surrender,
			// Token: 0x040016D8 RID: 5848
			Conversation,
			// Token: 0x040016D9 RID: 5849
			DefendAction,
			// Token: 0x040016DA RID: 5850
			Devastate,
			// Token: 0x040016DB RID: 5851
			Pillage,
			// Token: 0x040016DC RID: 5852
			ShowMercy,
			// Token: 0x040016DD RID: 5853
			Leaderboard,
			// Token: 0x040016DE RID: 5854
			OpenStash,
			// Token: 0x040016DF RID: 5855
			ManageGarrison,
			// Token: 0x040016E0 RID: 5856
			StagePrisonBreak,
			// Token: 0x040016E1 RID: 5857
			ManagePrisoners,
			// Token: 0x040016E2 RID: 5858
			Ransom,
			// Token: 0x040016E3 RID: 5859
			PracticeFight,
			// Token: 0x040016E4 RID: 5860
			BesiegeTown,
			// Token: 0x040016E5 RID: 5861
			SneakIn,
			// Token: 0x040016E6 RID: 5862
			LeadAssault,
			// Token: 0x040016E7 RID: 5863
			DonateTroops,
			// Token: 0x040016E8 RID: 5864
			DonatePrisoners,
			// Token: 0x040016E9 RID: 5865
			SiegeAmbush,
			// Token: 0x040016EA RID: 5866
			Warehouse,
			// Token: 0x040016EB RID: 5867
			VisitPort,
			// Token: 0x040016EC RID: 5868
			SetSail,
			// Token: 0x040016ED RID: 5869
			ManageFleet,
			// Token: 0x040016EE RID: 5870
			CallFleet,
			// Token: 0x040016EF RID: 5871
			OrderShipsToAttack,
			// Token: 0x040016F0 RID: 5872
			RepairShips
		}

		// Token: 0x02000565 RID: 1381
		[Flags]
		public enum IssueQuestFlags
		{
			// Token: 0x040016F2 RID: 5874
			None = 0,
			// Token: 0x040016F3 RID: 5875
			AvailableIssue = 1,
			// Token: 0x040016F4 RID: 5876
			ActiveIssue = 2,
			// Token: 0x040016F5 RID: 5877
			ActiveStoryQuest = 4,
			// Token: 0x040016F6 RID: 5878
			TrackedIssue = 8,
			// Token: 0x040016F7 RID: 5879
			TrackedStoryQuest = 16
		}
	}
}
