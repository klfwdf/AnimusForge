using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core;

public class InquiryElement
{
	public readonly string Title;

	public readonly ImageIdentifier ImageIdentifier;

	public readonly object Identifier;

	public readonly bool IsEnabled;

	public readonly string Hint;

	public InquiryElement(object identifier, string title, ImageIdentifier imageIdentifier)
	{
		Identifier = identifier;
		Title = title;
		ImageIdentifier = imageIdentifier;
		IsEnabled = true;
		Hint = null;
	}

	public InquiryElement(object identifier, string title, ImageIdentifier imageIdentifier, bool isEnabled, string hint)
	{
		Identifier = identifier;
		Title = title;
		ImageIdentifier = imageIdentifier;
		IsEnabled = isEnabled;
		Hint = hint;
	}

	public bool HasSameContentWith(object other)
	{
		if (other is InquiryElement inquiryElement)
		{
			if (Title == inquiryElement.Title)
			{
				if (ImageIdentifier != null || inquiryElement.ImageIdentifier != null)
				{
					ImageIdentifier imageIdentifier = ImageIdentifier;
					if (imageIdentifier == null || !imageIdentifier.Equals(inquiryElement.ImageIdentifier))
					{
						goto IL_0075;
					}
				}
				if (Identifier == inquiryElement.Identifier && IsEnabled == inquiryElement.IsEnabled)
				{
					return Hint == inquiryElement.Hint;
				}
			}
			goto IL_0075;
		}
		return false;
		IL_0075:
		return false;
	}
}
