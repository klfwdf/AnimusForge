using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerPracticeEscapeMenu))]
public class MissionGauntletMultiplayerPracticeEscapeMenu : MissionGauntletEscapeMenuBase
{
	public MissionGauntletMultiplayerPracticeEscapeMenu()
		: base("MultiplayerEscapeMenu")
	{
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		DataSource = new MPEscapeMenuVM(null);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		DataSource.Tick(dt);
	}

	protected override List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		return new List<EscapeMenuItemVM>
		{
			new EscapeMenuItemVM(new TextObject("{=e139gKZc}Return to the Game"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
			}, null, () => new Tuple<bool, TextObject>(item1: false, null)),
			new EscapeMenuItemVM(new TextObject("{=EXqcmGy4}Return to Lobby"), delegate
			{
				OnEscapeMenuToggled(isOpened: false);
				base.Mission.EndMission();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null))
		};
	}
}
