using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TaleWorlds.Library;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class GermanTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordGenderEnum
	{
		Masculine,
		Feminine,
		Neuter,
		Plural,
		NoDeclination
	}

	private static class NounTokens
	{
		public const string Nominative = ".n";

		public const string Accusative = ".a";

		public const string Genitive = ".g";

		public const string Dative = ".d";

		public const string NominativePlural = ".np";

		public const string AccusativePlural = ".ap";

		public const string GenitivePlural = ".gp";

		public const string DativePlural = ".dp";

		public static readonly string[] TokenList = new string[8] { ".n", ".a", ".g", ".d", ".np", ".ap", ".gp", ".dp" };
	}

	private static class AdjectiveTokens
	{
		public const string WeakNominative = ".wn";

		public const string MixedNominative = ".mn";

		public const string StrongNominative = ".sn";

		public const string WeakAccusative = ".wa";

		public const string MixedAccusative = ".ma";

		public const string StrongAccusative = ".sa";

		public const string WeakDative = ".wd";

		public const string MixedDative = ".md";

		public const string StrongDative = ".sd";

		public const string WeakGenitive = ".wg";

		public const string MixedGenitive = ".mg";

		public const string StrongGenitive = ".sg";

		public const string WeakNominativePlural = ".wnp";

		public const string MixedNominativePlural = ".mnp";

		public const string StrongNominativePlural = ".snp";

		public const string WeakAccusativePlural = ".wap";

		public const string MixedAccusativePlural = ".map";

		public const string StrongAccusativePlural = ".sap";

		public const string WeakDativePlural = ".wdp";

		public const string MixedDativePlural = ".mdp";

		public const string StrongDativePlural = ".sdp";

		public const string WeakGenitivePlural = ".wgp";

		public const string MixedGenitivePlural = ".mgp";

		public const string StrongGenitivePlural = ".sgp";

		public static readonly string[] TokenList = new string[24]
		{
			".wn", ".mn", ".sn", ".wa", ".ma", ".sa", ".wd", ".md", ".sd", ".wg",
			".mg", ".sg", ".wnp", ".mnp", ".snp", ".wap", ".map", ".sap", ".wdp", ".mdp",
			".sdp", ".wgp", ".mgp", ".sgp"
		};
	}

	private static class PronounAndArticleTokens
	{
		public const string Nominative = ".pn";

		public const string Accusative = ".pa";

		public const string Genitive = ".pg";

		public const string Dative = ".pd";

		public const string NominativePlural = ".pnp";

		public const string AccusativePlural = ".pap";

		public const string GenitivePlural = ".pgp";

		public const string DativePlural = ".pdp";

		public static readonly string[] TokenList = new string[8] { ".pn", ".pa", ".pg", ".pd", ".pnp", ".pap", ".pgp", ".pdp" };
	}

	private static class GenderTokens
	{
		public const string Masculine = ".M";

		public const string Feminine = ".F";

		public const string Neuter = ".N";

		public const string Plural = ".P";

		public static readonly string[] TokenList = new string[4] { ".M", ".F", ".N", ".P" };
	}

	private static class WordGroupTokens
	{
		public const string NounNominative = ".nn";

		public const string PronounAndArticleNominative = ".pngroup";

		public const string AdjectiveNominativeWeak = ".ajw";

		public const string AdjectiveNominativeMixed = ".ajm";

		public const string AdjectiveNominativeStrong = ".ajs";

		public const string NounNominativeWithBrackets = "{.nn}";

		public const string PronounAndArticleNominativeWithBrackets = "{.pngroup}";

		public const string AdjectiveNominativeWeakWithBrackets = "{.ajw}";

		public const string AdjectiveNominativeMixedWithBrackets = "{.ajm}";

		public const string AdjectiveNominativeStrongWithBrackets = "{.ajs}";

		public const string NounNominativePlural = ".nnp";

		public const string PronounAndArticleNominativePlural = ".pnpgroup";

		public const string AdjectiveNominativeWeakPlural = ".ajwp";

		public const string AdjectiveNominativeMixedPlural = ".ajmp";

		public const string AdjectiveNominativeStrongPlural = ".ajsp";

		public const string NounNominativePluralWithBrackets = "{.nnp}";

		public const string PronounAndArticleNominativePluralWithBrackets = "{.pnpgroup}";

		public const string AdjectiveNominativeWeakPluralWithBrackets = "{.ajwp}";

		public const string AdjectiveNominativeMixedPluralWithBrackets = "{.ajmp}";

		public const string AdjectiveNominativeStrongPluralWithBrackets = "{.ajsp}";

		public static readonly string[] TokenList = new string[10] { ".nn", ".pngroup", ".ajm", ".ajs", ".ajw", ".nnp", ".pnpgroup", ".ajmp", ".ajsp", ".ajwp" };
	}

	private static class OtherTokens
	{
		public const string PossessionToken = ".o";
	}

	private struct DictionaryWord
	{
		public readonly string Nominative;

		public readonly string NominativePlural;

		public readonly string Accusative;

		public readonly string Genitive;

		public readonly string Dative;

		public readonly string AccusativePlural;

		public readonly string GenitivePlural;

		public readonly string DativePlural;

		public DictionaryWord(string nominative, string nominativePlural, string genitive, string genitivePlural, string dative, string dativePlural, string accusative, string accusativePlural)
		{
			Nominative = nominative;
			NominativePlural = nominativePlural;
			Accusative = accusative;
			Genitive = genitive;
			Dative = dative;
			AccusativePlural = accusativePlural;
			GenitivePlural = genitivePlural;
			DativePlural = dativePlural;
		}
	}

	private static readonly CultureInfo CultureInfo = new CultureInfo("de-DE");

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

	private static char[] Vowels = new char[8] { 'a', 'e', 'i', 'o', 'u', 'ä', 'ü', 'ö' };

	private const string Consonants = "BbCcDdFfGgHhJjKkLlMmNnPpRrSsTtWwYyZz";

	private static char[] SSounds = new char[4] { 's', 'ß', 'z', 'x' };

	private static readonly Dictionary<char, List<DictionaryWord>> IrregularMasculineDictionary = new Dictionary<char, List<DictionaryWord>> { 
	{
		'N',
		new List<DictionaryWord>
		{
			new DictionaryWord("Name", "Namen", "Namens", "Namen", "Namen", "Namen", "Namen", "Namen")
		}
	} };

	private static readonly Dictionary<char, List<DictionaryWord>> IrregularFeminineDictionary = new Dictionary<char, List<DictionaryWord>>();

	private static readonly Dictionary<char, List<DictionaryWord>> IrregularNeuterDictionary = new Dictionary<char, List<DictionaryWord>> { 
	{
		'I',
		new List<DictionaryWord>
		{
			new DictionaryWord("Imperium", "Imperien", "Imperiums", "Imperien", "Imperium", "Imperien", "Imperium", "Imperien")
		}
	} };

	private static readonly Dictionary<char, List<DictionaryWord>> IrregularPluralDictionary = new Dictionary<char, List<DictionaryWord>>();

	private static readonly Dictionary<string, Dictionary<WordGenderEnum, DictionaryWord>> PronounArticleDictionary = new Dictionary<string, Dictionary<WordGenderEnum, DictionaryWord>>
	{
		{
			"der",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Der", "Die", "Des", "Der", "Dem", "Den", "Den", "Die")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Die", "Die", "Der", "Der", "Der", "Den", "Die", "Die")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Das", "Die", "Des", "Der", "Dem", "Den", "Das", "Die")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Die", "Die", "Der", "Der", "Den", "Den", "Die", "Die")
				}
			}
		},
		{
			"ein",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Ein", "", "Eines", "", "Einem", "", "Einen", "")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Eine", "", "Einer", "", "Einer", "", "Eine", "")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Ein", "", "Eines", "", "Einem", "", "Ein", "")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("", "", "", "", "", "", "", "")
				}
			}
		},
		{
			"dieser",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Dieser", "Diese", "Dieses", "Dieser", "Diesem", "Diesen", "Diesen", "Diese")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Diese", "Diese", "Dieser", "Dieser", "Dieser", "Diesen", "Diese", "Diese")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Dieses", "Diese", "Dieses", "Dieser", "Diesem", "Diesen", "Dieses", "Diese")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Diese", "Diese", "Dieser", "Dieser", "Diesen", "Diesen", "Diese", "Diese")
				}
			}
		},
		{
			"jeder",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Jeder", "Alle", "Jedes", "Aller", "Jedem", "Allen", "Jeden", "Alle")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Jede", "Alle", "Jeder", "Aller", "Jeder", "Allen", "Jede", "Alle")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Jedes", "Alle", "Jedes", "Aller", "Jedem", "Allen", "Jedes", "Alle")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Alle", "Alle", "Aller", "Aller", "Allen", "Allen", "Alle", "Alle")
				}
			}
		},
		{
			"kein",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Kein", "Keine", "Keines", "Keiner", "Keinem", "Keinen", "Keinen", "Keine")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Keine", "Keine", "Keiner", "Keiner", "Keiner", "Keinen", "Keine", "Keine")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Kein", "Keine", "Keines", "Keiner", "Keinem", "Keinen", "Kein", "Keine")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Keine", "Keine", "Keiner", "Keiner", "Keinen", "Keinen", "Keine", "Keine")
				}
			}
		},
		{
			"dein",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Dein", "Deine", "Deines", "Deiner", "Deinem", "Deinen", "Deinen", "Deine")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Deine", "Deine", "Deiner", "Deiner", "Deiner", "Deinen", "Deine", "Deine")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Dein", "Deine", "Deines", "Deiner", "Deinem", "Deinen", "Dein", "Deine")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Deine", "Deine", "Deiner", "Deiner", "Deinen", "Deinen", "Deine", "Deine")
				}
			}
		},
		{
			"ihr",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Ihr", "Ihre", "Ihres", "Ihrer", "Ihrem", "Ihren", "Ihren", "Ihre")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Ihre", "Ihre", "Ihrer", "Ihrer", "Ihrer", "Ihren", "Ihre", "Ihre")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Ihr", "Ihre", "Ihres", "Ihrer", "Ihrem", "Ihren", "Ihr", "Ihre")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Ihre", "Ihre", "Ihrer", "Ihrer", "Ihren", "Ihren", "Ihre", "Ihre")
				}
			}
		},
		{
			"euer",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Euer", "Eure", "Eures", "Eurer ", "Eurem", "Euren", "Euren", "Eure")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Eure", "Eure", "Eurer", "Eurer ", "Eurer", "Euren", "Eure", "Eure")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Euer", "Eure", "Eures", "Eurer ", "Eurem", "Euren", "Euer", "Eure")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Eure", "Eure", "Eurer ", "Eurer ", "Euren", "Euren", "Eure", "Eure")
				}
			}
		},
		{
			"welcher",
			new Dictionary<WordGenderEnum, DictionaryWord>
			{
				{
					WordGenderEnum.Masculine,
					new DictionaryWord("Welcher", "Welche", "Welches", "Welcher", "Welchem", "Welchen", "Welchen", "Welche")
				},
				{
					WordGenderEnum.Feminine,
					new DictionaryWord("Welche", "Welche", "Welcher", "Welcher", "Welcher", "Welchen", "Welche", "Welche")
				},
				{
					WordGenderEnum.Neuter,
					new DictionaryWord("Welches", "Welche", "Welches", "Welcher", "Welchem", "Welchen", "Welches", "Welche")
				},
				{
					WordGenderEnum.Plural,
					new DictionaryWord("Welche", "Welche", "Welcher", "Welcher", "Welchen", "Welchen", "Welche", "Welche")
				}
			}
		}
	};

	private static readonly Dictionary<string, Dictionary<string, string>> ArticlePronounReplacementDictionary = new Dictionary<string, Dictionary<string, string>> { 
	{
		"dem",
		new Dictionary<string, string>
		{
			{ "von", "vom" },
			{ "vom", "vom" },
			{ "zu", "zum" },
			{ "an", "am" }
		}
	} };

	private static readonly Dictionary<char, List<string>> DoNotDeclineList = new Dictionary<char, List<string>>
	{
		{
			'A',
			new List<string> { "Avlonos", "Argoron", "Arkit", "Airit", "Aldusunit", "Asraloving", "Acapanos", "Angarys" }
		},
		{
			'B',
			new List<string>
			{
				"Banu Hulyan", "Banu Sarmal", "Banu Sarran", "Baltait", "Banu Atij", "Banu Qaraz", "Banu Ruwaid", "Beni Zilal", "Banu Habbab", "Banu Qild",
				"Banu Arbas", "Bochit", "Boranoving", "Bani Aska", "Bani Dhamin", "Bani Fasus", "Bani Julul", "Bani Kinyan", "Bani Laikh", "Bani Mushala",
				"Bani Nir", "Bani Tharuq", "Bani Yatash", "Bani Zus", "Balastisos"
			}
		},
		{
			'C',
			new List<string> { "Chonis", "Corenios", "Comnos", "Charait", "Corstases" }
		},
		{
			'D',
			new List<string>
			{
				"dey Meroc", "Dolentos", "dey Molarn", "dey Gunric", "dey Cortain", "dey Rothad", "dey Jelind", "dey Fortes", "dey Arromanc", "dey Tihr",
				"Dionicos", "dey Valant", "dey Folcun", "Delicos"
			}
		},
		{
			'E',
			new List<string> { "Eleftheroi", "Elaches", "Elysos" }
		},
		{
			'F',
			new List<string>
			{
				"fen Uvain", "fen Caernacht", "fen Gruffendoc", "fen Morcar", "fen Penraic", "fen Giall", "fen Eingal", "fen Derngil", "fen Aertus", "fen Brachar",
				"fen Crusac", "fen Domus", "fen Earach", "fen Fiachan", "fen Loen", "fen Morain", "fen Seanel", "fen Tuil", "Folyoroving"
			}
		},
		{
			'G',
			new List<string> { "Gundaroving", "Ghilman", "Gendiroving", "Gessios" }
		},
		{
			'H',
			new List<string> { "Hongeros", "Harfit" }
		},
		{
			'I',
			new List<string> { "Isyaroving", "Impestores", "Ingchit", "Iskanoving" }
		},
		{
			'J',
			new List<string> { "Julios", "Jawwal", "Jalos" }
		},
		{
			'K',
			new List<string> { "Karakhergit", "Koltit", "Kuloving", "Khergit", "Kostoroving" }
		},
		{
			'L',
			new List<string> { "Lonalion", "Leoniparden", "Lestharos" }
		},
		{
			'M',
			new List<string> { "Mestricaros", "Maneolis", "Maranjit", "Maregoving", "Meones" }
		},
		{
			'N',
			new List<string> { "Neutral", "Neretzes", "Nathanys" }
		},
		{
			'O',
			new List<string> { "Ormidoving", "Oburit", "Osticos", "Oranarit", "Opynates" }
		},
		{
			'P',
			new List<string> { "Prienicos", "Phalentes", "Pethros", "Palladios", "Paltos", "Phenigos" }
		},
		{
			'S',
			new List<string> { "Skolderbroda", "Seeratten", "Sorados", "Serapides", "Seeräuber", "Sunit", "Suratoving", "Stracanasthes", "Sumessos" }
		},
		{
			'T',
			new List<string> { "Tigrit", "Togaroving", "Tokhit", "Therycos" }
		},
		{
			'U',
			new List<string> { "Urkhunait", "Ubroving", "Ubchit" }
		},
		{
			'V',
			new List<string> { "Varros", "Vizartos", "Vagiroving", "Verborgene Hand", "Vatatzes", "Vetranis", "Vezhoving", "Vyshoving" }
		},
		{
			'Y',
			new List<string> { "Yanserit", "Yujit", "Yerchoving" }
		},
		{
			'Z',
			new List<string> { "Zhanoving", "Zebales" }
		}
	};

	public override CultureInfo CultureInfoForLanguage => CultureInfo;

	private bool Masculine => _curGender == WordGenderEnum.Masculine;

	private bool Feminine => _curGender == WordGenderEnum.Feminine;

	private bool Neuter => _curGender == WordGenderEnum.Neuter;

	private bool Plural => _curGender == WordGenderEnum.Plural;

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
		int wordGroupIndex;
		if (token.EndsWith("Creator"))
		{
			outputString.Append("{" + token.Replace("Creator", "") + "}");
		}
		else if (Array.IndexOf(GenderTokens.TokenList, token) >= 0)
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
			}
			if (cursorPos == token.Length + 2 && sourceText.IndexOf("{.", cursorPos, StringComparison.InvariantCulture) == -1 && sourceText.IndexOf(' ', cursorPos) == -1)
			{
				WordGroups.Add((sourceText + "{.nn}", cursorPos));
				WordGroupsNoTags.Add(sourceText.Substring(cursorPos));
			}
		}
		else if (Array.IndexOf(WordGroupTokens.TokenList, token) >= 0)
		{
			switch (token)
			{
			case ".nnp":
			case ".ajmp":
			case ".ajwp":
			case ".ajsp":
			case ".ajm":
			case ".ajw":
			case ".ajs":
			{
				if (IsIrregularWord(sourceText, cursorPos, token, out var irregularWord, out var lengthOfWordToReplace))
				{
					outputString.Remove(outputString.Length - lengthOfWordToReplace, lengthOfWordToReplace);
					outputString.Append(irregularWord);
					break;
				}
				switch (token)
				{
				case ".nnp":
					AddSuffixNounNominativePlural(outputString);
					break;
				case ".ajmp":
					AddSuffixMixedNominativePlural(outputString);
					break;
				case ".ajwp":
					AddSuffixWeakNominativePlural(outputString);
					break;
				case ".ajsp":
					AddSuffixStrongNominativePlural(outputString);
					break;
				case ".ajm":
					AddSuffixMixedNominative(outputString);
					break;
				case ".ajw":
					AddSuffixWeakNominative(outputString);
					break;
				case ".ajs":
					AddSuffixStrongNominative(outputString);
					break;
				}
				break;
			}
			case ".pnpgroup":
			case ".pngroup":
				AddPronounArticle(sourceText, cursorPos, token, ref outputString);
				break;
			}
			_curGender = WordGenderEnum.NoDeclination;
			WordGroupProcessor(sourceText, cursorPos);
		}
		else if (Array.IndexOf(NounTokens.TokenList, token) >= 0 && (!_doesComeFromWordGroup || (_doesComeFromWordGroup && _curGender == WordGenderEnum.NoDeclination)) && IsWordGroup(token.Length, sourceText, cursorPos, out wordGroupIndex))
		{
			if (wordGroupIndex >= 0)
			{
				token = "{" + token + "}";
				_curGender = WordGenderEnum.NoDeclination;
				AddSuffixWordGroup(token, wordGroupIndex, outputString);
			}
		}
		else if (token == ".o")
		{
			HandlePossession(outputString, cursorPos);
		}
		else if (_curGender != WordGenderEnum.NoDeclination)
		{
			if (ShouldDeclineWord(sourceText, cursorPos, token))
			{
				if (IsIrregularWord(sourceText, cursorPos, token, out var irregularWord2, out var lengthOfWordToReplace2))
				{
					outputString.Remove(outputString.Length - lengthOfWordToReplace2, lengthOfWordToReplace2);
					outputString.Append(irregularWord2);
				}
				else
				{
					switch (token)
					{
					case ".np":
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
					case ".wn":
						AddSuffixWeakNominative(outputString);
						break;
					case ".wa":
						AddSuffixWeakAccusative(outputString);
						break;
					case ".wg":
						AddSuffixWeakGenitive(outputString);
						break;
					case ".wd":
						AddSuffixWeakDative(outputString);
						break;
					case ".mn":
						AddSuffixMixedNominative(outputString);
						break;
					case ".ma":
						AddSuffixMixedAccusative(outputString);
						break;
					case ".mg":
						AddSuffixMixedGenitive(outputString);
						break;
					case ".md":
						AddSuffixMixedDative(outputString);
						break;
					case ".sn":
						AddSuffixStrongNominative(outputString);
						break;
					case ".sa":
						AddSuffixStrongAccusative(outputString);
						break;
					case ".sg":
						AddSuffixStrongGenitive(outputString);
						break;
					case ".sd":
						AddSuffixStrongDative(outputString);
						break;
					case ".wnp":
					case ".wap":
					case ".wgp":
					case ".wdp":
					case ".mnp":
					case ".map":
					case ".mgp":
					case ".mdp":
					case ".sgp":
						AddSuffixMixedDativePlural(outputString);
						break;
					case ".snp":
					case ".sap":
						AddSuffixStrongAccusativePlural(outputString);
						break;
					case ".sdp":
						AddSuffixStrongDativePlural(outputString);
						break;
					case ".pn":
					case ".pnp":
					case ".pa":
					case ".pap":
					case ".pg":
					case ".pgp":
					case ".pd":
					case ".pdp":
						AddPronounArticle(sourceText, cursorPos, token, ref outputString);
						break;
					}
				}
			}
			_curGender = WordGenderEnum.NoDeclination;
		}
		if (flag)
		{
			cursorPos += LinkEndingLength;
			outputString.Append(LinkEnding);
		}
	}

	private void HandlePossession(StringBuilder outputString, int cursorPos)
	{
		string text = outputString.ToString();
		bool flag = false;
		if (text.Length < cursorPos)
		{
			cursorPos = text.Length;
		}
		if (text.EndsWith("</b></a>"))
		{
			cursorPos -= 8;
			outputString.Remove(outputString.Length - 8, 8);
			flag = true;
		}
		char lastCharacter = GetLastCharacter(outputString, cursorPos);
		if (SSounds.Contains(char.ToLower(lastCharacter, CultureInfo)))
		{
			outputString.Append('\'');
		}
		else
		{
			outputString.Append('s');
		}
		if (flag)
		{
			outputString.Append("</b></a>");
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
			item = item.Replace("{.nnp}", "{.np}");
			item = item.Replace("{.pnpgroup}", "{.pn}");
			item = item.Replace("{.ajw}", "{.wn}");
			item = item.Replace("{.ajm}", "{.mn}");
			item = item.Replace("{.ajs}", "{.sn}");
			item = item.Replace("{.ajwp}", "{.wnp}");
			item = item.Replace("{.ajmp}", "{.mnp}");
			item = item.Replace("{.ajsp}", "{.snp}");
		}
		else
		{
			item = item.Replace("{.ajw}", token.Insert(2, "w"));
			item = item.Replace("{.ajm}", token.Insert(2, "m"));
			item = item.Replace("{.ajs}", token.Insert(2, "s"));
			item = item.Replace("{.pngroup}", token.Insert(2, "p"));
			if (token.Contains("p"))
			{
				item = item.Replace("{.nnp}", token);
				item = item.Replace("{.ajwp}", token.Insert(2, "w"));
				item = item.Replace("{.ajmp}", token.Insert(2, "m"));
				item = item.Replace("{.ajsp}", token.Insert(2, "s"));
				item = item.Replace("{.pnpgroup}", token.Insert(2, "p"));
			}
			else
			{
				item = item.Replace("{.nnp}", token.Insert(3, "p"));
				item = item.Replace("{.ajwp}", token.Insert(2, "w").Insert(4, "p"));
				item = item.Replace("{.ajmp}", token.Insert(2, "m").Insert(4, "p"));
				item = item.Replace("{.ajsp}", token.Insert(2, "s").Insert(4, "p"));
				item = item.Replace("{.pnpgroup}", token.Insert(2, "p").Insert(4, "p"));
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
		int num = 0;
		wordGroupIndex = -1;
		int num2 = WordGroupsNoTags.Count - 1;
		while (0 <= num2)
		{
			if (curPos - tokenLength - 2 - WordGroupsNoTags[num2].Length >= 0 && WordGroupsNoTags[num2].Length > num && sourceText.Substring(curPos - tokenLength - 2 - WordGroupsNoTags[num2].Length, WordGroupsNoTags[num2].Length).Equals(WordGroupsNoTags[num2]))
			{
				wordGroupIndex = num2;
				num = WordGroupsNoTags[num2].Length;
			}
			num2--;
		}
		return num > 0;
	}

	private void AddSuffixNounNominativePlural(StringBuilder outputString)
	{
		if (Feminine)
		{
			if (GetLastCharacter(outputString) == 'e')
			{
				outputString.Append('n');
			}
			else if (GetEnding(outputString, 2) == "in")
			{
				outputString.Append("nen");
			}
			else
			{
				outputString.Append("e");
			}
		}
		else if ((Masculine || Neuter) && !AddSuffixForNDeclension(outputString))
		{
			outputString.Append('e');
		}
	}

	private void AddSuffixNounGenitive(StringBuilder outputString)
	{
		if (!Feminine && (Masculine || Neuter) && !AddSuffixForNDeclension(outputString))
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (!IsVowel(lastCharacter))
			{
				outputString.Append('e');
			}
			outputString.Append('s');
		}
	}

	private void AddSuffixNounGenitivePlural(StringBuilder outputString)
	{
		if (Feminine)
		{
			if (GetLastCharacter(outputString) == 'e')
			{
				outputString.Append('n');
			}
			else if (GetEnding(outputString, 2) == "in")
			{
				outputString.Append("nen");
			}
			else
			{
				outputString.Append("e");
			}
		}
		else if ((Masculine || Neuter) && !AddSuffixForNDeclension(outputString))
		{
			char lastCharacter = GetLastCharacter(outputString);
			if (!IsVowel(lastCharacter))
			{
				outputString.Append("e");
			}
		}
	}

	private void AddSuffixNounDative(StringBuilder outputString)
	{
		if (Masculine || Neuter)
		{
			AddSuffixForNDeclension(outputString);
		}
	}

	private void AddSuffixNounDativePlural(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (!IsVowel(lastCharacter) && lastCharacter != 'r')
		{
			outputString.Append('e');
		}
		outputString.Append('n');
	}

	private void AddSuffixNounAccusative(StringBuilder outputString)
	{
		if (Masculine || Neuter)
		{
			AddSuffixForNDeclension(outputString);
		}
	}

	private void AddSuffixNounAccusativePlural(StringBuilder outputString)
	{
		AddSuffixNounNominativePlural(outputString);
	}

	private void AddSuffixWeakNominative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Plural)
		{
			outputString.Append("en");
		}
		else
		{
			outputString.Append('e');
		}
	}

	private void AddSuffixMixedNominative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Masculine)
		{
			outputString.Append("er");
		}
		else if (Feminine)
		{
			outputString.Append("e");
		}
		else if (Neuter)
		{
			outputString.Append("es");
		}
		else if (Plural)
		{
			outputString.Append("en");
		}
	}

	private void AddSuffixStrongNominative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Masculine)
		{
			outputString.Append("er");
		}
		else if (Feminine || Plural)
		{
			outputString.Append("e");
		}
		else if (Neuter)
		{
			outputString.Append("es");
		}
	}

	private void AddSuffixWeakAccusative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Masculine || Plural)
		{
			outputString.Append("en");
		}
		else
		{
			outputString.Append("e");
		}
	}

	private void AddSuffixMixedAccusative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Masculine || Plural)
		{
			outputString.Append("en");
		}
		else if (Feminine)
		{
			outputString.Append("e");
		}
		else
		{
			outputString.Append("es");
		}
	}

	private void AddSuffixStrongAccusative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Masculine)
		{
			outputString.Append("en");
		}
		else if (Feminine || Plural)
		{
			outputString.Append("e");
		}
		else
		{
			outputString.Append("es");
		}
	}

	private void AddSuffixWeakDative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixMixedDative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixStrongDative(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Feminine)
		{
			outputString.Append("er");
		}
		else if (Masculine || Neuter)
		{
			outputString.Append("em");
		}
		else
		{
			outputString.Append("en");
		}
	}

	private void AddSuffixWeakGenitive(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixMixedGenitive(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixStrongGenitive(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		if (Feminine || Plural)
		{
			outputString.Append("er");
		}
		else if (Masculine || Neuter)
		{
			outputString.Append("en");
		}
	}

	private void AddSuffixWeakNominativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixMixedNominativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixStrongNominativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("e");
	}

	private void AddSuffixStrongAccusativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("e");
	}

	private void AddSuffixMixedDativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private void AddSuffixStrongDativePlural(StringBuilder outputString)
	{
		ModifyAdjective(outputString);
		outputString.Append("en");
	}

	private bool AddSuffixForNDeclension(StringBuilder outputString)
	{
		bool result = false;
		string ending = GetEnding(outputString, 3);
		switch (ending)
		{
		default:
			if (!ending.EndsWith("at"))
			{
				if (ending.EndsWith("e"))
				{
					result = true;
					outputString.Append("n");
				}
				break;
			}
			goto case "ent";
		case "ent":
		case "ant":
		case "ist":
			result = true;
			outputString.Append("en");
			break;
		}
		return result;
	}

	private void AddPronounArticle(string sourceText, int cursorPos, string token, ref StringBuilder outputString)
	{
		int num = sourceText.Remove(cursorPos - token.Length - 2).LastIndexOf('}') + 1;
		int num2 = cursorPos - token.Length - 2 - num;
		string text = "";
		if (num2 <= 0 || _curGender == WordGenderEnum.NoDeclination)
		{
			return;
		}
		string text2 = sourceText.Substring(num, num2);
		char c = text2[0];
		text2 = text2.ToLowerInvariant();
		if (PronounArticleDictionary.TryGetValue(text2, out var value))
		{
			text = DictionaryWordWithCase(token, value[_curGender]);
		}
		if (text.Length <= 0)
		{
			return;
		}
		if (char.IsLower(c))
		{
			text = text.ToLowerInvariant();
		}
		outputString.Remove(outputString.Length - num2, num2);
		outputString.Append(text);
		if (ArticlePronounReplacementDictionary.TryGetValue(text.ToLowerInvariant(), out var value2))
		{
			string previousWord = GetPreviousWord(sourceText, cursorPos, token, ref outputString);
			if (value2.TryGetValue(previousWord, out var value3))
			{
				outputString = outputString.Replace(previousWord + " " + text, value3);
			}
		}
	}

	private string GetPreviousWord(string sourceText, int cursorPos, string token, ref StringBuilder outputString)
	{
		string[] array = sourceText.Substring(0, cursorPos).Split(new char[1] { ' ' });
		int num = array.Length;
		if (num < 2)
		{
			return "";
		}
		return array[num - 2];
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
		_curGender = WordGenderEnum.Plural;
	}

	private bool IsVowel(char c)
	{
		return Array.IndexOf(Vowels, c) >= 0;
	}

	private int FindLastVowel(StringBuilder outputText)
	{
		for (int num = outputText.Length - 1; num >= 0; num--)
		{
			if (IsVowel(outputText[num]))
			{
				return num;
			}
		}
		return -1;
	}

	private void RemoveLastCharacter(StringBuilder outputString)
	{
		outputString.Remove(outputString.Length - 1, 1);
	}

	private bool IsLastCharVowel(StringBuilder outputString)
	{
		char c = outputString[outputString.Length - 1];
		return IsVowel(c);
	}

	private void ModifyAdjective(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (Neuter && ending == "es")
		{
			outputString.Remove(outputString.Length - 2, 2);
		}
		else if (_curGender != WordGenderEnum.NoDeclination && ending[1] == 'e')
		{
			outputString.Remove(outputString.Length - 1, 1);
		}
	}

	private string GetVowelEnding(StringBuilder outputString)
	{
		if (outputString.Length == 0)
		{
			return "";
		}
		char c = outputString[outputString.Length - 1];
		if (!IsVowel(c))
		{
			return "";
		}
		if (outputString.Length > 1 && IsVowel(outputString[outputString.Length - 2]))
		{
			return outputString[outputString.Length - 2].ToString() + c;
		}
		return c.ToString();
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

	private bool ShouldDeclineWord(string sourceText, int cursorPos, string token)
	{
		int num = sourceText.Remove(cursorPos - token.Length - 2).LastIndexOf('}') + 1;
		if (cursorPos - token.Length - 2 - num > 0)
		{
			string text = sourceText.Substring(num, cursorPos - token.Length - 2 - num);
			if (DoNotDeclineList.TryGetValue(char.ToUpper(text[0]), out var value))
			{
				return !value.Contains(text);
			}
		}
		return true;
	}

	private bool IsIrregularWord(string sourceText, int cursorPos, string token, out string irregularWord, out int lengthOfWordToReplace)
	{
		int num = sourceText.Remove(cursorPos - token.Length - 2).LastIndexOf('}') + 1;
		lengthOfWordToReplace = cursorPos - token.Length - 2 - num;
		irregularWord = "";
		string text = "";
		if (lengthOfWordToReplace > 0)
		{
			text = sourceText.Substring(num, lengthOfWordToReplace);
			char key = char.ToUpperInvariant(text[0]);
			if (Masculine && IrregularMasculineDictionary.TryGetValue(key, out var value))
			{
				for (int i = 0; i < value.Count; i++)
				{
					if (value[i].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = DictionaryWordWithCase(token, value[i]);
						break;
					}
				}
			}
			else if (Feminine && IrregularFeminineDictionary.TryGetValue(key, out value))
			{
				for (int j = 0; j < value.Count; j++)
				{
					if (value[j].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = DictionaryWordWithCase(token, value[j]);
						break;
					}
				}
			}
			else if (Neuter && IrregularNeuterDictionary.TryGetValue(key, out value))
			{
				for (int k = 0; k < value.Count; k++)
				{
					if (value[k].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = DictionaryWordWithCase(token, value[k]);
						break;
					}
				}
			}
			else if (Plural && IrregularPluralDictionary.TryGetValue(key, out value))
			{
				for (int l = 0; l < value.Count; l++)
				{
					if (value[l].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = DictionaryWordWithCase(token, value[l]);
						break;
					}
				}
			}
		}
		if (irregularWord.Length > 0)
		{
			if (char.IsLower(text[0]))
			{
				irregularWord = irregularWord.ToLower();
			}
			return true;
		}
		return false;
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
			text = text.Replace("{.nn}", "{.n}");
			text = text.Replace("{.pngroup}", "{.pn}");
			text = text.Replace("{.ajw}", "{.wn}");
			text = text.Replace("{.ajm}", "{.mn}");
			text = text.Replace("{.ajs}", "{.sn}");
			text = text.Replace("{.nnp}", "{.np}");
			text = text.Replace("{.pnpgroup}", "{.pnp}");
			text = text.Replace("{.ajwp}", "{.wnp}");
			text = text.Replace("{.ajmp}", "{.mnp}");
			text = text.Replace("{.ajsp}", "{.snp}");
			_doesComeFromWordGroup = true;
			WordGroupsNoTags.Add(Process(text));
			_doesComeFromWordGroup = false;
		}
	}

	private static string DictionaryWordWithCase(string token, DictionaryWord dictionaryWord)
	{
		switch (token)
		{
		case ".wn":
		case ".mn":
		case ".sn":
		case ".pn":
		case ".pngroup":
		case ".nn":
		case ".ajm":
		case ".ajw":
		case ".ajs":
		case ".n":
			return dictionaryWord.Nominative;
		case ".wnp":
		case ".mnp":
		case ".snp":
		case ".np":
		case ".pnp":
		case ".pnpgroup":
		case ".nnp":
		case ".ajmp":
		case ".ajwp":
		case ".ajsp":
			return dictionaryWord.NominativePlural;
		case ".wa":
		case ".ma":
		case ".sa":
		case ".a":
		case ".pa":
			return dictionaryWord.Accusative;
		case ".wap":
		case ".map":
		case ".sap":
		case ".ap":
		case ".pap":
			return dictionaryWord.AccusativePlural;
		case ".wg":
		case ".mg":
		case ".sg":
		case ".g":
		case ".pg":
			return dictionaryWord.Genitive;
		case ".wgp":
		case ".mgp":
		case ".sgp":
		case ".gp":
		case ".pgp":
			return dictionaryWord.GenitivePlural;
		case ".wd":
		case ".md":
		case ".sd":
		case ".d":
		case ".pd":
			return dictionaryWord.Dative;
		case ".wdp":
		case ".mdp":
		case ".sdp":
		case ".dp":
		case ".pdp":
			return dictionaryWord.DativePlural;
		default:
			return "MISSING IRREGULAR WORD IN LIST";
		}
	}

	private static char GetLastCharacter(StringBuilder outputText, int cursorPos)
	{
		if (cursorPos < outputText.Length)
		{
			cursorPos = outputText.Length;
		}
		for (int num = cursorPos - 1; num >= 0; num--)
		{
			if (char.IsLetter(outputText[num]))
			{
				return outputText[num];
			}
		}
		return '*';
	}

	private static char GetLastCharacter(StringBuilder outputString)
	{
		if (outputString.Length <= 0)
		{
			return '*';
		}
		return outputString[outputString.Length - 1];
	}

	private static char GetSecondLastCharacter(StringBuilder outputString)
	{
		if (outputString.Length <= 1)
		{
			return '*';
		}
		return outputString[outputString.Length - 2];
	}

	private static string GetEnding(StringBuilder outputString, int numChars)
	{
		numChars = TaleWorlds.Library.MathF.Min(numChars, outputString.Length);
		return outputString.ToString(outputString.Length - numChars, numChars);
	}
}
