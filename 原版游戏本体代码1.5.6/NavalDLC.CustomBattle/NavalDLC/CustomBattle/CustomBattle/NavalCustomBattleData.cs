using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.CustomBattle.CustomBattle;

public struct NavalCustomBattleData
{
	public string GameTypeStringId;

	public string SceneId;

	public string SeasonId;

	public BasicCharacterObject PlayerCharacter;

	public CustomBattleCombatant PlayerParty;

	public CustomBattleCombatant EnemyParty;

	public List<IShipOrigin> PlayerShips;

	public List<IShipOrigin> EnemyShips;

	public float TimeOfDay;

	public float WindStrength;

	public NavalCustomBattleWindConfig.Direction WindDirection;

	public TerrainType Terrain;

	public string ForcedSceneLevel;

	public static IEnumerable<Tuple<string, string>> GameTypes
	{
		get
		{
			yield return new Tuple<string, string>(new TextObject("{=lr2UaD9m}Naval Battle").ToString(), "NavalBattle");
			yield return new Tuple<string, string>(new TextObject("{=3oDQHZrf}Naval Raid").ToString(), "NavalRaid");
		}
	}

	public static IEnumerable<Tuple<string, NavalCustomBattlePlayerSide>> PlayerSides
	{
		get
		{
			yield return new Tuple<string, NavalCustomBattlePlayerSide>(new TextObject("{=KASD0tnO}Attacker").ToString(), NavalCustomBattlePlayerSide.Attacker);
			yield return new Tuple<string, NavalCustomBattlePlayerSide>(new TextObject("{=XEVFUaFj}Defender").ToString(), NavalCustomBattlePlayerSide.Defender);
		}
	}

	public static IEnumerable<BasicCharacterObject> Characters
	{
		get
		{
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_1");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_2");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_3");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_4");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_5");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_6");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_7");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_8");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_9");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_10");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_11");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_12");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_13");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_14");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_15");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_16");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_17");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_18");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_19");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_20");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_21");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_22");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_23");
			yield return Game.Current.ObjectManager.GetObject<BasicCharacterObject>("commander_24");
		}
	}

	public static IEnumerable<BasicCultureObject> Factions
	{
		get
		{
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("sturgia");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("aserai");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("vlandia");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("battania");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("khuzait");
			yield return Game.Current.ObjectManager.GetObject<BasicCultureObject>("nord");
		}
	}

	public static IEnumerable<ShipHull> ShipHulls
	{
		get
		{
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("northern_light_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("northern_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("nord_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("sturgia_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("western_light_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("central_light_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("empire_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("eastern_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("empire_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("aserai_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("khuzait_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("eastern_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("battanian_light_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("western_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("vlandia_heavy_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("northern_trade_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("eastern_trade_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("empire_trade_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("nord_mediumballista_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("battanian_medium_ship");
			yield return Game.Current.ObjectManager.GetObject<ShipHull>("western_trade_ship");
		}
	}

	public static IEnumerable<Tuple<string, NavalCustomBattleTimeOfDay>> TimesOfDay
	{
		get
		{
			yield return new Tuple<string, NavalCustomBattleTimeOfDay>(new TextObject("{=X3gcUz7C}Morning").ToString(), NavalCustomBattleTimeOfDay.Morning);
			yield return new Tuple<string, NavalCustomBattleTimeOfDay>(new TextObject("{=CTtjSwRb}Noon").ToString(), NavalCustomBattleTimeOfDay.Noon);
			yield return new Tuple<string, NavalCustomBattleTimeOfDay>(new TextObject("{=J2gvnexb}Afternoon").ToString(), NavalCustomBattleTimeOfDay.Afternoon);
			yield return new Tuple<string, NavalCustomBattleTimeOfDay>(new TextObject("{=gENb9SSW}Evening").ToString(), NavalCustomBattleTimeOfDay.Evening);
			yield return new Tuple<string, NavalCustomBattleTimeOfDay>(new TextObject("{=fAxjyMt5}Night").ToString(), NavalCustomBattleTimeOfDay.Night);
		}
	}

	public static IEnumerable<Tuple<string, string>> Seasons
	{
		get
		{
			yield return new Tuple<string, string>(new TextObject("{=f7vOVQb7}Summer").ToString(), "summer");
			yield return new Tuple<string, string>(new TextObject("{=cZzfNlxd}Fall").ToString(), "fall");
			yield return new Tuple<string, string>(new TextObject("{=nwqUFaU8}Winter").ToString(), "winter");
			yield return new Tuple<string, string>(new TextObject("{=nWbp3o3H}Spring").ToString(), "spring");
		}
	}

	public static IEnumerable<Tuple<string, float>> WindStrengths
	{
		get
		{
			yield return new Tuple<string, float>(new TextObject("{=windstrengthweak}Weak").ToString(), 0.4f);
			yield return new Tuple<string, float>(new TextObject("{=windstrengthmild}Mild").ToString(), 0.5f);
			yield return new Tuple<string, float>(new TextObject("{=windstrengthstrong}Strong").ToString(), 0.7f);
			yield return new Tuple<string, float>(new TextObject("{=windstrengthstormy}Stormy").ToString(), 1f);
		}
	}

	public static IEnumerable<Tuple<string, NavalCustomBattleWindConfig.Direction>> WindDirections
	{
		get
		{
			yield return new Tuple<string, NavalCustomBattleWindConfig.Direction>(new TextObject("{=vz4kmcdI}Towards the Defender").ToString(), NavalCustomBattleWindConfig.Direction.TowardsDefender);
			yield return new Tuple<string, NavalCustomBattleWindConfig.Direction>(new TextObject("{=OjOsvTkT}Towards the Side").ToString(), NavalCustomBattleWindConfig.Direction.Side);
			yield return new Tuple<string, NavalCustomBattleWindConfig.Direction>(new TextObject("{=M0Fiya6u}Towards the Attacker").ToString(), NavalCustomBattleWindConfig.Direction.TowardsAttacker);
			yield return new Tuple<string, NavalCustomBattleWindConfig.Direction>(new TextObject("{=vBkrw5VV}Random").ToString(), NavalCustomBattleWindConfig.Direction.Random);
		}
	}
}
