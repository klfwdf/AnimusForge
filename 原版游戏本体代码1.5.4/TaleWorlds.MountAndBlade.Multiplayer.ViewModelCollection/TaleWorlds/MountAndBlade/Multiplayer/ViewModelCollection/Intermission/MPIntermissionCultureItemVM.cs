using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Intermission;

public class MPIntermissionCultureItemVM : MPCultureItemVM
{
	private readonly Action<MPIntermissionCultureItemVM> _onPlayerVoted;

	private int _votes;

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

	public MPIntermissionCultureItemVM(string cultureCode, Action<MPIntermissionCultureItemVM> onPlayerVoted)
		: base(cultureCode, null)
	{
		_onPlayerVoted = onPlayerVoted;
	}

	public void ExecuteVote()
	{
		_onPlayerVoted(this);
	}
}
