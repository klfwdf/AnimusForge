using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B5 RID: 181
	public class DeclareDragonBannerSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000541 RID: 1345
		// (get) Token: 0x060013A8 RID: 5032 RVA: 0x0005B4DA File Offset: 0x000596DA
		public bool PlayerWantsToRestore { get; }

		// Token: 0x17000542 RID: 1346
		// (get) Token: 0x060013A9 RID: 5033 RVA: 0x0005B4E2 File Offset: 0x000596E2
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_declare_dragon_banner";
			}
		}

		// Token: 0x17000543 RID: 1347
		// (get) Token: 0x060013AA RID: 5034 RVA: 0x0005B4EC File Offset: 0x000596EC
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("PLAYER_WANTS_RESTORE", this.PlayerWantsToRestore ? 1 : 0);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_declare_dragon_banner", null);
			}
		}

		// Token: 0x060013AB RID: 5035 RVA: 0x0005B547 File Offset: 0x00059747
		public override Banner[] GetBanners()
		{
			return new Banner[] { Hero.MainHero.ClanBanner };
		}

		// Token: 0x060013AC RID: 5036 RVA: 0x0005B55C File Offset: 0x0005975C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			IOrderedEnumerable<Hero> clanHeroesPool = from h in Hero.MainHero.Clan.Heroes
				where !h.IsChild && h.IsAlive && h != Hero.MainHero
				orderby h.Level
				select h;
			for (int i = 0; i < 17; i++)
			{
				SceneNotificationData.SceneNotificationCharacter characterAtIndex = this.GetCharacterAtIndex(i, clanHeroesPool);
				list.Add(characterAtIndex);
			}
			return list.ToArray();
		}

		// Token: 0x060013AD RID: 5037 RVA: 0x0005B5EA File Offset: 0x000597EA
		public DeclareDragonBannerSceneNotificationItem(bool playerWantsToRestore)
		{
			this.PlayerWantsToRestore = playerWantsToRestore;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x060013AE RID: 5038 RVA: 0x0005B604 File Offset: 0x00059804
		private SceneNotificationData.SceneNotificationCharacter GetCharacterAtIndex(int index, IOrderedEnumerable<Hero> clanHeroesPool)
		{
			bool flag = false;
			int num = -1;
			string objectName = string.Empty;
			switch (index)
			{
			case 0:
				objectName = "battanian_picked_warrior";
				num = 0;
				break;
			case 1:
				objectName = "imperial_infantryman";
				break;
			case 2:
				objectName = "imperial_veteran_infantryman";
				break;
			case 3:
				objectName = "sturgian_warrior";
				num = 1;
				break;
			case 4:
				objectName = "imperial_menavliaton";
				break;
			case 5:
				objectName = "sturgian_ulfhednar";
				num = 2;
				break;
			case 6:
				objectName = "aserai_recruit";
				break;
			case 7:
				objectName = "aserai_skirmisher";
				break;
			case 8:
				objectName = "aserai_veteran_faris";
				break;
			case 9:
				objectName = "imperial_legionary";
				num = 3;
				break;
			case 10:
				objectName = "mountain_bandits_bandit";
				break;
			case 11:
				objectName = "mountain_bandits_chief";
				break;
			case 12:
				objectName = "forest_people_tier_3";
				num = 4;
				break;
			case 13:
				objectName = "mountain_bandits_raider";
				break;
			case 14:
				flag = true;
				break;
			case 15:
				objectName = "vlandian_pikeman";
				break;
			case 16:
				objectName = "vlandian_voulgier";
				break;
			}
			uint customColor = uint.MaxValue;
			uint customColor2 = uint.MaxValue;
			CharacterObject characterObject;
			if (flag)
			{
				characterObject = CharacterObject.PlayerCharacter;
				customColor = Hero.MainHero.MapFaction.Color;
				customColor2 = Hero.MainHero.MapFaction.Color2;
			}
			else if (num != -1 && clanHeroesPool.ElementAtOrDefault(num) != null)
			{
				Hero hero = clanHeroesPool.ElementAtOrDefault(num);
				characterObject = hero.CharacterObject;
				customColor = hero.MapFaction.Color;
				customColor2 = hero.MapFaction.Color2;
			}
			else
			{
				characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(objectName);
			}
			Equipment overriddenEquipment = characterObject.FirstBattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment, true, false);
			return new SceneNotificationData.SceneNotificationCharacter(characterObject, overriddenEquipment, default(BodyProperties), false, customColor, customColor2, false);
		}

		// Token: 0x04000674 RID: 1652
		private const int NumberOfCharacters = 17;

		// Token: 0x04000676 RID: 1654
		private readonly CampaignTime _creationCampaignTime;
	}
}
