using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor.LanguageProcessors;

public class TurkishTextProcessor : LanguageSpecificTextProcessor
{
	private static CultureInfo _curCultureInfo = CultureInfo.InvariantCulture;

	private static char[] Vowels = new char[8] { 'a', 'ı', 'o', 'u', 'e', 'i', 'ö', 'ü' };

	private static char[] BackVowels = new char[4] { 'a', 'ı', 'o', 'u' };

	private static int[] BackNumbers = new int[7] { 6, 9, 10, 30, 40, 60, 90 };

	private static char[] FrontVowels = new char[4] { 'e', 'i', 'ö', 'ü' };

	private static char[] OpenVowels = new char[4] { 'a', 'e', 'o', 'ö' };

	private static char[] ClosedVowels = new char[4] { 'ı', 'i', 'u', 'ü' };

	private static char[] Consonants = new char[21]
	{
		'b', 'c', 'ç', 'd', 'f', 'g', 'ğ', 'h', 'j', 'k',
		'l', 'm', 'n', 'p', 'r', 's', 'ş', 't', 'v', 'y',
		'z'
	};

	private static char[] UnvoicedConsonants = new char[8] { 'ç', 'f', 'h', 'k', 'p', 's', 'ş', 't' };

	private static char[] HardUnvoicedConsonants = new char[4] { 'p', 'ç', 't', 'k' };

	private static string[] NonMutatingWord = new string[20]
	{
		"ak", "at", "ek", "et", "göç", "ip", "çöp", "ok", "ot", "saç",
		"sap", "süt", "üç", "suç", "top", "ticaret", "kürk", "dük", "kont", "hizmet"
	};

	private static Dictionary<string, char> _exceptions = new Dictionary<string, char> { { "kontrol", 'e' } };

	[ThreadStatic]
	private static List<string> _linkList = new List<string>();

	private static CultureInfo _cultureInfo = new CultureInfo("tr-TR");

	public static List<string> LinkList
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

	public override CultureInfo CultureInfoForLanguage => _cultureInfo;

	private bool IsVowel(char c)
	{
		return Vowels.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private char GetNextVowel(StringBuilder stringBuilder)
	{
		string lastWord = GetLastWord(stringBuilder);
		if (lastWord != null && _exceptions.TryGetValue(lastWord.ToLower(CultureInfoForLanguage), out var value))
		{
			return value;
		}
		if (int.TryParse(lastWord, out var result))
		{
			return GetNextVowel(result);
		}
		value = GetLastVowel(stringBuilder);
		if (!BackVowels.Contains(char.ToLower(value, CultureInfoForLanguage)))
		{
			return 'e';
		}
		return 'a';
	}

	private char GetNextVowel(int number)
	{
		int num = Math.Abs(number) % 10;
		int num2 = Math.Abs(number) % 100;
		if (number == 0)
		{
			return 'a';
		}
		if (num != 0)
		{
			if (!BackNumbers.Contains(num))
			{
				return 'e';
			}
			return 'a';
		}
		if (num2 != 0)
		{
			if (!BackNumbers.Contains(num2))
			{
				return 'e';
			}
			return 'a';
		}
		return 'e';
	}

	private bool IsFrontVowel(char c)
	{
		return FrontVowels.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private bool IsClosedVowel(char c)
	{
		return ClosedVowels.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private bool IsConsonant(char c)
	{
		return Consonants.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private bool IsUnvoicedConsonant(char c)
	{
		return UnvoicedConsonants.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private bool IsHardUnvoicedConsonant(char c)
	{
		return HardUnvoicedConsonants.Contains(char.ToLower(c, CultureInfoForLanguage));
	}

	private char FrontVowelToBackVowel(char c)
	{
		c = char.ToLower(c, CultureInfoForLanguage);
		return c switch
		{
			'ü' => 'u', 
			'ö' => 'o', 
			'i' => 'ı', 
			'e' => 'a', 
			_ => '*', 
		};
	}

	private char OpenVowelToClosedVowel(char c)
	{
		c = char.ToLower(c, CultureInfoForLanguage);
		return c switch
		{
			'ö' => 'ü', 
			'o' => 'u', 
			'e' => 'i', 
			'a' => 'ı', 
			_ => '*', 
		};
	}

	private char HardConsonantToSoftConsonant(char c)
	{
		c = char.ToLower(c, CultureInfoForLanguage);
		return c switch
		{
			'k' => 'ğ', 
			't' => 'd', 
			'ç' => 'c', 
			'p' => 'b', 
			_ => '*', 
		};
	}

	private char GetLastVowel(StringBuilder outputText)
	{
		for (int num = outputText.Length - 1; num >= 0; num--)
		{
			if (IsVowel(outputText[num]))
			{
				return outputText[num];
			}
		}
		return 'i';
	}

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
		bool flag = false;
		if (token == ".link")
		{
			LinkList.Add(sourceText.Substring(7));
		}
		else if (sourceText.Contains("<a style=\"Link."))
		{
			flag = ((sourceText[cursorPos - (token.Length + 3)] != '\'') ? IsLink(sourceText, token.Length + 2, cursorPos) : IsLink(sourceText, token.Length + 2, cursorPos - 1));
		}
		if (flag)
		{
			if (sourceText[cursorPos - (token.Length + 3)] == '\'')
			{
				cursorPos -= 8;
				outputString.Remove(outputString.Length - 9, 9);
				outputString.Append('\'');
			}
			else
			{
				cursorPos -= 8;
				outputString.Remove(outputString.Length - 8, 8);
			}
		}
		switch (token)
		{
		case ".im":
			AddSuffix_im(outputString);
			break;
		case ".sin":
			AddSuffix_sin(outputString);
			break;
		case ".dir":
			AddSuffix_dir(outputString);
			break;
		case ".iz":
			AddSuffix_iz(outputString);
			break;
		case ".siniz":
			AddSuffix_siniz(outputString);
			break;
		case ".dirler":
			AddSuffix_dirler(outputString);
			break;
		case ".i":
			AddSuffix_i(outputString);
			break;
		case ".e":
			AddSuffix_e(outputString);
			break;
		case ".de":
			AddSuffix_de(outputString);
			break;
		case ".den":
			AddSuffix_den(outputString);
			break;
		case ".nin":
			AddSuffix_nin(outputString);
			break;
		case ".ler":
			AddSuffix_ler(outputString);
			break;
		case ".m":
			AddSuffix_m(outputString);
			break;
		case ".n":
			AddSuffix_n(outputString);
			break;
		case ".in":
			AddSuffix_in(outputString);
			break;
		case ".si":
			AddSuffix_si(outputString);
			break;
		case ".miz":
			AddSuffix_miz(outputString);
			break;
		case ".niz":
			AddSuffix_niz(outputString);
			break;
		case ".leri":
			AddSuffix_leri(outputString);
			break;
		}
		if (flag)
		{
			cursorPos += 8;
			outputString.Append("</b></a>");
		}
	}

	private void AddSuffix_im(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		SoftenLastCharacter(outputString);
		AddYIfNeeded(outputString);
		outputString.Append(value);
		outputString.Append('m');
	}

	private void AddSuffix_sin(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		outputString.Append('s');
		outputString.Append(value);
		outputString.Append('n');
	}

	private void AddSuffix_dir(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char harmonizedD = GetHarmonizedD(outputString);
		outputString.Append(harmonizedD);
		outputString.Append(value);
		outputString.Append('r');
	}

	private void AddSuffix_iz(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		SoftenLastCharacter(outputString);
		AddYIfNeeded(outputString);
		outputString.Append(value);
		outputString.Append('z');
	}

	private void AddSuffix_siniz(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		outputString.Append('s');
		outputString.Append(value);
		outputString.Append('n');
		outputString.Append(value);
		outputString.Append('z');
	}

	private void AddSuffix_dirler(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char nextVowel = GetNextVowel(outputString);
		char harmonizedD = GetHarmonizedD(outputString);
		outputString.Append(harmonizedD);
		outputString.Append(value);
		outputString.Append('r');
		outputString.Append('l');
		outputString.Append(nextVowel);
		outputString.Append('r');
	}

	private void AddSuffix_i(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		SoftenLastCharacter(outputString);
		if (GetLastCharacter(outputString) == '\'' && outputString.Length > 6 && outputString.ToString().EndsWith("Kalesi'", ignoreCase: true, _cultureInfo))
		{
			outputString.Append('n');
		}
		else
		{
			AddYIfNeeded(outputString);
		}
		outputString.Append(value);
	}

	private void AddSuffix_e(StringBuilder outputString)
	{
		char nextVowel = GetNextVowel(outputString);
		SoftenLastCharacter(outputString);
		AddYIfNeeded(outputString);
		outputString.Append(nextVowel);
	}

	private void AddSuffix_de(StringBuilder outputString)
	{
		char nextVowel = GetNextVowel(outputString);
		char harmonizedD = GetHarmonizedD(outputString);
		outputString.Append(harmonizedD);
		outputString.Append(nextVowel);
	}

	private void AddSuffix_den(StringBuilder outputString)
	{
		char nextVowel = GetNextVowel(outputString);
		char harmonizedD = GetHarmonizedD(outputString);
		outputString.Append(harmonizedD);
		outputString.Append(nextVowel);
		outputString.Append('n');
	}

	private void AddSuffix_nin(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char c = GetLastCharacter(outputString);
		if (c == '\'')
		{
			c = GetSecondLastCharacter(outputString);
		}
		else
		{
			SoftenLastCharacter(outputString);
		}
		if (IsVowel(c))
		{
			outputString.Append('n');
		}
		outputString.Append(value);
		outputString.Append('n');
	}

	private void AddSuffix_ler(StringBuilder outputString)
	{
		char nextVowel = GetNextVowel(outputString);
		outputString.Append('l');
		outputString.Append(nextVowel);
		outputString.Append('r');
	}

	private void AddSuffix_m(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char lastCharacter = GetLastCharacter(outputString);
		SoftenLastCharacter(outputString);
		if (IsConsonant(lastCharacter))
		{
			outputString.Append(value);
		}
		outputString.Append('m');
	}

	private void AddSuffix_n(StringBuilder outputString)
	{
		char lastLetter = GetLastLetter(outputString);
		char secondLastLetter = GetSecondLastLetter(outputString);
		if (IsVowel(lastLetter) && !IsVowel(secondLastLetter))
		{
			outputString.Append('n');
		}
	}

	private void AddSuffix_in(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char lastLetter = GetLastLetter(outputString);
		SoftenLastCharacter(outputString);
		if (IsConsonant(lastLetter))
		{
			outputString.Append(value);
		}
		outputString.Append('n');
	}

	private void AddSuffix_si(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char lastCharacter = GetLastCharacter(outputString);
		SoftenLastCharacter(outputString);
		if (IsVowel(lastCharacter))
		{
			outputString.Append('s');
		}
		outputString.Append(value);
	}

	private void AddSuffix_miz(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char lastCharacter = GetLastCharacter(outputString);
		SoftenLastCharacter(outputString);
		if (IsConsonant(lastCharacter))
		{
			outputString.Append(value);
		}
		outputString.Append('m');
		outputString.Append(value);
		outputString.Append('z');
	}

	private void AddSuffix_niz(StringBuilder outputString)
	{
		char lastVowel = GetLastVowel(outputString);
		char value = (IsClosedVowel(lastVowel) ? lastVowel : OpenVowelToClosedVowel(lastVowel));
		char lastCharacter = GetLastCharacter(outputString);
		SoftenLastCharacter(outputString);
		if (IsConsonant(lastCharacter))
		{
			outputString.Append(value);
		}
		outputString.Append('n');
		outputString.Append(value);
		outputString.Append('z');
	}

	private void AddSuffix_leri(StringBuilder outputString)
	{
		GetLastVowel(outputString);
		char nextVowel = GetNextVowel(outputString);
		char value = ((nextVowel == 'a') ? 'ı' : 'i');
		outputString.Append('l');
		outputString.Append(nextVowel);
		outputString.Append('r');
		outputString.Append(value);
	}

	private char GetHarmonizedD(StringBuilder outputString)
	{
		char c = GetLastCharacter(outputString);
		if (c == '\'')
		{
			c = GetSecondLastCharacter(outputString);
		}
		if (!IsUnvoicedConsonant(c))
		{
			return 'd';
		}
		return 't';
	}

	private void AddYIfNeeded(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (IsVowel(lastCharacter) || (lastCharacter == '\'' && IsVowel(GetSecondLastCharacter(outputString))))
		{
			outputString.Append('y');
		}
	}

	private void SoftenLastCharacter(StringBuilder outputString)
	{
		char lastCharacter = GetLastCharacter(outputString);
		if (IsHardUnvoicedConsonant(lastCharacter) && !LastWordNonMutating(outputString))
		{
			outputString[outputString.Length - 1] = HardConsonantToSoftConsonant(lastCharacter);
		}
	}

	private string GetLastWord(StringBuilder outputString)
	{
		int num = -1;
		int num2 = outputString.Length - 1;
		while (num2 >= 0 && num < 0)
		{
			if (outputString[num2] == ' ')
			{
				num = num2;
			}
			num2--;
		}
		if (num < outputString.Length - 1)
		{
			return outputString.ToString(num + 1, outputString.Length - num - 1).Trim('\n', '\'');
		}
		return null;
	}

	private bool LastWordNonMutating(StringBuilder outputString)
	{
		string lastWord = GetLastWord(outputString);
		if (lastWord != null)
		{
			return NonMutatingWord.Contains(lastWord.ToLower(CultureInfoForLanguage));
		}
		return false;
	}

	private char GetLastCharacter(StringBuilder outputString)
	{
		if (outputString.Length <= 0)
		{
			return '*';
		}
		return outputString[outputString.Length - 1];
	}

	private char GetLastLetter(StringBuilder outputString)
	{
		for (int num = outputString.Length - 1; num >= 0; num--)
		{
			if (char.IsLetter(outputString[num]))
			{
				return outputString[num];
			}
		}
		return 'x';
	}

	private char GetSecondLastLetter(StringBuilder outputString)
	{
		bool flag = false;
		for (int num = outputString.Length - 1; num >= 0; num--)
		{
			if (char.IsLetter(outputString[num]))
			{
				if (flag)
				{
					return outputString[num];
				}
				flag = true;
			}
		}
		return 'x';
	}

	private char GetSecondLastCharacter(StringBuilder outputString)
	{
		if (outputString.Length <= 1)
		{
			return '*';
		}
		return outputString[outputString.Length - 2];
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

	public override void ClearTemporaryData()
	{
		LinkList.Clear();
	}
}
