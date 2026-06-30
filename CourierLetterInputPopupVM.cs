using System;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class CourierLetterInputPopupVM : ViewModel
{
	private readonly Action<string> _onSubmit;
	private readonly Action _onCancel;
	private string _titleText;
	private string _subtitleText;
	private string _inputHintText;
	private string _inputText;

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
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
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, "SubtitleText");
			}
		}
	}

	[DataSourceProperty]
	public string InputHintText
	{
		get => _inputHintText;
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
	public string InputText
	{
		get => _inputText;
		set
		{
			string text = AnimusForgeTextInputSanitizer.SanitizeMultiline(value, AnimusForgeTextInputSanitizer.MaxCourierLetterChars);
			if (text != _inputText)
			{
				_inputText = text;
				OnPropertyChangedWithValue(text, "InputText");
			}
		}
	}

	public CourierLetterInputPopupVM(string titleText, string subtitleText, string inputHintText, string initialText, Action<string> onSubmit, Action onCancel)
	{
		_onSubmit = onSubmit;
		_onCancel = onCancel;
		TitleText = titleText ?? "";
		SubtitleText = subtitleText ?? "";
		InputHintText = inputHintText ?? "";
		InputText = initialText ?? "";
	}

	public void ExecuteSubmit()
	{
		if (string.IsNullOrWhiteSpace(InputText))
		{
			_onCancel?.Invoke();
		}
		else
		{
			_onSubmit?.Invoke(AnimusForgeTextInputSanitizer.SanitizeMultiline(InputText, AnimusForgeTextInputSanitizer.MaxCourierLetterChars));
		}
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
