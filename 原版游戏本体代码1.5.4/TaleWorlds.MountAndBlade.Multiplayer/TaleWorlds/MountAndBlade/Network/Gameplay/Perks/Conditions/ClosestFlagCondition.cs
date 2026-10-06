using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class ClosestFlagCondition : MPPerkCondition<MissionMultiplayerFlagDomination>
{
	private enum FlagOwner
	{
		Ally,
		Enemy,
		None,
		Any
	}

	protected static string StringType = "FlagDominationClosestFlag";

	private FlagOwner _owner;

	public override PerkEventFlags EventFlags => PerkEventFlags.FlagCapture | PerkEventFlags.FlagRemoval;

	public override bool IsPeerCondition => true;

	protected ClosestFlagCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		string text = node?.Attributes?["owner"]?.Value;
		_owner = FlagOwner.Any;
		if (text != null && !Enum.TryParse<FlagOwner>(text, ignoreCase: true, out _owner))
		{
			_owner = FlagOwner.Any;
			Debug.FailedAssert("provided 'owner' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\ClosestFlagCondition.cs", "Deserialize", 40);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		if (agent != null)
		{
			MissionMultiplayerFlagDomination gameModeInstance = base.GameModeInstance;
			FlagOwner flagOwner = FlagOwner.None;
			float num = float.MaxValue;
			foreach (FlagCapturePoint allCapturePoint in gameModeInstance.AllCapturePoints)
			{
				if (!allCapturePoint.IsDeactivated)
				{
					float num2 = agent.Position.DistanceSquared(allCapturePoint.Position);
					if (num2 < num)
					{
						num = num2;
						Team flagOwnerTeam = gameModeInstance.GetFlagOwnerTeam(allCapturePoint);
						flagOwner = ((flagOwnerTeam != null) ? ((flagOwnerTeam != agent.Team) ? FlagOwner.Enemy : FlagOwner.Ally) : FlagOwner.None);
					}
				}
			}
			if (_owner != FlagOwner.Any)
			{
				return _owner == flagOwner;
			}
			return true;
		}
		return false;
	}
}
