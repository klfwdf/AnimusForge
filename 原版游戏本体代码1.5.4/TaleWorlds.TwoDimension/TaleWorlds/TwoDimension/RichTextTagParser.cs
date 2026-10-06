using System;
using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class RichTextTagParser
{
	public static RichTextTag Parse(string text2, int tagBeginIndex, int tagEndIndex)
	{
		RichTextTagType richTextTagType = RichTextTagType.Open;
		int i = tagBeginIndex;
		RichTextTag richTextTag = null;
		int num = -1;
		int num2 = 0;
		int num3 = 0;
		for (; i < tagEndIndex; i++)
		{
			char c = text2[i];
			char c2 = ' ';
			bool flag = i + 1 < tagEndIndex;
			if (flag)
			{
				c2 = text2[i + 1];
			}
			switch (num3)
			{
			case 0:
				if (c == '<')
				{
					num3 = 1;
				}
				break;
			case 1:
				switch (c)
				{
				case '/':
					if (richTextTagType != RichTextTagType.Close)
					{
						richTextTagType = RichTextTagType.Close;
						break;
					}
					throw new RichTextException("Unexpected beginning in rich text tag");
				default:
					if (num == -1)
					{
						num = i;
					}
					num2++;
					num3 = 2;
					break;
				case ' ':
					break;
				}
				break;
			case 2:
				switch (c)
				{
				case ' ':
					richTextTag = new RichTextTag(text2.Substring(num, num2));
					num3 = 3;
					break;
				case '>':
					richTextTag = new RichTextTag(text2.Substring(num, num2));
					i--;
					num3 = 3;
					break;
				default:
					num2++;
					break;
				}
				break;
			case 3:
				switch (c)
				{
				case '>':
					num3 = 4;
					break;
				case '/':
					if (!flag)
					{
						throw new RichTextException("Unexpected ending in rich text tag");
					}
					if (c2 != '>')
					{
						throw new RichTextException("Unexpected ending in rich text tag");
					}
					if (richTextTagType == RichTextTagType.Close)
					{
						throw new RichTextException("Unexpected ending in rich text tag");
					}
					richTextTagType = RichTextTagType.SelfClose;
					num3 = 4;
					break;
				default:
				{
					int num4 = 0;
					bool flag2 = false;
					int startIndex = i;
					int num5 = 0;
					for (; i < tagEndIndex; i++)
					{
						char c3 = text2[i];
						num5++;
						if (!flag2)
						{
							if (c3 == '=')
							{
								flag2 = true;
							}
						}
						else if (c3 == '"' || c3 == '\'')
						{
							num4++;
							if (num4 == 2)
							{
								break;
							}
						}
					}
					if (num4 != 2)
					{
						throw new RichTextException("Could not parse attribute of tag in rich text");
					}
					KeyValuePair<string, string> keyValuePair = ParseAttribute(text2.Substring(startIndex, num5));
					if (richTextTag == null)
					{
						throw new RichTextException("Rich text tag name could not be parsed");
					}
					richTextTag.AddAtrribute(keyValuePair.Key, keyValuePair.Value);
					break;
				}
				case ' ':
					break;
				}
				break;
			}
		}
		if (num3 != 4)
		{
			richTextTag = new RichTextTag(text2);
			richTextTagType = RichTextTagType.TextAfterError;
		}
		if (richTextTag == null)
		{
			throw new RichTextException("Unexpected behavior in rich text tag parser");
		}
		richTextTag.Type = richTextTagType;
		return richTextTag;
	}

	private static KeyValuePair<string, string> ParseAttribute(string attributeText)
	{
		string[] array = attributeText.Split(new char[1] { '=' }, StringSplitOptions.RemoveEmptyEntries);
		string keyText = array[0];
		string valueText = array[1];
		string key = ParseAttributeKey(keyText);
		string value = ParseAttributeValue(valueText);
		return new KeyValuePair<string, string>(key, value);
	}

	private static string ParseAttributeKey(string keyText)
	{
		string text = "";
		bool flag = false;
		for (int i = 0; i < keyText.Length; i++)
		{
			char c = keyText[i];
			if (c == ' ')
			{
				flag = true;
				continue;
			}
			if (!flag)
			{
				text += c;
				continue;
			}
			throw new RichTextException("Unexpected character on attribute key in tag");
		}
		if (text.Length == 0)
		{
			throw new RichTextException("Unexpected attribute key length");
		}
		return text;
	}

	private static string ParseAttributeValue(string valueText)
	{
		if (valueText[0] != '"' && valueText[valueText.Length - 1] != '"' && valueText[0] != '\'' && valueText[valueText.Length - 1] != '\'')
		{
			throw new RichTextException("Unexpected quotes on attribute value in tag");
		}
		return valueText.Substring(1, valueText.Length - 2);
	}
}
