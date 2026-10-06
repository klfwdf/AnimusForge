using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MPOverlayPlayerVM : MPPlayerVM
{
	private readonly Action<MPOverlayPlayerVM> _onSelected;

	private static readonly HashSet<string> ExcludedHeaderIds = new HashSet<string> { "avatar", "badge", "name", "score", "ping" };

	private readonly List<string> _statIds = new List<string>();

	private MBBindingList<MPOverlayStatVM> _stats;

	[DataSourceProperty]
	public MBBindingList<MPOverlayStatVM> Stats
	{
		get
		{
			return _stats;
		}
		set
		{
			if (value != _stats)
			{
				_stats = value;
				OnPropertyChangedWithValue(value, "Stats");
			}
		}
	}

	public static bool IsExcludedHeaderId(string headerId)
	{
		return ExcludedHeaderIds.Contains(headerId);
	}

	public MPOverlayPlayerVM(MissionPeer peer, Action<MPOverlayPlayerVM> onSelected)
		: base(peer)
	{
		_onSelected = onSelected;
		Stats = new MBBindingList<MPOverlayStatVM>();
	}

	public override void ExecuteSelectPlayer()
	{
		_onSelected?.Invoke(this);
	}

	public void RebuildStats(MissionScoreboardComponent.ScoreboardHeader[] headers)
	{
		Stats.Clear();
		_statIds.Clear();
		if (headers == null)
		{
			return;
		}
		for (int i = 0; i < headers.Length; i++)
		{
			MissionScoreboardComponent.ScoreboardHeader scoreboardHeader = headers[i];
			if (!string.IsNullOrEmpty(scoreboardHeader.Id) && !IsExcludedHeaderId(scoreboardHeader.Id))
			{
				Stats.Add(new MPOverlayStatVM(scoreboardHeader.Id, scoreboardHeader.Name?.ToString() ?? string.Empty, scoreboardHeader.GetValueOf(base.Peer)));
				_statIds.Add(scoreboardHeader.Id);
			}
		}
	}

	public void RefreshStats(MissionScoreboardComponent.ScoreboardHeader[] headers)
	{
		if (headers == null || base.Peer == null)
		{
			return;
		}
		if (Stats.Count != _statIds.Count)
		{
			RebuildStats(headers);
			return;
		}
		int num = 0;
		for (int i = 0; i < headers.Length; i++)
		{
			MissionScoreboardComponent.ScoreboardHeader scoreboardHeader = headers[i];
			if (!string.IsNullOrEmpty(scoreboardHeader.Id) && !IsExcludedHeaderId(scoreboardHeader.Id))
			{
				if (num >= Stats.Count || _statIds[num] != scoreboardHeader.Id)
				{
					RebuildStats(headers);
					break;
				}
				Stats[num].Refresh(scoreboardHeader.GetValueOf(base.Peer));
				num++;
			}
		}
	}
}
