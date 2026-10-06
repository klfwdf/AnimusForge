using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class DeathOldAgeSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfAudienceHeroes = 5;

	private readonly CampaignTime _creationCampaignTime;

	public Hero DeadHero { get; }

	public override string SceneID => "scn_cutscene_death_old_age";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("NAME", DeadHero.Name);
			return GameTexts.FindText("str_died_of_old_age");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[1] { DeadHero.ClanBanner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = DeadHero.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(DeadHero, equipment));
		foreach (Hero item in CampaignSceneNotificationHelper.GetMilitaryAudienceForHero(DeadHero).Take(5))
		{
			Equipment equipment2 = item.CivilianEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(item, equipment2));
		}
		return list.ToArray();
	}

	public DeathOldAgeSceneNotificationItem(Hero deadHero)
	{
		DeadHero = deadHero;
		_creationCampaignTime = CampaignTime.Now;
	}
}
