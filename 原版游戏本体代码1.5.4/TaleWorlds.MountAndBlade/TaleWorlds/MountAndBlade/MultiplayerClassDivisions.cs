using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerClassDivisions
{
	public class MPHeroClass : MBObjectBase
	{
		private List<IReadOnlyPerkObject> _perks = new List<IReadOnlyPerkObject>();

		public BasicCharacterObject HeroCharacter { get; private set; }

		public BasicCharacterObject TroopCharacter { get; private set; }

		public BasicCharacterObject BannerBearerCharacter { get; private set; }

		public BasicCultureObject Culture { get; private set; }

		public MPHeroClassGroup ClassGroup { get; private set; }

		public string HeroIdleAnim { get; private set; }

		public string HeroMountIdleAnim { get; private set; }

		public string TroopIdleAnim { get; private set; }

		public string TroopMountIdleAnim { get; private set; }

		public int ArmorValue { get; private set; }

		public int Health { get; private set; }

		public float HeroMovementSpeedMultiplier { get; private set; }

		public float HeroCombatMovementSpeedMultiplier { get; private set; }

		public float HeroTopSpeedReachDuration { get; private set; }

		public float TroopMovementSpeedMultiplier { get; private set; }

		public float TroopCombatMovementSpeedMultiplier { get; private set; }

		public float TroopTopSpeedReachDuration { get; private set; }

		public float TroopMultiplier { get; private set; }

		public int TroopCost { get; private set; }

		public int TroopCasualCost { get; private set; }

		public int TroopBattleCost { get; private set; }

		public int MeleeAI { get; private set; }

		public int RangedAI { get; private set; }

		public TextObject HeroInformation { get; private set; }

		public TextObject TroopInformation { get; private set; }

		public TargetIconType IconType { get; private set; }

		public TextObject HeroName => HeroCharacter.Name;

		public TextObject TroopName => TroopCharacter.Name;

		public override bool Equals(object obj)
		{
			if (obj is MPHeroClass)
			{
				return ((MPHeroClass)obj).StringId.Equals(base.StringId);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public List<IReadOnlyPerkObject> GetAllAvailablePerksForListIndex(int index, string forcedForGameMode = null)
		{
			string value = forcedForGameMode ?? MultiplayerOptions.OptionType.GameType.GetStrValue();
			List<IReadOnlyPerkObject> list = new List<IReadOnlyPerkObject>();
			foreach (IReadOnlyPerkObject perk in _perks)
			{
				foreach (string gameMode in perk.GameModes)
				{
					if ((gameMode.Equals(value, StringComparison.InvariantCultureIgnoreCase) || gameMode.Equals("all", StringComparison.InvariantCultureIgnoreCase)) && perk.PerkListIndex == index)
					{
						list.Add(perk);
						break;
					}
				}
			}
			return list;
		}

		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			HeroCharacter = GetMPCharacter(node.Attributes["hero"].Value);
			TroopCharacter = GetMPCharacter(node.Attributes["troop"].Value);
			string text = node.Attributes["banner_bearer"]?.Value;
			if (text != null)
			{
				BannerBearerCharacter = GetMPCharacter(text);
			}
			HeroIdleAnim = node.Attributes["hero_idle_anim"]?.Value;
			HeroMountIdleAnim = node.Attributes["hero_mount_idle_anim"]?.Value;
			TroopIdleAnim = node.Attributes["troop_idle_anim"]?.Value;
			TroopMountIdleAnim = node.Attributes["troop_mount_idle_anim"]?.Value;
			Culture = HeroCharacter.Culture;
			ClassGroup = new MPHeroClassGroup(HeroCharacter.DefaultFormationClass.GetName());
			TroopMultiplier = (float)Convert.ToDouble(node.Attributes["multiplier"].Value);
			TroopCost = Convert.ToInt32(node.Attributes["cost"].Value);
			ArmorValue = Convert.ToInt32(node.Attributes["armor"].Value);
			XmlAttribute xmlAttribute = node.Attributes["casual_cost"];
			XmlAttribute xmlAttribute2 = node.Attributes["battle_cost"];
			TroopCasualCost = ((xmlAttribute != null) ? Convert.ToInt32(node.Attributes["casual_cost"].Value) : TroopCost);
			TroopBattleCost = ((xmlAttribute2 != null) ? Convert.ToInt32(node.Attributes["battle_cost"].Value) : TroopCost);
			Health = 100;
			MeleeAI = 50;
			RangedAI = 50;
			XmlNode xmlNode = node.Attributes["hitpoints"];
			if (xmlNode != null)
			{
				Health = Convert.ToInt32(xmlNode.Value);
			}
			HeroMovementSpeedMultiplier = (float)Convert.ToDouble(node.Attributes["movement_speed"].Value);
			HeroCombatMovementSpeedMultiplier = (float)Convert.ToDouble(node.Attributes["combat_movement_speed"].Value);
			HeroTopSpeedReachDuration = (float)Convert.ToDouble(node.Attributes["acceleration"].Value);
			XmlAttribute xmlAttribute3 = node.Attributes["troop_movement_speed"];
			XmlAttribute xmlAttribute4 = node.Attributes["troop_combat_movement_speed"];
			XmlAttribute xmlAttribute5 = node.Attributes["troop_acceleration"];
			TroopMovementSpeedMultiplier = ((xmlAttribute3 != null) ? ((float)Convert.ToDouble(xmlAttribute3.Value)) : HeroMovementSpeedMultiplier);
			TroopCombatMovementSpeedMultiplier = ((xmlAttribute4 != null) ? ((float)Convert.ToDouble(xmlAttribute4.Value)) : HeroCombatMovementSpeedMultiplier);
			TroopTopSpeedReachDuration = ((xmlAttribute5 != null) ? ((float)Convert.ToDouble(xmlAttribute5.Value)) : HeroTopSpeedReachDuration);
			MeleeAI = Convert.ToInt32(node.Attributes["melee_ai"].Value);
			RangedAI = Convert.ToInt32(node.Attributes["ranged_ai"].Value);
			if (Enum.TryParse<TargetIconType>(node.Attributes["icon"].Value, ignoreCase: true, out var result))
			{
				IconType = result;
			}
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Comment || !(childNode.Name == "Perks"))
				{
					continue;
				}
				_perks = new List<IReadOnlyPerkObject>();
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.NodeType != XmlNodeType.Comment)
					{
						_perks.Add(MPPerkObject.Deserialize(childNode2));
					}
				}
			}
		}

		public bool IsTroopCharacter(BasicCharacterObject character)
		{
			return TroopCharacter == character;
		}
	}

	public class MPHeroClassGroup
	{
		public readonly string StringId;

		public readonly TextObject Name;

		public MPHeroClassGroup(string stringId)
		{
			StringId = stringId;
			Name = GameTexts.FindText("str_troop_type_name", StringId);
		}

		public override bool Equals(object obj)
		{
			if (obj is MPHeroClassGroup)
			{
				return ((MPHeroClassGroup)obj).StringId.Equals(StringId);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public static IEnumerable<BasicCultureObject> AvailableCultures;

	public static List<MPHeroClassGroup> MultiplayerHeroClassGroups { get; private set; }

	public static IEnumerable<MPHeroClass> GetMPHeroClasses(BasicCultureObject culture)
	{
		return from x in MBObjectManager.Instance.GetObjectTypeList<MPHeroClass>()
			where x.Culture == culture
			select x;
	}

	public static MBReadOnlyList<MPHeroClass> GetMPHeroClasses()
	{
		return MBObjectManager.Instance.GetObjectTypeList<MPHeroClass>();
	}

	public static MPHeroClass GetMPHeroClassForCharacter(BasicCharacterObject character)
	{
		return MBObjectManager.Instance.GetObjectTypeList<MPHeroClass>().FirstOrDefault((MPHeroClass x) => x.HeroCharacter == character || x.TroopCharacter == character);
	}

	public static List<List<IReadOnlyPerkObject>> GetAllPerksForHeroClass(MPHeroClass heroClass, string forcedForGameMode = null)
	{
		List<List<IReadOnlyPerkObject>> list = new List<List<IReadOnlyPerkObject>>();
		for (int i = 0; i < 3; i++)
		{
			list.Add(heroClass.GetAllAvailablePerksForListIndex(i, forcedForGameMode).ToList());
		}
		return list;
	}

	public static MPHeroClass GetMPHeroClassForPeer(MissionPeer peer, bool skipTeamCheck = false)
	{
		Team team = peer.Team;
		if ((!skipTeamCheck && (team == null || team.Side == BattleSideEnum.None)) || (peer.SelectedTroopIndex < 0 && peer.ControlledAgent == null))
		{
			return null;
		}
		if (peer.ControlledAgent != null)
		{
			return GetMPHeroClassForCharacter(peer.ControlledAgent.Character);
		}
		if (peer.SelectedTroopIndex >= 0)
		{
			return GetMPHeroClasses(peer.Culture).ToList()[peer.SelectedTroopIndex];
		}
		Debug.FailedAssert("This should not be seen.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\MultiplayerClassDivisions.cs", "GetMPHeroClassForPeer", 255);
		return null;
	}

	public static TargetIconType GetMPHeroClassForFormation(Formation formation)
	{
		return formation.PhysicalClass switch
		{
			FormationClass.Infantry => TargetIconType.Infantry_Light, 
			FormationClass.Ranged => TargetIconType.Archer_Light, 
			FormationClass.Cavalry => TargetIconType.Cavalry_Light, 
			_ => TargetIconType.HorseArcher_Light, 
		};
	}

	public static List<List<IReadOnlyPerkObject>> GetAvailablePerksForPeer(MissionPeer missionPeer)
	{
		if (missionPeer?.Team != null)
		{
			return GetAllPerksForHeroClass(GetMPHeroClassForPeer(missionPeer));
		}
		return new List<List<IReadOnlyPerkObject>>();
	}

	public static void Initialize()
	{
		MultiplayerHeroClassGroups = new List<MPHeroClassGroup>
		{
			new MPHeroClassGroup("Infantry"),
			new MPHeroClassGroup("Ranged"),
			new MPHeroClassGroup("Cavalry"),
			new MPHeroClassGroup("HorseArcher")
		};
		AvailableCultures = from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().ToArray()
			where x.IsMainCulture
			select x;
	}

	public static void Release()
	{
		MultiplayerHeroClassGroups.Clear();
		AvailableCultures = null;
	}

	private static BasicCharacterObject GetMPCharacter(string stringId)
	{
		return MBObjectManager.Instance.GetObject<BasicCharacterObject>(stringId);
	}

	public static int GetMinimumTroopCost(BasicCultureObject culture = null)
	{
		MBReadOnlyList<MPHeroClass> mPHeroClasses = GetMPHeroClasses();
		if (culture != null)
		{
			return mPHeroClasses.Where((MPHeroClass c) => c.Culture == culture).Min((MPHeroClass troop) => troop.TroopCost);
		}
		return mPHeroClasses.Min((MPHeroClass troop) => troop.TroopCost);
	}
}
