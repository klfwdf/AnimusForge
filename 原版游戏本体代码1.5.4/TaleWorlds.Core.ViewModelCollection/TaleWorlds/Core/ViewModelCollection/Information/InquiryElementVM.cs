using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class InquiryElementVM : ViewModel
{
	public readonly InquiryElement InquiryElement;

	private readonly Action<InquiryElementVM, bool> _onSelectedStateChanged;

	private bool _isFilteredOut;

	private bool _isSelected;

	private bool _isEnabled;

	private string _text;

	private bool _hasVisuals;

	private ImageIdentifierVM _imageIdentifier;

	private HintViewModel _hint;

	[DataSourceProperty]
	public bool IsFilteredOut
	{
		get
		{
			return _isFilteredOut;
		}
		set
		{
			if (_isFilteredOut != value)
			{
				_isFilteredOut = value;
				OnPropertyChangedWithValue(value, "IsFilteredOut");
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
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
				_onSelectedStateChanged?.Invoke(this, value);
			}
		}
	}

	[DataSourceProperty]
	public bool HasVisuals
	{
		get
		{
			return _hasVisuals;
		}
		set
		{
			if (_hasVisuals != value)
			{
				_hasVisuals = value;
				OnPropertyChangedWithValue(value, "HasVisuals");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (_text != value)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	[DataSourceProperty]
	public ImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (_imageIdentifier != value)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (_hint != value)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public InquiryElementVM(InquiryElement elementData, TextObject hint, Action<InquiryElementVM, bool> onSelectedStateChanged = null)
	{
		Text = elementData.Title;
		ImageIdentifier = new GenericImageIdentifierVM(elementData.ImageIdentifier);
		InquiryElement = elementData;
		IsEnabled = elementData.IsEnabled;
		HasVisuals = elementData.ImageIdentifier != null;
		Hint = new HintViewModel(hint);
		_onSelectedStateChanged = onSelectedStateChanged;
	}
}
