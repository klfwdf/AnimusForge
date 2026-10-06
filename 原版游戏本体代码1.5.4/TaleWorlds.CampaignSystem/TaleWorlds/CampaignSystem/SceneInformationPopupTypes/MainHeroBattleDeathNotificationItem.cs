using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class MainHeroBattleDeathNotificationItem : SceneNotificationData
{
	private const int NumberOfCorpses = 23;

	private readonly CampaignTime _creationCampaignTime;

	public Hero DeadHero { get; }

	public CultureObject KillerCulture { get; }

	public override string SceneID => "scn_cutscene_main_hero_battle_death";

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
		for (int i = 0; i < 23; i++)
		{
			CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture((KillerCulture != null && (float)i > 11.5f) ? KillerCulture : DeadHero.MapFaction.Culture);
			Equipment equipment2 = randomTroopForCulture.FirstBattleEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
			BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment2, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationCharacter(randomTroopForCulture, equipment2, bodyProperties));
		}
		return list.ToArray();
	}

	public MainHeroBattleDeathNotificationItem(Hero deadHero, CultureObject killerCulture = null)
	{
		DeadHero = deadHero;
		KillerCulture = killerCulture;
		_creationCampaignTime = CampaignTime.Now;
	}
}
