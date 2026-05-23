using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.BarterSystem
{
	// Token: 0x02000473 RID: 1139
	public class BarterData
	{
		// Token: 0x17000E3D RID: 3645
		// (get) Token: 0x06004801 RID: 18433 RVA: 0x0016C82F File Offset: 0x0016AA2F
		public IFaction OffererMapFaction
		{
			get
			{
				Hero offererHero = this.OffererHero;
				return ((offererHero != null) ? offererHero.MapFaction : null) ?? this.OffererParty.MapFaction;
			}
		}

		// Token: 0x17000E3E RID: 3646
		// (get) Token: 0x06004802 RID: 18434 RVA: 0x0016C852 File Offset: 0x0016AA52
		public IFaction OtherMapFaction
		{
			get
			{
				Hero otherHero = this.OtherHero;
				return ((otherHero != null) ? otherHero.MapFaction : null) ?? this.OtherParty.MapFaction;
			}
		}

		// Token: 0x17000E3F RID: 3647
		// (get) Token: 0x06004803 RID: 18435 RVA: 0x0016C875 File Offset: 0x0016AA75
		public bool IsAiBarter { get; }

		// Token: 0x06004804 RID: 18436 RVA: 0x0016C880 File Offset: 0x0016AA80
		public BarterData(Hero offerer, Hero other, PartyBase offererParty, PartyBase otherParty, BarterManager.BarterContextInitializer contextInitializer = null, int persuasionCostReduction = 0, bool isAiBarter = false)
		{
			this.OffererParty = offererParty;
			this.OtherParty = otherParty;
			this.OffererHero = offerer;
			this.OtherHero = other;
			this.ContextInitializer = contextInitializer;
			this.PersuasionCostReduction = persuasionCostReduction;
			this._barterables = new List<Barterable>(16);
			this._barterGroups = Campaign.Current.Models.DiplomacyModel.GetBarterGroups().ToList<BarterGroup>();
			this.IsAiBarter = isAiBarter;
		}

		// Token: 0x06004805 RID: 18437 RVA: 0x0016C8F4 File Offset: 0x0016AAF4
		public void AddBarterable<T>(Barterable barterable, bool isContextDependent = false)
		{
			foreach (BarterGroup barterGroup in this._barterGroups)
			{
				if (barterGroup is T)
				{
					barterable.Initialize(barterGroup, isContextDependent);
					this._barterables.Add(barterable);
					break;
				}
			}
		}

		// Token: 0x06004806 RID: 18438 RVA: 0x0016C960 File Offset: 0x0016AB60
		public void AddBarterGroup(BarterGroup barterGroup)
		{
			this._barterGroups.Add(barterGroup);
		}

		// Token: 0x06004807 RID: 18439 RVA: 0x0016C96E File Offset: 0x0016AB6E
		public List<BarterGroup> GetBarterGroups()
		{
			return this._barterGroups;
		}

		// Token: 0x06004808 RID: 18440 RVA: 0x0016C976 File Offset: 0x0016AB76
		public List<Barterable> GetBarterables()
		{
			return this._barterables;
		}

		// Token: 0x06004809 RID: 18441 RVA: 0x0016C980 File Offset: 0x0016AB80
		public BarterGroup GetBarterGroup<T>()
		{
			IEnumerable<T> source = this._barterGroups.OfType<T>();
			if (source.IsEmpty<T>())
			{
				return null;
			}
			return source.First<T>() as BarterGroup;
		}

		// Token: 0x0600480A RID: 18442 RVA: 0x0016C9B3 File Offset: 0x0016ABB3
		public List<Barterable> GetOfferedBarterables()
		{
			return (from barterable in this.GetBarterables()
				where barterable.IsOffered
				select barterable).ToList<Barterable>();
		}

		// Token: 0x040013DF RID: 5087
		public readonly Hero OffererHero;

		// Token: 0x040013E0 RID: 5088
		public readonly Hero OtherHero;

		// Token: 0x040013E1 RID: 5089
		public readonly PartyBase OffererParty;

		// Token: 0x040013E2 RID: 5090
		public readonly PartyBase OtherParty;

		// Token: 0x040013E3 RID: 5091
		private List<Barterable> _barterables;

		// Token: 0x040013E4 RID: 5092
		private List<BarterGroup> _barterGroups;

		// Token: 0x040013E5 RID: 5093
		public readonly BarterManager.BarterContextInitializer ContextInitializer;

		// Token: 0x040013E6 RID: 5094
		public readonly int PersuasionCostReduction;
	}
}
