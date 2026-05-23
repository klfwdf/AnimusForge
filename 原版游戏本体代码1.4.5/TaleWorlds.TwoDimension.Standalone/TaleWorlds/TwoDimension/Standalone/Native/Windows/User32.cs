using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class User32
{
	public struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct MONITORINFOEX
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szDevice;
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern short GetAsyncKeyState(int vkey);

	[DllImport("user32.dll")]
	public static extern bool DestroyWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

	[DllImport("user32.dll")]
	public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetCursorPos(out Point lpPoint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	public static extern IntPtr SetCapture(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern IntPtr SetActiveWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPTStr)] string lpClassName, string lpWindowName, WindowStyle dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

	[DllImport("user32.dll")]
	public static extern bool CloseWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool PeekMessage(out NativeMessage lpMsg, [In] IntPtr hWnd, [In] uint wMsgFilterMin, [In] uint wMsgFilterMax, [In] uint wRemoveMsg);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool TranslateMessage([In] ref NativeMessage lpMsg);

	[DllImport("user32.dll")]
	public static extern IntPtr DispatchMessage([In] ref NativeMessage lpMsg);

	[DllImport("user32.dll")]
	public static extern ushort RegisterClass([In] ref WindowClass lpWndClass);

	[DllImport("user32.dll")]
	public static extern bool UnregisterClass([MarshalAs(UnmanagedType.LPTStr)] string lpClassName, IntPtr hInstance);

	[DllImport("user32.dll")]
	public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern IntPtr LoadCursorFromFile(string lpFileName);

	[DllImport("user32.dll")]
	public static extern IntPtr GetDesktopWindow();

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UpdateWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref System.Drawing.Point pptDst, ref Size psize, IntPtr hdcSrc, ref System.Drawing.Point pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll")]
	public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

	[DllImport("user32.dll")]
	public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
}
