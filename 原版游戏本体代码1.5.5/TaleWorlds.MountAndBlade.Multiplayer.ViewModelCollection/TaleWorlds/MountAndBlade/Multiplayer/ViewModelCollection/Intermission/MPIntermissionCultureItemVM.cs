using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Intermission;

public class MPIntermissionCultureItemVM : MPCultureItemVM
{
	private readonly Action<MPIntermissionCultureItemVM> _onPlayerVoted;

	private int _votes;

	private bool _isVotingAllowed;

	[DataSourceProperty]
	public bool IsVotingAllowed
	{
		get
		{
			return _isVotingAllowed;
		}
		set
		{
			if (value != _isVotingAllowed)
			{
				_isVotingAllowed = value;
				OnPropertyChangedWithValue(value, "IsVotingAllowed");
			}
		}
	}

	[DataSourceProperty]
	public int Votes
	{
		get
		{
			return _votes;
		}
		set
		{
			if (value != _votes)
			{
				_votes = value;
				OnPropertyChangedWithValue(value, "Votes");
			}
		}
	}

	public MPIntermissionCultureItemVM(string cultureCode, bool isVotingAllowed, Action<MPIntermissionCultureItemVM> onPlayerVoted)
		: base(cultureCode, null)
	{
		IsVotingAllowed = isVotingAllowed;
		_onPlayerVoted = onPlayerVoted;
	}

	public void ExecuteVote()
	{
		_onPlayerVoted(this);
	}
}
