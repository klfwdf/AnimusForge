using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Inquiries;

public class TextQueryPopUpVM : PopUpBaseVM
{
	private TextInquiryData _data;

	[DataSourceProperty]
	private string _inputText;

	private bool _isInputObfuscated;

	private HintViewModel _doneButtonDisabledReasonHint;

	[DataSourceProperty]
	public string InputText
	{
		get
		{
			return _inputText;
		}
		set
		{
			if (value != _inputText)
			{
				_inputText = value;
				OnPropertyChangedWithValue(value, "InputText");
				Tuple<bool, string> tuple = _data.TextCondition?.Invoke(value);
				base.IsButtonOkEnabled = tuple?.Item1 ?? true;
				DoneButtonDisabledReasonHint.HintText = (string.IsNullOrEmpty(tuple?.Item2) ? TextObject.GetEmpty() : new TextObject("{=!}" + tuple.Item2));
			}
		}
	}

	public bool IsInputObfuscated
	{
		get
		{
			return _isInputObfuscated;
		}
		set
		{
			if (value != _isInputObfuscated)
			{
				_isInputObfuscated = value;
				OnPropertyChangedWithValue(value, "IsInputObfuscated");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DoneButtonDisabledReasonHint
	{
		get
		{
			return _doneButtonDisabledReasonHint;
		}
		set
		{
			if (value != _doneButtonDisabledReasonHint)
			{
				_doneButtonDisabledReasonHint = value;
				OnPropertyChangedWithValue(value, "DoneButtonDisabledReasonHint");
			}
		}
	}

	public TextQueryPopUpVM(Action closeQuery)
		: base(closeQuery)
	{
		DoneButtonDisabledReasonHint = new HintViewModel();
	}

	public void SetData(TextInquiryData data)
	{
		_data = data;
		base.TitleText = _data.TitleText;
		base.PopUpLabel = _data.Text;
		base.ButtonOkLabel = _data.AffirmativeText;
		base.ButtonCancelLabel = _data.NegativeText;
		base.IsButtonOkShown = _data.IsAffirmativeOptionShown;
		base.IsButtonCancelShown = _data.IsNegativeOptionShown;
		IsInputObfuscated = _data.IsInputObfuscated;
		InputText = _data.DefaultInputText;
		base.IsButtonOkEnabled = _data.TextCondition?.Invoke(InputText).Item1 ?? true;
	}

	public override void ExecuteAffirmativeAction()
	{
		_data.AffirmativeAction?.Invoke(InputText);
		CloseQuery();
	}

	public override void ExecuteNegativeAction()
	{
		_data.NegativeAction?.Invoke();
		CloseQuery();
	}

	public override void OnClearData()
	{
		base.OnClearData();
		_data = null;
	}
}
