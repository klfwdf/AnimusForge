using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplatePartyMarkerItemVM : ViewModel
{
	private BannerImageIdentifierVM _visual;

	private bool _isCaravan;

	private bool _isLord;

	private bool _isDefault;

	private bool _isBandit;

	public MobileParty Party { get; private set; }

	public int SortIndex { get; private set; }

	public BannerImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	public bool IsCaravan
	{
		get
		{
			return _isCaravan;
		}
		set
		{
			if (value != _isCaravan)
			{
				_isCaravan = value;
				OnPropertyChangedWithValue(value, "IsCaravan");
			}
		}
	}

	public bool IsLord
	{
		get
		{
			return _isLord;
		}
		set
		{
			if (value != _isLord)
			{
				_isLord = value;
				OnPropertyChangedWithValue(value, "IsLord");
			}
		}
	}

	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			if (value != _isDefault)
			{
				_isDefault = value;
				OnPropertyChangedWithValue(value, "IsDefault");
			}
		}
	}

	public bool IsBandit
	{
		get
		{
			return _isBandit;
		}
		set
		{
			if (value != _isBandit)
			{
				_isBandit = value;
				OnPropertyChangedWithValue(value, "IsBandit");
			}
		}
	}

	public SettlementNameplatePartyMarkerItemVM(MobileParty mobileParty)
	{
		Party = mobileParty;
		IsBandit = mobileParty.IsBandit;
		if (mobileParty.IsCaravan)
		{
			IsCaravan = true;
			SortIndex = 1;
		}
		else if (mobileParty.IsLordParty && mobileParty.LeaderHero != null)
		{
			IsLord = true;
			Visual = new BannerImageIdentifierVM(mobileParty.ActualClan?.Banner, nineGrid: true);
			SortIndex = 0;
		}
		else
		{
			IsDefault = true;
			SortIndex = 2;
		}
	}
}
