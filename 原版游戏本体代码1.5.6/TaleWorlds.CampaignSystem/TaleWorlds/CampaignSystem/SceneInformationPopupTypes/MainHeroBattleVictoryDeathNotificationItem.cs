using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class MainHeroBattleVictoryDeathNotificationItem : SceneNotificationData
{
	private const int NumberOfCorpses = 2;

	private const int NumberOfCompanions = 3;

	private readonly CampaignTime _creationCampaignTime;

	public Hero DeadHero { get; }

	public List<CharacterObject> EncounterAllyCharacters { get; }

	public override string SceneID => "scn_cutscene_main_hero_battle_victory_death";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("NAME", DeadHero.Name);
			return GameTexts.FindText("str_main_hero_battle_death");
		}
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = DeadHero.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(DeadHero, equipment));
		for (int i = 0; i < 2; i++)
		{
			CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(DeadHero.MapFaction.Culture);
			Equipment equipment2 = randomTroopForCulture.FirstBattleEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
			BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment2, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationCharacter(randomTroopForCulture, equipment2, bodyProperties));
		}
		foreach (CharacterObject item in EncounterAllyCharacters?.Take(3))
		{
			if (item.IsHero)
			{
				Equipment equipment3 = item.HeroObject.BattleEquipment.Clone();
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment3);
				list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(item.HeroObject, equipment3));
			}
			else
			{
				Equipment equipment4 = item.FirstBattleEquipment.Clone();
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment4);
				list.Add(new SceneNotificationCharacter(item, equipment4));
			}
		}
		return list.ToArray();
	}

	public MainHeroBattleVictoryDeathNotificationItem(Hero deadHero, List<CharacterObject> encounterAllyCharacters)
	{
		DeadHero = deadHero;
		EncounterAllyCharacters = encounterAllyCharacters;
		_creationCampaignTime = CampaignTime.Now;
	}
}
