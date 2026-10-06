using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;

public class KingdomClanItemVM : KingdomItemVM
{
	private enum ClanTypes
	{
		Normal,
		Leader,
		Mercenary
	}

	private readonly Action<KingdomClanItemVM> _onSelect;

	public readonly Clan Clan;

	private string _name;

	private BannerImageIdentifierVM _banner;

	private BannerImageIdentifierVM _banner_9;

	private MBBindingList<HeroVM> _members;

	private MBBindingList<KingdomClanFiefItemVM> _fiefs;

	private int _influence;

	private int _numOfMembers;

	private int _numOfFiefs;

	private string _tierText;

	private int _clanType = -1;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public int ClanType
	{
		get
		{
			return _clanType;
		}
		set
		{
			if (value != _clanType)
			{
				_clanType = value;
				OnPropertyChangedWithValue(value, "ClanType");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfMembers
	{
		get
		{
			return _numOfMembers;
		}
		set
		{
			if (value != _numOfMembers)
			{
				_numOfMembers = value;
				OnPropertyChangedWithValue(value, "NumOfMembers");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfFiefs
	{
		get
		{
			return _numOfFiefs;
		}
		set
		{
			if (value != _numOfFiefs)
			{
				_numOfFiefs = value;
				OnPropertyChangedWithValue(value, "NumOfFiefs");
			}
		}
	}

	[DataSourceProperty]
	public string TierText
	{
		get
		{
			return _tierText;
		}
		set
		{
			if (value != _tierText)
			{
				_tierText = value;
				OnPropertyChangedWithValue(value, "TierText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner)
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner_9
	{
		get
		{
			return _banner_9;
		}
		set
		{
			if (value != _banner_9)
			{
				_banner_9 = value;
				OnPropertyChangedWithValue(value, "Banner_9");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Members
	{
		get
		{
			return _members;
		}
		set
		{
			if (value != _members)
			{
				_members = value;
				OnPropertyChangedWithValue(value, "Members");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomClanFiefItemVM> Fiefs
	{
		get
		{
			return _fiefs;
		}
		set
		{
			if (value != _fiefs)
			{
				_fiefs = value;
				OnPropertyChangedWithValue(value, "Fiefs");
			}
		}
	}

	[DataSourceProperty]
	public int Influence
	{
		get
		{
			return _influence;
		}
		set
		{
			if (value != _influence)
			{
				_influence = value;
				OnPropertyChangedWithValue(value, "Influence");
			}
		}
	}

	public KingdomClanItemVM(Clan clan, Action<KingdomClanItemVM> onSelect)
	{
		Clan = clan;
		_onSelect = onSelect;
		Banner = new BannerImageIdentifierVM(clan.Banner);
		Banner_9 = new BannerImageIdentifierVM(clan.Banner, nineGrid: true);
		RefreshValues();
		Refresh();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Clan.Name.ToString();
		GameTexts.SetVariable("TIER", Clan.Tier);
		TierText = GameTexts.FindText("str_clan_tier").ToString();
	}

	public void Refresh()
	{
		Members = new MBBindingList<HeroVM>();
		ClanType = 0;
		if (Clan.IsUnderMercenaryService)
		{
			ClanType = 2;
		}
		else if (Clan.Kingdom.RulingClan == Clan)
		{
			ClanType = 1;
		}
		foreach (Hero item in Clan.Heroes.Where((Hero h) => !h.IsDisabled && !h.IsNotSpawned && h.IsAlive && !h.IsChild))
		{
			Members.Add(new HeroVM(item));
		}
		NumOfMembers = Members.Count;
		Fiefs = new MBBindingList<KingdomClanFiefItemVM>();
		foreach (Settlement item2 in Clan.Settlements.Where((Settlement s) => s.IsTown || s.IsCastle))
		{
			Fiefs.Add(new KingdomClanFiefItemVM(item2));
		}
		NumOfFiefs = Fiefs.Count;
		Influence = (int)Clan.Influence;
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		_onSelect(this);
	}
}
