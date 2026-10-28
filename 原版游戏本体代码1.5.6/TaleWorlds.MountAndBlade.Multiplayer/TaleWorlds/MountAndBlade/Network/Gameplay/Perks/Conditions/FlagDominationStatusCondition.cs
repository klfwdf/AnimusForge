using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class FlagDominationStatusCondition : MPPerkCondition<MissionMultiplayerFlagDomination>
{
	private enum Status
	{
		Winning,
		Losing,
		Tie
	}

	protected static string StringType = "FlagDominationStatus";

	private Status _status;

	public override PerkEventFlags EventFlags => PerkEventFlags.FlagCapture | PerkEventFlags.FlagRemoval;

	public override bool IsPeerCondition => true;

	protected FlagDominationStatusCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		string text = node?.Attributes?["status"]?.Value;
		_status = Status.Tie;
		if (text != null && !Enum.TryParse<Status>(text, ignoreCase: true, out _status))
		{
			_status = Status.Tie;
			Debug.FailedAssert("provided 'status' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\FlagDominationStatusCondition.cs", "Deserialize", 39);
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
			int num = 0;
			int num2 = 0;
			foreach (FlagCapturePoint allCapturePoint in gameModeInstance.AllCapturePoints)
			{
				if (!allCapturePoint.IsDeactivated)
				{
					Team flagOwnerTeam = gameModeInstance.GetFlagOwnerTeam(allCapturePoint);
					if (flagOwnerTeam == agent.Team)
					{
						num++;
					}
					else if (flagOwnerTeam != null)
					{
						num2++;
					}
				}
			}
			if (_status != Status.Winning)
			{
				if (_status != Status.Losing)
				{
					return num == num2;
				}
				return num2 > num;
			}
			return num > num2;
		}
		return false;
	}
}
