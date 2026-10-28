using System.Linq;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FactionBanVote;

public class MultiplayerFactionBanVM : ViewModel
{
	private MBBindingList<MultiplayerFactionBanVoteVM> _banList;

	private MBBindingList<MultiplayerFactionBanVoteVM> _selectList;

	private string _selectTitle;

	private string _banTitle;

	[DataSourceProperty]
	public MBBindingList<MultiplayerFactionBanVoteVM> SelectList
	{
		get
		{
			return _selectList;
		}
		set
		{
			if (value != _selectList)
			{
				_selectList = value;
				OnPropertyChangedWithValue(value, "SelectList");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MultiplayerFactionBanVoteVM> BanList
	{
		get
		{
			return _banList;
		}
		set
		{
			if (value != _banList)
			{
				_banList = value;
				OnPropertyChangedWithValue(value, "BanList");
			}
		}
	}

	[DataSourceProperty]
	public string SelectTitle
	{
		get
		{
			return _selectTitle;
		}
		set
		{
			if (value != _selectTitle)
			{
				_selectTitle = value;
				OnPropertyChangedWithValue(value, "SelectTitle");
			}
		}
	}

	[DataSourceProperty]
	public string BanTitle
	{
		get
		{
			return _banTitle;
		}
		set
		{
			if (value != _banTitle)
			{
				_banTitle = value;
				OnPropertyChangedWithValue(value, "BanTitle");
			}
		}
	}

	public MultiplayerFactionBanVM()
	{
		SelectTitle = "SELECT FACTION";
		BanTitle = "BAN FACTION";
		_banList = new MBBindingList<MultiplayerFactionBanVoteVM>();
		foreach (BasicCultureObject availableCulture in MultiplayerClassDivisions.AvailableCultures)
		{
			_banList.Add(new MultiplayerFactionBanVoteVM(availableCulture, OnBanFaction));
		}
		_selectList = new MBBindingList<MultiplayerFactionBanVoteVM>();
		foreach (BasicCultureObject availableCulture2 in MultiplayerClassDivisions.AvailableCultures)
		{
			_selectList.Add(new MultiplayerFactionBanVoteVM(availableCulture2, OnSelectFaction));
		}
		foreach (MultiplayerFactionBanVoteVM select in _selectList)
		{
			if (select.IsEnabled)
			{
				select.IsSelected = true;
				break;
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
	}

	private void OnSelectFaction(MultiplayerFactionBanVoteVM vote)
	{
		VoteForCulture(CultureVoteTypes.Select, vote.Culture);
	}

	private void OnBanFaction(MultiplayerFactionBanVoteVM vote)
	{
		VoteForCulture(CultureVoteTypes.Ban, vote.Culture);
	}

	private void Refresh()
	{
		foreach (MultiplayerFactionBanVoteVM ban in _banList)
		{
			ban.IsSelected = false;
			ban.IsEnabled = false;
		}
		MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		bool flag = false;
		foreach (MultiplayerFactionBanVoteVM select in _selectList)
		{
			if (flag)
			{
				select.IsSelected = true;
				flag = false;
				break;
			}
			if (component.VotedForBan == select.Culture)
			{
				select.IsEnabled = false;
				if (select.IsSelected)
				{
					select.IsSelected = false;
					flag = true;
				}
			}
		}
		if (flag)
		{
			MultiplayerFactionBanVoteVM multiplayerFactionBanVoteVM = _selectList.FirstOrDefault((MultiplayerFactionBanVoteVM s) => s.IsEnabled);
			if (multiplayerFactionBanVoteVM != null)
			{
				multiplayerFactionBanVoteVM.IsSelected = true;
			}
		}
	}

	private static void VoteForCulture(CultureVoteTypes voteType, BasicCultureObject culture)
	{
		MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
		if (GameNetwork.IsServer)
		{
			component.HandleVoteChange(voteType, culture);
		}
		else if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new CultureVoteClient(voteType, culture));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
