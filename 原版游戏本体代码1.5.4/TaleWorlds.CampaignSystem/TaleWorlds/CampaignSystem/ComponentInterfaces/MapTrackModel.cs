using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MapTrackModel : MBGameModel<MapTrackModel>
{
	public abstract float MaxTrackLife { get; }

	public abstract float GetSkipTrackChance(MobileParty mobileParty);

	public abstract float GetMaxTrackSpottingDistanceForMainParty();

	public abstract bool CanPartyLeaveTrack(MobileParty mobileParty);

	public abstract float GetTrackDetectionDifficultyForMainParty(Track track, float trackSpottingDistance);

	public abstract float GetSkillFromTrackDetected(Track track);

	public abstract int GetTrackLife(MobileParty mobileParty);

	public abstract TextObject TrackTitle(Track track);

	public abstract IEnumerable<(TextObject, string)> GetTrackDescription(Track track);

	public abstract uint GetTrackColor(Track track);

	public abstract float GetTrackScale(Track track);
}
