using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000CA RID: 202
	public class PledgeAllegianceSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700059A RID: 1434
		// (get) Token: 0x0600143C RID: 5180 RVA: 0x0005DDD9 File Offset: 0x0005BFD9
		public Hero PlayerHero { get; }

		// Token: 0x1700059B RID: 1435
		// (get) Token: 0x0600143D RID: 5181 RVA: 0x0005DDE1 File Offset: 0x0005BFE1
		public bool PlayerWantsToRestore { get; }

		// Token: 0x1700059C RID: 1436
		// (get) Token: 0x0600143E RID: 5182 RVA: 0x0005DDE9 File Offset: 0x0005BFE9
		public override string SceneID
		{
			get
			{
				return "scn_pledge_allegiance_notification";
			}
		}

		// Token: 0x1700059D RID: 1437
		// (get) Token: 0x0600143F RID: 5183 RVA: 0x0005DDF0 File Offset: 0x0005BFF0
		public override TextObject TitleText
		{
			get
			{
				TextObject textObject = GameTexts.FindText("str_pledge_notification_title", null);
				textObject.SetCharacterProperties("RULER", this.PlayerHero.Clan.Kingdom.Leader.CharacterObject, false);
				textObject.SetTextVariable("PLAYER_WANTS_RESTORE", this.PlayerWantsToRestore ? 1 : 0);
				textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				textObject.SetTextVariable("YEAR", this._creationCampaignTime.GetYear);
				return textObject;
			}
		}

		// Token: 0x06001440 RID: 5184 RVA: 0x0005DE78 File Offset: 0x0005C078
		public override Banner[] GetBanners()
		{
			Banner[] array = new Banner[2];
			array[0] = Hero.MainHero.ClanBanner;
			int num = 1;
			Clan clan = this.PlayerHero.Clan.Kingdom.Leader.Clan;
			array[num] = ((clan != null) ? clan.Kingdom.Banner : null) ?? this.PlayerHero.Clan.Kingdom.Leader.ClanBanner;
			return array;
		}

		// Token: 0x06001441 RID: 5185 RVA: 0x0005DEE4 File Offset: 0x0005C0E4
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			ItemObject itemObject = null;
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment equipment = this.PlayerHero.BattleEquipment.Clone(false);
			if (equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
			{
				itemObject = CampaignSceneNotificationHelper.GetDefaultHorseItem();
				equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject, null, null, false);
			}
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.PlayerHero, equipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, true));
			Equipment equipment2 = this.PlayerHero.Clan.Kingdom.Leader.BattleEquipment.Clone(false);
			if (equipment2[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
			{
				if (itemObject == null)
				{
					itemObject = CampaignSceneNotificationHelper.GetDefaultHorseItem();
				}
				equipment2[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject, null, null, false);
			}
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.PlayerHero.Clan.Kingdom.Leader, equipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, true));
			IFaction mapFaction = this.PlayerHero.Clan.Kingdom.Leader.MapFaction;
			CultureObject culture = ((((mapFaction != null) ? mapFaction.Culture : null) != null) ? this.PlayerHero.Clan.Kingdom.Leader.MapFaction.Culture : this.PlayerHero.MapFaction.Culture);
			for (int i = 0; i < 24; i++)
			{
				CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(culture);
				Equipment equipment3 = randomTroopForCulture.FirstBattleEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment3, false, false);
				BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment3, MBRandom.RandomInt(100));
				list.Add(new SceneNotificationData.SceneNotificationCharacter(randomTroopForCulture, equipment3, bodyProperties, false, uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x06001442 RID: 5186 RVA: 0x0005E09E File Offset: 0x0005C29E
		public PledgeAllegianceSceneNotificationItem(Hero playerHero, bool playerWantsToRestore)
		{
			this.PlayerHero = playerHero;
			this.PlayerWantsToRestore = playerWantsToRestore;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x040006BD RID: 1725
		private const int NumberOfTroops = 24;

		// Token: 0x040006C0 RID: 1728
		private readonly CampaignTime _creationCampaignTime;
	}
}
