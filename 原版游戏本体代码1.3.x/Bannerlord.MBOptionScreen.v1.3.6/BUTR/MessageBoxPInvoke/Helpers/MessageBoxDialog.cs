using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BUTR.MessageBoxPInvoke.Helpers
{
	// Token: 0x0200004A RID: 74
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class MessageBoxDialog
	{
		// Token: 0x0600021F RID: 543
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("USER32.dll", EntryPoint = "MessageBoxW", ExactSpelling = true, SetLastError = true)]
		internal static extern MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, IntPtr lpText, IntPtr lpCaption, MESSAGEBOX_STYLE uType);

		// Token: 0x06000220 RID: 544 RVA: 0x0000887C File Offset: 0x00006A7C
		internal static MESSAGEBOX_RESULT MessageBox(IntPtr hWnd, string lpText, string lpCaption, MESSAGEBOX_STYLE uType)
		{
			IntPtr lpTextLocal = Marshal.StringToHGlobalUni(lpText);
			IntPtr lpCaptionLocal = Marshal.StringToHGlobalUni(lpCaption);
			return MessageBoxDialog.MessageBox(hWnd, lpTextLocal, lpCaptionLocal, uType);
		}

		// Token: 0x06000221 RID: 545 RVA: 0x000088A0 File Offset: 0x00006AA0
		public static MessageBoxResult Show(string text)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, "\0", MESSAGEBOX_STYLE.MB_OK);
		}

		// Token: 0x06000222 RID: 546 RVA: 0x000088B3 File Offset: 0x00006AB3
		public static MessageBoxResult Show(string text, string caption)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, caption, MESSAGEBOX_STYLE.MB_OK);
		}

		// Token: 0x06000223 RID: 547 RVA: 0x000088C2 File Offset: 0x00006AC2
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)buttons);
		}

		// Token: 0x06000224 RID: 548 RVA: 0x000088D1 File Offset: 0x00006AD1
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)(buttons | (MessageBoxButtons)icon));
		}

		// Token: 0x06000225 RID: 549 RVA: 0x000088E2 File Offset: 0x00006AE2
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)(buttons | (MessageBoxButtons)icon | (MessageBoxButtons)button));
		}

		// Token: 0x06000226 RID: 550 RVA: 0x000088F6 File Offset: 0x00006AF6
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button, MessageBoxModal modal)
		{
			return (MessageBoxResult)MessageBoxDialog.MessageBox(IntPtr.Zero, text, caption, (MESSAGEBOX_STYLE)(buttons | (MessageBoxButtons)icon | (MessageBoxButtons)button | (MessageBoxButtons)modal));
		}
	}
}
