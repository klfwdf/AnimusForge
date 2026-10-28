using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class SpanishTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordGenderEnum
	{
		MasculineSingular,
		MasculinePlural,
		FeminineSingular,
		FemininePlural,
		NeuterSingular,
		NeuterPlural,
		NoDeclination
	}

	private static class GenderTokens
	{
		public const string MasculineSingular = ".MS";

		public const string MasculinePlural = ".MP";

		public const string FeminineSingular = ".FS";

		public const string FemininePlural = ".FP";

		public const string NeuterSingular = ".NS";

		public const string NeuterPlural = ".NP";

		public static readonly List<string> TokenList = new List<string> { ".MS", ".FS", ".NS", ".MP", ".FP", ".NP" };
	}

	private static class FunctionTokens
	{
		public const string DefiniteArticle = ".l";

		public const string DefiniteArticleInUpperCase = ".L";
	}

	[ThreadStatic]
	private static WordGenderEnum _curGender;

	private static readonly Dictionary<string, Dictionary<string, string>> Contractions = new Dictionary<string, Dictionary<string, string>>
	{
		{
			"de",
			new Dictionary<string, string> { { "el", "l " } }
		},
		{
			"a",
			new Dictionary<string, string> { { "el", "l " } }
		}
	};

	private static Dictionary<WordGenderEnum, string> _genderToDefiniteArticle = new Dictionary<WordGenderEnum, string>
	{
		{
			WordGenderEnum.MasculineSingular,
			"el "
		},
		{
			WordGenderEnum.MasculinePlural,
			"los "
		},
		{
			WordGenderEnum.FeminineSingular,
			"la "
		},
		{
			WordGenderEnum.FemininePlural,
			"las "
		},
		{
			WordGenderEnum.NeuterSingular,
			""
		},
		{
			WordGenderEnum.NeuterPlural,
			""
		}
	};

	private static readonly CultureInfo CultureInfo = new CultureInfo("es-es");

	public override CultureInfo CultureInfoForLanguage => CultureInfo;

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		if (GenderTokens.TokenList.Contains(token))
		{
			SetGender(token);
		}
		if (token == ".l" || token == ".L")
		{
			HandleDefiniteArticles(sourceText, token, cursorPos, outputString);
			_curGender = WordGenderEnum.NoDeclination;
		}
	}

	private bool CheckWhiteSpaceAndTextEnd(string sourceText, int cursorPos)
	{
		if (cursorPos < sourceText.Length)
		{
			return !char.IsWhiteSpace(sourceText[cursorPos]);
		}
		return false;
	}

	private void SetGender(string token)
	{
		switch (token)
		{
		case ".MS":
			_curGender = WordGenderEnum.MasculineSingular;
			break;
		case ".MP":
			_curGender = WordGenderEnum.MasculinePlural;
			break;
		case ".FS":
			_curGender = WordGenderEnum.FeminineSingular;
			break;
		case ".FP":
			_curGender = WordGenderEnum.FemininePlural;
			break;
		case ".NS":
			_curGender = WordGenderEnum.NeuterSingular;
			break;
		case ".NP":
			_curGender = WordGenderEnum.NeuterPlural;
			break;
		}
	}

	private void HandleDefiniteArticles(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		if (!CheckWhiteSpaceAndTextEnd(text, cursorPos) || (_curGender != WordGenderEnum.MasculineSingular && _curGender != WordGenderEnum.MasculinePlural && _curGender != WordGenderEnum.FeminineSingular && _curGender != WordGenderEnum.FemininePlural))
		{
			return;
		}
		string text2 = _genderToDefiniteArticle[_curGender];
		bool flag = false;
		if (_curGender == WordGenderEnum.MasculineSingular && HandleContractions(text, text2, cursorPos, out var newVersion))
		{
			text2 = newVersion;
			flag = true;
			if (char.IsWhiteSpace(stringBuilder[stringBuilder.Length - 1]))
			{
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
			}
		}
		if (!flag && token == ".L")
		{
			text2 = char.ToUpper(text2[0]) + text2.Substring(1);
		}
		stringBuilder.Append(text2);
	}

	private bool HandleContractions(string text, string article, int cursorPos, out string newVersion)
	{
		string previousWord = GetPreviousWord(text, cursorPos);
		if (Contractions.TryGetValue(previousWord.ToLower(), out var value) && value.TryGetValue(article.TrimEnd(Array.Empty<char>()), out newVersion))
		{
			return true;
		}
		newVersion = string.Empty;
		return false;
	}

	private string GetPreviousWord(string sourceText, int cursorPos)
	{
		string[] array = sourceText.Substring(0, cursorPos).Split(new char[1] { ' ' });
		int num = array.Length;
		if (num < 2)
		{
			return "";
		}
		return array[num - 2];
	}

	public override void ClearTemporaryData()
	{
		_curGender = WordGenderEnum.NoDeclination;
	}
}
