using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public class DevHistoryEditPopupVM : ViewModel
{
	private readonly Action<string> _onSave;

	private readonly Action _onCancel;
	private readonly Func<string> _resetText;
	[DataSourceProperty] public bool CanRestoreDefaults => _resetText != null;
	[DataSourceProperty] public string RestoreDefaultsText => new TextObject("{=AF_MCM_Editor_RestoreDefault}Restore default").ToString();
	public void ExecuteRestoreDefaults() { if (_resetText != null) EditedText = _resetText() ?? ""; }

	private string _titleText;

	private string _dateText;

	private string _originalContentText;

	private string _inputHintText;

	private string _editedText;

	private string _cancelText;

	private string _saveText;

    private string _lengthStatusText;
    private bool _inputWasTruncated;

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
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxLongEditorChars, out bool wasTruncated);
            _inputWasTruncated |= wasTruncated;
			if (text != _editedText)
			{
				_editedText = text;
				OnPropertyChangedWithValue(text, "EditedText");
			}
            UpdateLengthStatus();
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

	public DevHistoryEditPopupVM(string titleText, string dateText, string originalContentText, string editedText, Action<string> onSave, Action onCancel, string inputHintText, string saveText, string cancelText, Func<string> resetText = null)
	{
		_resetText = resetText;
		_onSave = onSave;
		_onCancel = onCancel;
		TitleText = titleText ?? new TextObject("{=AF_UI_Editor_EditDialogueLine}Edit dialogue line").ToString();
		DateText = dateText ?? "";
		OriginalContentText = string.IsNullOrWhiteSpace(originalContentText) ? new TextObject("{=AF_UI_Editor_Empty}(Empty)").ToString() : originalContentText;
		InputHintText = (string.IsNullOrWhiteSpace(inputHintText) ? new TextObject("{=AF_UI_Editor_EditHint}Edit the entire line in the field below; leave it empty to delete the line.").ToString() : inputHintText);
		EditedText = editedText ?? "";
		CancelText = (string.IsNullOrWhiteSpace(cancelText) ? new TextObject("{=AF_UI_Common_Cancel}Cancel").ToString() : cancelText);
		SaveText = (string.IsNullOrWhiteSpace(saveText) ? new TextObject("{=AF_UI_Common_Save}Save").ToString() : saveText);
	}

    [DataSourceProperty]
    public string LengthStatusText
    {
        get => _lengthStatusText;
        private set
        {
            if (_lengthStatusText == value) return;
            _lengthStatusText = value;
            OnPropertyChangedWithValue(value, nameof(LengthStatusText));
        }
    }

    // The widget may truncate before its RealText reaches this VM; preserve that signal.
    public void NotifyInputTruncated()
    {
        if (_inputWasTruncated) return;
        _inputWasTruncated = true;
        UpdateLengthStatus();
    }

    private void UpdateLengthStatus()
    {
        string status = new TextObject("{=AF_UI_Editor_LengthStatus}Characters: {CURRENT} / {MAX}")
            .SetTextVariable("CURRENT", (_editedText ?? "").Length)
            .SetTextVariable("MAX", AnimusForgeTextInputSanitizer.MaxLongEditorChars).ToString();
        LengthStatusText = _inputWasTruncated ? status + "  " + DevTextEditorHelper.GetInputTruncatedText() : status;
    }

	public void ExecuteSave()
	{
		DevTextEditorHelper.InvokeSaveSafely(_onSave, EditedText);
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
