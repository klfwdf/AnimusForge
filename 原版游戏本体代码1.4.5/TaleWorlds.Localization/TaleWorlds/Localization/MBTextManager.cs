using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;
using TaleWorlds.Localization.TextProcessor.LanguageProcessors;

namespace TaleWorlds.Localization;

public static class MBTextManager
{
	public const string LinkAttribute = "LINK";

	internal const string LinkTag = ".link";

	internal const int LinkTagLength = 7;

	internal const string LinkEnding = "</b></a>";

	internal const int LinkEndingLength = 8;

	internal const string LinkStarter = "<a style=\"Link.";

	private const string CommentRegexPattern = "{%.+?}";

	private const string AnimationTagsRegexPattern = "\\[.+\\]";

	private static readonly TextProcessingContext TextContext = new TextProcessingContext();

	private static LanguageSpecificTextProcessor _languageProcessor = new EnglishTextProcessor();

	private static string _activeVoiceLanguageId = "English";

	private static string _activeTextLanguageId = "English";

	private static int _activeTextLanguageIndex = 0;

	[ThreadStatic]
	private static StringBuilder _idStringBuilder;

	[ThreadStatic]
	private static StringBuilder _targetStringBuilder;

	private static readonly Regex CommentRemoverRegex = new Regex("{%.+?}");

	private static readonly Regex AnimationTagRemoverRegex = new Regex("\\[.+\\]");

	internal static readonly Tokenizer Tokenizer = new Tokenizer();

	public static string ActiveTextLanguage => _activeTextLanguageId;

	public static bool LocalizationDebugMode { get; set; }

	public static bool LanguageExistsInCurrentConfiguration(string language, bool developmentMode)
	{
		return LocalizedTextManager.GetLanguageIds(developmentMode).Any((string l) => l == language);
	}

	public static bool ChangeLanguage(string language)
	{
		if (LocalizedTextManager.GetLanguageIds(developmentMode: true).Any((string l) => l == language))
		{
			_languageProcessor = LocalizedTextManager.CreateTextProcessorForLanguage(language);
			_activeTextLanguageId = language;
			_activeTextLanguageIndex = LocalizedTextManager.GetLanguageIndex(_activeTextLanguageId);
			LocalizedTextManager.LoadLanguage(_activeTextLanguageId);
			return true;
		}
		Debug.FailedAssert("Invalid language", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\MBTextManager.cs", "ChangeLanguage", 141);
		return false;
	}

	public static int GetActiveTextLanguageIndex()
	{
		return _activeTextLanguageIndex;
	}

	public static bool TryChangeVoiceLanguage(string language)
	{
		if (LocalizedVoiceManager.GetVoiceLanguageIds().Any((string l) => l == language))
		{
			_activeVoiceLanguageId = language;
			LocalizedVoiceManager.LoadLanguage(_activeVoiceLanguageId);
			return true;
		}
		return false;
	}

	private static TextObject ProcessNumber(object integer)
	{
		return new TextObject(integer.ToString());
	}

	internal static string ProcessTextToString(TextObject to, bool shouldClear)
	{
		if (to == null)
		{
			return null;
		}
		if (TextObject.IsNullOrEmpty(to))
		{
			return "";
		}
		string localizedText = GetLocalizedText(to.Value);
		string text;
		if (!string.IsNullOrEmpty(to.Value))
		{
			text = Process(localizedText, to);
			text = _languageProcessor.Process(text);
			if (shouldClear)
			{
				_languageProcessor.ClearTemporaryData();
			}
		}
		else
		{
			text = "";
		}
		if (LocalizationDebugMode)
		{
			string text2 = to.GetID();
			if (string.IsNullOrEmpty(text2))
			{
				text2 = "!";
			}
			return "(" + text2 + ") " + text;
		}
		return text;
	}

	internal static string ProcessWithoutLanguageProcessor(TextObject to)
	{
		if (to == null)
		{
			return null;
		}
		if (TextObject.IsNullOrEmpty(to))
		{
			return "";
		}
		string localizedText = GetLocalizedText(to.Value);
		if (!string.IsNullOrEmpty(to.Value))
		{
			return Process(localizedText, to);
		}
		return "";
	}

	private static string Process(string query, TextObject parent = null)
	{
		List<MBTextToken> list = null;
		if (parent != null)
		{
			list = parent.GetCachedTokens();
		}
		if (list == null)
		{
			list = Tokenizer.Tokenize(query);
		}
		return TextGrammarProcessor.Process(MBTextParser.Parse(list), TextContext, parent);
	}

	public static void ClearAll()
	{
		TextContext.ClearAll();
	}

	public static void SetTextVariable(string variableName, string text, bool sendClients = false)
	{
		if (text != null)
		{
			TextContext.SetTextVariable(variableName, new TextObject(text));
		}
	}

	public static void SetTextVariable(string variableName, TextObject text, bool sendClients = false)
	{
		if (!(text == null))
		{
			TextContext.SetTextVariable(variableName, text);
		}
	}

	public static void SetTextVariable(string variableName, int content)
	{
		TextObject text = ProcessNumber(content);
		SetTextVariable(variableName, text);
	}

	public static void SetTextVariable(string variableName, float content, int decimalDigits = 2)
	{
		TextObject text = ProcessNumber(TaleWorlds.Library.MathF.Round(content, decimalDigits));
		SetTextVariable(variableName, text);
	}

	public static void SetTextVariable(string variableName, object content)
	{
		if (content != null)
		{
			TextObject text = new TextObject(content.ToString());
			SetTextVariable(variableName, text);
		}
	}

	public static void SetTextVariable(string variableName, int arrayIndex, object content)
	{
		if (content != null)
		{
			string text = content.ToString();
			SetTextVariable(variableName + ":" + arrayIndex, text);
		}
	}

	public static void SetFunction(string funcName, string functionBody)
	{
		MBTextModel functionBody2 = MBTextParser.Parse(Tokenizer.Tokenize(functionBody));
		TextContext.SetFunction(funcName, functionBody2);
	}

	public static void ResetFunctions()
	{
		TextContext.ResetFunctions();
	}

	public static void ThrowLocalizationError(string message)
	{
		Debug.FailedAssert(message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\MBTextManager.cs", "ThrowLocalizationError", 342);
	}

	internal static string GetLocalizedText(string text)
	{
		if (text != null && text.Length > 2 && text[0] == '{' && text[1] == '=')
		{
			if (_idStringBuilder == null)
			{
				_idStringBuilder = new StringBuilder(8);
			}
			else
			{
				_idStringBuilder.Clear();
			}
			if (_targetStringBuilder == null)
			{
				_targetStringBuilder = new StringBuilder(100);
			}
			else
			{
				_targetStringBuilder.Clear();
			}
			for (int i = 2; i < text.Length; i++)
			{
				if (text[i] != '}')
				{
					_idStringBuilder.Append(text[i]);
					continue;
				}
				for (i++; i < text.Length; i++)
				{
					_targetStringBuilder.Append(text[i]);
				}
				string text2 = "";
				if (_activeTextLanguageId == "English")
				{
					text2 = _targetStringBuilder.ToString();
					return RemoveComments(text2);
				}
				if ((_idStringBuilder.Length == 1 && _idStringBuilder[0] == '*') || (_idStringBuilder.Length == 1 && _idStringBuilder[0] == '!'))
				{
					break;
				}
				if (_activeTextLanguageId != "English")
				{
					text2 = LocalizedTextManager.GetTranslatedText(_activeTextLanguageId, _idStringBuilder.ToString());
				}
				if (text2 == null)
				{
					break;
				}
				return RemoveComments(text2);
			}
			return _targetStringBuilder.ToString();
		}
		return text;
	}

	private static string RemoveComments(string localizedText)
	{
		foreach (Match item in CommentRemoverRegex.Matches(localizedText))
		{
			localizedText = localizedText.Replace(item.Value, "");
		}
		return localizedText;
	}

	public static string DiscardAnimationTagsAndCheckAnimationTagPositions(string text)
	{
		return DiscardAnimationTags(text);
	}

	public static string DiscardAnimationTags(string text)
	{
		string text2 = "";
		bool flag = false;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '[')
			{
				flag = true;
			}
			if (!flag)
			{
				text2 += text[i];
			}
			if (text[i] == ']')
			{
				flag = false;
			}
		}
		return text2;
	}

	private static bool CheckAnimationTagPositions(string text)
	{
		string text2 = "";
		Match match = AnimationTagRemoverRegex.Match(text);
		if (match.Success)
		{
			text2 = DiscardAnimationTags(match.Value);
		}
		return string.IsNullOrEmpty(text2.Replace(" ", ""));
	}

	public static string[] GetConversationAnimations(TextObject to)
	{
		string text = to.CopyTextObject().ToString();
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = new string[4];
		bool flag = false;
		int num = 0;
		if (!string.IsNullOrEmpty(text))
		{
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '[')
				{
					flag = true;
				}
				else
				{
					if (!flag)
					{
						continue;
					}
					if (text[i] == ',' || text[i] == ']')
					{
						to.Value.Contains("{=!}");
						array[num] = stringBuilder.ToString();
						stringBuilder.Clear();
						if (text[i] == ']')
						{
							flag = false;
						}
					}
					else if (text[i] == ':')
					{
						string text2 = stringBuilder.ToString();
						stringBuilder.Clear();
						switch (text2)
						{
						case "ib":
							num = 0;
							break;
						case "if":
							num = 1;
							break;
						case "rb":
							num = 2;
							break;
						case "rf":
							num = 3;
							break;
						}
					}
					else if (text[i] != ' ')
					{
						stringBuilder.Append(text[i]);
					}
				}
			}
		}
		return array;
	}

	public static bool TryGetVoiceObject(TextObject to, out VoiceObject vo, out string vocalizationId)
	{
		if (!TextObject.IsNullOrEmpty(to))
		{
			vo = ProcessTextForVocalization(to, out vocalizationId);
			return true;
		}
		vo = null;
		vocalizationId = null;
		return false;
	}

	private static VoiceObject ProcessTextForVocalization(TextObject to, out string vocalizationId)
	{
		vocalizationId = null;
		if (TextObject.IsNullOrEmpty(to))
		{
			return null;
		}
		string localizationId = GetLocalizationId(to);
		if (localizationId != "!")
		{
			vocalizationId = localizationId;
			return LocalizedVoiceManager.GetLocalizedVoice(localizationId);
		}
		VoiceObject voiceObject = null;
		List<MBTextToken> list = to.GetCachedTokens();
		if (list == null)
		{
			list = Tokenizer.Tokenize(to.Value);
		}
		foreach (MBTextToken item in list)
		{
			if (item.TokenType == TokenType.Identifier)
			{
				voiceObject = ProcessTextForVocalization(TextContext.GetRawTextVariable(item.Value, to), out vocalizationId);
				if (voiceObject != null)
				{
					return voiceObject;
				}
			}
		}
		return null;
	}

	private static string GetLocalizationId(TextObject to)
	{
		if (TextObject.IsNullOrEmpty(to))
		{
			return string.Empty;
		}
		string value = to.Value;
		if (value != null && value.Length > 2 && value[0] == '{' && value[1] == '=')
		{
			int num = 2;
			int i;
			for (i = num; i < value.Length && value[i] != '}'; i++)
			{
			}
			return value.Substring(num, i - num);
		}
		return string.Empty;
	}
}
