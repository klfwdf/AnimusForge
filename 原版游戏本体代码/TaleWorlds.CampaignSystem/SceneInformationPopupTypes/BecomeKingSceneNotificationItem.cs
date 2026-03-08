using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B0 RID: 176
	public class BecomeKingSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000534 RID: 1332
		// (get) Token: 0x06001381 RID: 4993 RVA: 0x0005A58B File Offset: 0x0005878B
		public Hero NewLeaderHero { get; }

		// Token: 0x17000535 RID: 1333
		// (get) Token: 0x06001382 RID: 4994 RVA: 0x0005A593 File Offset: 0x00058793
		public override string SceneID
		{
			get
			{
				return "scn_become_king_notification";
			}
		}

		// Token: 0x17000536 RID: 1334
		// (get) Token: 0x06001383 RID: 4995 RVA: 0x0005A59C File Offset: 0x0005879C
		public override TextObject TitleText
		{
			get
			{
				TextObject textObject;
				if (this.NewLeaderHero.Clan.Kingdom.Culture.StringId.Equals("empire", StringComparison.InvariantCultureIgnoreCase))
				{
					textObject = GameTexts.FindText("str_become_king_empire", null);
				}
				else
				{
					TextObject variable = (this.NewLeaderHero.IsFemale ? GameTexts.FindText("str_liege_title_female", this.NewLeaderHero.Clan.Kingdom.Culture.StringId) : GameTexts.FindText("str_liege_title", this.NewLeaderHero.Clan.Kingdom.Culture.StringId));
					textObject = GameTexts.FindText("str_become_king_nonempire", null);
					textObject.SetTextVariable("TITLE_NAME", variable);
				}
				textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				textObject.SetTextVariable("YEAR", this._creationCampaignTime.GetYear);
				textObject.SetTextVariable("KING_NAME", this.NewLeaderHero.Name);
				textObject.SetTextVariable("IS_KING_MALE", this.NewLeaderHero.IsFemale ? 0 : 1);
				return textObject;
			}
		}

		// Token: 0x06001384 RID: 4996 RVA: 0x0005A6B5 File Offset: 0x000588B5
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				this.NewLeaderHero.Clan.Kingdom.Banner,
				this.NewLeaderHero.Clan.Kingdom.Banner
			};
		}

		// Token: 0x06001385 RID: 4997 RVA: 0x0005A6F0 File Offset: 0x000588F0
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			Equipment overriddenEquipment = this.NewLeaderHero.CharacterObject.Equipment.Clone(true);
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			list.Add(new SceneNotificationData.SceneNotificationCharacter(this.NewLeaderHero.CharacterObject, overriddenEquipment, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
			for (int i = 0; i < 14; i++)
			{
				CharacterObject characterObject = (this.IsAudienceFemale(i) ? this.NewLeaderHero.Clan.Kingdom.Culture.Townswoman : this.NewLeaderHero.Clan.Kingdom.Culture.Townsman);
				Equipment equipment = characterObject.FirstCivilianEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, true, false);
				uint color = BannerManager.Instance.ReadOnlyColorPalette.GetRandomElementInefficiently<KeyValuePair<int, BannerColor>>().Value.Color;
				uint color2 = BannerManager.Instance.ReadOnlyColorPalette.GetRandomElementInefficiently<KeyValuePair<int, BannerColor>>().Value.Color;
				list.Add(new SceneNotificationData.SceneNotificationCharacter(characterObject, equipment, characterObject.GetBodyProperties(equipment, MBRandom.RandomInt(100)), false, color, color2, false));
			}
			for (int j = 0; j < 2; j++)
			{
				list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(this.NewLeaderHero.Clan.Kingdom.MapFaction.Culture));
			}
			foreach (Hero hero in CampaignSceneNotificationHelper.GetMilitaryAudienceForHero(this.NewLeaderHero, false, false).Take(4))
			{
				Equipment overridenEquipment = hero.CivilianEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, false, false);
				list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x06001386 RID: 4998 RVA: 0x0005A8D0 File Offset: 0x00058AD0
		public BecomeKingSceneNotificationItem(Hero newLeaderHero)
		{
			this.NewLeaderHero = newLeaderHero;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x06001387 RID: 4999 RVA: 0x0005A8EA File Offset: 0x00058AEA
		private bool IsAudienceFemale(int indexOfAudience)
		{
			return indexOfAudience == 2 || indexOfAudience == 5 || indexOfAudience - 11 <= 2;
		}

		// Token: 0x04000665 RID: 1637
		private const int NumberOfAudience = 14;

		// Token: 0x04000666 RID: 1638
		private const int NumberOfGuards = 2;

		// Token: 0x04000667 RID: 1639
		private const int NumberOfCompanions = 4;

		// Token: 0x04000669 RID: 1641
		private readonly CampaignTime _creationCampaignTime;
	}
}
