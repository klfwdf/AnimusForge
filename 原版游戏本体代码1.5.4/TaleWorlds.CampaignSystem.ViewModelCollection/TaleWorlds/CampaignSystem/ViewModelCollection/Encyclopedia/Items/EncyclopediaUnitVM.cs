using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaUnitVM : ViewModel
{
	private CharacterObject _character;

	private CharacterImageIdentifierVM _imageIdentifier;

	private string _nameText;

	private bool _isActiveUnit;

	private StringItemWithHintVM _tierIconData;

	private StringItemWithHintVM _typeIconData;

	[DataSourceProperty]
	public bool IsActiveUnit
	{
		get
		{
			return _isActiveUnit;
		}
		set
		{
			if (value != _isActiveUnit)
			{
				_isActiveUnit = value;
				OnPropertyChangedWithValue(value, "IsActiveUnit");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ImageIdentifier
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
				OnPropertyChangedWithValue(value, "ImageIdentifier");
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
	public StringItemWithHintVM TierIconData
	{
		get
		{
			return _tierIconData;
		}
		set
		{
			if (value != _tierIconData)
			{
				_tierIconData = value;
				OnPropertyChangedWithValue(value, "TierIconData");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TypeIconData
	{
		get
		{
			return _typeIconData;
		}
		set
		{
			if (value != _typeIconData)
			{
				_typeIconData = value;
				OnPropertyChangedWithValue(value, "TypeIconData");
			}
		}
	}

	public EncyclopediaUnitVM(CharacterObject character, bool isActive)
	{
		if (character != null)
		{
			CharacterCode characterCode = CharacterCode.CreateFrom(character);
			ImageIdentifier = new CharacterImageIdentifierVM(characterCode);
			_character = character;
			IsActiveUnit = isActive;
			TierIconData = CampaignUIHelper.GetCharacterTierData(character, isBig: true);
			TypeIconData = CampaignUIHelper.GetCharacterTypeData(character, isBig: true);
		}
		else
		{
			IsActiveUnit = false;
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_character != null)
		{
			NameText = _character.Name.ToString();
		}
	}

	public void ExecuteLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(_character.EncyclopediaLink);
	}

	public virtual void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(CharacterObject), _character);
	}

	public virtual void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
