using System;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationCultureVM : ViewModel
{
	private readonly Action<CharacterCreationCultureVM> _onSelection;

	private string _descriptionText = "";

	private string _nameText;

	private string _shortenedNameText;

	private bool _isSelected;

	private string _cultureID;

	private Color _cultureColor1;

	private MBBindingList<CharacterCreationCultureFeatVM> _feats;

	public CultureObject Culture { get; }

	[DataSourceProperty]
	public string CultureID
	{
		get
		{
			return _cultureID;
		}
		set
		{
			if (value != _cultureID)
			{
				_cultureID = value;
				OnPropertyChangedWithValue(value, "CultureID");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor1
	{
		get
		{
			return _cultureColor1;
		}
		set
		{
			if (value != _cultureColor1)
			{
				_cultureColor1 = value;
				OnPropertyChangedWithValue(value, "CultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
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
	public string ShortenedNameText
	{
		get
		{
			return _shortenedNameText;
		}
		set
		{
			if (value != _shortenedNameText)
			{
				_shortenedNameText = value;
				OnPropertyChangedWithValue(value, "ShortenedNameText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CharacterCreationCultureFeatVM> Feats
	{
		get
		{
			return _feats;
		}
		set
		{
			if (value != _feats)
			{
				_feats = value;
				OnPropertyChangedWithValue(value, "Feats");
			}
		}
	}

	public CharacterCreationCultureVM(CultureObject culture, Action<CharacterCreationCultureVM> onSelection)
	{
		_onSelection = onSelection;
		Culture = culture;
		TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreationContent characterCreationContent = (GameStateManager.Current.ActiveState as CharacterCreationState)?.CharacterCreationManager.CharacterCreationContent;
		MBTextManager.SetTextVariable("FOCUS_VALUE", characterCreationContent.GetFocusToAddByCulture(culture));
		MBTextManager.SetTextVariable("EXP_VALUE", characterCreationContent.GetSkillLevelToAddByCulture(culture));
		DescriptionText = GameTexts.FindText("str_culture_description", Culture.StringId).ToString();
		ShortenedNameText = GameTexts.FindText("str_culture_rich_name", Culture.StringId).ToString();
		NameText = GameTexts.FindText("str_culture_rich_name", Culture.StringId).ToString();
		CultureID = culture?.StringId ?? "";
		CultureColor1 = Color.FromUint(culture?.Color ?? Color.White.ToUnsignedInteger());
		Feats = new MBBindingList<CharacterCreationCultureFeatVM>();
		foreach (FeatObject culturalFeat in Culture.GetCulturalFeats((FeatObject x) => x.IsPositive))
		{
			Feats.Add(new CharacterCreationCultureFeatVM(isPositive: true, culturalFeat.Description.ToString()));
		}
		foreach (FeatObject culturalFeat2 in Culture.GetCulturalFeats((FeatObject x) => !x.IsPositive))
		{
			Feats.Add(new CharacterCreationCultureFeatVM(isPositive: false, culturalFeat2.Description.ToString()));
		}
	}

	public void ExecuteSelectCulture()
	{
		_onSelection(this);
	}
}
