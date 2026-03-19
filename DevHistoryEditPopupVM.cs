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
			if (value != _editedText)
			{
				_editedText = value;
				OnPropertyChangedWithValue(value, "EditedText");
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
		TitleText = titleText ?? "编辑对话行";
		DateText = dateText ?? "";
		OriginalContentText = string.IsNullOrWhiteSpace(originalContentText) ? "（空）" : originalContentText;
		InputHintText = (string.IsNullOrWhiteSpace(inputHintText) ? "下方输入框可直接修改整条内容，留空则删除该行。" : inputHintText);
		EditedText = editedText ?? "";
		CancelText = (string.IsNullOrWhiteSpace(cancelText) ? "取消" : cancelText);
		SaveText = (string.IsNullOrWhiteSpace(saveText) ? "保存" : saveText);
	}

	public void ExecuteSave()
	{
		_onSave?.Invoke(EditedText ?? "");
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
