using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.Library;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class RussianTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordGenderEnum
	{
		MasculineInanimate,
		MasculineAnimate,
		FeminineInanimate,
		FeminineAnimate,
		NeuterInanimate,
		NeuterAnimate,
		NoDeclination
	}

	private static class NounTokens
	{
		public const string Nominative = ".n";

		public const string NominativePlural = ".p";

		public const string Accusative = ".a";

		public const string Genitive = ".g";

		public const string Instrumental = ".i";

		public const string Locative = ".l";

		public const string Dative = ".d";

		public const string AccusativePlural = ".ap";

		public const string GenitivePlural = ".gp";

		public const string InstrumentalPlural = ".ip";

		public const string LocativePlural = ".lp";

		public const string DativePlural = ".dp";

		public static readonly string[] TokenList = new string[12]
		{
			".n", ".p", ".a", ".g", ".i", ".l", ".d", ".ap", ".gp", ".ip",
			".lp", ".dp"
		};
	}

	private static class AdjectiveTokens
	{
		public const string Nominative = ".j";

		public const string NominativePlural = ".jp";

		public const string Accusative = ".ja";

		public const string Genitive = ".jg";

		public const string Instrumental = ".ji";

		public const string Locative = ".jl";

		public const string Dative = ".jd";

		public const string AccusativePlural = ".jap";

		public const string GenitivePlural = ".jgp";

		public const string InstrumentalPlural = ".jip";

		public const string LocativePlural = ".jlp";

		public const string DativePlural = ".jdp";

		public static readonly string[] TokenList = new string[12]
		{
			".j", ".jp", ".ja", ".jap", ".jg", ".jgp", ".jd", ".jdp", ".jl", ".jlp",
			".ji", ".jip"
		};
	}

	private static class GenderTokens
	{
		public const string MasculineAnimate = ".MA";

		public const string MasculineInanimate = ".MI";

		public const string FeminineAnimate = ".FA";

		public const string FeminineInanimate = ".FI";

		public const string NeuterAnimate = ".NA";

		public const string NeuterInanimate = ".NI";

		public static readonly string[] TokenList = new string[6] { ".MI", ".MA", ".FI", ".FA", ".NI", ".NA" };
	}

	private static class WordGroupTokens
	{
		public const string NounNominativePlural = ".nnp";

		public const string NounNominative = ".nn";

		public const string AdjectiveNominativePlural = ".ajp";

		public const string AdjectiveNominative = ".aj";

		public const string NounNominativePluralWithBrackets = "{.nnp}";

		public const string NounNominativeWithBrackets = "{.nn}";

		public const string AdjectiveNominativePluralWithBrackets = "{.ajp}";

		public const string AdjectiveNominativeWithBrackets = "{.aj}";
	}

	private struct IrregularWord
	{
		public readonly string Nominative;

		public readonly string NominativePlural;

		public readonly string Accusative;

		public readonly string Genitive;

		public readonly string Instrumental;

		public readonly string Locative;

		public readonly string Dative;

		public readonly string AccusativePlural;

		public readonly string GenitivePlural;

		public readonly string InstrumentalPlural;

		public readonly string LocativePlural;

		public readonly string DativePlural;

		public IrregularWord(string nominative, string nominativePlural, string genitive, string genitivePlural, string dative, string dativePlural, string accusative, string accusativePlural, string instrumental, string instrumentalPlural, string locative, string locativePlural)
		{
			Nominative = nominative;
			NominativePlural = nominativePlural;
			Accusative = accusative;
			Genitive = genitive;
			Instrumental = instrumental;
			Locative = locative;
			Dative = dative;
			AccusativePlural = accusativePlural;
			GenitivePlural = genitivePlural;
			InstrumentalPlural = instrumentalPlural;
			LocativePlural = locativePlural;
			DativePlural = dativePlural;
		}
	}

	private static readonly CultureInfo CultureInfo = new CultureInfo("ru-RU");

	[ThreadStatic]
	private static WordGenderEnum _curGender;

	[ThreadStatic]
	private static List<(string wordGroup, int firstMarkerPost)> _wordGroups = new List<(string, int)>();

	[ThreadStatic]
	private static List<string> _wordGroupsNoTags = new List<string>();

	[ThreadStatic]
	private static List<string> _linkList = new List<string>();

	[ThreadStatic]
	private static bool _doesComeFromWordGroup = false;

	private static readonly char[] _vowels = new char[10] { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я' };

	private static readonly char[] _sibilants = new char[4] { 'ж', 'ч', 'ш', 'щ' };

	private static readonly char[] _velars = new char[3] { 'г', 'к', 'х' };

	private static readonly string[] _nounTokens = new string[12]
	{
		".n", ".p", ".g", ".gp", ".d", ".dp", ".a", ".ap", ".i", ".ip",
		".l", ".lp"
	};

	private static readonly Dictionary<char, List<IrregularWord>> _irregularMasculineAnimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private static readonly Dictionary<char, List<IrregularWord>> _irregularMasculineInanimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private static readonly Dictionary<char, List<IrregularWord>> _irregularFeminineAnimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private static readonly Dictionary<char, List<IrregularWord>> _irregularFeminineInanimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private static readonly Dictionary<char, List<IrregularWord>> _irregularNeuterAnimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private static readonly Dictionary<char, List<IrregularWord>> _irregularNeuterInanimateDictionary = new Dictionary<char, List<IrregularWord>> { 
	{
		'A',
		new List<IrregularWord>
		{
			new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym")
		}
	} };

	private const string Consonants = "БбВвГгДдЖжЗзЙйКкЛлМмНнПпРрСсТтФфХхЦцЧчШшЩщЬьЪъ";

	private static Dictionary<string, Dictionary<string, string>> exceptions = new Dictionary<string, Dictionary<string, string>>
	{
		{
			"стургиец",
			new Dictionary<string, string>
			{
				{ ".g", "стургийца" },
				{ ".d", "стургийцу" },
				{ ".a", "стургийца" },
				{ ".i", "стургийцем" },
				{ ".l", "стургийце" },
				{ ".p", "стургийцы" },
				{ ".gp", "стургийцев" },
				{ ".dp", "стургийцам" },
				{ ".ap", "стургийцев" },
				{ ".ip", "стургийцами" },
				{ ".lp", "стургийцах" }
			}
		},
		{
			"путь",
			new Dictionary<string, string>
			{
				{ ".g", "Пути" },
				{ ".d", "Пути" },
				{ ".a", "Путь" },
				{ ".i", "Путем" },
				{ ".l", "Пути" },
				{ ".p", "Пути" },
				{ ".gp", "Путей" },
				{ ".dp", "Путям" },
				{ ".ap", "Пути" },
				{ ".ip", "Путями" },
				{ ".lp", "Путях" }
			}
		},
		{
			"вилы",
			new Dictionary<string, string>
			{
				{ ".g", "вил" },
				{ ".d", "вилам" },
				{ ".a", "вилы" },
				{ ".i", "вилами" },
				{ ".l", "вилах" },
				{ ".p", "вилы" },
				{ ".gp", "вил" },
				{ ".dp", "вилам" },
				{ ".ap", "вилы" },
				{ ".ip", "вилами" },
				{ ".lp", "вилах" }
			}
		},
		{
			"лес",
			new Dictionary<string, string>
			{
				{ ".g", "Леса" },
				{ ".d", "Лесу" },
				{ ".a", "Лес" },
				{ ".i", "Лесом" },
				{ ".l", "Лесу" },
				{ ".p", "Леса" },
				{ ".gp", "Лесов" },
				{ ".dp", "Лесам" },
				{ ".ap", "Леса" },
				{ ".ip", "Лесами" },
				{ ".lp", "Лесах" }
			}
		},
		{
			"дочь",
			new Dictionary<string, string>
			{
				{ ".g", "Дочери" },
				{ ".d", "Дочери" },
				{ ".a", "Дочь" },
				{ ".i", "Дочерью" },
				{ ".l", "Дочери" },
				{ ".p", "Дочери" },
				{ ".gp", "Дочерей" },
				{ ".dp", "Дочерям" },
				{ ".ap", "Дочерей" },
				{ ".ip", "Дочерями" },
				{ ".lp", "Дочерях" }
			}
		},
		{
			"угол",
			new Dictionary<string, string>
			{
				{ ".g", "Угла" },
				{ ".d", "Углу" },
				{ ".a", "Угол" },
				{ ".i", "Углом" },
				{ ".l", "Углу" },
				{ ".p", "Углы" },
				{ ".gp", "Углов" },
				{ ".dp", "Углам" },
				{ ".ap", "Углы" },
				{ ".ip", "Углами" },
				{ ".lp", "Углах" }
			}
		},
		{
			"козёл",
			new Dictionary<string, string>
			{
				{ ".g", "Козла" },
				{ ".d", "Козлу" },
				{ ".a", "Козла" },
				{ ".i", "Козлом" },
				{ ".l", "Козле" },
				{ ".p", "Козлы" },
				{ ".gp", "Козлов" },
				{ ".dp", "Козлам" },
				{ ".ap", "Козлов" },
				{ ".ip", "Козлами" },
				{ ".lp", "Козлах" }
			}
		},
		{
			"берег",
			new Dictionary<string, string>
			{
				{ ".g", "Берега" },
				{ ".d", "Берегу" },
				{ ".a", "Берег" },
				{ ".i", "Берегом" },
				{ ".l", "Берегу" },
				{ ".p", "Берега" },
				{ ".gp", "Берегов" },
				{ ".dp", "Берегам" },
				{ ".ap", "Берега" },
				{ ".ip", "Берегами" },
				{ ".lp", "Берегах" }
			}
		},
		{
			"стул",
			new Dictionary<string, string>
			{
				{ ".g", "Стула" },
				{ ".d", "Стулу" },
				{ ".a", "Стул" },
				{ ".i", "Стулом" },
				{ ".l", "Стуле" },
				{ ".p", "Стулья" },
				{ ".gp", "Стульев" },
				{ ".dp", "Стульям" },
				{ ".ap", "Стулья" },
				{ ".ip", "Стульями" },
				{ ".lp", "Стульяах" }
			}
		},
		{
			"человек",
			new Dictionary<string, string>
			{
				{ ".g", "Человека" },
				{ ".d", "Человеку" },
				{ ".a", "Человека" },
				{ ".i", "Человеком" },
				{ ".l", "о человеке" },
				{ ".p", "Люди" },
				{ ".gp", "Людей" },
				{ ".dp", "Людям" },
				{ ".ap", "Людей" },
				{ ".ip", "Людьми" },
				{ ".lp", "о людях" }
			}
		},
		{
			"судно",
			new Dictionary<string, string>
			{
				{ ".g", "Судна" },
				{ ".d", "Судну" },
				{ ".a", "Судно" },
				{ ".i", "Судном" },
				{ ".l", "о судне" },
				{ ".p", "Суда" },
				{ ".gp", "Судов" },
				{ ".dp", "Судам" },
				{ ".ap", "Суда" },
				{ ".ip", "Судами" },
				{ ".lp", "о судах" }
			}
		},
		{
			"время",
			new Dictionary<string, string>
			{
				{ ".g", "Времени" },
				{ ".d", "Времени" },
				{ ".a", "Время" },
				{ ".i", "Временем" },
				{ ".l", "о времени" },
				{ ".p", "Времена" },
				{ ".gp", "Времён" },
				{ ".dp", "Временам" },
				{ ".ap", "Времена" },
				{ ".ip", "Временами" },
				{ ".lp", "о временах" }
			}
		},
		{
			"горожанин",
			new Dictionary<string, string>
			{
				{ ".g", "Горожанина" },
				{ ".d", "Горожанину" },
				{ ".a", "Горожанина" },
				{ ".i", "Горожанином" },
				{ ".l", "о горожанине" },
				{ ".p", "Горожане" },
				{ ".gp", "Горожан" },
				{ ".dp", "Горожанам" },
				{ ".ap", "Горожан" },
				{ ".ip", "Горожанами" },
				{ ".lp", "о горожанах" }
			}
		},
		{
			"никто",
			new Dictionary<string, string>
			{
				{ ".g", "Никого" },
				{ ".d", "Никому" },
				{ ".a", "Никого" },
				{ ".i", "Никем" },
				{ ".l", "Ни о ком" },
				{ ".p", "Никто" },
				{ ".gp", "Никого" },
				{ ".dp", "Никому" },
				{ ".ap", "Никого" },
				{ ".ip", "Никем" },
				{ ".lp", "Ни о ком" }
			}
		},
		{
			"ничто",
			new Dictionary<string, string>
			{
				{ ".g", "Ничего" },
				{ ".d", "Ничему" },
				{ ".a", "Ничего" },
				{ ".i", "Ничем" },
				{ ".l", "Ни о чём" },
				{ ".p", "Ничто" },
				{ ".gp", "Ничего" },
				{ ".dp", "Ничему" },
				{ ".ap", "Ничего" },
				{ ".ip", "Ничем" },
				{ ".lp", "Ни о чём" }
			}
		},
		{
			"наш",
			new Dictionary<string, string>
			{
				{ ".g", "Нашего" },
				{ ".d", "Нашему" },
				{ ".a", "Нашего" },
				{ ".i", "Нашим" },
				{ ".l", "о нашем" },
				{ ".p", "Наши" },
				{ ".gp", "Наших" },
				{ ".dp", "Нашим" },
				{ ".ap", "Наших" },
				{ ".ip", "Нашими" },
				{ ".lp", "о наших" }
			}
		},
		{
			"мать",
			new Dictionary<string, string>
			{
				{ ".g", "Матери" },
				{ ".d", "Матери" },
				{ ".a", "Мать" },
				{ ".i", "Матерью" },
				{ ".l", "о матери" },
				{ ".p", "Матери" },
				{ ".gp", "Матерей" },
				{ ".dp", "Матерям" },
				{ ".ap", "Матерей" },
				{ ".ip", "Матерями" },
				{ ".lp", "о матерях" }
			}
		},
		{
			"мастерская",
			new Dictionary<string, string>
			{
				{ ".g", "мастерской" },
				{ ".d", "мастерской" },
				{ ".a", "мастерскую" },
				{ ".i", "мастерской" },
				{ ".l", "мастерской" },
				{ ".p", "мастерские" },
				{ ".gp", "мастерских" },
				{ ".dp", "мастерским" },
				{ ".ap", "мастерские" },
				{ ".ip", "мастерскими" },
				{ ".lp", "мастерских" }
			}
		},
		{
			"медвежья",
			new Dictionary<string, string>
			{
				{ ".g", "медвежьей" },
				{ ".d", "медвежьей" },
				{ ".a", "медвежью" },
				{ ".i", "медвежьей" },
				{ ".l", "медвежьей" }
			}
		},
		{
			"волчья",
			new Dictionary<string, string>
			{
				{ ".g", "волчьей" },
				{ ".d", "волчьей" },
				{ ".a", "волчью" },
				{ ".i", "волчьей" },
				{ ".l", "волчьей" }
			}
		},
		{
			"медвежий",
			new Dictionary<string, string>
			{
				{ ".g", "медвежьего" },
				{ ".d", "медвежьему" },
				{ ".a", "медвежий" },
				{ ".i", "медвежьим" },
				{ ".l", "медвежьем" }
			}
		},
		{
			"волчий",
			new Dictionary<string, string>
			{
				{ ".g", "волчьим" },
				{ ".d", "волчьему" },
				{ ".a", "волчий" },
				{ ".i", "волчьим" },
				{ ".l", "волчьем" }
			}
		}
	};

	public override CultureInfo CultureInfoForLanguage => CultureInfo;

	private bool MasculineAnimate => _curGender == WordGenderEnum.MasculineAnimate;

	private bool MasculineInanimate => _curGender == WordGenderEnum.MasculineInanimate;

	private bool Masculine
	{
		get
		{
			if (_curGender != WordGenderEnum.MasculineAnimate)
			{
				return _curGender == WordGenderEnum.MasculineInanimate;
			}
			return true;
		}
	}

	private bool FeminineAnimate => _curGender == WordGenderEnum.FeminineAnimate;

	private bool FeminineInanimate => _curGender == WordGenderEnum.FeminineInanimate;

	private bool Feminine
	{
		get
		{
			if (_curGender != WordGenderEnum.FeminineAnimate)
			{
				return _curGender == WordGenderEnum.FeminineInanimate;
			}
			return true;
		}
	}

	private bool Neuter
	{
		get
		{
			if (_curGender != WordGenderEnum.NeuterInanimate)
			{
				return _curGender == WordGenderEnum.NeuterAnimate;
			}
			return true;
		}
	}

	private bool NeuterInanimate => _curGender == WordGenderEnum.NeuterInanimate;

	private bool NeuterAnimate => _curGender == WordGenderEnum.NeuterAnimate;

	private bool Animate
	{
		get
		{
			if (_curGender != WordGenderEnum.MasculineAnimate && _curGender != WordGenderEnum.FeminineAnimate)
			{
				return _curGender == WordGenderEnum.NeuterAnimate;
			}
			return true;
		}
	}

	private static List<(string wordGroup, int firstMarkerPost)> WordGroups
	{
		get
		{
			if (_wordGroups == null)
			{
				_wordGroups = new List<(string, int)>();
			}
			return _wordGroups;
		}
	}

	private static List<string> WordGroupsNoTags
	{
		get
		{
			if (_wordGroupsNoTags == null)
			{
				_wordGroupsNoTags = new List<string>();
			}
			return _wordGroupsNoTags;
		}
	}

	private static List<string> LinkList
	{
		get
		{
			if (_linkList == null)
			{
				_linkList = new List<string>();
			}
			return _linkList;
		}
	}

	private string LinkTag => ".link";

	private int LinkTagLength => 7;

	private string LinkStarter => "<a style=\"Link.";

	private string LinkEnding => "</b></a>";

	private int LinkEndingLength => 8;

	public override void ClearTemporaryData()
	{
		LinkList.Clear();
		WordGroups.Clear();
		WordGroupsNoTags.Clear();
		_curGender = WordGenderEnum.NoDeclination;
		_doesComeFromWordGroup = false;
	}

	private static char GetLastCharacter(StringBuilder outputString)
	{
		if (outputString.Length <= 0)
		{
			return '*';
		}
		return outputString[outputString.Length - 1];
	}

	private static string GetEnding(StringBuilder outputString, int numChars)
	{
		numChars = TaleWorlds.Library.MathF.Min(numChars, outputString.Length);
		return outputString.ToString(outputString.Length - numChars, numChars);
	}

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		bool flag = false;
		if (token == LinkTag)
		{
			LinkList.Add(sourceText.Substring(LinkTagLength));
		}
		else if (sourceText.Contains(LinkStarter))
		{
			flag = IsLink(sourceText, token.Length + 2, cursorPos);
		}
		if (flag)
		{
			cursorPos -= LinkEndingLength;
			outputString.Remove(outputString.Length - LinkEndingLength, LinkEndingLength);
		}
		if (token.EndsWith("Creator"))
		{
			outputString.Append("{" + token.Replace("Creator", "") + "}");
		}
		else if (Array.IndexOf(GenderTokens.TokenList, token) >= 0)
		{
			switch (token)
			{
			case ".MA":
				SetMasculineAnimate();
				break;
			case ".MI":
				SetMasculineInanimate();
				break;
			case ".FI":
				SetFeminineInanimate();
				break;
			case ".FA":
				SetFeminineAnimate();
				break;
			case ".NA":
				SetNeuterAnimate();
				break;
			case ".NI":
				SetNeuterInanimate();
				break;
			}
			if (cursorPos == token.Length + 2 && sourceText.IndexOf("{.", cursorPos, StringComparison.InvariantCulture) == -1 && sourceText.IndexOf(' ', cursorPos) == -1)
			{
				WordGroups.Add((sourceText + "{.nn}", cursorPos));
				WordGroupsNoTags.Add(sourceText.Substring(cursorPos));
			}
		}
		else
		{
			switch (token)
			{
			case ".nnp":
			case ".ajp":
			case ".aj":
			case ".nn":
				switch (token)
				{
				case ".nnp":
				case ".ajp":
				case ".aj":
				{
					if (IsIrregularWord(sourceText, cursorPos, token, out var irregularWord2, out var lengthOfWordToReplace2))
					{
						outputString.Remove(outputString.Length - lengthOfWordToReplace2, lengthOfWordToReplace2);
						outputString.Append(irregularWord2);
						break;
					}
					switch (token)
					{
					case ".nnp":
						AddSuffixNounNominativePlural(outputString);
						break;
					case ".ajp":
						AddSuffixAdjectiveNominativePlural(outputString);
						break;
					case ".aj":
						AddSuffixAdjectiveNominative(outputString);
						break;
					}
					break;
				}
				}
				_curGender = WordGenderEnum.NoDeclination;
				WordGroupProcessor(sourceText, cursorPos);
				break;
			default:
			{
				if (Array.IndexOf(NounTokens.TokenList, token) >= 0 && (!_doesComeFromWordGroup || (_doesComeFromWordGroup && _curGender == WordGenderEnum.NoDeclination)) && IsWordGroup(token.Length, sourceText, cursorPos, out var wordGroupIndex))
				{
					if (wordGroupIndex >= 0)
					{
						token = "{" + token + "}";
						_curGender = WordGenderEnum.NoDeclination;
						AddSuffixWordGroup(token, wordGroupIndex, outputString);
					}
				}
				else
				{
					if (_curGender == WordGenderEnum.NoDeclination)
					{
						break;
					}
					if (IsIrregularWord(sourceText, cursorPos, token, out var irregularWord, out var lengthOfWordToReplace))
					{
						outputString.Remove(outputString.Length - lengthOfWordToReplace, lengthOfWordToReplace);
						outputString.Append(irregularWord);
					}
					else
					{
						switch (token)
						{
						case ".p":
							AddSuffixNounNominativePlural(outputString);
							break;
						case ".a":
							AddSuffixNounAccusative(outputString);
							break;
						case ".ap":
							AddSuffixNounAccusativePlural(outputString);
							break;
						case ".g":
							AddSuffixNounGenitive(outputString);
							break;
						case ".gp":
							AddSuffixNounGenitivePlural(outputString);
							break;
						case ".d":
							AddSuffixNounDative(outputString);
							break;
						case ".dp":
							AddSuffixNounDativePlural(outputString);
							break;
						case ".l":
							AddSuffixNounLocative(outputString);
							break;
						case ".lp":
							AddSuffixNounLocativePlural(outputString);
							break;
						case ".i":
							AddSuffixNounInstrumental(outputString);
							break;
						case ".ip":
							AddSuffixNounInstrumentalPlural(outputString);
							break;
						case ".j":
							AddSuffixAdjectiveNominative(outputString);
							break;
						case ".jp":
							AddSuffixAdjectiveNominativePlural(outputString);
							break;
						case ".ja":
							AddSuffixAdjectiveAccusative(outputString);
							break;
						case ".jap":
							AddSuffixAdjectiveAccusativePlural(outputString);
							break;
						case ".jg":
							AddSuffixAdjectiveGenitive(outputString);
							break;
						case ".jgp":
							AddSuffixAdjectiveGenitivePlural(outputString);
							break;
						case ".jd":
							AddSuffixAdjectiveDative(outputString);
							break;
						case ".jdp":
							AddSuffixAdjectiveDativePlural(outputString);
							break;
						case ".jl":
							AddSuffixAdjectiveLocative(outputString);
							break;
						case ".jlp":
							AddSuffixAdjectiveLocativePlural(outputString);
							break;
						case ".ji":
							AddSuffixAdjectiveInstrumental(outputString);
							break;
						case ".jip":
							AddSuffixAdjectiveInstrumentalPlural(outputString);
							break;
						}
					}
					_curGender = WordGenderEnum.NoDeclination;
				}
				break;
			}
			}
		}
		if (flag)
		{
			cursorPos += LinkEndingLength;
			outputString.Append(LinkEnding);
		}
	}

	private void AddSuffixWordGroup(string token, int wordGroupIndex, StringBuilder outputString)
	{
		bool flag = char.IsUpper(outputString[outputString.Length - WordGroupsNoTags[wordGroupIndex].Length]);
		string item = WordGroups[wordGroupIndex].wordGroup;
		outputString.Remove(outputString.Length - WordGroupsNoTags[wordGroupIndex].Length, WordGroupsNoTags[wordGroupIndex].Length);
		item = item.Replace("{.nn}", token);
		if (token.Equals("{.n}"))
		{
			item = item.Replace("{.nnp}", "{.p}");
			item = item.Replace("{.ajp}", "{.jp}");
			item = item.Replace("{.aj}", "{.j}");
		}
		else
		{
			item = item.Replace("{.aj}", token.Insert(2, "j"));
			if (token.Contains("p"))
			{
				item = item.Replace("{.nnp}", token);
				item = item.Replace("{.ajp}", token.Insert(2, "j"));
			}
			else
			{
				item = item.Replace("{.nnp}", token.Insert(3, "p"));
				item = item.Replace("{.ajp}", token.Insert(2, "j").Insert(4, "p"));
			}
		}
		_doesComeFromWordGroup = true;
		string text = Process(item);
		_doesComeFromWordGroup = false;
		if (flag && char.IsLower(text[0]))
		{
			outputString.Append(char.ToUpperInvariant(text[0]));
			outputString.Append(text.Substring(1));
		}
		else if (!flag && char.IsUpper(text[0]))
		{
			outputString.Append(char.ToLowerInvariant(text[0]));
			outputString.Append(text.Substring(1));
		}
		else
		{
			outputString.Append(text);
		}
	}

	private bool IsWordGroup(int tokenLength, string sourceText, int curPos, out int wordGroupIndex)
	{
		for (int i = 0; i < WordGroupsNoTags.Count && curPos - tokenLength - 2 - WordGroupsNoTags[i].Length >= 0; i++)
		{
			if (sourceText.Substring(curPos - tokenLength - 2 - WordGroupsNoTags[i].Length, WordGroupsNoTags[i].Length).Equals(WordGroupsNoTags[i]))
			{
				wordGroupIndex = i;
				return true;
			}
		}
		wordGroupIndex = -1;
		return false;
	}

	private void AddSuffixNounNominativePlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'г' || lastCharacter == 'ж' || lastCharacter == 'к' || lastCharacter == 'х' || lastCharacter == 'ч' || lastCharacter == 'ш' || lastCharacter == 'щ')
			{
				outputString.Append('и');
			}
			else
			{
				outputString.Append('ы');
			}
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append('а');
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append('а');
				}
				else
				{
					outputString.Append('я');
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("ена");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append('и');
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append('я');
			return;
		case 'м':
			outputString.Append('а');
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Remove(outputString.Length - 2, 2).Append('е');
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (Animate && (GetEnding(outputString, 4) == "енок" || GetEnding(outputString, 4) == "ёнок"))
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ята");
		}
		else if (Animate && GetEnding(outputString, 4) == "онок")
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ата");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ки");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьки");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йки");
			}
		}
		else if (lastCharacter == 'г' || lastCharacter == 'ж' || lastCharacter == 'к' || lastCharacter == 'х' || lastCharacter == 'ч' || lastCharacter == 'ш' || lastCharacter == 'щ')
		{
			outputString.Append('и');
		}
		else
		{
			outputString.Append('ы');
		}
	}

	private void AddSuffixNounGenitive(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'ж' || lastCharacter == 'ш' || lastCharacter == 'к' || lastCharacter == 'щ' || lastCharacter == 'ч' || lastCharacter == 'г' || lastCharacter == 'х')
			{
				outputString.Append('и');
			}
			else
			{
				outputString.Append('ы');
			}
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append('а');
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append('а');
				}
				else
				{
					outputString.Append('я');
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("ени");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append('и');
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append('я');
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append('я');
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append('и');
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append('я');
			return;
		case 'м':
			outputString.Append('а');
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Append('а');
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (GetEnding(outputString, 3) == "нок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ка");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ка");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ька");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йка");
			}
		}
		else
		{
			outputString.Append('а');
		}
	}

	private void AddSuffixNounGenitivePlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (Masculine)
			{
				if (lastCharacter == 'ж' || lastCharacter == 'ш')
				{
					outputString.Append("ей");
				}
			}
			else if (lastCharacter == 'к')
			{
				outputString.Remove(outputString.Length - 1, 1);
				switch (GetLastCharacter(outputString))
				{
				case 'ц':
				case 'ч':
				case 'ш':
				case 'щ':
					outputString.Append("ек");
					break;
				case 'ь':
					outputString.Remove(outputString.Length - 1, 1);
					outputString.Append("ек");
					break;
				default:
					outputString.Append("ок");
					break;
				}
			}
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (Masculine)
			{
				outputString.Append('ь');
				return;
			}
			switch (lastCharacter)
			{
			case 'л':
				outputString.Remove(outputString.Length - 1, 1).Append("ель");
				break;
			case 'и':
				outputString.Append('й');
				break;
			default:
				outputString.Append('ь');
				break;
			}
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				lastCharacter = GetLastCharacter(outputString);
				outputString.Remove(outputString.Length - 1, 1);
				if (lastCharacter == 'н')
				{
					lastCharacter = GetLastCharacter(outputString);
					if (lastCharacter == 'к')
					{
						outputString.Append("он");
					}
					else
					{
						outputString.Append("ен");
					}
				}
				else
				{
					outputString.Append(lastCharacter).Append("ов");
				}
				return;
			case 'е':
				switch (GetLastCharacter(outputString))
				{
				case 'ь':
					outputString.Remove(outputString.Length - 1, 1);
					outputString.Append("ий");
					break;
				case 'и':
					outputString.Append('й');
					break;
				default:
					outputString.Append("ей");
					break;
				case 'щ':
					break;
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("ен");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append("ей");
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append("ей");
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append("ев");
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'к')
			{
				outputString.Remove(outputString.Length - 1, 1).Append("ек");
			}
			return;
		case 'е':
			outputString.Append('в');
			return;
		case 'м':
			outputString.Append("ов");
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Remove(outputString.Length - 2, 2);
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (Animate && (GetEnding(outputString, 4) == "енок" || GetEnding(outputString, 4) == "ёнок"))
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ят");
		}
		else if (Animate && GetEnding(outputString, 4) == "онок")
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ат");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ков");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьков");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йков");
			}
		}
		else if (GetEnding(outputString, 2) == "нц")
		{
			outputString.Append("ев");
		}
		else if (lastCharacter == 'ч' || lastCharacter == 'ц' || lastCharacter == 'ш' || lastCharacter == 'щ')
		{
			outputString.Append("ей");
		}
		else
		{
			outputString.Append("ов");
		}
	}

	private void AddSuffixNounDative(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('е');
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'и')
			{
				outputString.Append('и');
			}
			else
			{
				outputString.Append('е');
			}
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append('у');
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append('у');
				}
				else
				{
					outputString.Append('ю');
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("ени");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append('и');
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append('ю');
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append('ю');
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append('е');
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append('ю');
			return;
		case 'м':
			outputString.Append('у');
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Append('у');
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (GetEnding(outputString, 3) == "нок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ку");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ку");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьку");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йку");
			}
		}
		else
		{
			outputString.Append('у');
		}
	}

	private void AddSuffixNounDativePlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Append('м');
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Append('м');
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append("ам");
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append("ам");
				}
				else
				{
					outputString.Append("ям");
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("енам");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append("ям");
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append("ям");
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append("ям");
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append("ам");
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append("ям");
			return;
		case 'м':
			outputString.Append("ам");
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ам");
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (Animate && (GetEnding(outputString, 4) == "енок" || GetEnding(outputString, 4) == "ёнок"))
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ятам");
		}
		else if (Animate && GetEnding(outputString, 4) == "онок")
		{
			outputString.Remove(outputString.Length - 4, 4).Append("атам");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("кам");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ькам");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йкам");
			}
		}
		else
		{
			outputString.Append("ам");
		}
	}

	private void AddSuffixNounAccusative(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('у');
		}
		else if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('ю');
		}
		else
		{
			if (Neuter || Feminine || !MasculineAnimate)
			{
				return;
			}
			if (GetEnding(outputString, 3) == "нок")
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ка");
			}
			else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ка");
			}
			else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
			{
				if (IsConsonant(GetEnding(outputString, 3)[0]))
				{
					outputString.Remove(outputString.Length - 2, 2).Append("ьки");
				}
				else
				{
					outputString.Remove(outputString.Length - 2, 2).Append("йки");
				}
			}
			else
			{
				AddSuffixNounGenitive(outputString);
			}
		}
	}

	private void AddSuffixNounAccusativePlural(StringBuilder outputString)
	{
		if (Animate)
		{
			AddSuffixNounGenitivePlural(outputString);
		}
		else
		{
			AddSuffixNounNominativePlural(outputString);
		}
	}

	private void AddSuffixNounInstrumental(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'ж' || lastCharacter == 'ш' || lastCharacter == 'щ' || lastCharacter == 'ч')
			{
				outputString.Append("ей");
			}
			else
			{
				outputString.Append("ой");
			}
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append("ей");
			return;
		}
		if (Neuter)
		{
			if (lastCharacter == 'о' || lastCharacter == 'е')
			{
				outputString.Append('м');
			}
			else if (GetEnding(outputString, 2) == "мя")
			{
				outputString.Remove(outputString.Length - 1, 1).Append("енем");
			}
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Append('ю');
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append("ем");
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append("ем");
			return;
		case 'о':
			outputString.Append('м');
			return;
		case 'е':
			outputString.Append('м');
			return;
		case 'м':
			outputString.Append("ом");
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Append("ом");
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (GetEnding(outputString, 3) == "нок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ком");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ком");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьком");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йком");
			}
		}
		else
		{
			outputString.Append("ом");
		}
	}

	private void AddSuffixNounInstrumentalPlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Append("ми");
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Append("ми");
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append("ами");
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append("ами");
				}
				else
				{
					outputString.Append("ями");
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("енами");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append("ями");
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append("ями");
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append("ями");
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append("ами");
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append("ями");
			return;
		case 'м':
			outputString.Append("ами");
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ами");
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (Animate && (GetEnding(outputString, 4) == "енок" || GetEnding(outputString, 4) == "ёнок"))
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ятами");
		}
		else if (Animate && GetEnding(outputString, 4) == "онок")
		{
			outputString.Remove(outputString.Length - 4, 4).Append("атами");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ками");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьками");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йками");
			}
		}
		else
		{
			outputString.Append("ами");
		}
	}

	private void AddSuffixNounLocative(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1).Append('е');
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'и')
			{
				outputString.Append('и');
			}
			else
			{
				outputString.Append('е');
			}
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append('е');
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'и')
				{
					outputString.Append('и');
				}
				else
				{
					outputString.Append('е');
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("ени");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append('и');
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append('е');
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1);
			lastCharacter = GetLastCharacter(outputString);
			if (lastCharacter == 'и')
			{
				outputString.Append('и');
			}
			else
			{
				outputString.Append('е');
			}
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append('е');
			return;
		case 'м':
			outputString.Append('е');
			return;
		case 'е':
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Append('е');
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (GetEnding(outputString, 3) == "нок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ке");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ке");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьке");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йке");
			}
		}
		else
		{
			outputString.Append('е');
		}
	}

	private void AddSuffixNounLocativePlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (lastCharacter == 'а' && !Neuter)
		{
			outputString.Append('х');
			return;
		}
		if (lastCharacter == 'я' && !Neuter)
		{
			outputString.Append('х');
			return;
		}
		if (Neuter)
		{
			outputString.Remove(outputString.Length - 1, 1);
			switch (lastCharacter)
			{
			case 'о':
				outputString.Append("ах");
				return;
			case 'е':
				lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'ч' || lastCharacter == 'щ')
				{
					outputString.Append("ах");
				}
				else
				{
					outputString.Append("ях");
				}
				return;
			case 'я':
				if (GetLastCharacter(outputString) == 'м')
				{
					outputString.Append("енах");
					return;
				}
				break;
			}
			outputString.Append(lastCharacter);
			return;
		}
		if (Feminine)
		{
			if (lastCharacter == 'ь')
			{
				outputString.Remove(outputString.Length - 1, 1).Append("ях");
			}
			return;
		}
		switch (lastCharacter)
		{
		case 'ь':
			outputString.Remove(outputString.Length - 1, 1).Append("ях");
			return;
		case 'й':
			outputString.Remove(outputString.Length - 1, 1).Append("ях");
			return;
		case 'о':
			outputString.Remove(outputString.Length - 1, 1).Append("ах");
			return;
		case 'е':
			outputString.Remove(outputString.Length - 1, 1).Append("ях");
			return;
		case 'м':
			outputString.Append("ах");
			return;
		}
		if (MasculineAnimate && GetEnding(outputString, 2) == "ин")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ах");
			return;
		}
		if (!IsConsonant(lastCharacter))
		{
			return;
		}
		if (Animate && (GetEnding(outputString, 4) == "енок" || GetEnding(outputString, 4) == "ёнок"))
		{
			outputString.Remove(outputString.Length - 4, 4).Append("ятах");
		}
		else if (Animate && GetEnding(outputString, 4) == "онок")
		{
			outputString.Remove(outputString.Length - 4, 4).Append("атах");
		}
		else if (GetEnding(outputString, 2) == "ок" && outputString.ToString() != "сок")
		{
			outputString.Remove(outputString.Length - 2, 2).Append("ках");
		}
		else if (GetEnding(outputString, 2) == "ек" && GetEnding(outputString, 2) == "ёк")
		{
			if (IsConsonant(GetEnding(outputString, 3)[0]))
			{
				outputString.Remove(outputString.Length - 2, 2).Append("ьках");
			}
			else
			{
				outputString.Remove(outputString.Length - 2, 2).Append("йках");
			}
		}
		else
		{
			outputString.Append("ах");
		}
	}

	private void AddSuffixAdjectiveNominative(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		switch (ending)
		{
		case "ый":
			if (Feminine)
			{
				outputString.Append("ая");
			}
			else if (Neuter)
			{
				outputString.Append("ое");
			}
			else
			{
				outputString.Append("ый");
			}
			break;
		case "ой":
			if (Feminine)
			{
				outputString.Append("ая");
			}
			else if (Neuter)
			{
				outputString.Append("ое");
			}
			else
			{
				outputString.Append("ой");
			}
			break;
		case "ий":
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (Feminine)
			{
				if (IsVelarOrSibilant(lastCharacter))
				{
					outputString.Append("ая");
				}
				else
				{
					outputString.Append("яя");
				}
			}
			else if (Neuter)
			{
				if (IsVelarOrSibilant(lastCharacter))
				{
					outputString.Append("ое");
				}
				else
				{
					outputString.Append("ее");
				}
			}
			else
			{
				outputString.Append("ий");
			}
			break;
		}
		}
	}

	private void AddSuffixAdjectiveNominativePlural(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		string ending2 = GetEnding(outputString, 1);
		if (IsVelarOrSibilant(ending2[0]) || (ending == "ий" && IsSoftStemAdjective(ending2[0])))
		{
			outputString.Append("ие");
		}
		else
		{
			outputString.Append("ые");
		}
	}

	private void AddSuffixAdjectiveGenitive(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		switch (ending)
		{
		case "ый":
		case "ой":
			if (Feminine)
			{
				outputString.Append("ой");
			}
			else
			{
				outputString.Append("ого");
			}
			break;
		case "ий":
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (Feminine)
			{
				if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
				{
					outputString.Append("ой");
				}
				else
				{
					outputString.Append("ей");
				}
			}
			else if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
			{
				outputString.Append("ого");
			}
			else
			{
				outputString.Append("его");
			}
			break;
		}
		}
	}

	private void AddSuffixAdjectiveGenitivePlural(StringBuilder outputString)
	{
		switch (GetEnding(outputString, 2))
		{
		case "ый":
		case "ой":
			outputString.Remove(outputString.Length - 2, 2).Append("ых");
			break;
		case "ий":
			outputString.Remove(outputString.Length - 2, 2).Append("их");
			break;
		}
	}

	private void AddSuffixAdjectiveDative(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		switch (ending)
		{
		case "ый":
		case "ой":
			if (Feminine)
			{
				outputString.Append("ой");
			}
			else
			{
				outputString.Append("ому");
			}
			break;
		case "ий":
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (Feminine)
			{
				if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
				{
					outputString.Append("ой");
				}
				else
				{
					outputString.Append("ей");
				}
			}
			else if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
			{
				outputString.Append("ому");
			}
			else
			{
				outputString.Append("ему");
			}
			break;
		}
		}
	}

	private void AddSuffixAdjectiveDativePlural(StringBuilder outputString)
	{
		switch (GetEnding(outputString, 2))
		{
		case "ый":
		case "ой":
			outputString.Remove(outputString.Length - 2, 2).Append("ым");
			break;
		case "ий":
			outputString.Remove(outputString.Length - 2, 2).Append("им");
			break;
		}
	}

	private void AddSuffixAdjectiveAccusative(StringBuilder outputString)
	{
		if (Feminine)
		{
			string ending = GetEnding(outputString, 2);
			outputString.Remove(outputString.Length - 2, 2);
			switch (ending)
			{
			case "ый":
			case "ой":
				outputString.Append("ую");
				break;
			case "ий":
			{
				char lastCharacter = GetLastCharacter(outputString);
				if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
				{
					outputString.Append("ую");
				}
				else
				{
					outputString.Append("юю");
				}
				break;
			}
			}
		}
		else if (MasculineAnimate)
		{
			AddSuffixAdjectiveGenitive(outputString);
		}
		else
		{
			AddSuffixAdjectiveNominative(outputString);
		}
	}

	private void AddSuffixAdjectiveAccusativePlural(StringBuilder outputString)
	{
		if (Animate)
		{
			AddSuffixAdjectiveGenitivePlural(outputString);
		}
		else
		{
			AddSuffixAdjectiveNominativePlural(outputString);
		}
	}

	private void AddSuffixAdjectiveInstrumental(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		switch (ending)
		{
		case "ый":
		case "ой":
			if (Feminine)
			{
				outputString.Append("ой");
			}
			else
			{
				outputString.Append("ым");
			}
			break;
		case "ий":
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (Feminine)
			{
				if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
				{
					outputString.Append("ой");
				}
				else
				{
					outputString.Append("ей");
				}
			}
			else if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
			{
				outputString.Append("им");
			}
			else
			{
				outputString.Append("им");
			}
			break;
		}
		}
	}

	private void AddSuffixAdjectiveInstrumentalPlural(StringBuilder outputString)
	{
		switch (GetEnding(outputString, 2))
		{
		case "ый":
		case "ой":
			outputString.Remove(outputString.Length - 2, 2).Append("ыми");
			break;
		case "ий":
			outputString.Remove(outputString.Length - 2, 2).Append("ими");
			break;
		}
	}

	private void AddSuffixAdjectiveLocative(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		outputString.Remove(outputString.Length - 2, 2);
		switch (ending)
		{
		case "ый":
		case "ой":
			if (Feminine)
			{
				outputString.Append("ой");
			}
			else
			{
				outputString.Append("ом");
			}
			break;
		case "ий":
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (Feminine)
			{
				if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
				{
					outputString.Append("ой");
				}
				else
				{
					outputString.Append("ей");
				}
			}
			else if (lastCharacter == 'к' || lastCharacter == 'г' || lastCharacter == 'х')
			{
				outputString.Append("ом");
			}
			else
			{
				outputString.Append("ем");
			}
			break;
		}
		}
	}

	private void AddSuffixAdjectiveLocativePlural(StringBuilder outputString)
	{
		switch (GetEnding(outputString, 2))
		{
		case "ый":
		case "ой":
			outputString.Remove(outputString.Length - 2, 2).Append("ых");
			break;
		case "ий":
			outputString.Remove(outputString.Length - 2, 2).Append("их");
			break;
		}
	}

	private void SetFeminineAnimate()
	{
		_curGender = WordGenderEnum.FeminineAnimate;
	}

	private void SetFeminineInanimate()
	{
		_curGender = WordGenderEnum.FeminineInanimate;
	}

	private void SetNeuterInanimate()
	{
		_curGender = WordGenderEnum.NeuterInanimate;
	}

	private void SetNeuterAnimate()
	{
		_curGender = WordGenderEnum.NeuterAnimate;
	}

	private void SetMasculineAnimate()
	{
		_curGender = WordGenderEnum.MasculineAnimate;
	}

	private void SetMasculineInanimate()
	{
		_curGender = WordGenderEnum.MasculineInanimate;
	}

	private bool IsRecordedWithPreviousTag(string sourceText, int cursorPos)
	{
		for (int i = 0; i < WordGroups.Count; i++)
		{
			if (WordGroups[i].wordGroup == sourceText && WordGroups[i].firstMarkerPost != cursorPos)
			{
				return true;
			}
		}
		return false;
	}

	private void WordGroupProcessor(string sourceText, int cursorPos)
	{
		if (!IsRecordedWithPreviousTag(sourceText, cursorPos))
		{
			WordGroups.Add((sourceText, cursorPos));
			string text = sourceText;
			text = text.Replace("{.nnp}", "{.p}");
			text = text.Replace("{.ajp}", "{.jp}");
			text = text.Replace("{.nn}", "{.n}");
			text = text.Replace("{.aj}", "{.j}");
			_doesComeFromWordGroup = true;
			WordGroupsNoTags.Add(Process(text));
			_doesComeFromWordGroup = false;
		}
	}

	private bool IsLink(string sourceText, int tokenLength, int cursorPos)
	{
		string text = sourceText.Remove(cursorPos - tokenLength);
		for (int i = 0; i < LinkList.Count; i++)
		{
			if (sourceText.Length >= LinkList[i].Length && text.EndsWith(LinkList[i]))
			{
				LinkList.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	private bool IsIrregularWord(string sourceText, int cursorPos, string token, out string irregularWord, out int lengthOfWordToReplace)
	{
		int num = sourceText.Remove(cursorPos - token.Length - 2).LastIndexOf('}') + 1;
		lengthOfWordToReplace = cursorPos - token.Length - 2 - num;
		irregularWord = "";
		string text = "";
		bool flag = false;
		if (lengthOfWordToReplace > 0)
		{
			text = sourceText.Substring(num, lengthOfWordToReplace);
			flag = char.IsLower(text[0]);
			text = text.ToLowerInvariant();
			if (exceptions.ContainsKey(text) && exceptions[text] != null && exceptions[text].ContainsKey(token))
			{
				irregularWord = exceptions[text][token];
			}
		}
		if (irregularWord.Length > 0)
		{
			if (flag)
			{
				irregularWord = irregularWord.ToLower();
			}
			return true;
		}
		return false;
	}

	private bool IsVowel(char c)
	{
		return Array.IndexOf(_vowels, c) >= 0;
	}

	private bool IsVelarOrSibilant(char c)
	{
		if (Array.IndexOf(_sibilants, c) < 0)
		{
			return Array.IndexOf(_velars, c) >= 0;
		}
		return true;
	}

	private bool IsSoftStemAdjective(char c)
	{
		return c == 'н';
	}

	private bool IsConsonant(char c)
	{
		return "БбВвГгДдЖжЗзЙйКкЛлМмНнПпРрСсТтФфХхЦцЧчШшЩщЬьЪъ".IndexOf(c) >= 0;
	}

	private static string IrregularWordWithCase(string token, IrregularWord irregularWord)
	{
		switch (token)
		{
		case ".j":
			return irregularWord.Nominative;
		case ".jp":
		case ".p":
		case ".nnp":
		case ".ajp":
			return irregularWord.NominativePlural;
		case ".ja":
		case ".a":
			return irregularWord.Accusative;
		case ".jap":
		case ".ap":
			return irregularWord.AccusativePlural;
		case ".jg":
		case ".g":
			return irregularWord.Genitive;
		case ".jgp":
		case ".gp":
			return irregularWord.GenitivePlural;
		case ".jd":
		case ".d":
			return irregularWord.Dative;
		case ".jdp":
		case ".dp":
			return irregularWord.DativePlural;
		case ".jl":
		case ".l":
			return irregularWord.Locative;
		case ".jlp":
		case ".lp":
			return irregularWord.LocativePlural;
		case ".ji":
		case ".i":
			return irregularWord.Instrumental;
		case ".jip":
		case ".ip":
			return irregularWord.InstrumentalPlural;
		default:
			return "";
		}
	}

	public string PrepareNounCheckString(string noun)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "PrepareNounCheckString");
		mBStringBuilder.Append("\"Есть ").Append(Process(noun)).Append('"')
			.Append(",\"Нет ")
			.Append(Process(noun + "{.g}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process(noun + "{.d}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process(noun + "{.a}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process(noun + "{.i}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process(noun + "{.l}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process(noun + "{.p}"))
			.Append('"')
			.Append(",\"Нет ")
			.Append(Process(noun + "{.gp}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process(noun + "{.dp}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process(noun + "{.ap}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process(noun + "{.ip}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process(noun + "{.lp}"))
			.Append('"');
		return mBStringBuilder.ToStringAndRelease();
	}

	public string PrepareAdjectiveCheckString(string adj)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "PrepareAdjectiveCheckString");
		mBStringBuilder.Append("\"Есть ").Append(Process("{.MI}" + adj + "{.j} {.MI}меч{.n}")).Append('"')
			.Append(",\"Нет ")
			.Append(Process("{.MI}" + adj + "{.jg} {.MI}меч{.g}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process("{.MI}" + adj + "{.jd} {.MI}меч{.d}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.MI}" + adj + "{.ja} {.MI}меч{.a}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process("{.MI}" + adj + "{.ji} {.MI}меч{.i}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process("{.MI}" + adj + "{.jl} {.MI}меч{.l}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.MA}" + adj + "{.j} {.MA}юноша{.n}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.MA}" + adj + "{.ja} {.MA}юноша{.a}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.FI}" + adj + "{.j} {.FI}доска"))
			.Append('"')
			.Append(",\"Нет ")
			.Append(Process("{.FI}" + adj + "{.jg} {.FI}доска{.g}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process("{.FI}" + adj + "{.jd} {.FI}доска{.d}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.FI}" + adj + "{.ja} {.FI}доска{.a}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process("{.FI}" + adj + "{.ji} {.FI}доска{.i}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process("{.FI}" + adj + "{.jl} {.FI}доска{.l}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.FA}" + adj + "{.j} {.FA}девушка{.n}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.FA}" + adj + "{.ja} {.FA}девушка{.a}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.NI}" + adj + "{.j} {.NI}бревно{.n}"))
			.Append('"')
			.Append(",\"Нет ")
			.Append(Process("{.NI}" + adj + "{.jg} {.NI}бревно{.g}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process("{.NI}" + adj + "{.jd} {.NI}бревно{.d}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.NI}" + adj + "{.ja} {.NI}бревно{.a}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process("{.NI}" + adj + "{.ji} {.NI}бревно{.i}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process("{.NI}" + adj + "{.jl} {.NI}бревно{.l}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.MI}" + adj + "{.jp} {.MI}меч{.p}"))
			.Append('"')
			.Append(",\"Нет ")
			.Append(Process("{.MI}" + adj + "{.jgp} {.MI}меч{.gp}"))
			.Append('"')
			.Append(",\"Рад ")
			.Append(Process("{.MI}" + adj + "{.jdp} {.MI}меч{.dp}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.MI}" + adj + "{.jap} {.MI}меч{.ap}"))
			.Append('"')
			.Append(",\"Доволен ")
			.Append(Process("{.MI}" + adj + "{.jip} {.MI}меч{.ip}"))
			.Append('"')
			.Append(",\"Думаю о ")
			.Append(Process("{.MI}" + adj + "{.jlp} {.MI}меч{.lp}"))
			.Append('"')
			.Append(",\"Есть ")
			.Append(Process("{.FA}" + adj + "{.jp} {.FA}девушка{.p}"))
			.Append('"')
			.Append(",\"Вижу ")
			.Append(Process("{.FA}" + adj + "{.jap} {.FA}девушка{.ap}"))
			.Append('"');
		return mBStringBuilder.ToStringAndRelease();
	}

	public static string[] GetProcessedNouns(string str, string gender, string[] tokens = null)
	{
		if (tokens == null)
		{
			tokens = new string[12]
			{
				".n", ".g", ".d", ".a", ".i", ".l", ".p", ".gp", ".dp", ".ap",
				".ip", ".lp"
			};
		}
		List<string> list = new List<string>();
		RussianTextProcessor russianTextProcessor = new RussianTextProcessor();
		string[] array = tokens;
		foreach (string text in array)
		{
			string text2 = "{=!}" + gender + str + "{" + text + "}";
			list.Add(russianTextProcessor.Process(text2));
		}
		return list.ToArray();
	}
}
