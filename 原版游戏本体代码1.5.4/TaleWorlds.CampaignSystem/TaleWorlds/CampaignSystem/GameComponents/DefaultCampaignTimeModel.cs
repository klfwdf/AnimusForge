using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCampaignTimeModel : CampaignTimeModel
{
	public override CampaignTime CampaignStartTime => CampaignTime.Years(1084f) + CampaignTime.Weeks(CampaignTime.WeeksInSeason) + CampaignTime.Hours(9f);

	public override int SunRise => 2;

	public override int SunSet => 22;

	public override long TimeTicksPerMillisecond => 10L;

	public override int MillisecondInSecond => 1000;

	public override int SecondsInMinute => 60;

	public override int MinutesInHour => 60;

	public override int HoursInDay => 24;

	public override int DaysInWeek
	{
		get
		{
			if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
			{
				return 7;
			}
			return 3;
		}
	}

	public override int WeeksInSeason
	{
		get
		{
			if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
			{
				return 3;
			}
			return 2;
		}
	}

	public override int SeasonsInYear => 4;
}
