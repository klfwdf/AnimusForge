using System;
using TaleWorlds.Library;

namespace AnimusForge;

public class DevHistoryEditPopupVM : ViewModel
{
	private readonly Action<string> _onSave;

	private readonly Action _onCancel;

	private string _titleText;

	private string _dateText;

	private string _originalContentText;

	private string _inputHintText;

	private string _editedText;

	private string _cancelText;

	private string _saveText;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string DateText
	{
		get
		{
			return _dateText;
		}
		set
		{
			if (value != _dateText)
			{
				_dateText = value;
				OnPropertyChangedWithValue(value, "DateText");
			}
		}
	}

	[DataSourceProperty]
	public string OriginalContentText
	{
		get
		{
			return _originalContentText;
		}
		set
		{
			if (value != _originalContentText)
			{
				_originalContentText = value;
				OnPropertyChangedWithValue(value, "OriginalContentText");
			}
		}
	}

	[DataSourceProperty]
	public string InputHintText
	{
		get
		{
			return _inputHintText;
		}
		set
		{
			if (value != _inputHintText)
			{
				_inputHintText = value;
				OnPropertyChangedWithValue(value, "InputHintText");
			}
		}
	}

	[DataSourceProperty]
	public string EditedText
	{
		get
		{
			return _editedText;
		}
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxLongEditorChars);
			if (text != _editedText)
			{
				_editedText = text;
				OnPropertyChangedWithValue(text, "EditedText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string SaveText
	{
		get
		{
			return _saveText;
		}
		set
		{
			if (value != _saveText)
			{
				_saveText = value;
				OnPropertyChangedWithValue(value, "SaveText");
			}
		}
	}

	public DevHistoryEditPopupVM(string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText, string saveText, string cancelText)
	{
		_onSave = onSave;
		_onCancel = onCancel;
		TitleText = titleText ?? "{=AF_UI_Editor_EditDialogueLine}Edit dialogue line";
		DateText = dateText ?? "";
		OriginalContentText = string.IsNullOrWhiteSpace(originalContentText) ? "{=AF_UI_Editor_Empty}(Empty)" : originalContentText;
		InputHintText = (string.IsNullOrWhiteSpace(inputHintText) ? "{=AF_UI_Editor_EditHint}Edit the entire line in the field below; leave it empty to delete the line." : inputHintText);
		EditedText = editedText ?? "";
		CancelText = (string.IsNullOrWhiteSpace(cancelText) ? "{=AF_UI_Common_Cancel}Cancel" : cancelText);
		SaveText = (string.IsNullOrWhiteSpace(saveText) ? "{=AF_UI_Common_Save}Save" : saveText);
	}

	public void ExecuteSave()
	{
		_onSave?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(EditedText, AnimusForgeTextInputSanitizer.MaxLongEditorChars));
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}

	public void StartTyping()
	{
	}

	public void StopTyping()
	{
	}
}
