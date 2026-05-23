using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class ItalianTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordType
	{
		Vowel,
		SpecialConsonant,
		Consonant,
		Other
	}

	private enum WordGenderEnum
	{
		MasculineSingular,
		MasculinePlural,
		FeminineSingular,
		FemininePlural,
		MaleNoun,
		FemaleNoun,
		NoDeclination
	}

	private enum Prepositions
	{
		To,
		Of,
		From,
		In,
		On
	}

	private static class GenderTokens
	{
		public const string MasculineSingular = ".MS";

		public const string MasculinePlural = ".MP";

		public const string FeminineSingular = ".FS";

		public const string FemininePlural = ".FP";

		public const string MaleNoun = ".MN";

		public const string FemaleNoun = ".FN";

		public static readonly List<string> TokenList = new List<string> { ".MS", ".MP", ".FS", ".FP", ".MN", ".FN" };
	}

	private static class FunctionTokens
	{
		public const string DefiniteArticle = ".l";

		public const string IndefiniteArticle = ".un";

		public const string OfPreposition = ".di";

		public const string ToPreposition = ".a";

		public const string FromPreposition = ".da";

		public const string OnPreposition = ".su";

		public const string InPreposition = ".in";

		public static readonly List<string> TokenList = new List<string> { ".l", ".un", ".di", ".a", ".da", ".su", ".in" };
	}

	private static char[] Vowels = new char[5] { 'a', 'e', 'i', 'o', 'u' };

	private static char[] SpecialConsonantBeginnings = new char[1] { 's' };

	private static string[] SpecialConsonants = new string[6] { "x", "y", "gn", "z", "ps", "pn" };

	private static char[] Consonants = new char[18]
	{
		'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm',
		'n', 'p', 'q', 'r', 's', 't', 'v', 'z'
	};

	[ThreadStatic]
	private static WordGenderEnum _curGender;

	[ThreadStatic]
	private static Dictionary<string, (string, int)> _wordGroups = new Dictionary<string, (string, int)>();

	private static Dictionary<Prepositions, Dictionary<WordGenderEnum, Dictionary<WordType, string>>> _prepositionDictionary = new Dictionary<Prepositions, Dictionary<WordGenderEnum, Dictionary<WordType, string>>>
	{
		{
			Prepositions.Of,
			new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
			{
				{
					WordGenderEnum.MasculineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"del "
						},
						{
							WordType.SpecialConsonant,
							"dello "
						},
						{
							WordType.Vowel,
							"dell'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MasculinePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"dei "
						},
						{
							WordType.SpecialConsonant,
							"degli "
						},
						{
							WordType.Vowel,
							"degli "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FeminineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"della "
						},
						{
							WordType.SpecialConsonant,
							"della "
						},
						{
							WordType.Vowel,
							"dell'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemininePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"delle "
						},
						{
							WordType.SpecialConsonant,
							"delle "
						},
						{
							WordType.Vowel,
							"delle "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"di "
						},
						{
							WordType.SpecialConsonant,
							"di "
						},
						{
							WordType.Vowel,
							"di "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"di "
						},
						{
							WordType.SpecialConsonant,
							"di "
						},
						{
							WordType.Vowel,
							"di "
						},
						{
							WordType.Other,
							""
						}
					}
				}
			}
		},
		{
			Prepositions.To,
			new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
			{
				{
					WordGenderEnum.MasculineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"al "
						},
						{
							WordType.SpecialConsonant,
							"allo "
						},
						{
							WordType.Vowel,
							"all'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MasculinePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"ai "
						},
						{
							WordType.SpecialConsonant,
							"agli "
						},
						{
							WordType.Vowel,
							"agli "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FeminineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"alla "
						},
						{
							WordType.SpecialConsonant,
							"alla "
						},
						{
							WordType.Vowel,
							"all'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemininePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"alle "
						},
						{
							WordType.SpecialConsonant,
							"alle "
						},
						{
							WordType.Vowel,
							"alle "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"a "
						},
						{
							WordType.SpecialConsonant,
							"a "
						},
						{
							WordType.Vowel,
							"a "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"a "
						},
						{
							WordType.SpecialConsonant,
							"a "
						},
						{
							WordType.Vowel,
							"a "
						},
						{
							WordType.Other,
							""
						}
					}
				}
			}
		},
		{
			Prepositions.From,
			new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
			{
				{
					WordGenderEnum.MasculineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"dal "
						},
						{
							WordType.SpecialConsonant,
							"dallo "
						},
						{
							WordType.Vowel,
							"dall'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MasculinePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"dai "
						},
						{
							WordType.SpecialConsonant,
							"dagli "
						},
						{
							WordType.Vowel,
							"dagli "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FeminineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"dalla "
						},
						{
							WordType.SpecialConsonant,
							"dalla "
						},
						{
							WordType.Vowel,
							"dall'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemininePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"dalle "
						},
						{
							WordType.SpecialConsonant,
							"dalle "
						},
						{
							WordType.Vowel,
							"dalle "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"da "
						},
						{
							WordType.SpecialConsonant,
							"da "
						},
						{
							WordType.Vowel,
							"da "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"da "
						},
						{
							WordType.SpecialConsonant,
							"da "
						},
						{
							WordType.Vowel,
							"da "
						},
						{
							WordType.Other,
							""
						}
					}
				}
			}
		},
		{
			Prepositions.On,
			new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
			{
				{
					WordGenderEnum.MasculineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"sul "
						},
						{
							WordType.SpecialConsonant,
							"sullo "
						},
						{
							WordType.Vowel,
							"sull'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MasculinePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"sui "
						},
						{
							WordType.SpecialConsonant,
							"sugli "
						},
						{
							WordType.Vowel,
							"sugli "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FeminineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"sulla "
						},
						{
							WordType.SpecialConsonant,
							"sulla "
						},
						{
							WordType.Vowel,
							"sull'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemininePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"sulle "
						},
						{
							WordType.SpecialConsonant,
							"sulle "
						},
						{
							WordType.Vowel,
							"sulle "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"su "
						},
						{
							WordType.SpecialConsonant,
							"su "
						},
						{
							WordType.Vowel,
							"su "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"su "
						},
						{
							WordType.SpecialConsonant,
							"su "
						},
						{
							WordType.Vowel,
							"su "
						},
						{
							WordType.Other,
							""
						}
					}
				}
			}
		},
		{
			Prepositions.In,
			new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
			{
				{
					WordGenderEnum.MasculineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"nel "
						},
						{
							WordType.SpecialConsonant,
							"nello "
						},
						{
							WordType.Vowel,
							"nell'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MasculinePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"nei "
						},
						{
							WordType.SpecialConsonant,
							"negli "
						},
						{
							WordType.Vowel,
							"negli "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FeminineSingular,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"nella "
						},
						{
							WordType.SpecialConsonant,
							"nella "
						},
						{
							WordType.Vowel,
							"nell'"
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemininePlural,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"nelle "
						},
						{
							WordType.SpecialConsonant,
							"nelle "
						},
						{
							WordType.Vowel,
							"nelle "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.MaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"in "
						},
						{
							WordType.SpecialConsonant,
							"in "
						},
						{
							WordType.Vowel,
							"in "
						},
						{
							WordType.Other,
							""
						}
					}
				},
				{
					WordGenderEnum.FemaleNoun,
					new Dictionary<WordType, string>
					{
						{
							WordType.Consonant,
							"in "
						},
						{
							WordType.SpecialConsonant,
							"in "
						},
						{
							WordType.Vowel,
							"in "
						},
						{
							WordType.Other,
							""
						}
					}
				}
			}
		}
	};

	private static Dictionary<WordGenderEnum, Dictionary<WordType, string>> _genderWordTypeDefiniteArticleDictionary = new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
	{
		{
			WordGenderEnum.MasculineSingular,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"il "
				},
				{
					WordType.SpecialConsonant,
					"lo "
				},
				{
					WordType.Vowel,
					"l'"
				}
			}
		},
		{
			WordGenderEnum.MasculinePlural,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"i "
				},
				{
					WordType.SpecialConsonant,
					"gli "
				},
				{
					WordType.Vowel,
					"gli "
				}
			}
		},
		{
			WordGenderEnum.FeminineSingular,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"la "
				},
				{
					WordType.SpecialConsonant,
					"la "
				},
				{
					WordType.Vowel,
					"l'"
				}
			}
		},
		{
			WordGenderEnum.FemininePlural,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"le "
				},
				{
					WordType.SpecialConsonant,
					"le "
				},
				{
					WordType.Vowel,
					"le "
				}
			}
		}
	};

	private static Dictionary<WordGenderEnum, Dictionary<WordType, string>> _genderWordTypeIndefiniteArticleDictionary = new Dictionary<WordGenderEnum, Dictionary<WordType, string>>
	{
		{
			WordGenderEnum.MasculineSingular,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"un "
				},
				{
					WordType.SpecialConsonant,
					"uno "
				},
				{
					WordType.Vowel,
					"un "
				}
			}
		},
		{
			WordGenderEnum.MasculinePlural,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"dei "
				},
				{
					WordType.SpecialConsonant,
					"degli "
				},
				{
					WordType.Vowel,
					"degli "
				}
			}
		},
		{
			WordGenderEnum.FeminineSingular,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"una "
				},
				{
					WordType.SpecialConsonant,
					"una "
				},
				{
					WordType.Vowel,
					"un'"
				}
			}
		},
		{
			WordGenderEnum.FemininePlural,
			new Dictionary<WordType, string>
			{
				{
					WordType.Consonant,
					"delle "
				},
				{
					WordType.SpecialConsonant,
					"delle "
				},
				{
					WordType.Vowel,
					"delle "
				}
			}
		}
	};

	private static readonly CultureInfo CultureInfo = new CultureInfo("it-IT");

	private string LinkTag => ".link";

	private int LinkTagLength => 7;

	private string LinkStarter => "<a style=\"Link.";

	private string LinkEnding => "</b></a>";

	public static Dictionary<string, (string, int)> WordGroups
	{
		get
		{
			if (_wordGroups == null)
			{
				_wordGroups = new Dictionary<string, (string, int)>();
			}
			return _wordGroups;
		}
	}

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
		}
		else if (FunctionTokens.TokenList.IndexOf(text2) >= 0 && CheckWhiteSpaceAndTextEnd(sourceText, cursorPos))
		{
			if (IsWordGroup(sourceText, token, cursorPos, out var tag))
			{
				SetGenderInfo(tag);
			}
			switch (text2)
			{
			case ".l":
				HandleDefiniteArticles(sourceText, token, cursorPos, outputString);
				break;
			case ".un":
				HandleIndefiniteArticles(sourceText, token, cursorPos, outputString);
				break;
			case ".da":
				HandleFromPrepositions(sourceText, token, cursorPos, outputString);
				break;
			case ".in":
				HandleInPrepositions(sourceText, token, cursorPos, outputString);
				break;
			case ".su":
				HandleOnPrepositions(sourceText, token, cursorPos, outputString);
				break;
			case ".di":
				HandleOfPrepositions(sourceText, token, cursorPos, outputString);
				break;
			case ".a":
				HandleToPrepositions(sourceText, token, cursorPos, outputString);
				break;
			}
			_curGender = WordGenderEnum.NoDeclination;
		}
	}

	private bool IsWordGroup(string sourceText, string token, int cursorPos, out string tag)
	{
		int num = 0;
		string text = string.Empty;
		tag = string.Empty;
		foreach (KeyValuePair<string, (string, int)> wordGroup in WordGroups)
		{
			if (wordGroup.Key.Length > 0 && sourceText.Length >= cursorPos + wordGroup.Key.Length && wordGroup.Key.Length > num && wordGroup.Key.Equals(sourceText.Substring(cursorPos, wordGroup.Key.Length)))
			{
				text = wordGroup.Key;
				num = wordGroup.Key.Length;
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			tag = WordGroups[text].Item1;
			return true;
		}
		return false;
	}

	private WordType GetNextWordType(string sourceText, int cursorPos)
	{
		if (cursorPos >= sourceText.Length - 1)
		{
			return WordType.Other;
		}
		char c = char.ToLower(sourceText[cursorPos]);
		char value = char.ToLower(sourceText[cursorPos + 1]);
		string text = sourceText.Substring(cursorPos, 2).ToLowerInvariant();
		if (Vowels.Contains(c))
		{
			return WordType.Vowel;
		}
		string[] specialConsonants = SpecialConsonants;
		foreach (string value2 in specialConsonants)
		{
			if (text.StartsWith(value2))
			{
				return WordType.SpecialConsonant;
			}
		}
		if (SpecialConsonantBeginnings.Contains(c) && Consonants.Contains(value))
		{
			return WordType.SpecialConsonant;
		}
		if (char.IsLetter(c))
		{
			return WordType.Consonant;
		}
		return WordType.Other;
	}

	private bool CheckWhiteSpaceAndTextEnd(string sourceText, int cursorPos)
	{
		if (cursorPos < sourceText.Length)
		{
			return !char.IsWhiteSpace(sourceText[cursorPos]);
		}
		return false;
	}

	private void HandleOnPrepositions(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		Prepositions key = Prepositions.On;
		Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary = _prepositionDictionary[key];
		HandlePrepositionsInternal(text, token, cursorPos, dictionary, stringBuilder);
	}

	private void HandleInPrepositions(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		Prepositions key = Prepositions.In;
		Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary = _prepositionDictionary[key];
		HandlePrepositionsInternal(text, token, cursorPos, dictionary, stringBuilder);
	}

	private void HandleOfPrepositions(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		Prepositions key = Prepositions.Of;
		Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary = _prepositionDictionary[key];
		HandlePrepositionsInternal(text, token, cursorPos, dictionary, stringBuilder);
	}

	private void HandleToPrepositions(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		Prepositions key = Prepositions.To;
		Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary = _prepositionDictionary[key];
		HandlePrepositionsInternal(text, token, cursorPos, dictionary, stringBuilder);
	}

	private void HandleFromPrepositions(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		Prepositions key = Prepositions.From;
		Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary = _prepositionDictionary[key];
		HandlePrepositionsInternal(text, token, cursorPos, dictionary, stringBuilder);
	}

	private void HandlePrepositionsInternal(string text, string token, int cursorPos, Dictionary<WordGenderEnum, Dictionary<WordType, string>> dictionary, StringBuilder stringBuilder)
	{
		WordType nextWordType = GetNextWordType(text, cursorPos);
		if (nextWordType != WordType.Other && _curGender != WordGenderEnum.NoDeclination)
		{
			string text2 = dictionary[_curGender][nextWordType];
			if (char.IsUpper(token[1]))
			{
				text2 = char.ToUpper(text2[0]) + text2.Substring(1);
			}
			stringBuilder.Append(text2);
		}
	}

	private void HandleDefiniteArticles(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		WordType nextWordType = GetNextWordType(text, cursorPos);
		if (nextWordType != WordType.Other && _curGender != WordGenderEnum.MaleNoun && _curGender != WordGenderEnum.FemaleNoun && _curGender != WordGenderEnum.NoDeclination)
		{
			string text2 = _genderWordTypeDefiniteArticleDictionary[_curGender][nextWordType];
			if (char.IsUpper(token[1]))
			{
				text2 = char.ToUpper(text2[0]) + text2.Substring(1);
			}
			stringBuilder.Append(text2);
		}
	}

	private void HandleIndefiniteArticles(string text, string token, int cursorPos, StringBuilder stringBuilder)
	{
		WordType nextWordType = GetNextWordType(text, cursorPos);
		if (nextWordType != WordType.Other && _curGender != WordGenderEnum.MaleNoun && _curGender != WordGenderEnum.FemaleNoun && _curGender != WordGenderEnum.NoDeclination)
		{
			string text2 = _genderWordTypeIndefiniteArticleDictionary[_curGender][nextWordType];
			if (char.IsUpper(token[1]))
			{
				text2 = char.ToUpper(text2[0]) + text2.Substring(1);
			}
			stringBuilder.Append(text2);
		}
	}

	private void SetGenderInfo(string token)
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
		case ".MN":
			_curGender = WordGenderEnum.MaleNoun;
			break;
		case ".FN":
			_curGender = WordGenderEnum.FemaleNoun;
			break;
		}
	}

	private void ProcessWordGroup(string text, string token, int cursorPos)
	{
		string key = text.Substring(text.LastIndexOf('}') + 1);
		if (WordGroups.TryGetValue(key, out var value))
		{
			WordGroups[key] = (value.Item1, value.Item2);
		}
		else
		{
			WordGroups.Add(key, (token, cursorPos));
		}
	}

	private int ProcessLink(string text, int cursorPos, string token, StringBuilder outputString)
	{
		int num = text.IndexOf(LinkEnding, cursorPos);
		if (num > cursorPos)
		{
			string text2 = text.Substring(cursorPos, num - cursorPos);
			string text3 = text2.Substring(0, text2.LastIndexOf('>') + 1);
			string key = text2.Substring(text3.Length);
			if (token != LinkTag && WordGroups.TryGetValue(key, out var value))
			{
				SetGenderInfo(value.Item1);
			}
			outputString.Append(text3);
			return cursorPos + text3.Length;
		}
		return cursorPos;
	}

	public override void ClearTemporaryData()
	{
		WordGroups.Clear();
		_curGender = WordGenderEnum.NoDeclination;
	}
}
