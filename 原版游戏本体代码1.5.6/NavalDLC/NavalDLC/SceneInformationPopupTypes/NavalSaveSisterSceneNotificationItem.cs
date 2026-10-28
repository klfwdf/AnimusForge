using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.SceneInformationPopupTypes;

public class NavalSaveSisterSceneNotificationItem : SceneNotificationData
{
	private readonly Action _onCloseAction;

	public Hero MainHero { get; private set; }

	public Hero Sister { get; private set; }

	public override string SceneID => "cutscene_saving_sister";

	public override RelevantContextType RelevantContext => RelevantContextType.Map;

	public override TextObject TitleText => new TextObject("{=kpBuCL0h}The danger has passed. Your sister is now out of harm's way.");

	public NavalSaveSisterSceneNotificationItem(Hero mainHero, Hero sister, Action onCloseAction)
	{
		MainHero = mainHero;
		Sister = sister;
		_onCloseAction = onCloseAction;
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		new List<SceneNotificationCharacter>();
		Equipment equipment = MainHero.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		Equipment equipment2 = Sister.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, removeHelmet: true);
		Equipment equipment3 = Sister.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment3, removeHelmet: true);
		return new SceneNotificationCharacter[3]
		{
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(MainHero, equipment),
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Sister, equipment2),
			CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(Sister, equipment3)
		};
	}

	public override void OnCloseAction()
	{
		base.OnCloseAction();
		_onCloseAction?.Invoke();
	}
}
