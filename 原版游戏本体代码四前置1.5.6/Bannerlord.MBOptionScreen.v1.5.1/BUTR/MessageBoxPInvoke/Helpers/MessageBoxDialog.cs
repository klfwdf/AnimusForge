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
	internal static extern BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, IntPtr lpText, IntPtr lpCaption, BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE uType);

	internal static BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, string lpText, string lpCaption, BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE uType)
	{
		IntPtr lpText2 = Marshal.StringToHGlobalUni(lpText);
		IntPtr lpCaption2 = Marshal.StringToHGlobalUni(lpCaption);
		return MessageBox(hWnd, lpText2, lpCaption2, uType);
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, "\0", BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE.MB_OK);
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text, string caption)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE.MB_OK);
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text, string caption, BUTR.MessageBoxPInvoke.Helpers.MessageBoxButtons buttons)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE)buttons);
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text, string caption, BUTR.MessageBoxPInvoke.Helpers.MessageBoxButtons buttons, BUTR.MessageBoxPInvoke.Helpers.MessageBoxIcon icon)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE)((uint)buttons | (uint)icon));
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text, string caption, BUTR.MessageBoxPInvoke.Helpers.MessageBoxButtons buttons, BUTR.MessageBoxPInvoke.Helpers.MessageBoxIcon icon, BUTR.MessageBoxPInvoke.Helpers.MessageBoxDefaultButton button)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE)((uint)buttons | (uint)icon | (uint)button));
	}

	public static BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult Show(string text, string caption, BUTR.MessageBoxPInvoke.Helpers.MessageBoxButtons buttons, BUTR.MessageBoxPInvoke.Helpers.MessageBoxIcon icon, BUTR.MessageBoxPInvoke.Helpers.MessageBoxDefaultButton button, BUTR.MessageBoxPInvoke.Helpers.MessageBoxModal modal)
	{
		return (BUTR.MessageBoxPInvoke.Helpers.MessageBoxResult)MessageBox(IntPtr.Zero, text, caption, (BUTR.MessageBoxPInvoke.Helpers.MESSAGEBOX_STYLE)((uint)buttons | (uint)icon | (uint)button | (uint)modal));
	}
}
