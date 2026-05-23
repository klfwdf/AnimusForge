using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaFactionVM : ViewModel
{
	private BannerImageIdentifierVM _imageIdentifier;

	private string _nameText;

	private bool _isDestroyed;

	public IFaction Faction { get; private set; }

	[DataSourceProperty]
	public BannerImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChanged("Banner");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDestroyed
	{
		get
		{
			return _isDestroyed;
		}
		set
		{
			if (value != _isDestroyed)
			{
				_isDestroyed = value;
				OnPropertyChangedWithValue(value, "IsDestroyed");
			}
		}
	}

	public EncyclopediaFactionVM(IFaction faction)
	{
		Faction = faction;
		if (faction != null)
		{
			ImageIdentifier = new BannerImageIdentifierVM(faction.Banner, nineGrid: true);
			IsDestroyed = faction.IsEliminated;
		}
		else
		{
			ImageIdentifier = new BannerImageIdentifierVM(null);
			IsDestroyed = false;
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Faction != null)
		{
			NameText = Faction.Name.ToString();
		}
		else
		{
			NameText = new TextObject("{=2abtb4xu}Independent").ToString();
		}
	}

	public void ExecuteLink()
	{
		if (Faction != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Faction.EncyclopediaLink);
		}
	}

	public void ExecuteBeginHint()
	{
		if (Faction is Clan)
		{
			InformationManager.ShowTooltip(typeof(Clan), Faction);
		}
		else if (Faction is Kingdom)
		{
			InformationManager.ShowTooltip(typeof(Kingdom), Faction);
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
