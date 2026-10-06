using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class FrenchTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordGenderEnum
	{
		Masculine,
		Feminine,
		Neuter,
		NoDeclination
	}

	private enum WordType
	{
		StartingWithaVowel,
		Masculine,
		Feminine,
		Plural,
		None
	}

	private static class GenderTokens
	{
		public const string Masculine = ".M";

		public const string Feminine = ".F";

		public const string Neuter = ".N";

		public const string Plural = ".P";

		public const string Singular = ".S";

		public static readonly List<string> TokenList = new List<string> { ".M", ".F", ".N", ".P", ".S" };
	}

	private static class FunctionTokens
	{
		public const string DefiniteArticle = ".l";

		public const string DefiniteArticleWithBrackets = "{.l}";

		public const string IndefiniteArticle = ".a";

		public const string APreposition = ".c";

		public const string APrepositionFollowedByDefiniteArticle = ".cl";

		public const string DePreposition = ".d";

		public const string DePrepositionFollowedByDefiniteArticle = ".dl";

		public static readonly List<string> TokenList = new List<string> { ".l", ".a", ".d", ".c", ".cl", ".dl" };
	}

	private static char[] Vowels = new char[6] { 'a', 'e', 'i', 'o', 'u', 'h' };

	[ThreadStatic]
	private static Dictionary<string, (string, int, bool)> _wordGroups;

	[ThreadStatic]
	private static WordGenderEnum _curGender;

	[ThreadStatic]
	private static bool _isPlural = false;

	private static List<string> _articles = new List<string> { "le", "la", "les" };

	private static string _articleVowelStart = "l'";

	private static string _dePreposition = "de";

	private static string _dePrepositionWithVowel = "d'";

	private static string _aPreposition = "à";

	private static readonly Dictionary<string, Dictionary<string, string>> Contractions = new Dictionary<string, Dictionary<string, string>>
	{
		{
			"de",
			new Dictionary<string, string>
			{
				{ "les", "des" },
				{ "le", "du" }
			}
		},
		{
			"à",
			new Dictionary<string, string>
			{
				{ "les", "aux" },
				{ "le", "au" }
			}
		}
	};

	private static Dictionary<WordType, string> _genderToDefiniteArticle = new Dictionary<WordType, string>
	{
		{
			WordType.Masculine,
			"le "
		},
		{
			WordType.Feminine,
			"la "
		},
		{
			WordType.Plural,
			"les "
		},
		{
			WordType.StartingWithaVowel,
			"l'"
		},
		{
			WordType.None,
			""
		}
	};

	private static Dictionary<WordType, string> _genderToIndefiniteArticle = new Dictionary<WordType, string>
	{
		{
			WordType.Masculine,
			"un "
		},
		{
			WordType.Feminine,
			"une "
		},
		{
			WordType.Plural,
			"des "
		},
		{
			WordType.StartingWithaVowel,
			""
		},
		{
			WordType.None,
			""
		}
	};

	private static List<string> _shouldBeConsideredConsonants = new List<string>
	{
		"hache", "hachette", "héros", "houe", "haute", "hardes", "hachoir", "harnais", "harpon", "haubert",
		"haut", "horde"
	};

	private static readonly CultureInfo CultureInfo = new CultureInfo("fr-FR");

	public static Dictionary<string, (string, int, bool)> WordGroups
	{
		get
		{
			if (_wordGroups == null)
			{
				_wordGroups = new Dictionary<string, (string, int, bool)>();
			}
			return _wordGroups;
		}
	}

	private string LinkTag => ".link";

	private int LinkTagLength => 7;

	private string LinkStarter => "<a style=\"Link.";

	private string LinkEnding => "</b></a>";

	public override CultureInfo CultureInfoForLanguage => CultureInfo;

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		if (sourceText.Length > LinkStarter.Length + cursorPos)
		{
			string text = sourceText.Substring(cursorPos, LinkStarter.Length);
			if (token == LinkTag || text.Equals(LinkStarter))
			{
				cursorPos = ProcessLink(sourceText, cursorPos, token, outputString);
			}
		}
		string text2 = token.ToLower();
		if (GenderTokens.TokenList.IndexOf(token) >= 0)
		{
			SetGenderInfo(token);
			ProcessWordGroup(sourceText, token, cursorPos);
			ResetGender();
		}
		else
		{
			if (FunctionTokens.TokenList.IndexOf(text2) < 0 || !CheckWhiteSpaceAndTextEnd(sourceText, cursorPos))
			{
				return;
			}
			if (IsWordGroup(sourceText, token, cursorPos, out var tags))
			{
				SetGenderInfo(tags.Item1);
				if (tags.Item2)
				{
					SetPlural();
				}
				else
				{
					SetSingular();
				}
			}
			switch (text2)
			{
			case ".cl":
				HandleAPrepositionFollowedByDefiniteArticle(sourceText, token, ref cursorPos, outputString);
				break;
			case ".dl":
				HandleDePrepositionFollowedByArticle(sourceText, token, ref cursorPos, outputString);
				break;
			case ".l":
				HandleDefiniteArticles(sourceText, token, cursorPos, outputString);
				break;
			case ".a":
				HandleIndefiniteArticles(sourceText, token, cursorPos, outputString);
				break;
			case ".d":
				HandleDePreposition(sourceText, token, ref cursorPos, outputString);
				break;
			case ".c":
				HandleAPreposition(sourceText, token, ref cursorPos, outputString);
				break;
			}
			_isPlural = false;
			_curGender = WordGenderEnum.NoDeclination;
		}
	}

	private bool IsWordGroup(string sourceText, string token, int cursorPos, out (string, bool) tags)
	{
		int num = 0;
		string text = string.Empty;
		tags = (string.Empty, false);
		foreach (KeyValuePair<string, (string, int, bool)> wordGroup in WordGroups)
		{
			if (wordGroup.Key.Length > 0 && sourceText.Length >= cursorPos + wordGroup.Key.Length && wordGroup.Key.Length > num && wordGroup.Key.Equals(sourceText.Substring(cursorPos, wordGroup.Key.Length)))
			{
				text = wordGroup.Key;
				num = wordGroup.Key.Length;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			tags = (WordGroups[text].Item1, WordGroups[text].Item3);
			return true;
		}
		return false;
	}

	private bool CheckNextCharIsVowel(string sourceText, int cursorPos)
	{
		if (cursorPos < sourceText.Length)
		{
			return Vowels.Contains(char.ToLower(sourceText[cursorPos]));
		}
		return false;
	}

	private bool CheckWhiteSpaceAndTextEnd(string sourceText, int cursorPos)
	{
		if (cursorPos < sourceText.Length)
		{
			return !char.IsWhiteSpace(sourceText[cursorPos]);
		}
		return false;
	}

	private void SetFeminine()
	{
		_curGender = WordGenderEnum.Feminine;
	}

	private void SetNeuter()
	{
		_curGender = WordGenderEnum.Neuter;
	}

	private void SetMasculine()
	{
		_curGender = WordGenderEnum.Masculine;
	}

	private void SetPlural()
	{
		_isPlural = true;
	}

	private void SetSingular()
	{
		_isPlural = false;
	}

	private void HandleDefiniteArticles(string text, string token, int cursorPos, StringBuilder outputString)
	{
		string definiteArticle = GetDefiniteArticle(text, token, cursorPos);
		if (!string.IsNullOrEmpty(definiteArticle))
		{
			outputString.Append(definiteArticle);
		}
	}

	private string GetDefiniteArticle(string text, string token, int cursorPos)
	{
		string text2 = null;
		if (_curGender != WordGenderEnum.NoDeclination)
		{
			text2 = (_isPlural ? _genderToDefiniteArticle[WordType.Plural] : ((!CheckNextCharIsVowel(text, cursorPos) || CheckIfNextWordShouldBeConsideredAConsonant(text, token, cursorPos)) ? _genderToDefiniteArticle[GetWordTypeFromGender(_curGender)] : _genderToDefiniteArticle[WordType.StartingWithaVowel]));
			string text3 = token.ToLowerInvariant();
			if (text3 == ".cl" || text3 == ".dl")
			{
				if (char.IsUpper(token[2]))
				{
					text2 = char.ToUpper(text2[0]) + text2.Substring(1);
				}
			}
			else if (text2 != null && token.All((char x) => !char.IsLetter(x) || char.IsUpper(x)))
			{
				text2 = char.ToUpper(text2[0]) + text2.Substring(1);
			}
		}
		return text2;
	}

	private void HandleIndefiniteArticles(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		string text2 = null;
		if (_isPlural)
		{
			text2 = _genderToIndefiniteArticle[WordType.Plural];
		}
		else if (_curGender != WordGenderEnum.NoDeclination)
		{
			text2 = _genderToIndefiniteArticle[GetWordTypeFromGender(_curGender)];
		}
		if (!string.IsNullOrEmpty(text2))
		{
			if (token.All((char x) => !char.IsLetter(x) || char.IsUpper(x)))
			{
				text2 = char.ToUpper(text2[0]) + text2.Substring(1);
			}
			stringBuilder.Append(text2);
		}
	}

	private void HandleAPreposition(string text, string token, ref int cursorPos, StringBuilder outputString)
	{
		string aPreposition = GetAPreposition(text, token, cursorPos);
		string nextWord = GetNextWord(text, token, cursorPos);
		if (CheckIfWordsHaveContraction(aPreposition, nextWord.Trim(), out var result))
		{
			outputString.Append(result);
			cursorPos += nextWord.Length;
		}
		else
		{
			outputString.Append(aPreposition + " ");
		}
	}

	private string GetAPreposition(string text, string token, int cursorPos)
	{
		string text2 = _aPreposition;
		if (char.IsUpper(token[1]))
		{
			text2 = char.ToUpper(text2[0]) + text2.Substring(1);
		}
		return text2;
	}

	private void HandleAPrepositionFollowedByDefiniteArticle(string text, string token, ref int cursorPos, StringBuilder outputString)
	{
		string aPreposition = GetAPreposition(text, token, cursorPos);
		string definiteArticle = GetDefiniteArticle(text, token, cursorPos);
		string result = string.Empty;
		if (definiteArticle != null && CheckIfWordsHaveContraction(aPreposition, definiteArticle.Trim(), out result))
		{
			if (char.IsUpper(token[1]))
			{
				result = char.ToUpper(result[0]) + result.Substring(1);
			}
			outputString.Append(result + " ");
		}
		else
		{
			outputString.Append(aPreposition + " " + definiteArticle);
		}
	}

	private void HandleDePrepositionFollowedByArticle(string text, string token, ref int cursorPos, StringBuilder outputString)
	{
		string dePreposition = GetDePreposition(text, token, cursorPos);
		string definiteArticle = GetDefiniteArticle(text, token, cursorPos);
		string result = string.Empty;
		if (definiteArticle != null && CheckIfWordsHaveContraction(dePreposition, definiteArticle.Trim(), out result))
		{
			if (char.IsUpper(token[1]))
			{
				result = char.ToUpper(result[0]) + result.Substring(1);
			}
			outputString.Append(result + " ");
		}
		else
		{
			outputString.Append(dePreposition + " " + definiteArticle);
		}
	}

	private string GetDePreposition(string text, string token, int cursorPos)
	{
		string text2 = _dePreposition;
		if (char.IsUpper(token[1]))
		{
			text2 = char.ToUpper(text2[0]) + text2.Substring(1);
		}
		return text2;
	}

	private void HandleDePreposition(string text, string token, ref int cursorPos, StringBuilder outputString)
	{
		string dePreposition = GetDePreposition(text, token, cursorPos);
		string nextWord = GetNextWord(text, token, cursorPos);
		bool flag = CheckNextCharIsVowel(text, cursorPos) && !CheckIfNextWordShouldBeConsideredAConsonant(text, token, cursorPos);
		string result;
		if (!CheckIfWordIsAnArticle(nextWord) && flag)
		{
			outputString.Append(_dePrepositionWithVowel);
		}
		else if (CheckIfWordsHaveContraction(dePreposition, nextWord.Trim(), out result))
		{
			outputString.Append(result);
			cursorPos += nextWord.Length;
		}
		else
		{
			outputString.Append(dePreposition + " ");
		}
	}

	private bool CheckIfNextWordShouldBeConsideredAConsonant(string text, string token, int cursorPos)
	{
		string nextWord = GetNextWord(text, token, cursorPos);
		if (!string.IsNullOrEmpty(nextWord))
		{
			return _shouldBeConsideredConsonants.Contains(nextWord.ToLowerInvariant());
		}
		return false;
	}

	private bool CheckIfWordsHaveContraction(string t1, string t2, out string result)
	{
		result = string.Empty;
		if (Contractions.TryGetValue(t1.ToLowerInvariant(), out var value) && value.TryGetValue(t2.ToLowerInvariant(), out var value2))
		{
			result = value2;
			return true;
		}
		return false;
	}

	private bool CheckIfWordIsAnArticle(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			text = text.ToLowerInvariant();
			if (!_articles.Contains(text))
			{
				return text.StartsWith(_articleVowelStart);
			}
			return true;
		}
		return false;
	}

	private string GetNextWord(string text, string token, int cursorPos)
	{
		if (cursorPos - token.Length - 2 < text.IndexOf('}'))
		{
			return text.Remove(0, text.IndexOf('}') + 1).Split(new char[1] { ' ' })[0];
		}
		return "";
	}

	private WordType GetWordTypeFromGender(WordGenderEnum gender)
	{
		return gender switch
		{
			WordGenderEnum.Masculine => WordType.Masculine, 
			WordGenderEnum.Feminine => WordType.Feminine, 
			_ => WordType.None, 
		};
	}

	private void SetGenderInfo(string token)
	{
		switch (token)
		{
		case ".M":
			SetMasculine();
			break;
		case ".F":
			SetFeminine();
			break;
		case ".N":
			SetNeuter();
			break;
		case ".P":
			SetPlural();
			break;
		case ".S":
			SetSingular();
			break;
		}
	}

	private void ProcessWordGroup(string text, string token, int cursorPos)
	{
		string key = text.Substring(text.LastIndexOf('}') + 1);
		if (WordGroups.TryGetValue(key, out var value))
		{
			WordGroups[key] = (value.Item1, value.Item2, _isPlural);
		}
		else
		{
			WordGroups.Add(key, (token, cursorPos, _isPlural));
		}
	}

	private void ResetGender()
	{
		_curGender = WordGenderEnum.NoDeclination;
		_isPlural = false;
	}

	private int ProcessLink(string text, int cursorPos, string token, StringBuilder outputString)
	{
		int num = text.IndexOf(LinkEnding, cursorPos);
		if (num > cursorPos)
		{
			string text2 = text.Substring(cursorPos, num - cursorPos);
			string text3 = text2.Substring(0, text2.LastIndexOf('>') + 1);
			string key = text2.Substring(text3.Length);
			ResetGender();
			if (token != LinkTag && WordGroups.TryGetValue(key, out var value))
			{
				SetGenderInfo(value.Item1);
				if (value.Item3)
				{
					SetPlural();
				}
				else
				{
					SetSingular();
				}
			}
			outputString.Append(text3);
			return cursorPos + text3.Length;
		}
		return cursorPos;
	}

	public override void ClearTemporaryData()
	{
		WordGroups.Clear();
		_isPlural = false;
		_curGender = WordGenderEnum.NoDeclination;
	}
}
