using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks;

namespace TaleWorlds.MountAndBlade;

public abstract class MPPerkCondition
{
	[Flags]
	public enum PerkEventFlags
	{
		None = 0,
		MoraleChange = 1,
		FlagCapture = 2,
		FlagRemoval = 4,
		HealthChange = 8,
		AliveBotCountChange = 0x10,
		PeerControlledAgentChange = 0x20,
		BannerPickUp = 0x40,
		BannerDrop = 0x80,
		SpawnEnd = 0x100,
		MountHealthChange = 0x200,
		MountChange = 0x400,
		AgentEventsMask = 0x628
	}

	protected static Dictionary<string, Type> Registered;

	public virtual PerkEventFlags EventFlags => PerkEventFlags.None;

	public virtual bool IsPeerCondition => false;

	static MPPerkCondition()
	{
		Registered = new Dictionary<string, Type>();
		foreach (Type item in from t in PerkAssemblyCollection.GetPerkAssemblyTypes()
			where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MPPerkCondition))
			select t)
		{
			string key = (string)item.GetField("StringType", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
			Registered.Add(key, item);
		}
	}

	public abstract bool Check(MissionPeer peer);

	public abstract bool Check(Agent agent);

	protected virtual bool IsGameModesValid(List<string> gameModes)
	{
		return true;
	}

	protected abstract void Deserialize(XmlNode node);

	public static MPPerkCondition CreateFrom(List<string> gameModes, XmlNode node)
	{
		string key = node?.Attributes?["type"]?.Value;
		MPPerkCondition obj = (MPPerkCondition)Activator.CreateInstance(Registered[key], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, CultureInfo.InvariantCulture);
		obj.Deserialize(node);
		return obj;
	}
}
public abstract class MPPerkCondition<T> : MPPerkCondition where T : MissionMultiplayerGameModeBase
{
	protected T GameModeInstance
	{
		get
		{
			Mission current = Mission.Current;
			if (current == null)
			{
				return null;
			}
			return current.GetMissionBehavior<T>();
		}
	}

	protected override bool IsGameModesValid(List<string> gameModes)
	{
		if (typeof(MissionMultiplayerFlagDomination).IsAssignableFrom(typeof(T)))
		{
			string value = MultiplayerGameType.Skirmish.ToString();
			string value2 = MultiplayerGameType.Captain.ToString();
			foreach (string gameMode in gameModes)
			{
				if (!gameMode.Equals(value, StringComparison.InvariantCultureIgnoreCase) && !gameMode.Equals(value2, StringComparison.InvariantCultureIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		if (typeof(MissionMultiplayerTeamDeathmatch).IsAssignableFrom(typeof(T)))
		{
			string value3 = MultiplayerGameType.TeamDeathmatch.ToString();
			foreach (string gameMode2 in gameModes)
			{
				if (!gameMode2.Equals(value3, StringComparison.InvariantCultureIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		if (typeof(MissionMultiplayerSiege).IsAssignableFrom(typeof(T)))
		{
			string value4 = MultiplayerGameType.Siege.ToString();
			foreach (string gameMode3 in gameModes)
			{
				if (!gameMode3.Equals(value4, StringComparison.InvariantCultureIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}
		Debug.FailedAssert("Not implemented game mode check", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\Perks\\MPPerkCondition.cs", "IsGameModesValid", 134);
		return false;
	}
}
