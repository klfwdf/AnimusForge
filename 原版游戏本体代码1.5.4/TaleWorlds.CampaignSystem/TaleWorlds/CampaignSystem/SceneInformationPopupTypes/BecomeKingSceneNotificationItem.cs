using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class BecomeKingSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfAudience = 14;

	private const int NumberOfGuards = 2;

	private const int NumberOfCompanions = 4;

	private readonly CampaignTime _creationCampaignTime;

	public Hero NewLeaderHero { get; }

	public override string SceneID => "scn_become_king_notification";

	public override TextObject TitleText
	{
		get
		{
			TextObject textObject;
			if (NewLeaderHero.Clan.Kingdom.Culture.StringId.Equals("empire", StringComparison.InvariantCultureIgnoreCase))
			{
				textObject = GameTexts.FindText("str_become_king_empire");
			}
			else
			{
				TextObject variable = (NewLeaderHero.IsFemale ? GameTexts.FindText("str_liege_title_female", NewLeaderHero.Clan.Kingdom.Culture.StringId) : GameTexts.FindText("str_liege_title", NewLeaderHero.Clan.Kingdom.Culture.StringId));
				textObject = GameTexts.FindText("str_become_king_nonempire");
				textObject.SetTextVariable("TITLE_NAME", variable);
			}
			textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			textObject.SetTextVariable("YEAR", _creationCampaignTime.GetYear);
			textObject.SetTextVariable("KING_NAME", NewLeaderHero.Name);
			textObject.SetTextVariable("IS_KING_MALE", (!NewLeaderHero.IsFemale) ? 1 : 0);
			return textObject;
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[2]
		{
			NewLeaderHero.Clan.Kingdom.Banner,
			NewLeaderHero.Clan.Kingdom.Banner
		};
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		Equipment overriddenEquipment = NewLeaderHero.CharacterObject.Equipment.Clone(cloneWithoutWeapons: true);
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		list.Add(new SceneNotificationCharacter(NewLeaderHero.CharacterObject, overriddenEquipment));
		for (int i = 0; i < 14; i++)
		{
			CharacterObject characterObject = (IsAudienceFemale(i) ? NewLeaderHero.Clan.Kingdom.Culture.Townswoman : NewLeaderHero.Clan.Kingdom.Culture.Townsman);
			Equipment equipment = characterObject.FirstCivilianEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
			uint color = BannerManager.Instance.ReadOnlyColorPalette.GetRandomElementInefficiently().Value.Color;
			uint color2 = BannerManager.Instance.ReadOnlyColorPalette.GetRandomElementInefficiently().Value.Color;
			list.Add(new SceneNotificationCharacter(characterObject, equipment, characterObject.GetBodyProperties(equipment, MBRandom.RandomInt(100)), useCivilianEquipment: false, color, color2));
		}
		for (int j = 0; j < 2; j++)
		{
			list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(NewLeaderHero.Clan.Kingdom.MapFaction.Culture));
		}
		foreach (Hero item in CampaignSceneNotificationHelper.GetMilitaryAudienceForHero(NewLeaderHero, includeClanLeader: false).Take(4))
		{
			Equipment equipment2 = item.CivilianEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(item, equipment2));
		}
		return list.ToArray();
	}

	public BecomeKingSceneNotificationItem(Hero newLeaderHero)
	{
		NewLeaderHero = newLeaderHero;
		_creationCampaignTime = CampaignTime.Now;
	}

	private bool IsAudienceFemale(int indexOfAudience)
	{
		if (indexOfAudience == 2 || indexOfAudience == 5 || (uint)(indexOfAudience - 11) <= 2u)
		{
			return true;
		}
		return false;
	}
}
