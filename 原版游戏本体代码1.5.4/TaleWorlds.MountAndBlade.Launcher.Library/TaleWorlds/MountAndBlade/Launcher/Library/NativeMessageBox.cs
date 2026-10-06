using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public static class NativeMessageBox
{
	public enum Buttons : uint
	{
		OK = 0u,
		OKCancel = 1u,
		YesNo = 4u,
		YesNoCancel = 3u
	}

	public enum Icon : uint
	{
		None = 0u,
		Information = 64u,
		Warning = 48u,
		Error = 16u,
		Question = 32u
	}

	public enum Result
	{
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

	public static Result Show(string text, string caption = "Message", Buttons buttons = Buttons.OK, Icon icon = Icon.None)
	{
		return MessageBox(IntPtr.Zero, text, caption, (uint)buttons | (uint)icon) switch
		{
			1 => Result.OK, 
			2 => Result.Cancel, 
			6 => Result.Yes, 
			7 => Result.No, 
			_ => Result.OK, 
		};
	}
}
