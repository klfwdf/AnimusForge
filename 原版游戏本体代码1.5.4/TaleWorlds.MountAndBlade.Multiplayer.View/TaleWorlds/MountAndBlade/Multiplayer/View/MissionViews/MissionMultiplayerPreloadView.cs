using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

public class MissionMultiplayerPreloadView : MissionView
{
	private PreloadHelper _helperInstance = new PreloadHelper();

	private bool _preloadDone;

	public override void OnPreMissionTick(float dt)
	{
		if (_preloadDone)
		{
			return;
		}
		MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
		IEnumerable<MultiplayerClassDivisions.MPHeroClass> mPHeroClasses = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue()));
		IEnumerable<MultiplayerClassDivisions.MPHeroClass> mPHeroClasses2 = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue()));
		List<BasicCharacterObject> list = new List<BasicCharacterObject>();
		foreach (MultiplayerClassDivisions.MPHeroClass item in mPHeroClasses)
		{
			list.Add(item.HeroCharacter);
			if (missionBehavior.GameType == MultiplayerGameType.Captain)
			{
				list.Add(item.TroopCharacter);
			}
		}
		foreach (MultiplayerClassDivisions.MPHeroClass item2 in mPHeroClasses2)
		{
			list.Add(item2.HeroCharacter);
			if (missionBehavior.GameType == MultiplayerGameType.Captain)
			{
				list.Add(item2.TroopCharacter);
			}
		}
		_helperInstance.PreloadCharacters(list);
		MissionMultiplayerSiegeClient missionBehavior2 = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
		if (missionBehavior2 != null)
		{
			_helperInstance.PreloadItems(missionBehavior2.GetSiegeMissiles());
		}
		_preloadDone = true;
	}

	public override void OnSceneRenderingStarted()
	{
		_helperInstance.WaitForMeshesToBeLoaded();
	}

	public override void OnMissionStateDeactivated()
	{
		base.OnMissionStateDeactivated();
		_helperInstance.Clear();
	}
}
