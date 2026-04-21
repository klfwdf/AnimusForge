using System;
using System.Threading;
using System.Windows.Forms;
using TaleWorlds.InputSystem;

namespace AnimusForge;

internal static class WindowsClipboardHelper
{
	public static bool TryGetUnicodeText(out string text)
	{
		text = string.Empty;
		try
		{
			string capturedText = null;
			Exception exception = null;
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					capturedText = (Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText) : Clipboard.GetText());
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
			if (exception == null)
			{
				text = capturedText ?? string.Empty;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static string GetText()
	{
		if (TryGetUnicodeText(out var text))
		{
			return text;
		}
		return Input.GetClipboardText() ?? string.Empty;
	}

	public static bool TrySetUnicodeText(string text)
	{
		if (text == null)
		{
			text = string.Empty;
		}
		try
		{
			Exception exception = null;
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					Clipboard.SetText(text, TextDataFormat.UnicodeText);
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			});
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
			return exception == null;
		}
		catch
		{
		}
		return false;
	}

	public static void SetText(string text)
	{
		if (!TrySetUnicodeText(text))
		{
			Input.SetClipboardText(text);
		}
	}
}
