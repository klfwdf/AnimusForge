using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MPOverlaySideVM : ViewModel
{
	private class OverlayPlayerComparer : IComparer<MPOverlayPlayerVM>
	{
		public int Compare(MPOverlayPlayerVM left, MPOverlayPlayerVM right)
		{
			int num = left?.Peer?.Score ?? 0;
			int num2 = right?.Peer?.Score ?? 0;
			if (num != num2)
			{
				if (num <= num2)
				{
					return 1;
				}
				return -1;
			}
			int num3 = left?.Peer?.Peer?.Index ?? int.MaxValue;
			int num4 = right?.Peer?.Peer?.Index ?? int.MaxValue;
			if (num3 != num4)
			{
				if (num3 >= num4)
				{
					return 1;
				}
				return -1;
			}
			return 0;
		}
	}

	private static readonly OverlayPlayerComparer PlayerComparer = new OverlayPlayerComparer();

	private readonly List<string> _headerIds = new List<string>();

	private readonly List<MPOverlayPlayerVM> _staleScratch = new List<MPOverlayPlayerVM>();

	private MissionPeer _followedPeer;

	private MBBindingList<MPOverlayPlayerVM> _players;

	private MBBindingList<MPOverlayStatVM> _statHeaders;

	private string _overflowText;

	private bool _showOverflow;

	private int _followedPlayerToken;

	[DataSourceProperty]
	public MBBindingList<MPOverlayPlayerVM> Players
	{
		get
		{
			return _players;
		}
		set
		{
			if (value != _players)
			{
				_players = value;
				OnPropertyChangedWithValue(value, "Players");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPOverlayStatVM> StatHeaders
	{
		get
		{
			return _statHeaders;
		}
		set
		{
			if (value != _statHeaders)
			{
				_statHeaders = value;
				OnPropertyChangedWithValue(value, "StatHeaders");
			}
		}
	}

	[DataSourceProperty]
	public string OverflowText
	{
		get
		{
			return _overflowText;
		}
		set
		{
			if (value != _overflowText)
			{
				_overflowText = value;
				OnPropertyChangedWithValue(value, "OverflowText");
			}
		}
	}

	[DataSourceProperty]
	public int FollowedPlayerToken
	{
		get
		{
			return _followedPlayerToken;
		}
		set
		{
			if (value != _followedPlayerToken)
			{
				_followedPlayerToken = value;
				OnPropertyChangedWithValue(value, "FollowedPlayerToken");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowOverflow
	{
		get
		{
			return _showOverflow;
		}
		set
		{
			if (value != _showOverflow)
			{
				_showOverflow = value;
				OnPropertyChangedWithValue(value, "ShowOverflow");
			}
		}
	}

	public MPOverlaySideVM()
	{
		Players = new MBBindingList<MPOverlayPlayerVM>();
		StatHeaders = new MBBindingList<MPOverlayStatVM>();
	}

	public void ApplyPlayers(List<MPOverlayPlayerVM> desired)
	{
		_staleScratch.Clear();
		for (int i = 0; i < Players.Count; i++)
		{
			MPOverlayPlayerVM item = Players[i];
			if (!desired.Contains(item))
			{
				_staleScratch.Add(item);
			}
		}
		for (int j = 0; j < _staleScratch.Count; j++)
		{
			Players.Remove(_staleScratch[j]);
		}
		_staleScratch.Clear();
		for (int k = 0; k < desired.Count; k++)
		{
			MPOverlayPlayerVM item2 = desired[k];
			if (!Players.Contains(item2))
			{
				Players.Add(item2);
			}
		}
		Players.Sort(PlayerComparer);
	}

	public void SetFollowedPeer(MissionPeer followedPeer)
	{
		if (_followedPeer != followedPeer)
		{
			_followedPeer = followedPeer;
			FollowedPlayerToken++;
		}
	}

	public void RefreshStatHeaders(MissionScoreboardComponent.ScoreboardHeader[] headers)
	{
		if (headers == null || !HasHeaderShapeChanged(headers))
		{
			return;
		}
		StatHeaders.Clear();
		_headerIds.Clear();
		for (int i = 0; i < headers.Length; i++)
		{
			MissionScoreboardComponent.ScoreboardHeader scoreboardHeader = headers[i];
			if (!string.IsNullOrEmpty(scoreboardHeader.Id) && !MPOverlayPlayerVM.IsExcludedHeaderId(scoreboardHeader.Id))
			{
				StatHeaders.Add(new MPOverlayStatVM(scoreboardHeader.Id, scoreboardHeader.Name?.ToString() ?? string.Empty, string.Empty));
				_headerIds.Add(scoreboardHeader.Id);
			}
		}
	}

	private bool HasHeaderShapeChanged(MissionScoreboardComponent.ScoreboardHeader[] headers)
	{
		int num = 0;
		for (int i = 0; i < headers.Length; i++)
		{
			MissionScoreboardComponent.ScoreboardHeader scoreboardHeader = headers[i];
			if (!string.IsNullOrEmpty(scoreboardHeader.Id) && !MPOverlayPlayerVM.IsExcludedHeaderId(scoreboardHeader.Id))
			{
				if (num >= _headerIds.Count || _headerIds[num] != scoreboardHeader.Id)
				{
					return true;
				}
				num++;
			}
		}
		return num != _headerIds.Count;
	}
}
