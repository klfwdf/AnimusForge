using System;
using System.Threading;
using System.Windows.Forms;
using TaleWorlds.InputSystem;

namespace AnimusForge;

internal static class WindowsClipboardHelper
{
	public static string GetText()
	{
		try
		{
			string result = null;
			Exception exception = null;
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					result = Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText) : Clipboard.GetText();
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
				return result ?? string.Empty;
			}
		}
		catch
		{
		}
		return Input.GetClipboardText() ?? string.Empty;
	}

	public static void SetText(string text)
	{
		text ??= string.Empty;
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
			if (exception == null)
			{
				return;
			}
		}
		catch
		{
		}
		Input.SetClipboardText(text);
	}
}
