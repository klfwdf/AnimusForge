using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.Library;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class PolishTextProcessor : LanguageSpecificTextProcessor
{
	private enum WordGenderEnum
	{
		MasculinePersonal,
		MasculineAnimate,
		MasculineInanimate,
		Feminine,
		Neuter,
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

		public const string Vocative = ".v";

		public const string AccusativePlural = ".ap";

		public const string GenitivePlural = ".gp";

		public const string InstrumentalPlural = ".ip";

		public const string LocativePlural = ".lp";

		public const string DativePlural = ".dp";

		public const string VocativePlural = ".vp";

		public static readonly string[] TokenList = new string[14]
		{
			".n", ".p", ".a", ".g", ".i", ".l", ".d", ".v", ".ap", ".gp",
			".ip", ".lp", ".dp", ".vp"
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

		public const string Vocative = ".jv";

		public const string AccusativePlural = ".jap";

		public const string GenitivePlural = ".jgp";

		public const string InstrumentalPlural = ".jip";

		public const string LocativePlural = ".jlp";

		public const string DativePlural = ".jdp";

		public const string VocativePlural = ".jvp";

		public static readonly string[] TokenList = new string[14]
		{
			".j", ".jg", ".jd", ".ja", ".ji", ".jl", ".jv", ".jp", ".jgp", ".jdp",
			".jap", ".jip", ".jlp", ".jvp"
		};
	}

	private static class GenderTokens
	{
		public const string MasculinePersonal = ".MP";

		public const string MasculineInanimate = ".MI";

		public const string MasculineAnimate = ".MA";

		public const string Feminine = ".F";

		public const string Neuter = ".N";

		public static readonly string[] TokenList = new string[5] { ".MP", ".MI", ".MA", ".F", ".N" };
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

		public readonly string Vocative;

		public readonly string AccusativePlural;

		public readonly string GenitivePlural;

		public readonly string InstrumentalPlural;

		public readonly string LocativePlural;

		public readonly string DativePlural;

		public readonly string VocativePlural;

		public IrregularWord(string nominative, string nominativePlural, string genitive, string genitivePlural, string dative, string dativePlural, string accusative, string accusativePlural, string instrumental, string instrumentalPlural, string locative, string locativePlural)
		{
			Nominative = nominative;
			NominativePlural = nominativePlural;
			Accusative = accusative;
			Genitive = genitive;
			Instrumental = instrumental;
			Locative = locative;
			Dative = dative;
			Vocative = locative;
			AccusativePlural = accusativePlural;
			GenitivePlural = genitivePlural;
			InstrumentalPlural = instrumentalPlural;
			LocativePlural = locativePlural;
			DativePlural = dativePlural;
			VocativePlural = nominativePlural;
		}

		public IrregularWord(string nominative, string nominativePlural, string genitive, string genitivePlural, string dative, string dativePlural, string accusative, string accusativePlural, string instrumental, string instrumentalPlural, string locative, string locativePlural, string vocative, string vocativePlural)
		{
			Nominative = nominative;
			NominativePlural = nominativePlural;
			Accusative = accusative;
			Genitive = genitive;
			Instrumental = instrumental;
			Locative = locative;
			Dative = dative;
			Vocative = vocative;
			AccusativePlural = accusativePlural;
			GenitivePlural = genitivePlural;
			InstrumentalPlural = instrumentalPlural;
			LocativePlural = locativePlural;
			DativePlural = dativePlural;
			VocativePlural = vocativePlural;
		}

		public IrregularWord(string nominative, string genitive, string dative, string accusative, string instrumental, string locative, string vocative)
		{
			Nominative = nominative;
			NominativePlural = nominative;
			Accusative = accusative;
			Genitive = genitive;
			Instrumental = instrumental;
			Locative = locative;
			Dative = dative;
			Vocative = vocative;
			AccusativePlural = accusative;
			GenitivePlural = genitive;
			InstrumentalPlural = instrumental;
			LocativePlural = locative;
			DativePlural = dative;
			VocativePlural = vocative;
		}
	}

	private static readonly CultureInfo CultureInfo = new CultureInfo("pl-PL");

	[ThreadStatic]
	private static WordGenderEnum _curGender = WordGenderEnum.NoDeclination;

	[ThreadStatic]
	private static List<(string wordGroup, int firstMarkerPost)> _wordGroups;

	[ThreadStatic]
	private static List<string> _wordGroupsNoTags;

	[ThreadStatic]
	private static List<string> _linkList;

	[ThreadStatic]
	private static bool _doesComeFromWordGroup = false;

	private static readonly char[] Vowels = new char[9] { 'a', 'ą', 'e', 'ę', 'i', 'o', 'ó', 'u', 'y' };

	private static readonly char[] SoftConsonants = new char[5] { 'ć', 'ń', 'ś', 'ź', 'j' };

	private static readonly string[] HardenedConsonants = new string[6] { "dz", "c", "sz", "rz", "cz", "ż" };

	private static readonly string[] HardConsonants = new string[16]
	{
		"g", "k", "ch", "r", "w", "f", "p", "m", "b", "d",
		"t", "n", "s", "z", "ł", "h"
	};

	private static readonly Dictionary<string, string> Palatalization = new Dictionary<string, string>
	{
		{ "g", "gi" },
		{ "k", "ki" },
		{ "ch", "sz" },
		{ "r", "rz" },
		{ "w", "wi" },
		{ "f", "fi" },
		{ "p", "pi" },
		{ "m", "mi" },
		{ "j", "j" },
		{ "b", "bi" },
		{ "d", "dzi" },
		{ "t", "ci" },
		{ "n", "ni" },
		{ "s", "si" },
		{ "z", "zi" },
		{ "ł", "l" },
		{ "ś", "si" },
		{ "ź", "zi" },
		{ "ń", "ni" },
		{ "ć", "ci" }
	};

	private static readonly Dictionary<char, List<IrregularWord>> IrregularMasculinePersonalDictionary = new Dictionary<char, List<IrregularWord>>
	{
		{
			'A',
			new List<IrregularWord>
			{
				new IrregularWord("Alary", "Alary", "Alarego", "Alarego", "Alaremu", "Alaremu", "Alarego", "Alarego", "Alarym", "Alarym", "Alarym", "Alarym"),
				new IrregularWord("Alcza", "Alcza", "Alczy", "Alczy", "Alczy", "Alczy", "Alczę", "Alczę", "Alczą", "Alczą", "Alczy", "Alczy")
			}
		},
		{
			'B',
			new List<IrregularWord>
			{
				new IrregularWord("Bandyta", "Bandyci", "Bandyty", "Bandytów", "Bandycie", "Bandytom", "Bandytę", "Bandytów", "Bandytą", "Bandytami", "Bandycie", "Bandytach"),
				new IrregularWord("Bat", "Batowie", "Bata", "Batów", "Batowi", "Batom", "Bata", "Batów", "Batem", "Batami", "Bacie", "Batach"),
				new IrregularWord("Berserk", "Berserkowie", "Berserka", "Berserków", "Berserkowi", "Berserkom", "Berserka", "Berserków", "Berserkiem", "Berserkami", "Berserku", "Berserkach"),
				new IrregularWord("Bilija", "Bilija", "Bilii", "Bilii", "Bilii", "Bilii", "Biliję", "Biliję", "Biliją", "Biliją", "Bilii", "Bilii"),
				new IrregularWord("Bohater", "Bohaterowie", "Bohatera", "Bohaterów", "Bohaterowi", "Bohaterom", "Bohatera", "Bohaterów", "Bohateram", "Bohateremi", "Bohaterze", "Bohaterach"),
				new IrregularWord("Borcza", "Borcza", "Borczy", "Borczy", "Borczy", "Borczy", "Borczę", "Borczę", "Borczą", "Borczą", "Borczy", "Borczy"),
				new IrregularWord("Botero", "Botero", "Botera", "Botera", "Boterowi", " Boterowi", "Botera", "Botera", "Boterem", "Boterem", "Boterze", "Boterze"),
				new IrregularWord("Bratanek", "Bratankowie", "Bratanka", "Bratanków", "Bratankowi", "Bratankom", "Bratanka", "Bratanków", "Bratankiem", "Bratankami", "Bratanku", "Bratankach"),
				new IrregularWord("Budowniczy", "Budowniczowie", "Budowniczego", "Budowniczych", "Budowniczemu", "Budowniczym", "Budowniczego", "Budowniczych", "Budowniczym", "Budowniczymi", "Budowniczym", "Budowniczych")
			}
		},
		{
			'C',
			new List<IrregularWord>
			{
				new IrregularWord("Cesarski", "Cesarscy", "Cesarskiego", "Cesarskich", "Cesarskiemu", "Cesarskim", "Cesarskiego", "Cesarskich", "Cesarskim", "Cesarskimi", "Cesarskim", "Cesarskich"),
				new IrregularWord("Chłopiec", "Chłopcy", "Chłopca", "Chłopców", "Chłopcu", "Chłopcom", "Chłopca", "Chłopców", "Chłopcem", "Chłopcami", "Chłopca", "Chłopcach"),
				new IrregularWord("Chorąży", "Chorążowie", "Chorążego", "Chorążych", "Chorążemu", "Chorążym", "Chorążego", "Chorążych", "Chorążym", "Chorążymi", "Chorążym", "Chorążych"),
				new IrregularWord("Cieśla", "Cieśle", "Cieśli", "Cieśli", "Cieśli", "Cieślom", "Cieślę", "Cieśli", "Cieślą", "Cieślami", "Cieśli", "Cieślach"),
				new IrregularWord("Członek", "Członkowie", "Członka", "Członków", "Członkowi", "Członkom", "Członka", "Członków", "Członkiem", "Członkami", "Członku", "Członkach"),
				new IrregularWord("Człowiek", "Ludzie", "Człowieka", "Ludzi", "Człowiekowi", "Ludziom", "Człowieka", "Ludzi", "Człowiekiem", "Ludźmi", "Człowieku", "Ludziach")
			}
		},
		{
			'D',
			new List<IrregularWord>
			{
				new IrregularWord("Dalibol", "Dalibol", "Dalibola", "Dalibola", "Dalibolowi", "Dalibolowi", "Dalibola", "Dalibola", "Dalibolem", "Dalibolem", "Dalibolu", "Dalibolu"),
				new IrregularWord("Daszwal", "Daszwal", "Daszwala", "Daszwala", "Daszwalowi", "Daszwalowi", "Daszwala", "Daszwala", "Daszwalem", "Daszwalem", "Daszwalu", "Daszwalu"),
				new IrregularWord("Despota", "Despoci", "Despoty", "Despotów", "Despocie", "Despotom", "Despotę", "Despotów", "Despotą", "Despotami", "Despocie", "Despotach"),
				new IrregularWord("Dijul", "Dijul", "Dijula", "Dijula", "Dijulowi", "Dijulowi", "Dijula", "Dijula", "Dijulem", "Dijulem", "Dijulu", "Dijulu"),
				new IrregularWord("Dowódca", "Dowódcy", "Dowódcy", "Dowódców", "Dowódcy", "Dowódcom", "Dowódcę", "Dowódców", "Dowódcą", "Dowódcami", "Dowódcy", "Dowódcach")
			}
		},
		{
			'E',
			new List<IrregularWord>
			{
				new IrregularWord("Eksmałżonek", "Eksmałżonkowie", "Eksmałżonka", "Eksmałżonków", "Eksmałżonkowi", "Eksmałżonkom", "Eksmałżonka", "Eksmałżonków", "Eksmałżonkiem", "Eksmałżonkami", "Eksmałżonku", "Eksmałżonkach"),
				new IrregularWord("Ekwita", "Ekwici", "Ekwity", "Ekwitów", "Ekwicie", "Ekwitom", "Ekwitę", "Ekwitów", "Ekwitą", "Ekwitami", "Ekwicie", "Ekwitach"),
				new IrregularWord("Erigaj", "Erigaj", "Erigaja", "Erigaja", "Erigajow", "Erigajowii", "Erigaja", "Erigaja", "Erigajem", "Erigajem", "Erigaju", "Erigaju"),
				new IrregularWord("Esos", "Esos", "Esosa", "Esosa", "Esosowi", "Esosowi", "Esosa", "Esosa", "Esosem", "Esosem", "Esosie", "Esosie"),
				new IrregularWord("Elefter", "Elefterowie", "Eleftera", "Elefterów", "Elefterowi", "Elefterom", "Eleftera", "Elefterów", "Elefterem", "Elefterami", "Elefterze", "Elefterach")
			}
		},
		{
			'G',
			new List<IrregularWord>
			{
				new IrregularWord("Garwi", "Garwi", "Garwiego", "Garwiego", "Garwiemu", "Garwiemu", "Garwiego", "Garwiego", "Garwim", "Garwim", "Garwim", "Garwim"),
				new IrregularWord("Ghilman", "Ghilmanowie", "Ghilmana", "Ghilmanów", "Ghilmanowi", "Ghilmanom", "Ghilmana", "Ghilmanów", "Ghilmanem", "Ghilmanami", "Ghilmanie", "Ghilmanach"),
				new IrregularWord("Gorgi", "Gorgi", "Gorgiego", "Gorgiego", "Gorgiemu", "Gorgiemu", "Gorgiego", "Gorgiego", "Gorgim", "Gorgim", "Gorgim", "Gorgim"),
				new IrregularWord("Grabieżca", "Grabieżcy", "Grabieżcy", "Grabieżców", "Grabieżcy", "Grabieżcom", "Grabieżcę", "Grabieżców", "Grabieżcą", "Grabieżcami", "Grabieżcy", "Grabieżcach")
			}
		},
		{
			'H',
			new List<IrregularWord>
			{
				new IrregularWord("Hodowca", "Hodowcy", "Hodowcy", "Hodowców", "Hodowcy", "Hodowcami", "Hodowcę", "Hodowców", "Hodowcą", "Hodowcami", "Hodowcy", "Hodowcach"),
				new IrregularWord("Harcownik", "Harcownicy", "Harcownika", "Harcowników", "Harcownikowi", "Harcownikom", "Harcownika", "Harcowników", "Harcownikiem", "Harcownikami", "Harcowniku", "Harcownikach")
			}
		},
		{
			'I',
			new List<IrregularWord>
			{
				new IrregularWord("Impestor", "Impestorowie", "Impestorów", "Impestorów", "Impestorom", "Impestorom", "Impestorów", "Impestorów", "Impestorami", "Impestorami", "Impestorach", "Impestorach"),
				new IrregularWord("Ingunde", "Ingunde", "Ingundego", "Ingundego", "Ingundemu", "Ingundemu", "Ingundego", "Ingundego", "Ingundem", "Ingundem", "Ingundem", "Ingundem"),
				new IrregularWord("Itsul", "Itsul", "Itsula", "Itsula", "Itsulowi", "Itsulowi", "Itsula", "Itsula", "Itsulem", "Itsulem", "Itsulu", "Itsulu")
			}
		},
		{
			'J',
			new List<IrregularWord>
			{
				new IrregularWord("Jeniec", "Jeńcy", "Jeńca", "Jeńców", "Jeńcowi", "Jeńcom", "Jeńca", "Jeńców", "Jeńcem", "Jeńcami", "Jeńcu", "Jeńcach"),
				new IrregularWord("Jeździec", "Jeźdźcy", "Jeźdźca", "Jeźdźców", "Jeźdźcowi", "Jeźdźcom", "Jeźdźca", "Jeźdźców", "Jeźdźcem", "Jeźdźcami", "Jeźdźcu", "Jeźdźcach")
			}
		},
		{
			'K',
			new List<IrregularWord>
			{
				new IrregularWord("Kada", "Kada", "Kady", "Kady", "Kadzie", "Kadzie", "Kadę", "Kadę", "Kadą", "Kadą", "Kadzie", "Kadzie"),
				new IrregularWord("Karakergita", "Karakergici", "Karakergity", "Karakergitów", "Karakergicie", "Karakergitom", "Karakergitę", "Karakergitów", "Karakergitą", "Karakergitami", "Karakergicie", "Karakergitach"),
				new IrregularWord("Kirasław", "Kirasław", "Kirasława", "Kirasława", "Kirasławowi", "Kirasławowi", "Kirasława", "Kirasława", "Kirasławem", "Kirasławem", "Kirasławie", "Kirasławie"),
				new IrregularWord("Komendant", "Komendanci", "Komendanta", "Komendantów", "Komendantowi", "Komendantom", "Komendanta", "Komendantów", "Komendantem", "Komendantami", "Komendancie", "Komendantach"),
				new IrregularWord("Koniuszy", "Koniuszowie", "Koniuszego", "Koniuszych", "Koniuszemu", "Koniuszym", "Koniuszego", "Koniuszych", "Koniuszym", "Koniuszymi", "Koniuszym", "Koniuszych"),
				new IrregularWord("Książę", "Książęta", "Księcia", "Książąt", "Księciu", "Książętom", "Księcia", "Książąt", "Księciem", "Książętami", "Księciu", "Książętach")
			}
		},
		{
			'L',
			new List<IrregularWord>
			{
				new IrregularWord("Legionista", "Legioniści", "Legionisty", "Legionistów", "Legioniście", "Legionistom", "Legionistę", "Legionistów", "Legionistą", "Legionistami", "Legioniście", "Legionistach"),
				new IrregularWord("Leśny", "Leśni", "Leśnego", "Leśnych", "Leśnemu", "Leśnym", "Leśnego", "Leśnych", "Leśnym", "Leśnymi", "Leśnym", "Leśnych")
			}
		},
		{
			'M',
			new List<IrregularWord>
			{
				new IrregularWord("Marszałek", "Marszałkowie", "Marszałka", "Marszałków", "Marszałkowi", "Marszałkom", "Marszałka", "Marszałków", "Marszałkiem", "Marszałkami", "Marszałku", "Marszałkach"),
				new IrregularWord("Merović", "Merović", "Merovicia", "Merovicia", "Meroviciem", "Meroviciem", "Merovicia", "Merovicia", "Meroviciem", "Meroviciem", "Meroviciu", "Meroviciu"),
				new IrregularWord("Mieszczanin", "Mieszczanie", "Mieszczanina", "Mieszczan", "Mieszczaninowi", "Mieszczanom", "Mieszczanina", "Mieszczan", "Mieszczaninem", "Mieszczanami", "Mieszczaninie", "Mieszczanach"),
				new IrregularWord("Mikri", "Mikri", "Mikriego", "Mikriego", "Mikriemu", "Mikriemu", "Mikriego", "Mikriego", "Mikrim", "Mikrim", "Mikrim", "Mikrim"),
				new IrregularWord("Minstrel", "Minstrele", "Minstrela", "Minstreli", "Minstrelowi", "Minstrelom", "Minstrela", "Minstreli", "Minstrelem", "Minstrelami", "Minstrelu", "Minstrelach"),
				new IrregularWord("Mój", "Moi", "Mojego", "Moich", "Mojemu", "Moim", "Mojego", "Moich", "Moim", "Moimi", "Moim", "Moich", "Mój", "Moi"),
				new IrregularWord("Myśliwy", "Myśliwi", "Myśliwego", "Myśliwych", "Myśliwemu", "Myśliwym", "Myśliwego", "Myśliwych", "Myśliwym", "Myśliwymi", "Myśliwym", "Myśliwych"),
				new IrregularWord("Mąż", "Mężowie", "Męża", "Mężów", "Mężowi", "Mężom", "Męża", "Mężów", "Mężem", "Mężami", "Mężu", "Mężach")
			}
		},
		{
			'N',
			new List<IrregularWord>
			{
				new IrregularWord("Nadrzewny", "Nadrzewni", "Nadrzewnego", "Nadrzewnych", "Nadrzewnemu", "Nadrzewnym", "Nadrzewnego", "Nadrzewnych", "Nadrzewnym", "Nadrzewnymi", "Nadrzewnym", "Nadrzewnych"),
				new IrregularWord("Nal", "Nal", "Nala", "Nala", "Nalowi", "Nalowi", "Nala", "Nala", "Nalem", "Nalem", "Nalu", "Nalu"),
				new IrregularWord("Neutralny", "Neutralni", "Neutralnego", "Neutralnych", "Neutralnemu", "Neutralnym", "Neutralnego", "Neutralnych", "Neutralnym", "Neutralnymi", "Neutralnym", "Neutralnych"),
				new IrregularWord("Nikt", "Nikt", "Nikogo", "Nikogo", "Nikomu", "Nikomu", "Nikogo", "Nikogo", "Nikim", "Nikim", "Nikim", "Nikim")
			}
		},
		{
			'O',
			new List<IrregularWord>
			{
				new IrregularWord("Obrońca", "Obrońcy", "Obrońcy", "Obrońców", "Obrońcy", "Obrońcom", "Obrońcę", "Obrońców", "Obrońcą", "Obrońcami", "Obrońcy", "Obrońcach"),
				new IrregularWord("Obywatel", "Obywatele", "Obywatela", "Obywateli", "Obywatelowi", "Obywatelom", "Obywatela", "Obywateli", "Obywatelem", "Obywatelami", "Obywatelu", "Obywatelach"),
				new IrregularWord("Ocalały", "Ocalali", "Ocalałego", "Ocalałych", "Ocalałemu", "Ocalałym", "Ocalałego", "Ocalałych", "Ocalałym", "Ocalałymi", "Ocalałym", "Ocalałych"),
				new IrregularWord("Ochotnik", "Ochotnicy", "Ochotnika", "Ochotników", "Ochotnikowi", "Ochotnikom", "Ochotnika", "Ochotników", "Ochotnikiem", "Ochotnikami", "Ochotniku", "Ochotnikach"),
				new IrregularWord("Oczekiwany", "Oczekiwani", "Oczekiwanego", "Oczekiwanych", "Oczekiwanemu", "Oczekiwanym", "Oczekiwanego", "Oczekiwanych", "Oczekiwanym", "Oczekiwanymi", "Oczekiwanym", "Oczekiwanych"),
				new IrregularWord("Oddelegowany", "Oddelegowani", "Oddelegowanego", "Oddelegowanych", "Oddelegowanemu", "Oddelegowanym", "Oddelegowanego", "Oddelegowanych", "Oddelegowanem", "Oddelegowanymi", "Oddelegowanem", "Oddelegowanych"),
				new IrregularWord("Ojciec", "Ojcowie", "Ojca", "Ojców", "Ojcu", "Ojcom", "Ojca", "Ojców", "Ojcem", "Ojcami", "Ojcu", "Ojcach"),
				new IrregularWord("Orest", "Orest", "Oresta", "Oresta", "Orestowi", "Orestowi", "Oresta", "Oresta", "Orestem", "Orestem", "Oreście", "Oreście"),
				new IrregularWord("Oto", "Oto", "Otona", "Otona", "Otonowi", "Otonowi", "Otona", "Otona", "Otonem", "Otonem", "Otonie", "Otonie")
			}
		},
		{
			'P',
			new List<IrregularWord>
			{
				new IrregularWord("Pan", "Panowie", "Pana", "Panów", "Panu", "Panom", "Pana", "Panów", "Panem", "Panami", "Panie", "Panach", "Panie", "Panowie"),
				new IrregularWord("Pasterz", "Pasterze", "Pasterza", "Pasterzy", "Pasterzowi", "Pasterzom", "Pasterza", "Pasterzy", "Pasterzem", "Pasterzami", "Pasterzu", "Pasterzach"),
				new IrregularWord("Pazel", "Pazel", "Pazela", "Pazela", "Pazelowi", "Pazelowi", "Pazela", "Pazela", "Pazelem", "Pazelem", "Pazelu", "Pazelu"),
				new IrregularWord("Pikinier", "Pikinierzy", "Pikiniera", "Pikinierów", "Pikinierowi", "Pikinierom", "Pikiniera", "Pikinierów", "Pikinierem", "Pikinierami", "Pikinierze", "Pikinierach"),
				new IrregularWord("Planista", "Planiści", "Planisty", "Planistów", "Planiście", "Planistom", "Planistę", "Planistów", "Planistą", "Planistami", "Planiście", "Planistach"),
				new IrregularWord("Przestępca", "Przestępcy", "Przestępcy", "Przestępców", "Przestępcy", "Przestępcom", "Przestępcę", "Przestępców", "Przestępcy", "Przestępcami", "Przestępcy", "Przestępcach"),
				new IrregularWord("Przywódca", "Przywódcy", "Przywódcy", "Przywódców", "Przywódcą", "Przywódcom", "Przywódcę", "Przywódców", "Przywódcą", "Przywódcami", "Przywódcy", "Przywódcach")
			}
		},
		{
			'R',
			new List<IrregularWord>
			{
				new IrregularWord("Ratagost", "Ratagost", "Ratagosta", "Ratagosta", "Ratagostowi", "Ratagostowi", "Ratagosta", "Ratagosta", "Ratagostem", "Ratagostem", "Ratagoście", "Ratagoście"),
				new IrregularWord("Rożywol", "Rożywol", "Rożywola", "Rożywola", "Rożywolowi", "Rożywolowi", "Rożywola", "Rożywola", "Rożywolem", "Rożywolem", "Rożywolu", "Rożywolu"),
				new IrregularWord("Rzeczoznawca", "Rzeczoznawcy", "Rzeczoznawcy", "Rzeczoznawców", "Rzeczoznawcy", "Rzeczoznawcom", "Rzeczoznawcę", "Rzeczoznawców", "Rzeczoznawcą", "Rzeczoznawcami", "Rzeczoznawcy", "Rzeczoznawcach"),
				new IrregularWord("Rządca", "Rządcy", "Rządcy", "Rządców", "Rządcy", "Rządcom", "Rządcę", "Rządców", "Rządcą", "Rządcami", "Rządcy", "Rządcach")
			}
		},
		{
			'S',
			new List<IrregularWord>
			{
				new IrregularWord("Sacha", "Sacha", "Sachy", "Sachy", "Sasze", "Sasze", "Sachę", "Sachę", "Sachą", "Sachą", "Sasze", "Sasze"),
				new IrregularWord("Sasal", "Sasal", "Sasala", "Sasala", "Sasalowi", "Sasalowi", "Sasala", "Sasala", "Sasalem", "Sasalem", "Sasalu", "Sasalu"),
				new IrregularWord("Sierota", "Sieroty", "Sieroty", "Sierotach", "Sierocie", "Sierotom", "Sierotę", "Sieroty", "Sierotą", "Sierotami", "Sierocie", "Sierotach"),
				new IrregularWord("Skene", "Skene", "Skenego", "Skenego", "Skenemu", "Skenemu", "Skenego", "Skenego", "Skenem", "Skenem", "Skenie", "Skenie"),
				new IrregularWord("Skolderbrod", "Skolderbrodowie", "Skolderbroda", "Skolderbrodów", "Skolderbrodowi", "Skolderbrodom", "Skolderbroda", "Skolderbrodów", "Skolderbrodem", "Skolderbrodami", "Skolderbrodzie", "Skolderbrodach"),
				new IrregularWord("Spartanin", "Spartanie", "Spartanina", "Spartan", "Spartaninie", "Spartanom", "Spartanina", "Spartan", "Spartaninem", "Spartanami", "Spartaninie", "Spartanach"),
				new IrregularWord("Starszy", "Starsi", "Starszego", "Starszych", "Starszemu", "Starszym", "Starszego", "Starszych", "Starszym", "Starszymi", "Starszym", "Starszych"),
				new IrregularWord("Strażnik", "Strażnicy", "Strażnika", "Strażników", "Strażnikowi", "Strażnikom", "Strażnika", "Strażników", "Strażnikiem", "Strażnikami", "Strażniku", "Strażnikach"),
				new IrregularWord("Stróż", "Stróże", "Stróża", "Stróżów", "Stróżowi", "Stróżom", "Stróża", "Stróżów", "Stróżem", "Stróżami", "Stróżu", "Stróżach"),
				new IrregularWord("Strzelec", "Strzelcy", "Strzelca", "Strzelców", "Strzelcowi", "Strzelcom", "Strzelca", "Strzelców", "Strzelciem", "Strzelcami", "Strzelcu", "Strzelcach"),
				new IrregularWord("Sujkana", "Sujkana", "Sujkany", "Sujkany", "Sujkanie", "Sujkanie", "Sujkanę", "Sujkanę", "Sujkaną", "Sujkaną", "Sujkanie", "Sujkanie"),
				new IrregularWord("Szawił", "Szawił", "Szawiła", "Szawiła", "Szawiłowi", "Szawiłowi", "Szawiła", "Szawiła", "Szawiłem", "Szawiłem", "Szawile", "Szawile"),
				new IrregularWord("Szkoleniowiec", "Szkoleniowcy", "Szkoleniowca", "Szkoleniowców", "Szkoleniowcowi", "Szkoleniowcom", "Szkoleniowca", "Szkoleniowców", "Szkoleniowcem", "Szkoleniowcami", "Szkoleniowcu", "Szkoleniowcach"),
				new IrregularWord("Szumowina", "Szumowiny", "Szumowiny", "Szumowin", "Szumowinie", "Szumowinom", "Szumowinę", "Szumowiny", "Szumowiną", "Szumowinami", "Szumowinie", "Szumowinach"),
				new IrregularWord("Sędzia", "Sędziowie", "Sędzia", "Sędziów", "Sędziemu", "Sędziom", "Sędziego", "Sędziów", "Sędzią", "Sędziami", "Sędzi", "Sędziach"),
				new IrregularWord("Sierżant", "Sierżanci", "Sierżanta", "Sierżantów", "Sierżantowi", "Sierżantom", "Sierżanta", "Sierżantów", "Sierżantem", "Sierżantami", "Sierżancie", "Sierżantach")
			}
		},
		{
			'T',
			new List<IrregularWord>
			{
				new IrregularWord("Taja", "Taja", "Tai", "Tai", "Tai", "Tai", "Taję", "Taję", "Tają", "Tają", "Tai", "Tai"),
				new IrregularWord("Tamza", "Tamza", "Tamzy", "Tamzy", "Tamzie", "Tamzie", "Tamzę", "Tamzę", "Tamzą", "Tamzą", "Tamzie", "Tamzie"),
				new IrregularWord("Ten", "Ci", "Tego", "Tych", "Temu", "Tym", "Tego", "Tych", "Tym", "Tymi", "Tym", "Tych"),
				new IrregularWord("Tochi", "Tochi", "Tochiego", "Tochiego", "Tochiemu", "Tochiemu", "Tochiego", "Tochiego", "Tochim", "Tochim", "Tochim", "Tochim")
			}
		},
		{
			'U',
			new List<IrregularWord>
			{
				new IrregularWord("Untery", "Untery", "Unterego", "Unterego", "Unteremu", "Unteremu", "Unterego", "Unterego", "Unterym", "Unterym", "Unterym", "Unterym")
			}
		},
		{
			'V',
			new List<IrregularWord>
			{
				new IrregularWord("VIP", "VIP-owie", "VIP-a", "VIP-ów", "VIP-owi", "VIP-om", "VIP-a", "VIP-ów", "VIP-em", "VIP-ami", "VIP-ie", "VIP-ach")
			}
		},
		{
			'W',
			new List<IrregularWord>
			{
				new IrregularWord("Waszorki", "Waszorki", "Waszorkiego", "Waszorkiego", "Waszorkiemu", "Waszorkiemu", "Waszorkiego", "Waszorkiego", "Waszorkim", "Waszorkim", "Waszorkim", "Waszorkim"),
				new IrregularWord("Weliszyn", "Weliszyn", "Weliszyna", "Weliszyna", "Weliszynowi", "Weliszynowi", "Weliszyn", "Weliszyn", "Weliszynem", "Weliszynem", "Weliszynie", "Weliszynie"),
				new IrregularWord("Wilczoskóry", "Wilczoskórzy", "Wilczoskórego", "Wilczoskórych", "Wilczoskóremu", "Wilczoskórym", "Wilczoskórego", "Wilczoskórych", "Wilczoskórym", "Wilczoskórymi", "Wilczoskórym", "Wilczoskórych"),
				new IrregularWord("Wiejski", "Wiejscy", "Wiejskiego", "Wiejskich", "Wiejskiemu", "Wiejskim", "Wiejskiego", "Wiejskich", "Wiejskim", "Wiejskimi", "Wiejskim", "Wiejskich"),
				new IrregularWord("Więzień", "Więźniowie", "Więźnia", "Więźniów", "Więźniowi", "Więźniom", "Więźnia", "Więźniów", "Więźniem", "Więźniami", "Więźniu", "Więźniach"),
				new IrregularWord("Władca", "Władcy", "Władcy", "Władców", "Władcy", "Władcom", "Władcę", "Władców", "Władcą", "Władcami", "Władcy", "Władcach"),
				new IrregularWord("Włócznik", "Włócznicy", "Włócznika", "Włóczników", "Włócznikowi", "Włócznikom", "Włócznika", "Włóczników", "Włócznikiem", "Włócznikami", "Włóczniku", "Włócznikach"),
				new IrregularWord("Wojownik", "Wojownicy", "Wojownika", "Wojowników", "Wojownikowi", "Wojownikami", "Wojownika", "Wojowników", "Wojownikiem", "Wojownikami", "Wojowniku", "Wojownikach"),
				new IrregularWord("Wór", "Wory", "Wora", "Worów", "Worowi", "Worom", "Wór", "Wory", "Worem", "Worami", "Worze", "Worach"),
				new IrregularWord("Wspierający", "Wspierający", "Wspierającego", "Wspierających", "Wspierającemu", "Wspierającym", "Wspierającego", "Wspierających", "Wspierającym", "Wspierającymi", "Wspierającym", "Wspierających"),
				new IrregularWord("Współwinny", "Współwinni", "Współwinnego", "Współwinnych", "Współwinnemu", "Współwinnym", "Współwinnego", "Współwinnych", "Współwinnym", "Współwinnymi", "Współwinnym", "Współwinnych"),
				new IrregularWord("Wybraniec", "Wybrańcy", "Wybrańca", "Wybrańców", "Wybrańcowi", "Wybrańcom", "Wybrańca", "Wybrańców", "Wybrańcem", "Wybrańcami", "Wybrańcu", "Wybrańcach"),
				new IrregularWord("Wędrowiec", "Wędrowcy", "Wędrowca", "Wędrowców", "Wędrowcowi", "Wędrowcom", "Wędrowca", "Wędrowców", "Wędrowcem", "Wędrowcami", "Wędrowcu", "Wędrowcach"),
				new IrregularWord("Wojewoda", "Wojewodowie", "Wojewody", "Wojewodów", "Wojewodzie", "Wojewodom", "Wojewodę", "Wojewodów", "Wojewodą", "Wojewodami", "Wojewodzie", "Wojewodach")
			}
		},
		{
			'Z',
			new List<IrregularWord>
			{
				new IrregularWord("Zabity", "Zabici", "Zabitego", "Zabitych", "Zabitemu", "Zabitym", "Zabitego", "Zabitych", "Zabitym", "Zabitymi", "Zabitym", "Zabitych"),
				new IrregularWord("Zarządca", "Zarządcy", "Zarządcy", "Zarządców", "Zarządcy", "Zarządcom", "Zarządcę", "Zarządców", "Zarządcą", "Zarządcami", "Zarządcy", "Zarządcach"),
				new IrregularWord("Zbójca", "Zbójcy", "Zbójcy", "Zbójców", "Zbójcy", "Zbójcom", "Zbójcę", "Zbójców", "Zbójcą", "Zbójcami", "Zbójcy", "Zbójcach"),
				new IrregularWord("Zgniłozęby", "Zgniłozębi", "Zgniłozębego", "Zgniłozębych", "Zgniłozębemu", "Zgniłozębym", "Zgniłozębego", "Zgniłozębych", "Zgniłozębym", "Zgniłozębymi", "Zgniłozębym", "Zgniłozębych"),
				new IrregularWord("Znakowy", "Znakowi", "Znakowego", "Znakowych", "Znakowemu", "Znakowym", "Znakowego", "Znakowych", "Znakowym", "Znakowymi", "Znakowym", "Znakowych")
			}
		},
		{
			'Ż',
			new List<IrregularWord>
			{
				new IrregularWord("Żołnierz", "Żołnierze", "Żołnierza", "Żołnierzy", "Żołnierzowi", "Żołnierzom", "Żołnierza", "Żołnierzy", "Żołnierzem", "Żołnierzami", "Żołnierzu", "Żołnierzach")
			}
		},
		{
			'Ł',
			new List<IrregularWord>
			{
				new IrregularWord("Łowca", "Łowcy", "Łowcy", "Łowców", "Łowcy", "Łowcom", "Łowcę", "Łowców", "Łowcą", "Łowcami", "Łowcy", "Łowcach"),
				new IrregularWord("Łupieżca", "Łupieżcy", "Łupieżcy", "Łupieżców", "Łupieżcy", "Łupieżcom", "Łupieżcę", "Łupieżców", "Łupieżcą", "Łupieżcami", "Łupieżcy", "Łupieżcach")
			}
		}
	};

	private static readonly Dictionary<char, List<IrregularWord>> IrregularMasculineAnimateDictionary = new Dictionary<char, List<IrregularWord>>
	{
		{
			'K',
			new List<IrregularWord>
			{
				new IrregularWord("Koń", "Konie", "Konia", "Koni", "Koniowi", "Koniom", "Konia", "Konie", "Koniem", "Końmi", "Koniu", "Koniach")
			}
		},
		{
			'M',
			new List<IrregularWord>
			{
				new IrregularWord("Muł", "Muły", "Muła", "Mułów", "Mułowi", "Mułom", "Muła", "Muły", "Mułem", "Mułami", "Mule", "Mułach")
			}
		},
		{
			'P',
			new List<IrregularWord>
			{
				new IrregularWord("Pionek", "Pionki", "Pionka", "Pionków", "Pionkowi", "Pionkom", "Pionka", "Pionki", "Pionkiem", "Pionkami", "Pionku", "Pionkach")
			}
		},
		{
			'T',
			new List<IrregularWord>
			{
				new IrregularWord("Ten", "Te", "Tego", "Tych", "Temu", "Tym", "Tego", "Te", "Tym", "Tymi", "Tym", "Tych")
			}
		},
		{
			'W',
			new List<IrregularWord>
			{
				new IrregularWord("Wiejski", "Wiejskie", "Wiejskiego", "Wiejskich", "Wiejskiemu", "Wiejskim", "Wiejskiego", "Wiejskich", "Wiejskim", "Wiejskimi", "Wiejskim", "Wiejskich")
			}
		}
	};

	private static readonly Dictionary<char, List<IrregularWord>> IrregularMasculineInanimateDictionary = new Dictionary<char, List<IrregularWord>>
	{
		{
			'A',
			new List<IrregularWord>
			{
				new IrregularWord("Ałow", "Ałow", "Ałowu", "Ałowu", "Ałowowi", "Ałowowi", "Ałow", "Ałow", "Ałowem", "Ałowem", "Ałowie", "Ałowie")
			}
		},
		{
			'C',
			new List<IrregularWord>
			{
				new IrregularWord("Ciesielski", "Ciesielskie", "Ciesielskiego", "Ciesielskich", "Ciesielskiemu", "Ciesielskim", "Ciesielski", "Ciesielskie", "Ciesielskim", "Ciesielskimi", "Ciesielskim", "Ciesielskich", "Ciesielski", "Ciesielskie"),
				new IrregularWord("Czepiec", "Czepce", "Czepca", "Czepców", "Czepcowi", "Czepcom", "Czepiec", "Czepce", "Czepcem", "Czepcami", "Czepcu", "Czepcach")
			}
		},
		{
			'D',
			new List<IrregularWord>
			{
				new IrregularWord("Dech", "Tchy", "Tchu", "Tchów", "Tchowi", "Tchom", "Dech", "Tchy", "Tchem", "Tchami", "Tchu", "Tchach")
			}
		},
		{
			'G',
			new List<IrregularWord>
			{
				new IrregularWord("Gławstrom", "Gławstrom", "Gławstromu", "Gławstromu", "Gławstromowi", "Gławstromowi", "Gławstrom", "Gławstrom", "Gławstromem", "Gławstromem", "Gławstromie", "Gławstromie")
			}
		},
		{
			'K',
			new List<IrregularWord>
			{
				new IrregularWord("Kufer", "Kufry", "Kufra", "Kufrów", "Kufrowi", "Kufrom", "Kufer", "Kufry", "Kufrem", "Kuframi", "Kufrze", "Kufrach")
			}
		},
		{
			'M',
			new List<IrregularWord>
			{
				new IrregularWord("Monopol", "Monopole", "Monopolu", "Monopoli", "Monopolowi", "Monopolom", "Monopol", "Monopole", "Monopolem", "Monopolami", "Monopolu", "Monopolach", "Monopolu", "Monopole")
			}
		},
		{
			'N',
			new List<IrregularWord>
			{
				new IrregularWord("Newijańsk", "Newijańsk", "Newijańska", "Newijańska", "Newijańskowi", "Newijańskowi", "Newijańsk", "Newijańsk", "Newijańskiem", "Newijańskiem", "Newijańsku", "Newijańsku")
			}
		},
		{
			'P',
			new List<IrregularWord>
			{
				new IrregularWord("Pęd", "Pędy", "Pędu", "Pędów", "Pędowi", "Pędom", "Pęd", "Pędy", "Pędem", "Pędami", "Pędzie", "Pędach"),
				new IrregularWord("Pojedynek", "Pojedynki", "Pojedynku", "Pojedynków", "Pojedynkowi", "Pojedynkom", "Pojedynek", "Pojedynki", "Pojedynkiem", "Pojedynkami", "Pojedynku", "Pojedynkach")
			}
		},
		{
			'S',
			new List<IrregularWord>
			{
				new IrregularWord("Szpikulec", "Szpikulce", "Szpikulca", "Szpikulców", "Szpikulcowi", "Szpikulcom", "Szpikulec", "Szpikulce", "Szpikulcem", "Szpikulcami", "Szpikulcu", "Szpikulcach"),
				new IrregularWord("Spryt", "Sprytu", "Sprytowi", "Spryt", "Sprytem", "Sprycie", "Sprycie"),
				new IrregularWord("Samouczek", "Samouczki", "Samouczka", "Samouczków", "Samouczkowi", "Samouczkom", "Samouczek", "Samouczki", "Samouczkiem", "Samouczkami", "Samouczku", "Samouczkach")
			}
		},
		{
			'T',
			new List<IrregularWord>
			{
				new IrregularWord("Ten", "Te", "Tego", "Tych", "Temu", "Tym", "Ten", "Te", "Tym", "Tymi", "Tym", "Tych")
			}
		},
		{
			'U',
			new List<IrregularWord>
			{
				new IrregularWord("Ustokoł", "Ustokoł", "Ustokołu", "Ustokołu", "Ustokołowi", "Ustokołowi", "Ustokoł", "Ustokoł", "Ustokołem", "Ustokołem", "Ustokole", "Ustokole")
			}
		},
		{
			'W',
			new List<IrregularWord>
			{
				new IrregularWord("Wiejski", "Wiejskie", "Wiejskiego", "Wiejskich", "Wiejskiemu", "Wiejskim", "Wiejski", "Wiejskich", "Wiejskim", "Wiejskimi", "Wiejskim", "Wiejskich"),
				new IrregularWord("Wigor", "Wigoru", "Wigorowi", "Wigor", "Wigorem", "Wigorze", "Wigorze"),
				new IrregularWord("Wydatek", "Wydatki", "Wydatku", "Wydatków", "Wydatkowi", "Wydatkom", "Wydatek", "Wydatki", "Wydatkiem", "Wydatkami", "Wydatku", "Wydatkach"),
				new IrregularWord("Władyw", "Władyw", "Władywu", "Władywu", "Władywowi", "Władywowi", "Władyw", "Władyw", "Władywem", "Władywem", "Władywie", "Władywie")
			}
		},
		{
			'Z',
			new List<IrregularWord>
			{
				new IrregularWord("Zamek", "Zamki", "Zamku", "Zamków", "Zamkowi", "Zamkom", "Zamek", "Zamki", "Zamkiem", "Zamkami", "Zamku", "Zamkach")
			}
		}
	};

	private static readonly Dictionary<char, List<IrregularWord>> IrregularFeminineDictionary = new Dictionary<char, List<IrregularWord>>
	{
		{
			'A',
			new List<IrregularWord>
			{
				new IrregularWord("Aika", "Aika", "Aiki", "Aiki", "Aice", "Aice", "Aikę", "Aikę", "Aiką", "Aiką", "Aice", "Aice"),
				new IrregularWord("Arga", "Arga", "Argi", "Argi", "Ardze", "Ardze", "Argę", "Argę", "Argą", "Argą", "Ardze", "Ardze"),
				new IrregularWord("Asta", "Asta", "Asty", "Asty", "Aście", "Aście", "Astę", "Astę", "Astą", "Astą", "Aście", "Aście")
			}
		},
		{
			'B',
			new List<IrregularWord>
			{
				new IrregularWord("Bela", "Bele", "Beli", "Beli", "Beli", "Belom", "Belę", "Bele", "Belą", "Belami", "Beli", "Belach", "Belo", "Bele")
			}
		},
		{
			'C',
			new List<IrregularWord>
			{
				new IrregularWord("Część", "Części", "Części", "Części", "Części", "Częściom", "Część", "Części", "Częścią", "Częściami", "Części", "Częściach")
			}
		},
		{
			'D',
			new List<IrregularWord>
			{
				new IrregularWord("Dłoń", "Dłonie", "Dłoni", "Dłoni", "Dłoni", "Dłoniom", "Dłoń", "Dłonie", "Dłonią", "Dłońmi", "Dłoni", "Dłoniach")
			}
		},
		{
			'E',
			new List<IrregularWord>
			{
				new IrregularWord("Echa", "Echa", "Esze", "Esze", "Esze", "Esze", "Echę", "Echę", "Echą", "Echą", "Esze", "Esze"),
				new IrregularWord("Epikrotea", "Epikrotea", "Epikrotei", "Epikrotei", "Epikrotei", "Epikrotei", "Epikroteę", "Epikroteę", "Epikroteą", "Epikroteą", "Epikrotei", "Epikrotei", "Epikroteo", "Epikroteo")
			}
		},
		{
			'F',
			new List<IrregularWord>
			{
				new IrregularWord("Fianna", "Fianna", "Fianny", "Fianny", "Fiannie", "Fiannie", "Fiannę", "Fiannę", "Fianną", "Fianną", "Fiannie", "Fiannie")
			}
		},
		{
			'I',
			new List<IrregularWord>
			{
				new IrregularWord("Inteligencja", "Inteligencji", "Inteligencji", "Inteligencję", "Inteligencją", "Inteligencji", "Inteligencjo")
			}
		},
		{
			'K',
			new List<IrregularWord>
			{
				new IrregularWord("Kieszeń", "Kieszenie", "Kieszeni", "Kieszeni", "Kieszeni", "Kieszeniom", "Kieszeń", "Kieszenie", "Kieszenią", "Kieszeniami", "Kieszeni", "Kieszeniach"),
				new IrregularWord("Kuka", "Kuka", "Kuki", "Kuki", "Kuce", "Kuce", "Kukę", "Kukę", "Kuką", "Kuką", "Kuce", "Kuce"),
				new IrregularWord("Kontrola", "Kontroli", "Kontroli", "Kontrolę", "Kontrolą", "Kontroli", "Kontrolo")
			}
		},
		{
			'L',
			new List<IrregularWord>
			{
				new IrregularWord("Laska", "Laska", "Laski", "Laski", "Lasce", "Lasce", "Laskę", "Laskę", "Laską", "Laską", "Lasce", "Lasce"),
				new IrregularWord("Litka", "Litka", "Litki", "Litki", "Litce", "Litce", "Litkę", "Litkę", "Litką", "Litką", "Litce", "Litce")
			}
		},
		{
			'P',
			new List<IrregularWord>
			{
				new IrregularWord("Postać", "Postacie", "Postaci", "Postaci", "Postaci", "Postaciom", "Postać", "Postacie", "Postacią", "Postaciami", "Postaci", "Postaciach")
			}
		},
		{
			'R',
			new List<IrregularWord>
			{
				new IrregularWord("Ręka", "Ręce", "Rąk", "Ręki", "Ręce", "Rękom", "Rękę", "Ręce", "Ręką", "Rękami", "Ręce", "Rękach"),
				new IrregularWord("Rotea", "Rotea", "Rotei", "Rotei", "Rotei", "Rotei", "Roteę", "Roteę", "Roteą", "Roteą", "Rotei", "Rotei", "Roteo", "Roteo")
			}
		},
		{
			'S',
			new List<IrregularWord>
			{
				new IrregularWord("Sieć", "Sieci", "Sieci", "Sieci", "Sieci", "Sieciom", "Sieć", "Sieci", "Siecią", "Sieciami", "Sieci", "Sieciach"),
				new IrregularWord("Sztuka", "Sztuki", "Sztuki", "Sztuk", "Sztuce", "Sztukom", "Sztukę", "Sztuki", "Sztuką", "Sztukami", "Sztuce", "Sztukach")
			}
		},
		{
			'V',
			new List<IrregularWord>
			{
				new IrregularWord("Vikka", "Vikka", "Vikki", "Vikki", "Vikce", "Vikce", "Vikkę", "Vikkę", "Vikką", "Vikką", "Vikce", "Vikce")
			}
		},
		{
			'W',
			new List<IrregularWord>
			{
				new IrregularWord("Wdowa", "Wdowy", "Wdowy", "Wdów", "Wdowie", "Wdowom", "Wdowę", "Wdowy", "Wdową", "Wdowami", "Wdowie", "Wdowach"),
				new IrregularWord("Wiejska", "Wiejskie", "Wiejskiej", "Wiejskich", "Wiejskiej", "Wiejskim", "Wiejską", "Wiejskich", "Wiejską", "Wiejskimi", "Wiejskiej", "Wiejskich"),
				new IrregularWord("Wieś", "Wsie", "Wsi", "Wsi", "Wsi", "Wsiom", "Wieś", "Wsie", "Wsią", "Wsiami", "Wsi", "Wsiach"),
				new IrregularWord("Wić", "Wici", "Wici", "Wici", "Wici", "Wiciom", "Wić", "Wici", "Wicią", "Wićmi", "Wici", "Wiciach")
			}
		},
		{
			'U',
			new List<IrregularWord>
			{
				new IrregularWord("Uprząż", "Uprzęże", "Uprzęży", "Uprzęży", "Uprzęży", "Uprzężom", "Uprząż", "Uprzęże", "Uprzężą", "Uprzężami", "Uprzęży", "Uprzężach")
			}
		}
	};

	private static readonly Dictionary<char, List<IrregularWord>> IrregularNeuterDictionary = new Dictionary<char, List<IrregularWord>>
	{
		{
			'D',
			new List<IrregularWord>
			{
				new IrregularWord("Drzwi", "Drzwi", "Drzwi", "Drzwi", "Drzwiom", "Drzwiom", "Drzwi", "Drzwi", "Drzwiami", "Drzwiami", "Drzwiach", "Drzwiach")
			}
		},
		{
			'P',
			new List<IrregularWord>
			{
				new IrregularWord("Prosię", "Prosięta", "Prosięcia", "Prosiąt", "Prosięciu", "Prosiętami", "Prosię", "Prosięta", "Prosięciem", "Prosiętami", "Prosięciu", "Prosiętach")
			}
		},
		{
			'R',
			new List<IrregularWord>
			{
				new IrregularWord("Rusztowanie", "Rusztowania", "Rusztowania", "Rusztowań", "Rusztowaniu", "Rusztowaniom", "Rusztowanie", "Rusztowania", "Rusztowaniem", "Rusztowaniami", "Rusztowaniu", "Rusztowaniach")
			}
		},
		{
			'S',
			new List<IrregularWord>
			{
				new IrregularWord("Sanie", "Sanie", "Sań", "Sań", "Saniom", "Saniom", "Sanie", "Sanie", "Saniami", "Saniami", "Saniach", "Saniach"),
				new IrregularWord("Spodnie", "Spodnie", "Spodni", "Spodni", "Spodniom", "Spodniom", "Spodnie", "Spodnie", "Spodniami", "Spodniami", "Spodniach", "Spodniach")
			}
		},
		{
			'T',
			new List<IrregularWord>
			{
				new IrregularWord("Twarde drewno", "Twarde drewno", "Twardego drewna", "Twardego drewna", "Twardemu drewnu", "Twardemu drewnu", "Twarde drewno", "Twarde drewno", "Twardym drewnem", "Twardym drewnem", "Twardym drewnie", "Twardym drewnie", "Twarde drewno", "Twarde drewno")
			}
		},
		{
			'W',
			new List<IrregularWord>
			{
				new IrregularWord("Wiejskie", "Wiejskie", "Wiejskiego", "Wiejskich", "Wiejskiemu", "Wiejskim", "Wiejskie", "Wiejskie", "Wiejskim", "Wiejskimi", "Wiejskim", "Wiejskich")
			}
		},
		{
			'Z',
			new List<IrregularWord>
			{
				new IrregularWord("Zarękawie", "Zarękawia", "Zarękawia", "Zarękawi", "Zarękawiu", "Zarękawiom", "Zarękawie", "Zarękawia", "Zarękawiem", "Zarękawiami", "Zarękawiu", "Zarękawiach"),
				new IrregularWord("Zarośla", "Zarośla", "Zarośli", "Zarośli", "Zaroślom", "Zaroślom", "Zarośla", "Zarośla", "Zaroślami", "Zaroślami", "Zaroślach", "Zaroślach"),
				new IrregularWord("Zwierzę", "Zwierzęta", "Zwierzęcia", "Zwierząt", "Zwierzęciu", "Zwierzętom", "Zwierzę", "Zwierzęta", "Zwierzęciem", "Zwierzętami", "Zwierzeciu", "Zwierzętach")
			}
		}
	};

	public override CultureInfo CultureInfoForLanguage => CultureInfo;

	private bool MasculinePersonal => _curGender == WordGenderEnum.MasculinePersonal;

	private bool MasculineAnimate => _curGender == WordGenderEnum.MasculineAnimate;

	private bool MasculineInanimate => _curGender == WordGenderEnum.MasculineInanimate;

	private bool Feminine => _curGender == WordGenderEnum.Feminine;

	private bool Neuter => _curGender == WordGenderEnum.Neuter;

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
		if (token.EndsWith("Creator"))
		{
			outputString.Append("{" + token.Replace("Creator", "") + "}");
		}
		else if (Array.IndexOf(GenderTokens.TokenList, token) >= 0)
		{
			switch (token)
			{
			case ".MP":
				SetMasculinePersonal();
				break;
			case ".MI":
				SetMasculineInanimate();
				break;
			case ".MA":
				SetMasculineAnimate();
				break;
			case ".F":
				SetFeminine();
				break;
			case ".N":
				SetNeuter();
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
						case ".v":
							AddSuffixNounVocative(outputString);
							break;
						case ".vp":
							AddSuffixNounVocativePlural(outputString);
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
						case ".jv":
							AddSuffixAdjectiveVocative(outputString);
							break;
						case ".jvp":
							AddSuffixAdjectiveVocativePlural(outputString);
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
		bool flag = char.IsUpper(text[0]);
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
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Neuter)
		{
			if (IsVowel(ending[1]))
			{
				outputString.Remove(outputString.Length - 1, 1);
			}
			if (!GetEnding(outputString, 2).Equals("um"))
			{
				outputString.Append('a');
			}
		}
		else if (Feminine || MasculineAnimate || MasculineInanimate || GetLastCharacter(outputString) == 'a')
		{
			if (ending[1] == 'a' && ending[0] != 't')
			{
				outputString.Remove(outputString.Length - 1, 1);
				ending = GetEnding(outputString, 2);
			}
			if (ending.Equals("ta"))
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("ci");
			}
			else if (GetEnding(outputString, 3).Equals("ość"))
			{
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append("ci");
			}
			else if (MasculineInanimate && ending.Equals("to"))
			{
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append('a');
			}
			else if (ending.Equals("ch"))
			{
				outputString.Append("y");
			}
			else if (ending[1] == 'g' || ending[1] == 'k')
			{
				outputString.Append('i');
			}
			else if (outputString.Length > 4 && GetEnding(outputString, 5).Equals("rzecz"))
			{
				outputString.Append('y');
			}
			else if (IsVowel(ending[1]) || IsSoftConsonant(ending) || IsHardenedConsonant(ending))
			{
				if (IsSoftConsonant(ending))
				{
					PalatalizeConsonant(outputString, ending);
					outputString.Append("e");
				}
				else
				{
					outputString.Append('e');
				}
			}
			else
			{
				outputString.Append('y');
			}
		}
		else if (ending.Equals("ch"))
		{
			outputString.Remove(outputString.Length - 2, 2);
			outputString.Append("si");
		}
		else if (ending[1] == 'g')
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append("dzy");
		}
		else if (ending[1] == 'k')
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append("cy");
		}
		else if (ending[1] == 'r')
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append("rzy");
		}
		else if (ending.Equals("ec"))
		{
			outputString.Append("y");
		}
		else if (ending[1] == 't')
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append("ci");
		}
		else if (IsSoftConsonant(ending) || IsHardenedConsonant(ending))
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
				outputString.Append("e");
			}
			else
			{
				outputString.Append('e');
			}
		}
		else
		{
			outputString.Append('i');
		}
	}

	private void AddSuffixNounAccusative(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
			if (IsVowel(ending[1]))
			{
				if (ending[1] != 'a')
				{
					outputString.Append('a');
				}
			}
			else if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
				outputString.Append("a");
			}
			else
			{
				outputString.Append('a');
			}
		}
		else if (Feminine && IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append('ę');
		}
	}

	private void AddSuffixNounAccusativePlural(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Neuter)
		{
			if (IsVowel(ending[1]))
			{
				outputString.Remove(outputString.Length - 1, 1);
			}
			if (!GetEnding(outputString, 2).Equals("um"))
			{
				outputString.Append('a');
			}
		}
		else if (Feminine || MasculineAnimate || MasculineInanimate || GetLastCharacter(outputString) == 'a')
		{
			if (ending[1] == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				ending = GetEnding(outputString, 2);
			}
			if (GetEnding(outputString, 3).Equals("ość"))
			{
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append("ci");
			}
			else if (MasculineInanimate && ending.Equals("to"))
			{
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append('a');
			}
			else if (ending[1] == 'g' || ending[1] == 'k')
			{
				outputString.Append('i');
			}
			else if (outputString.Length > 4 && GetEnding(outputString, 5).Equals("rzecz"))
			{
				outputString.Append('y');
			}
			else if (IsVowel(ending[1]) || IsSoftConsonant(ending) || IsHardenedConsonant(ending))
			{
				outputString.Append('e');
			}
			else
			{
				outputString.Append('y');
			}
		}
		else if (IsSoftConsonant(ending))
		{
			PalatalizeConsonant(outputString, ending);
			outputString.Append("ów");
		}
		else
		{
			outputString.Append("ów");
		}
	}

	private void AddSuffixNounGenitive(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Feminine)
		{
			if (ending[1] == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				ending = GetEnding(outputString, 2);
			}
			if (!IsVowel(ending[1]))
			{
				if (IsSoftConsonant(ending))
				{
					PalatalizeConsonant(outputString, ending);
				}
				else if (ending[1] == 'g' || ending[1] == 'k' || GetEnding(outputString, 2).Equals("ch"))
				{
					outputString.Append('i');
				}
				else
				{
					outputString.Append('y');
				}
			}
			return;
		}
		if (IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
		}
		if (!GetEnding(outputString, 2).Equals("um"))
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append('a');
		}
	}

	private void AddSuffixNounGenitivePlural(StringBuilder outputString)
	{
		if (IsVowel(GetLastCharacter(outputString)))
		{
			outputString.Remove(outputString.Length - 1, 1);
		}
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Feminine && IsHardenedConsonant(ending))
		{
			outputString.Append('y');
		}
		else if (MasculinePersonal)
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append("ów");
		}
		else
		{
			if (!MasculineAnimate && !MasculineInanimate)
			{
				return;
			}
			if (MasculineInanimate && ending.Equals("to"))
			{
				outputString.Remove(outputString.Length - 1, 1);
				return;
			}
			if (IsSoftConsonant(ending))
			{
				outputString.Append('i');
				return;
			}
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append("ów");
		}
	}

	private void AddSuffixNounDative(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Feminine)
		{
			if (lastCharacter == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				lastCharacter = GetLastCharacter(outputString);
				ending = GetEnding(outputString, 2);
			}
			if (IsVowel(lastCharacter))
			{
				return;
			}
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
				return;
			}
			if (IsHardenedConsonant(ending))
			{
				outputString.Append('y');
				return;
			}
			if (ending.Equals("ch"))
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("szie");
				return;
			}
			switch (lastCharacter)
			{
			case 'g':
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append("dzie");
				break;
			case 'k':
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append("cie");
				break;
			default:
				outputString.Append("ie");
				break;
			}
		}
		else if (Neuter)
		{
			if (IsVowel(lastCharacter))
			{
				outputString.Remove(outputString.Length - 1, 1);
			}
			if (!GetEnding(outputString, 2).Equals("um"))
			{
				outputString.Append('u');
			}
		}
		else if (MasculineInanimate && ending.Equals("to"))
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append('u');
		}
		else
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append("owi");
		}
	}

	private void AddSuffixNounDativePlural(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
		}
		if (!ending.Equals("um"))
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append("om");
		}
	}

	private void AddSuffixNounLocative(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Feminine)
		{
			if (ending[1] == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				ending = GetEnding(outputString, 2);
			}
			if (!IsVowel(ending[1]))
			{
				if (IsSoftConsonant(ending))
				{
					outputString.Append('i');
				}
				else if (IsHardenedConsonant(ending))
				{
					outputString.Append('y');
				}
				else if (ending.Equals("ch"))
				{
					outputString.Remove(outputString.Length - 2, 2);
					outputString.Append("szie");
				}
				else if (ending[1] == 'g')
				{
					outputString.Remove(outputString.Length - 1, 1);
					outputString.Append("dzie");
				}
				else if (ending[1] == 'k')
				{
					outputString.Remove(outputString.Length - 1, 1);
					outputString.Append("cie");
				}
				else
				{
					outputString.Append("ie");
				}
			}
			else
			{
				if (IsVowel(ending[1]))
				{
					outputString.Remove(outputString.Length - 1, 1);
				}
				if (IsSoftConsonant(ending) || IsHardenedConsonant(ending) || ending.Equals("ch") || ending[1] == 'g' || ending[1] == 'k')
				{
					outputString.Append('u');
				}
				else
				{
					outputString.Append('e');
				}
			}
			return;
		}
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			AddSuffixNounVocative(outputString);
			return;
		}
		if (IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
			ending = GetEnding(outputString, 2);
		}
		if (!IsVowel(ending[1]) && !ending.Equals("um"))
		{
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
				outputString.Append("u");
			}
			else if (IsHardenedConsonant(ending) || ending.Equals("ch") || ending[1] == 'g' || ending[1] == 'k')
			{
				outputString.Append("u");
			}
			else
			{
				PalatalizeConsonant(outputString, ending);
				outputString.Append("e");
			}
		}
		else
		{
			outputString.Append("u");
		}
	}

	private void AddSuffixNounLocativePlural(StringBuilder outputString)
	{
		if (IsVowel(GetLastCharacter(outputString)))
		{
			outputString.Remove(outputString.Length - 1, 1);
		}
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (IsSoftConsonant(ending))
		{
			PalatalizeConsonant(outputString, ending);
		}
		outputString.Append("ach");
	}

	private void AddSuffixNounVocative(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
				outputString.Append("z");
				ending = GetEnding(outputString, 2);
			}
		}
		if (Feminine || lastCharacter == 'a')
		{
			if (lastCharacter == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				outputString.Append('o');
			}
			else if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			else
			{
				outputString.Append('i');
			}
		}
		else
		{
			if (Neuter)
			{
				return;
			}
			if (IsSoftConsonant(ending) || ending.Equals("ch") || lastCharacter == 'g' || lastCharacter == 'k')
			{
				if (IsSoftConsonant(ending))
				{
					PalatalizeConsonant(outputString, ending);
				}
				outputString.Append("u");
			}
			else if (IsHardConsonant(ending))
			{
				if (!IsHardenedConsonant(ending))
				{
					PalatalizeConsonant(outputString, ending);
				}
				outputString.Append("e");
			}
			else
			{
				outputString.Append("u");
			}
		}
	}

	private void AddSuffixNounVocativePlural(StringBuilder outputString)
	{
		AddSuffixNounNominativePlural(outputString);
	}

	private void AddSuffixNounInstrumental(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (Feminine)
		{
			if (ending[1] == 'a')
			{
				outputString.Remove(outputString.Length - 1, 1);
				ending = GetEnding(outputString, 2);
			}
			if (IsSoftConsonant(ending))
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append('ą');
			return;
		}
		if (IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
			ending = GetEnding(outputString, 2);
		}
		if (!ending.Equals("um"))
		{
			if (IsSoftConsonant(ending) || ending[1] == 'k')
			{
				PalatalizeConsonant(outputString, ending);
			}
			outputString.Append("em");
		}
	}

	private void AddSuffixNounInstrumentalPlural(StringBuilder outputString)
	{
		string ending = GetEnding(outputString, 2);
		if (MasculineAnimate || MasculineInanimate || MasculinePersonal)
		{
			if (!MasculinePersonal && ending[0] == 'ó')
			{
				outputString.Remove(outputString.Length - 2, 2);
				outputString.Append("o" + ending[1]);
			}
			if (GetEnding(outputString, 3).Equals("iec"))
			{
				if (GetEnding(outputString, 4).Equals("niec"))
				{
					outputString.Remove(outputString.Length - 4, 4);
					outputString.Append("ńc");
				}
				else
				{
					outputString.Remove(outputString.Length - 3, 2);
				}
			}
		}
		if (IsVowel(ending[1]))
		{
			outputString.Remove(outputString.Length - 1, 1);
			ending = GetEnding(outputString, 2);
		}
		if (IsSoftConsonant(ending))
		{
			PalatalizeConsonant(outputString, ending);
		}
		outputString.Append("ami");
	}

	private void AddSuffixAdjectiveNominative(StringBuilder outputString)
	{
		char c = RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append('a');
		}
		else if (Neuter)
		{
			outputString.Append('e');
		}
		else if (c == 'y' && (outputString.Length < 4 || !outputString.ToString().EndsWith("Nasz", ignoreCase: true, CultureInfo.InvariantCulture)))
		{
			outputString.Append('y');
		}
	}

	private void AddSuffixAdjectiveNominativePlural(StringBuilder outputString)
	{
		char c = RemoveSuffixFromAdjective(outputString);
		string ending = GetEnding(outputString, 2);
		string text = outputString.ToString();
		if (outputString.Length >= 4 && text.EndsWith("Nasz", ignoreCase: true, CultureInfo.InvariantCulture))
		{
			outputString.Remove(outputString.Length - 1, 1);
			outputString.Append('i');
		}
		else if (MasculinePersonal)
		{
			if (c == 'y')
			{
				PalatalizeConsonant(outputString, ending);
				text = outputString.ToString();
				if ((outputString.Length >= 10 && text.EndsWith("Wyszkoloni", ignoreCase: true, CultureInfo.InvariantCulture)) || (outputString.Length >= 11 && text.EndsWith("Zgromadzoni", ignoreCase: true, CultureInfo.InvariantCulture)))
				{
					outputString.Replace('o', 'e', outputString.Length - 3, 1);
				}
				else if (GetLastCharacter(outputString) == 'l')
				{
					outputString.Append('i');
				}
				else if (!IsVowel(GetLastCharacter(outputString)))
				{
					outputString.Append('y');
				}
			}
		}
		else
		{
			outputString.Append('e');
		}
	}

	private void AddSuffixAdjectiveAccusative(StringBuilder outputString)
	{
		RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append('ą');
		}
		else if (Neuter)
		{
			outputString.Append('e');
		}
		else if (MasculineAnimate || MasculinePersonal)
		{
			outputString.Append("ego");
		}
		else if (MasculineInanimate)
		{
			outputString.Append('y');
		}
	}

	private void AddSuffixAdjectiveAccusativePlural(StringBuilder outputString)
	{
		char c = RemoveSuffixFromAdjective(outputString);
		if (MasculinePersonal)
		{
			if (c == 'y')
			{
				outputString.Append("y");
			}
			outputString.Append("ch");
		}
		else
		{
			outputString.Append('e');
		}
	}

	private void AddSuffixAdjectiveVocative(StringBuilder outputString)
	{
		AddSuffixAdjectiveNominative(outputString);
	}

	private void AddSuffixAdjectiveVocativePlural(StringBuilder outputString)
	{
		AddSuffixAdjectiveNominativePlural(outputString);
	}

	private void AddSuffixAdjectiveGenitive(StringBuilder outputString)
	{
		RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append("ej");
		}
		else
		{
			outputString.Append("ego");
		}
	}

	private void AddSuffixAdjectiveGenitivePlural(StringBuilder outputString)
	{
		if ('y' == RemoveSuffixFromAdjective(outputString))
		{
			outputString.Append("y");
		}
		outputString.Append("ch");
	}

	private void AddSuffixAdjectiveDative(StringBuilder outputString)
	{
		RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append("ej");
		}
		else
		{
			outputString.Append("emu");
		}
	}

	private void AddSuffixAdjectiveDativePlural(StringBuilder outputString)
	{
		if ('y' == RemoveSuffixFromAdjective(outputString))
		{
			outputString.Append("y");
		}
		outputString.Append("m");
	}

	private void AddSuffixAdjectiveLocative(StringBuilder outputString)
	{
		char c = RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append("ej");
			return;
		}
		if (c == 'y')
		{
			outputString.Append("y");
		}
		outputString.Append("m");
	}

	private void AddSuffixAdjectiveLocativePlural(StringBuilder outputString)
	{
		AddSuffixAdjectiveGenitivePlural(outputString);
	}

	private void AddSuffixAdjectiveInstrumental(StringBuilder outputString)
	{
		char c = RemoveSuffixFromAdjective(outputString);
		if (Feminine)
		{
			outputString.Append('ą');
			return;
		}
		if (c == 'y')
		{
			outputString.Append("y");
		}
		outputString.Append("m");
	}

	private void AddSuffixAdjectiveInstrumentalPlural(StringBuilder outputString)
	{
		if ('y' == RemoveSuffixFromAdjective(outputString))
		{
			outputString.Append("y");
		}
		outputString.Append("mi");
	}

	private char RemoveSuffixFromAdjective(StringBuilder outputString)
	{
		if (GetLastCharacter(outputString) == 'i')
		{
			return 'i';
		}
		if (outputString.Length >= 4 && outputString.ToString().EndsWith("Nasz", ignoreCase: true, CultureInfo.InvariantCulture))
		{
			return 'y';
		}
		outputString.Remove(outputString.Length - 1, 1);
		if (GetLastCharacter(outputString) == 'i')
		{
			return 'i';
		}
		return 'y';
	}

	private void SetFeminine()
	{
		_curGender = WordGenderEnum.Feminine;
	}

	private void SetNeuter()
	{
		_curGender = WordGenderEnum.Neuter;
	}

	private void SetMasculineAnimate()
	{
		_curGender = WordGenderEnum.MasculineAnimate;
	}

	private void SetMasculineInanimate()
	{
		_curGender = WordGenderEnum.MasculineInanimate;
	}

	private void SetMasculinePersonal()
	{
		_curGender = WordGenderEnum.MasculinePersonal;
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
		if (lengthOfWordToReplace > 0)
		{
			text = sourceText.Substring(num, lengthOfWordToReplace);
			char key = char.ToUpperInvariant(text[0]);
			if (MasculinePersonal && IrregularMasculinePersonalDictionary.TryGetValue(key, out var value))
			{
				for (int i = 0; i < value.Count; i++)
				{
					if (value[i].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = IrregularWordWithCase(token, value[i]);
						break;
					}
				}
			}
			else if (MasculineAnimate && IrregularMasculineAnimateDictionary.TryGetValue(key, out value))
			{
				for (int j = 0; j < value.Count; j++)
				{
					if (value[j].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = IrregularWordWithCase(token, value[j]);
						break;
					}
				}
			}
			else if (MasculineInanimate && IrregularMasculineInanimateDictionary.TryGetValue(key, out value))
			{
				for (int k = 0; k < value.Count; k++)
				{
					if (value[k].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = IrregularWordWithCase(token, value[k]);
						break;
					}
				}
			}
			else if (Feminine && IrregularFeminineDictionary.TryGetValue(key, out value))
			{
				for (int l = 0; l < value.Count; l++)
				{
					if (value[l].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = IrregularWordWithCase(token, value[l]);
						break;
					}
				}
			}
			else if (Neuter && IrregularNeuterDictionary.TryGetValue(key, out value))
			{
				for (int m = 0; m < value.Count; m++)
				{
					if (value[m].Nominative.Equals(text, StringComparison.InvariantCultureIgnoreCase))
					{
						irregularWord = IrregularWordWithCase(token, value[m]);
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

	private static bool IsVowel(char c)
	{
		return Array.IndexOf(Vowels, c) >= 0;
	}

	private static bool IsSoftConsonant(string s)
	{
		return Array.IndexOf(SoftConsonants, s[1]) >= 0;
	}

	private static bool IsHardenedConsonant(string s)
	{
		if (Array.IndexOf(HardenedConsonants, s) < 0)
		{
			return Array.IndexOf(HardenedConsonants, s[1].ToString()) >= 0;
		}
		return true;
	}

	private static bool IsHardConsonant(string s)
	{
		if (Array.IndexOf(HardConsonants, s) < 0)
		{
			return Array.IndexOf(HardConsonants, s[1].ToString()) >= 0;
		}
		return true;
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

	private static void PalatalizeConsonant(StringBuilder outputString, string lastTwoCharacters)
	{
		int num = 1;
		if (!Palatalization.TryGetValue(lastTwoCharacters[1].ToString(), out var value) && Palatalization.TryGetValue(lastTwoCharacters, out value))
		{
			num = 2;
			value = "";
		}
		outputString.Remove(outputString.Length - num, num);
		outputString.Append(value);
	}

	private static string IrregularWordWithCase(string token, IrregularWord irregularWord)
	{
		switch (token)
		{
		case ".j":
		case ".aj":
		case ".nn":
			return irregularWord.Nominative;
		case ".jp":
		case ".p":
		case ".nnp":
		case ".ajp":
			return irregularWord.NominativePlural;
		case ".jvp":
		case ".vp":
			return irregularWord.VocativePlural;
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
		case ".jv":
		case ".v":
			return irregularWord.Vocative;
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

	public static string[] GetProcessedNouns(string str, string gender, string[] tokens = null)
	{
		if (tokens == null)
		{
			tokens = new string[14]
			{
				".n", ".g", ".d", ".a", ".i", ".l", ".v", ".p", ".gp", ".dp",
				".ap", ".ip", ".lp", ".vp"
			};
		}
		List<string> list = new List<string>();
		PolishTextProcessor polishTextProcessor = new PolishTextProcessor();
		string[] array = tokens;
		foreach (string text in array)
		{
			string text2 = "{=!}" + gender + str + "{" + text + "}";
			list.Add(polishTextProcessor.Process(text2));
		}
		return list.ToArray();
	}

	public static string[] GetProcessedAdjectives(string str, string gender, string[] tokens = null)
	{
		if (tokens == null)
		{
			tokens = AdjectiveTokens.TokenList;
		}
		List<string> list = new List<string>();
		PolishTextProcessor polishTextProcessor = new PolishTextProcessor();
		string[] array = tokens;
		foreach (string text in array)
		{
			string text2 = "{=!}" + gender + str + "{" + text + "}";
			list.Add(polishTextProcessor.Process(text2));
		}
		return list.ToArray();
	}
}
