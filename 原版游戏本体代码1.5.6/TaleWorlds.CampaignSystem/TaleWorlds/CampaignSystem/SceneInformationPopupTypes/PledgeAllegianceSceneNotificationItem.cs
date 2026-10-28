using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class PledgeAllegianceSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfTroops = 24;

	private readonly CampaignTime _creationCampaignTime;

	public Hero PlayerHero { get; }

	public bool PlayerWantsToRestore { get; }

	public override string SceneID => "scn_pledge_allegiance_notification";

	public override TextObject TitleText
	{
		get
		{
			TextObject textObject = GameTexts.FindText("str_pledge_notification_title");
			textObject.SetCharacterProperties("RULER", PlayerHero.Clan.Kingdom.Leader.CharacterObject);
			textObject.SetTextVariable("PLAYER_WANTS_RESTORE", PlayerWantsToRestore ? 1 : 0);
			textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			textObject.SetTextVariable("YEAR", _creationCampaignTime.GetYear);
			return textObject;
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[2]
		{
			Hero.MainHero.ClanBanner,
			PlayerHero.Clan.Kingdom.Leader.Clan?.Kingdom.Banner ?? PlayerHero.Clan.Kingdom.Leader.ClanBanner
		};
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		ItemObject itemObject = null;
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = PlayerHero.BattleEquipment.Clone();
		if (equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
		{
			itemObject = CampaignSceneNotificationHelper.GetDefaultHorseItem();
			equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject);
		}
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(PlayerHero, equipment, useCivilian: false, default(BodyProperties), uint.MaxValue, uint.MaxValue, useHorse: true));
		Equipment equipment2 = PlayerHero.Clan.Kingdom.Leader.BattleEquipment.Clone();
		if (equipment2[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
		{
			if (itemObject == null)
			{
				itemObject = CampaignSceneNotificationHelper.GetDefaultHorseItem();
			}
			equipment2[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject);
		}
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, removeHelmet: true);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(PlayerHero.Clan.Kingdom.Leader, equipment2, useCivilian: false, default(BodyProperties), uint.MaxValue, uint.MaxValue, useHorse: true));
		CultureObject culture = ((PlayerHero.Clan.Kingdom.Leader.MapFaction?.Culture != null) ? PlayerHero.Clan.Kingdom.Leader.MapFaction.Culture : PlayerHero.MapFaction.Culture);
		for (int i = 0; i < 24; i++)
		{
			CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(culture);
			Equipment equipment3 = randomTroopForCulture.FirstBattleEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment3);
			BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment3, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationCharacter(randomTroopForCulture, equipment3, bodyProperties));
		}
		return list.ToArray();
	}

	public PledgeAllegianceSceneNotificationItem(Hero playerHero, bool playerWantsToRestore)
	{
		PlayerHero = playerHero;
		PlayerWantsToRestore = playerWantsToRestore;
		_creationCampaignTime = CampaignTime.Now;
	}
}
