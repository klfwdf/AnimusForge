using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class EnglishTextProcessor : LanguageSpecificTextProcessor
{
	private Dictionary<string, string> IrregularNouns = new Dictionary<string, string>
	{
		{ "man", "men" },
		{ "footman", "footmen" },
		{ "crossbowman", "crossbowmen" },
		{ "pikeman", "pikemen" },
		{ "shieldman", "shieldmen" },
		{ "shieldsman", "shieldsmen" },
		{ "woman", "women" },
		{ "child", "children" },
		{ "mouse", "mice" },
		{ "louse", "lice" },
		{ "tooth", "teeth" },
		{ "goose", "geese" },
		{ "foot", "feet" },
		{ "ox", "oxen" },
		{ "sheep", "sheep" },
		{ "fish", "fish" },
		{ "species", "species" },
		{ "aircraft", "aircraft" },
		{ "news", "news" },
		{ "advice", "advice" },
		{ "information", "information" },
		{ "luggage", "luggage" },
		{ "athletics", "athletics" },
		{ "linguistics", "linguistics" },
		{ "curriculum", "curricula" },
		{ "analysis", "analyses" },
		{ "ellipsis", "ellipses" },
		{ "bison", "bison" },
		{ "corpus", "corpora" },
		{ "crisis", "crises" },
		{ "criterion", "criteria" },
		{ "die", "dice" },
		{ "graffito", "graffiti" },
		{ "cactus", "cacti" },
		{ "focus", "foci" },
		{ "fungus", "fungi" },
		{ "headquarters", "headquarters" },
		{ "trousers", "trousers" },
		{ "cattle", "cattle" },
		{ "scissors", "scissors" },
		{ "index", "indices" },
		{ "vertex", "vertices" },
		{ "matrix", "matrices" },
		{ "radius", "radii" },
		{ "photo", "photos" },
		{ "piano", "pianos" },
		{ "dwarf", "dwarves" },
		{ "wharf", "wharves" },
		{ "formula", "formulae" },
		{ "moose", "moose" },
		{ "phenomenon", "phenomena" }
	};

	private string[] Sibilants = new string[6] { "s", "x", "ch", "sh", "es", "ss" };

	private const string Vowels = "aeiouAEIOU";

	private const string Consonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";

	public override CultureInfo CultureInfoForLanguage => CultureInfo.InvariantCulture;

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		char c = token[1];
		switch (c)
		{
		case 'a':
			if (CheckNextCharIsVowel(sourceText, cursorPos))
			{
				outputString.Append("an");
			}
			else
			{
				outputString.Append("a");
			}
			break;
		case 'A':
			if (CheckNextCharIsVowel(sourceText, cursorPos))
			{
				outputString.Append("An");
			}
			else
			{
				outputString.Append("A");
			}
			break;
		case 's':
		{
			string text = "";
			int startIndex = 0;
			for (int num = outputString.Length - 1; num >= 0; num--)
			{
				if (outputString[num] == ' ')
				{
					startIndex = num + 1;
					break;
				}
				text += outputString[num];
			}
			text = new string(text.Reverse().ToArray());
			int length = text.Length;
			if (text.Length > 1)
			{
				if (HandleIrregularNouns(text, out var resultPlural))
				{
					outputString.Replace(text, resultPlural, startIndex, length);
				}
				else if (Handle_ves_Suffix(text, out resultPlural))
				{
					outputString.Replace(text, resultPlural, startIndex, length);
				}
				else if (Handle_ies_Suffix(text, out resultPlural))
				{
					outputString.Replace(text, resultPlural, startIndex, length);
				}
				else if (Handle_es_Suffix(text, out resultPlural))
				{
					outputString.Replace(text, resultPlural, startIndex, length);
				}
				else if (Handle_s_Suffix(text, out resultPlural))
				{
					outputString.Replace(text, resultPlural, startIndex, length);
				}
				else
				{
					outputString.Append(c);
				}
			}
			break;
		}
		case 'o':
			HandleApostrophe(outputString, cursorPos);
			break;
		}
	}

	private char GetLastCharacter(StringBuilder outputText, int cursorPos)
	{
		for (int num = cursorPos - 1; num >= 0; num--)
		{
			if (char.IsLetter(outputText[num]))
			{
				return outputText[num];
			}
		}
		return 'x';
	}

	private void HandleApostrophe(StringBuilder outputString, int cursorPos)
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
		outputString.Append('\'');
		if (lastCharacter != 's')
		{
			outputString.Append('s');
		}
		if (flag)
		{
			outputString.Append("</b></a>");
		}
	}

	private bool CheckNextCharIsVowel(string sourceText, int cursorPos)
	{
		while (cursorPos < sourceText.Length)
		{
			char value = sourceText[cursorPos];
			if (Enumerable.Contains("aeiouAEIOU", value))
			{
				return true;
			}
			if (Enumerable.Contains("bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ", value))
			{
				return false;
			}
			cursorPos++;
		}
		return false;
	}

	private bool HandleIrregularNouns(string text, out string resultPlural)
	{
		resultPlural = null;
		char.IsLower(text[text.Length - 1]);
		string key = text.ToLower();
		if (IrregularNouns.TryGetValue(key, out var value))
		{
			if (text.All((char c) => char.IsUpper(c)))
			{
				resultPlural = value.ToUpper();
			}
			else if (char.IsUpper(text[0]))
			{
				char[] array = value.ToCharArray();
				array[0] = char.ToUpper(array[0]);
				resultPlural = new string(array);
			}
			else
			{
				resultPlural = value.ToLower();
			}
			return true;
		}
		return false;
	}

	private bool Handle_ves_Suffix(string text, out string resultPlural)
	{
		resultPlural = null;
		bool flag = char.IsLower(text[text.Length - 1]);
		char c = char.ToLower(text[text.Length - 1]);
		char c2 = char.ToLower(text[text.Length - 2]);
		if (c2 != 'o' && Enumerable.Contains("aeiouAEIOU", c2) && c == 'f')
		{
			resultPlural = text.Remove(text.Length - 1);
			resultPlural += (flag ? "ves" : "VES");
			return true;
		}
		if (c2 == 'f' && Enumerable.Contains("aeiouAEIOU", c))
		{
			resultPlural = text.Remove(text.Length - 2, 2);
			resultPlural += (flag ? "v" : "V");
			resultPlural += (flag ? c : char.ToUpper(c));
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (c2 == 'l' && c == 'f')
		{
			resultPlural = text.Remove(text.Length - 1);
			resultPlural += (flag ? "ves" : "VES");
			return true;
		}
		return false;
	}

	private bool Handle_ies_Suffix(string text, out string resultPlural)
	{
		resultPlural = null;
		bool flag = char.IsLower(text[text.Length - 1]);
		char c = char.ToLower(text[text.Length - 1]);
		char value = char.ToLower(text[text.Length - 2]);
		if (Enumerable.Contains("bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ", value) && c == 'y')
		{
			resultPlural = text.Remove(text.Length - 1);
			resultPlural += (flag ? "ies" : "IES");
			return true;
		}
		return false;
	}

	private bool Handle_es_Suffix(string text, out string resultPlural)
	{
		resultPlural = null;
		bool flag = char.IsLower(text[text.Length - 1]);
		string text2 = text[text.Length - 1].ToString();
		string text3 = text[text.Length - 2].ToString();
		if (text2 == "z")
		{
			resultPlural = text;
			resultPlural += (flag ? "zes" : "ZES");
			return true;
		}
		if (Sibilants.Contains(text2))
		{
			resultPlural = text;
			resultPlural += (flag ? "es" : "ES");
			return true;
		}
		if (Sibilants.Contains(text3 + text2))
		{
			resultPlural = text;
			resultPlural += (flag ? "es" : "ES");
			return true;
		}
		if ("bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ".Contains(text3) && text2 == "o")
		{
			resultPlural = text;
			resultPlural += (flag ? "es" : "ES");
			return true;
		}
		if (text3 == "o" && text2 == "e")
		{
			resultPlural = text;
			resultPlural = resultPlural.Remove(resultPlural.Length - 1);
			resultPlural += (flag ? "es" : "ES");
			return true;
		}
		if (text3 == "i" && text2 == "s")
		{
			resultPlural = text;
			resultPlural = resultPlural.Remove(resultPlural.Length - 2);
			resultPlural += (flag ? "es" : "ES");
			return true;
		}
		return false;
	}

	private bool Handle_s_Suffix(string text, out string resultPlural)
	{
		resultPlural = null;
		bool flag = char.IsLower(text[text.Length - 1]);
		char c = char.ToLower(text[text.Length - 1]);
		char c2 = char.ToLower(text[text.Length - 2]);
		if (Enumerable.Contains("bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ", c))
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (c == 'e')
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (Enumerable.Contains("aeiouAEIOU", c2) && c == 'y')
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (c2 == 'f' && c == 'f')
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (c2 == 'o' && c == 'f')
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		if (Enumerable.Contains("aeiouAEIOU", c2) && c == 'o')
		{
			resultPlural = text;
			resultPlural += (flag ? "s" : "S");
			return true;
		}
		return false;
	}

	public override void ClearTemporaryData()
	{
	}
}
