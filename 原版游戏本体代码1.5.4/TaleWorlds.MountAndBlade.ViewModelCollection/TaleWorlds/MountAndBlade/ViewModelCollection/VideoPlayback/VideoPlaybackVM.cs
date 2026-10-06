using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.VideoPlayback;

public class VideoPlaybackVM : ViewModel
{
	private List<SRTHelper.SubtitleItem> subTitleLines;

	private string _subtitleText;

	[DataSourceProperty]
	public string SubtitleText
	{
		get
		{
			return _subtitleText;
		}
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, "SubtitleText");
			}
		}
	}

	public void Tick(float totalElapsedTimeInVideoInSeconds)
	{
		if (subTitleLines != null)
		{
			SRTHelper.SubtitleItem itemInTimeframe = GetItemInTimeframe(totalElapsedTimeInVideoInSeconds);
			if (itemInTimeframe != null)
			{
				SubtitleText = string.Join("\n", itemInTimeframe.Lines);
			}
			else
			{
				SubtitleText = string.Empty;
			}
		}
	}

	public SRTHelper.SubtitleItem GetItemInTimeframe(float timeInSecondsInVideo)
	{
		int num = (int)(timeInSecondsInVideo * 1000f);
		for (int i = 0; i < subTitleLines.Count; i++)
		{
			if (subTitleLines[i].StartTime < num && subTitleLines[i].EndTime > num)
			{
				return subTitleLines[i];
			}
		}
		return null;
	}

	public void SetSubtitles(List<SRTHelper.SubtitleItem> lines)
	{
		subTitleLines = lines;
		SubtitleText = string.Empty;
	}
}
