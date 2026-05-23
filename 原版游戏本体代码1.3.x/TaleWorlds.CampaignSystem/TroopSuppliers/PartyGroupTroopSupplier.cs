using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TroopSuppliers
{
	// Token: 0x020000AF RID: 175
	public class PartyGroupTroopSupplier : IMissionTroopSupplier
	{
		// Token: 0x17000530 RID: 1328
		// (get) Token: 0x06001370 RID: 4976 RVA: 0x0005A261 File Offset: 0x00058461
		// (set) Token: 0x06001371 RID: 4977 RVA: 0x0005A269 File Offset: 0x00058469
		internal MapEventSide PartyGroup { get; private set; }

		// Token: 0x06001372 RID: 4978 RVA: 0x0005A274 File Offset: 0x00058474
		public PartyGroupTroopSupplier(MapEvent mapEvent, BattleSideEnum side, FlattenedTroopRoster priorTroops = null, Func<UniqueTroopDescriptor, MapEventParty, bool> customAllocationConditions = null)
		{
			this._customAllocationConditions = customAllocationConditions;
			this.PartyGroup = mapEvent.GetMapEventSide(side);
			this._isPlayerSide = mapEvent.PlayerSide == side;
			this._initialTroopCount = this.PartyGroup.TroopCount;
			this.PartyGroup.MakeReadyForMission(priorTroops);
			this._nextTroopRank = 0;
		}

		// Token: 0x06001373 RID: 4979 RVA: 0x0005A2D8 File Offset: 0x000584D8
		public IEnumerable<IAgentOriginBase> SupplyTroops(int numberToAllocate)
		{
			List<UniqueTroopDescriptor> list = null;
			this.PartyGroup.AllocateTroops(ref list, numberToAllocate, this._customAllocationConditions);
			PartyGroupAgentOrigin[] array = new PartyGroupAgentOrigin[list.Count];
			this._numAllocated += list.Count;
			for (int i = 0; i < array.Length; i++)
			{
				PartyGroupAgentOrigin[] array2 = array;
				int num = i;
				UniqueTroopDescriptor descriptor = list[i];
				int nextTroopRank = this._nextTroopRank;
				this._nextTroopRank = nextTroopRank + 1;
				array2[num] = new PartyGroupAgentOrigin(this, descriptor, nextTroopRank);
			}
			if (array.Length < numberToAllocate)
			{
				this._anyTroopRemainsToBeSupplied = false;
			}
			return array;
		}

		// Token: 0x06001374 RID: 4980 RVA: 0x0005A358 File Offset: 0x00058558
		public IAgentOriginBase SupplyOneTroop()
		{
			UniqueTroopDescriptor uniqueTroopDescriptor;
			if (this.PartyGroup.AllocateTroop(this._customAllocationConditions, out uniqueTroopDescriptor))
			{
				UniqueTroopDescriptor descriptor = uniqueTroopDescriptor;
				int nextTroopRank = this._nextTroopRank;
				this._nextTroopRank = nextTroopRank + 1;
				IAgentOriginBase result = new PartyGroupAgentOrigin(this, descriptor, nextTroopRank);
				this._anyTroopRemainsToBeSupplied = this._anyTroopRemainsToBeSupplied && this.PartyGroup.HasReadyTroops;
				return result;
			}
			this._anyTroopRemainsToBeSupplied = false;
			return null;
		}

		// Token: 0x06001375 RID: 4981 RVA: 0x0005A3B8 File Offset: 0x000585B8
		public IEnumerable<IAgentOriginBase> GetAllTroops()
		{
			List<UniqueTroopDescriptor> list = null;
			this.PartyGroup.GetAllTroops(ref list);
			PartyGroupAgentOrigin[] array = new PartyGroupAgentOrigin[list.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new PartyGroupAgentOrigin(this, list[i], i);
			}
			return array;
		}

		// Token: 0x06001376 RID: 4982 RVA: 0x0005A400 File Offset: 0x00058600
		public BasicCharacterObject GetGeneralCharacter()
		{
			return this.PartyGroup.LeaderParty.General;
		}

		// Token: 0x17000531 RID: 1329
		// (get) Token: 0x06001377 RID: 4983 RVA: 0x0005A412 File Offset: 0x00058612
		public int NumRemovedTroops
		{
			get
			{
				return this._numWounded + this._numKilled + this._numRouted;
			}
		}

		// Token: 0x17000532 RID: 1330
		// (get) Token: 0x06001378 RID: 4984 RVA: 0x0005A428 File Offset: 0x00058628
		public int NumTroopsNotSupplied
		{
			get
			{
				return this._initialTroopCount - this._numAllocated;
			}
		}

		// Token: 0x17000533 RID: 1331
		// (get) Token: 0x06001379 RID: 4985 RVA: 0x0005A437 File Offset: 0x00058637
		public bool AnyTroopRemainsToBeSupplied
		{
			get
			{
				return this._anyTroopRemainsToBeSupplied;
			}
		}

		// Token: 0x0600137A RID: 4986 RVA: 0x0005A440 File Offset: 0x00058640
		public int GetNumberOfPlayerControllableTroops()
		{
			int num = 0;
			foreach (MapEventParty mapEventParty in this.PartyGroup.Parties)
			{
				PartyBase party = mapEventParty.Party;
				if (PartyBase.IsPartyUnderPlayerCommand(party) || (party.Side == PartyBase.MainParty.Side && this.PartyGroup.MapEvent.IsPlayerSergeant()))
				{
					num += party.NumberOfHealthyMembers;
				}
			}
			return num;
		}

		// Token: 0x0600137B RID: 4987 RVA: 0x0005A4D0 File Offset: 0x000586D0
		public void OnTroopWounded(UniqueTroopDescriptor troopDescriptor)
		{
			this._numWounded++;
			this.PartyGroup.OnTroopWounded(troopDescriptor);
		}

		// Token: 0x0600137C RID: 4988 RVA: 0x0005A4EC File Offset: 0x000586EC
		public void OnTroopKilled(UniqueTroopDescriptor troopDescriptor)
		{
			this._numKilled++;
			this.PartyGroup.OnTroopKilled(troopDescriptor);
		}

		// Token: 0x0600137D RID: 4989 RVA: 0x0005A508 File Offset: 0x00058708
		public void OnTroopRouted(UniqueTroopDescriptor troopDescriptor, bool isOrderRetreat)
		{
			this._numRouted++;
			this.PartyGroup.OnTroopRouted(troopDescriptor, isOrderRetreat);
		}

		// Token: 0x0600137E RID: 4990 RVA: 0x0005A525 File Offset: 0x00058725
		internal CharacterObject GetTroop(UniqueTroopDescriptor troopDescriptor)
		{
			return this.PartyGroup.GetAllocatedTroop(troopDescriptor) ?? this.PartyGroup.GetReadyTroop(troopDescriptor);
		}

		// Token: 0x0600137F RID: 4991 RVA: 0x0005A544 File Offset: 0x00058744
		public PartyBase GetParty(UniqueTroopDescriptor troopDescriptor)
		{
			PartyBase partyBase = this.PartyGroup.GetAllocatedTroopParty(troopDescriptor);
			if (partyBase == null)
			{
				partyBase = this.PartyGroup.GetReadyTroopParty(troopDescriptor);
			}
			return partyBase;
		}

		// Token: 0x06001380 RID: 4992 RVA: 0x0005A56F File Offset: 0x0005876F
		public void OnTroopScoreHit(UniqueTroopDescriptor descriptor, BasicCharacterObject attackedCharacter, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
		{
			this.PartyGroup.OnTroopScoreHit(descriptor, (CharacterObject)attackedCharacter, damage, isFatal, isTeamKill, attackerWeapon, false);
		}

		// Token: 0x0400065C RID: 1628
		private readonly int _initialTroopCount;

		// Token: 0x0400065D RID: 1629
		private int _numAllocated;

		// Token: 0x0400065E RID: 1630
		private int _numWounded;

		// Token: 0x0400065F RID: 1631
		private int _numKilled;

		// Token: 0x04000660 RID: 1632
		private int _numRouted;

		// Token: 0x04000661 RID: 1633
		private bool _isPlayerSide;

		// Token: 0x04000662 RID: 1634
		private Func<UniqueTroopDescriptor, MapEventParty, bool> _customAllocationConditions;

		// Token: 0x04000663 RID: 1635
		private bool _anyTroopRemainsToBeSupplied = true;

		// Token: 0x04000664 RID: 1636
		private int _nextTroopRank;
	}
}
