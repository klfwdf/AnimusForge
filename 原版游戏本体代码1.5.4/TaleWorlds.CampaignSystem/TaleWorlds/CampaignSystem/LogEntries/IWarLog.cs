namespace TaleWorlds.CampaignSystem.LogEntries;

public interface IWarLog
{
	bool IsRelatedToWar(StanceLink stance, out IFaction effector, out IFaction effected);
}
