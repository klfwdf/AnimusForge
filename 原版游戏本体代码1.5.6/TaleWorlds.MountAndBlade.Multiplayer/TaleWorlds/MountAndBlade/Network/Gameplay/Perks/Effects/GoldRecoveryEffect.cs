using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class GoldRecoveryEffect : MPPerkEffect
{
	protected static string StringType = "GoldRecovery";

	private int _value;

	private int _period;

	public override bool IsTickRequired => true;

	protected GoldRecoveryEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !int.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\GoldRecoveryEffect.cs", "Deserialize", 29);
		}
		string text2 = node?.Attributes?["period"]?.Value;
		if (text2 == null || !int.TryParse(text2, out _period) || _period < 1)
		{
			Debug.FailedAssert("provided 'period' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\GoldRecoveryEffect.cs", "Deserialize", 35);
		}
	}

	public override void OnTick(Agent agent, int tickCount)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		MissionPeer missionPeer = agent?.MissionPeer ?? agent?.OwningAgentMissionPeer;
		if (tickCount % _period == 0 && missionPeer != null)
		{
			(Mission.Current?.GetMissionBehavior<MissionMultiplayerGameModeBase>())?.ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + _value);
		}
	}
}
