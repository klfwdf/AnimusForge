using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace BUTR.MessageBoxPInvoke.Helpers;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal static class MessageBoxDialog
{
	[DllImport("USER32.dll", EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static extern MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, IntPtr lpText, IntPtr lpCaption, MESSAGEBOX_STYLE uType);

	internal static MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType)
	{
		IntPtr lpText2 = Marshal.StringToHGlobalUni(lpText);
		IntPtr lpCaption2 = Marshal.StringToHGlobalUni(lpCaption);
		return MessageBox(hWnd, lpText2, lpCaption2, uType);
	}

	public static MessageBoxResult Show(string text)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, "\0", MESSAGEBOX_STYLE.MB_OK);
	}

	public static MessageBoxResult Show(string text, string caption)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, MESSAGEBOX_STYLE.MB_OK);
	}

	public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)buttons);
	}

	public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)((uint)buttons | (uint)icon));
	}

	public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)((uint)buttons | (uint)icon | (uint)button));
	}

	public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button, MessageBoxModal modal)
	{
		return (MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)((uint)buttons | (uint)icon | (uint)button | (uint)modal));
	}
}
