using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class OwnedFlagCountCondition : MPPerkCondition<MissionMultiplayerFlagDomination>
{
	protected static string StringType = "FlagDominationOwnedFlagCount";

	private int _min;

	private int _max;

	public override PerkEventFlags EventFlags => PerkEventFlags.FlagCapture | PerkEventFlags.FlagRemoval;

	public override bool IsPeerCondition => true;

	protected OwnedFlagCountCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		string text = node?.Attributes?["min"]?.Value;
		if (text == null)
		{
			_min = 0;
		}
		else if (!int.TryParse(text, out _min))
		{
			Debug.FailedAssert("provided 'min' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\OwnedFlagCountCondition.cs", "Deserialize", 35);
		}
		string text2 = node?.Attributes?["max"]?.Value;
		if (text2 == null)
		{
			_max = int.MaxValue;
		}
		else if (!int.TryParse(text2, out _max))
		{
			Debug.FailedAssert("provided 'max' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\OwnedFlagCountCondition.cs", "Deserialize", 45);
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
			foreach (FlagCapturePoint allCapturePoint in gameModeInstance.AllCapturePoints)
			{
				if (!allCapturePoint.IsDeactivated && gameModeInstance.GetFlagOwnerTeam(allCapturePoint) == agent.Team)
				{
					num++;
				}
			}
			if (num >= _min)
			{
				return num <= _max;
			}
			return false;
		}
		return false;
	}
}
