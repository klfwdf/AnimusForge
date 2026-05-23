using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class DeclareDragonBannerSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfCharacters = 17;

	private readonly CampaignTime _creationCampaignTime;

	public bool PlayerWantsToRestore { get; }

	public override string SceneID => "scn_cutscene_declare_dragon_banner";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("PLAYER_WANTS_RESTORE", PlayerWantsToRestore ? 1 : 0);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_declare_dragon_banner");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[1] { Hero.MainHero.ClanBanner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		IOrderedEnumerable<Hero> clanHeroesPool = from h in Hero.MainHero.Clan.Heroes
			where !h.IsChild && h.IsAlive && h != Hero.MainHero
			orderby h.Level
			select h;
		for (int num = 0; num < 17; num++)
		{
			SceneNotificationCharacter characterAtIndex = GetCharacterAtIndex(num, clanHeroesPool);
			list.Add(characterAtIndex);
		}
		return list.ToArray();
	}

	public DeclareDragonBannerSceneNotificationItem(bool playerWantsToRestore)
	{
		PlayerWantsToRestore = playerWantsToRestore;
		_creationCampaignTime = CampaignTime.Now;
	}

	private SceneNotificationCharacter GetCharacterAtIndex(int index, IOrderedEnumerable<Hero> clanHeroesPool)
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
			Hero? hero = clanHeroesPool.ElementAtOrDefault(num);
			characterObject = hero.CharacterObject;
			customColor = hero.MapFaction.Color;
			customColor2 = hero.MapFaction.Color2;
		}
		else
		{
			characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(objectName);
		}
		Equipment equipment = characterObject.FirstBattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		return new SceneNotificationCharacter(characterObject, equipment, default(BodyProperties), useCivilianEquipment: false, customColor, customColor2);
	}
}
