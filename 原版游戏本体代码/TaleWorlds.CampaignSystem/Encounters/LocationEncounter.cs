using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Encounters
{
	// Token: 0x020002E9 RID: 745
	public class LocationEncounter
	{
		// Token: 0x17000A06 RID: 2566
		// (get) Token: 0x06002964 RID: 10596 RVA: 0x000ABF8A File Offset: 0x000AA18A
		public Settlement Settlement { get; }

		// Token: 0x17000A07 RID: 2567
		// (get) Token: 0x06002965 RID: 10597 RVA: 0x000ABF92 File Offset: 0x000AA192
		// (set) Token: 0x06002966 RID: 10598 RVA: 0x000ABF9A File Offset: 0x000AA19A
		public List<AccompanyingCharacter> CharactersAccompanyingPlayer { get; private set; }

		// Token: 0x06002967 RID: 10599 RVA: 0x000ABFA3 File Offset: 0x000AA1A3
		protected LocationEncounter(Settlement settlement)
		{
			this.Settlement = settlement;
			this.CharactersAccompanyingPlayer = new List<AccompanyingCharacter>();
		}

		// Token: 0x06002968 RID: 10600 RVA: 0x000ABFC0 File Offset: 0x000AA1C0
		public void AddAccompanyingCharacter(LocationCharacter locationCharacter, bool isFollowing = false)
		{
			if (!this.CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter.Character == locationCharacter.Character))
			{
				AccompanyingCharacter item = new AccompanyingCharacter(locationCharacter, isFollowing);
				this.CharactersAccompanyingPlayer.Add(item);
			}
		}

		// Token: 0x06002969 RID: 10601 RVA: 0x000AC00C File Offset: 0x000AA20C
		public AccompanyingCharacter GetAccompanyingCharacter(LocationCharacter locationCharacter)
		{
			return this.CharactersAccompanyingPlayer.Find((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter);
		}

		// Token: 0x0600296A RID: 10602 RVA: 0x000AC040 File Offset: 0x000AA240
		public AccompanyingCharacter GetAccompanyingCharacter(CharacterObject character)
		{
			return this.CharactersAccompanyingPlayer.Find(delegate(AccompanyingCharacter x)
			{
				LocationCharacter locationCharacter = x.LocationCharacter;
				return ((locationCharacter != null) ? locationCharacter.Character : null) == character;
			});
		}

		// Token: 0x0600296B RID: 10603 RVA: 0x000AC074 File Offset: 0x000AA274
		public void RemoveAccompanyingCharacter(LocationCharacter locationCharacter)
		{
			if (this.CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter))
			{
				AccompanyingCharacter item = this.CharactersAccompanyingPlayer.Find((AccompanyingCharacter x) => x.LocationCharacter == locationCharacter);
				this.CharactersAccompanyingPlayer.Remove(item);
			}
		}

		// Token: 0x0600296C RID: 10604 RVA: 0x000AC0CC File Offset: 0x000AA2CC
		public void RemoveAccompanyingCharacter(Hero hero)
		{
			for (int i = this.CharactersAccompanyingPlayer.Count - 1; i >= 0; i--)
			{
				if (this.CharactersAccompanyingPlayer[i].LocationCharacter.Character.IsHero && this.CharactersAccompanyingPlayer[i].LocationCharacter.Character.HeroObject == hero)
				{
					this.CharactersAccompanyingPlayer.Remove(this.CharactersAccompanyingPlayer[i]);
					return;
				}
			}
		}

		// Token: 0x0600296D RID: 10605 RVA: 0x000AC145 File Offset: 0x000AA345
		public void RemoveAllAccompanyingCharacters()
		{
			this.CharactersAccompanyingPlayer.Clear();
		}

		// Token: 0x0600296E RID: 10606 RVA: 0x000AC152 File Offset: 0x000AA352
		public void OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
		{
			if ((fromLocation == CampaignMission.Current.Location && toLocation == null) || (fromLocation == null && toLocation == CampaignMission.Current.Location))
			{
				CampaignMission.Current.OnCharacterLocationChanged(locationCharacter, fromLocation, toLocation);
			}
		}

		// Token: 0x0600296F RID: 10607 RVA: 0x000AC181 File Offset: 0x000AA381
		public virtual bool IsWorkshopLocation(Location location)
		{
			return false;
		}

		// Token: 0x06002970 RID: 10608 RVA: 0x000AC184 File Offset: 0x000AA384
		public virtual bool IsTavern(Location location)
		{
			return false;
		}

		// Token: 0x06002971 RID: 10609 RVA: 0x000AC187 File Offset: 0x000AA387
		public virtual IMission CreateAndOpenMissionController(Location nextLocation, Location previousLocation = null, CharacterObject talkToChar = null, string playerSpecialSpawnTag = null)
		{
			return null;
		}

		// Token: 0x04000BE9 RID: 3049
		public bool IsInsideOfASettlement;
	}
}
