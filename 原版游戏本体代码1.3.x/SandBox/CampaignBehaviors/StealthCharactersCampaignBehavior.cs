using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000DD RID: 221
	public class StealthCharactersCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000ABB RID: 2747 RVA: 0x0004FF78 File Offset: 0x0004E178
		public override void RegisterEvents()
		{
			CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
		}

		// Token: 0x06000ABC RID: 2748 RVA: 0x0004FF94 File Offset: 0x0004E194
		private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedPoints)
		{
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			if (settlement.IsHideout)
			{
				return;
			}
			Location location = settlement.LocationComplex.GetListOfLocations().First<Location>();
			int num;
			if (unusedPoints.TryGetValue("stealth_agent", out num) && num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateStealthCharacter), settlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
			if (unusedPoints.TryGetValue("stealth_agent_forced", out num) && num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreteForcedStealthCharacter), settlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
			if (unusedPoints.TryGetValue("disguise_default_agent", out num) && num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateDisguiseDefaultCharacter), settlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
			if (unusedPoints.TryGetValue("disguise_officer_agent", out num) && num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateDisguiseOfficerCharacter), settlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
			if (unusedPoints.TryGetValue("disguise_shadow_agent", out num) && num > 0)
			{
				location.AddLocationCharacters(new CreateLocationCharacterDelegate(this.CreateDisguiseShadowTargetCharacter), settlement.Culture, LocationCharacter.CharacterRelations.Enemy, num);
			}
		}

		// Token: 0x06000ABD RID: 2749 RVA: 0x000500A7 File Offset: 0x0004E2A7
		private LocationCharacter CreateStealthCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			return this.CreateStealthAgentInternal("stealth_agent", "stealth_character");
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x000500B9 File Offset: 0x0004E2B9
		private LocationCharacter CreteForcedStealthCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			LocationCharacter locationCharacter = this.CreateStealthAgentInternal("stealth_agent_forced", "stealth_character");
			locationCharacter.ForceSpawnInSpecialTargetTag = true;
			return locationCharacter;
		}

		// Token: 0x06000ABF RID: 2751 RVA: 0x000500D2 File Offset: 0x0004E2D2
		private LocationCharacter CreateDisguiseDefaultCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			return this.CreateStealthAgentInternal("disguise_default_agent", "disguise_default_character");
		}

		// Token: 0x06000AC0 RID: 2752 RVA: 0x000500E4 File Offset: 0x0004E2E4
		private LocationCharacter CreateDisguiseOfficerCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			return this.CreateStealthAgentInternal("disguise_officer_agent", "disguise_officer_character");
		}

		// Token: 0x06000AC1 RID: 2753 RVA: 0x000500F6 File Offset: 0x0004E2F6
		private LocationCharacter CreateDisguiseShadowTargetCharacter(CultureObject culture, LocationCharacter.CharacterRelations relation)
		{
			return this.CreateStealthAgentInternal("disguise_shadow_agent", "disguise_shadow_target");
		}

		// Token: 0x06000AC2 RID: 2754 RVA: 0x00050108 File Offset: 0x0004E308
		private LocationCharacter CreateStealthAgentInternal(string spawnTag, string characterId)
		{
			CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>(characterId);
			int minValue;
			int maxValue;
			Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(@object, out minValue, out maxValue, "");
			return new LocationCharacter(new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement_slow")).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors), spawnTag, true, LocationCharacter.CharacterRelations.Enemy, null, true, false, null, false, false, true, null, false);
		}

		// Token: 0x06000AC3 RID: 2755 RVA: 0x0005019D File Offset: 0x0004E39D
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
