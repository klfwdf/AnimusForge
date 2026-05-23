using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E8 RID: 1000
	internal class DisorganizedStateCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003D83 RID: 15747 RVA: 0x0010BFAC File Offset: 0x0010A1AC
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnd));
			CampaignEvents.PartyRemovedFromArmyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyRemovedFromArmy));
			CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, new Action<GameMenu, GameMenuOption>(this.OnGameMenuOptionSelected));
		}

		// Token: 0x06003D84 RID: 15748 RVA: 0x0010C018 File Offset: 0x0010A218
		private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
		{
			if (this._checkForEvent && (gameMenuOption.IdString == "str_order_attack" || gameMenuOption.IdString == "attack"))
			{
				foreach (MapEventParty mapEventParty in MobileParty.MainParty.MapEvent.DefenderSide.Parties)
				{
					if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(mapEventParty.Party))
					{
						mapEventParty.Party.MobileParty.SetDisorganized(true);
					}
				}
			}
		}

		// Token: 0x06003D85 RID: 15749 RVA: 0x0010C0D0 File Offset: 0x0010A2D0
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.IsSallyOut)
			{
				if (!mapEvent.AttackerSide.IsMainPartyAmongParties())
				{
					using (List<MapEventParty>.Enumerator enumerator = mapEvent.DefenderSide.Parties.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							MapEventParty mapEventParty = enumerator.Current;
							if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(mapEventParty.Party))
							{
								mapEventParty.Party.MobileParty.SetDisorganized(true);
							}
						}
						return;
					}
				}
				this._checkForEvent = true;
			}
		}

		// Token: 0x06003D86 RID: 15750 RVA: 0x0010C16C File Offset: 0x0010A36C
		private void OnPartyRemovedFromArmy(MobileParty mobileParty)
		{
			if (Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(mobileParty.Party))
			{
				mobileParty.SetDisorganized(true);
			}
		}

		// Token: 0x06003D87 RID: 15751 RVA: 0x0010C194 File Offset: 0x0010A394
		private void OnMapEventEnd(MapEvent mapEvent)
		{
			bool flag;
			if (mapEvent.AttackerSide.Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart) == mapEvent.AttackerSide.Parties.Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers))
			{
				flag = mapEvent.DefenderSide.Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart) != mapEvent.DefenderSide.Parties.Sum((MapEventParty x) => x.Party.NumberOfHealthyMembers);
			}
			else
			{
				flag = true;
			}
			if (flag && !mapEvent.IsHideoutBattle)
			{
				foreach (PartyBase partyBase in mapEvent.InvolvedParties)
				{
					if (partyBase.IsActive)
					{
						MobileParty mobileParty = partyBase.MobileParty;
						if ((mobileParty == null || !mobileParty.IsMainParty || !mapEvent.DiplomaticallyFinished || !mapEvent.AttackerSide.MapFaction.IsAtWarWith(mapEvent.DefenderSide.MapFaction)) && (!mapEvent.IsSallyOut || partyBase.MapEventSide.MissionSide == BattleSideEnum.Defender) && Campaign.Current.Models.PartyImpairmentModel.CanGetDisorganized(partyBase) && (mapEvent.RetreatingSide == BattleSideEnum.None || mapEvent.RetreatingSide != partyBase.Side))
						{
							partyBase.MobileParty.SetDisorganized(true);
						}
					}
				}
			}
			this._checkForEvent = false;
		}

		// Token: 0x06003D88 RID: 15752 RVA: 0x0010C350 File Offset: 0x0010A550
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<bool>("_checkForEvent", ref this._checkForEvent);
		}

		// Token: 0x0400128C RID: 4748
		private bool _checkForEvent;
	}
}
