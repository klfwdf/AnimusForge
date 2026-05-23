using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows
{
	// Token: 0x02000025 RID: 37
	public static class User32
	{
		// Token: 0x06000112 RID: 274
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern short GetAsyncKeyState(int vkey);

		// Token: 0x06000113 RID: 275
		[DllImport("user32.dll")]
		public static extern bool DestroyWindow(IntPtr hWnd);

		// Token: 0x06000114 RID: 276
		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);

		// Token: 0x06000115 RID: 277
		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

		// Token: 0x06000116 RID: 278
		[DllImport("user32.dll")]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		// Token: 0x06000117 RID: 279
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

		// Token: 0x06000118 RID: 280
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out Point lpPoint);

		// Token: 0x06000119 RID: 281
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReleaseCapture();

		// Token: 0x0600011A RID: 282
		[DllImport("user32.dll")]
		public static extern IntPtr SetCapture(IntPtr hWnd);

		// Token: 0x0600011B RID: 283
		[DllImport("user32.dll")]
		public static extern IntPtr SetActiveWindow(IntPtr hWnd);

		// Token: 0x0600011C RID: 284
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		// Token: 0x0600011D RID: 285
		[DllImport("user32.dll")]
		public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPTStr)] string lpClassName, string lpWindowName, WindowStyle dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

		// Token: 0x0600011E RID: 286
		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

		// Token: 0x0600011F RID: 287
		[DllImport("user32.dll")]
		public static extern bool CloseWindow(IntPtr hWnd);

		// Token: 0x06000120 RID: 288
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PeekMessage(out NativeMessage lpMsg, [In] IntPtr hWnd, [In] uint wMsgFilterMin, [In] uint wMsgFilterMax, [In] uint wRemoveMsg);

		// Token: 0x06000121 RID: 289
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TranslateMessage([In] ref NativeMessage lpMsg);

		// Token: 0x06000122 RID: 290
		[DllImport("user32.dll")]
		public static extern IntPtr DispatchMessage([In] ref NativeMessage lpMsg);

		// Token: 0x06000123 RID: 291
		[DllImport("user32.dll")]
		public static extern ushort RegisterClass([In] ref WindowClass lpWndClass);

		// Token: 0x06000124 RID: 292
		[DllImport("user32.dll")]
		public static extern bool UnregisterClass([MarshalAs(UnmanagedType.LPTStr)] string lpClassName, IntPtr hInstance);

		// Token: 0x06000125 RID: 293
		[DllImport("user32.dll")]
		public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

		// Token: 0x06000126 RID: 294
		[DllImport("user32.dll")]
		public static extern IntPtr LoadCursorFromFile(string lpFileName);

		// Token: 0x06000127 RID: 295
		[DllImport("user32.dll")]
		public static extern IntPtr GetDesktopWindow();

		// Token: 0x06000128 RID: 296
		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

		// Token: 0x06000129 RID: 297
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

		// Token: 0x0600012A RID: 298
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		// Token: 0x0600012B RID: 299
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		// Token: 0x0600012C RID: 300
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UpdateWindow(IntPtr hWnd);

		// Token: 0x0600012D RID: 301
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

		// Token: 0x0600012E RID: 302
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);

		// Token: 0x0600012F RID: 303
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

		// Token: 0x06000130 RID: 304
		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		// Token: 0x06000131 RID: 305
		[DllImport("user32.dll")]
		public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

		// Token: 0x06000132 RID: 306
		[DllImport("user32.dll")]
		public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, User32.MonitorEnumDelegate lpfnEnum, IntPtr dwData);

		// Token: 0x06000133 RID: 307
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool GetMonitorInfo(IntPtr hMonitor, ref User32.MONITORINFOEX lpmi);

		// Token: 0x0200004B RID: 75
		public struct RECT
		{
			// Token: 0x040002F9 RID: 761
			public int left;

			// Token: 0x040002FA RID: 762
			public int top;

			// Token: 0x040002FB RID: 763
			public int right;

			// Token: 0x040002FC RID: 764
			public int bottom;
		}

		// Token: 0x0200004C RID: 76
		// (Invoke) Token: 0x060001A0 RID: 416
		public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref User32.RECT lprcMonitor, IntPtr lParam);

		// Token: 0x0200004D RID: 77
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct MONITORINFOEX
		{
			// Token: 0x040002FD RID: 765
			public int cbSize;

			// Token: 0x040002FE RID: 766
			public User32.RECT rcMonitor;

			// Token: 0x040002FF RID: 767
			public User32.RECT rcWork;

			// Token: 0x04000300 RID: 768
			public uint dwFlags;

			// Token: 0x04000301 RID: 769
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string szDevice;
		}
	}
}
