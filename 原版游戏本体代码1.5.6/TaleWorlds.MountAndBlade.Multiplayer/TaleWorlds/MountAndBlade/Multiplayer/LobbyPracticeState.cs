using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class LobbyPracticeState : GameState
{
	private bool _practiceOpened;

	protected override void OnActivate()
	{
		base.OnActivate();
		if (_practiceOpened)
		{
			base.GameStateManager.PopState();
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_practiceOpened)
		{
			OpenPracticeMission();
			_practiceOpened = true;
		}
	}

	private void OpenPracticeMission()
	{
		BasicCharacterObject basicCharacterObject = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_heavy_cavalry_empire_hero");
		BasicCharacterObject characterObject = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_skirmisher_battania_troop");
		BasicCharacterObject characterObject2 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("mp_light_ranged_khuzait_troop");
		Game.Current.PlayerTroop = basicCharacterObject;
		BasicCultureObject basicCultureObject2;
		BasicCultureObject basicCultureObject = (basicCultureObject2 = Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire"));
		Banner banner = basicCultureObject2.Banner;
		Banner banner2 = basicCultureObject.Banner;
		CustomBattleCombatant customBattleCombatant = new CustomBattleCombatant(new TextObject("{=sSJSTe5p}Player Party"), basicCultureObject2, banner);
		CustomBattleCombatant customBattleCombatant2 = new CustomBattleCombatant(new TextObject("{=0xC75dN6}Enemy Party"), basicCultureObject, banner2);
		customBattleCombatant.AddCharacter(basicCharacterObject, 1);
		customBattleCombatant2.AddCharacter(basicCharacterObject, 1);
		customBattleCombatant.AddCharacter(characterObject, 3);
		customBattleCombatant2.AddCharacter(characterObject, 3);
		customBattleCombatant.AddCharacter(characterObject2, 8);
		customBattleCombatant2.AddCharacter(characterObject2, 8);
		customBattleCombatant.SetGeneral(basicCharacterObject);
		customBattleCombatant2.SetGeneral(basicCharacterObject);
		customBattleCombatant.Side = BattleSideEnum.Attacker;
		customBattleCombatant2.Side = BattleSideEnum.Defender;
		MultiplayerPracticeMissions.OpenMultiplayerPracticeMission("mp_practice_battle", basicCharacterObject, customBattleCombatant, customBattleCombatant2, isPlayerGeneral: true, null, "", "summer");
	}
}
