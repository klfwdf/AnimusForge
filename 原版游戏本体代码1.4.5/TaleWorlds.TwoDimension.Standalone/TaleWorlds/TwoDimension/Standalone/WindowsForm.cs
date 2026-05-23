using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class WindowsForm
{
	private static int classNameCount;

	private WindowClass wc;

	private string windowClassName;

	private WndProc _windowProcedure;

	private List<WindowsFormMessageHandler> _messageHandlers = new List<WindowsFormMessageHandler>();

	public int Width { get; set; }

	public int Height { get; set; }

	public string Text { get; set; }

	public IntPtr Handle { get; set; }

	public WindowsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, string name = null)
		: this(x, y, width, height, resourceDepot, IntPtr.Zero, borderlessWindow, enableWindowBlur, name)
	{
	}

	public WindowsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, IntPtr parent, bool borderlessWindow = false, bool enableWindowBlur = false, string name = null)
	{
		Handle = IntPtr.Zero;
		classNameCount++;
		Width = width;
		Height = height;
		Text = "Form";
		windowClassName = "Form" + classNameCount;
		wc = default(WindowClass);
		_windowProcedure = WndProc;
		wc.style = 0u;
		wc.lpfnWndProc = _windowProcedure;
		wc.cbClsExtra = 0;
		wc.cbWndExtra = 0;
		wc.hCursor = User32.LoadCursorFromFile(resourceDepot.GetFilePath("mb_cursor.cur"));
		wc.hInstance = Kernel32.GetModuleHandle(null);
		wc.lpszMenuName = null;
		wc.lpszClassName = windowClassName;
		wc.hbrBackground = Gdi32.CreateSolidBrush(IntPtr.Zero);
		User32.RegisterClass(ref wc);
		if (string.IsNullOrEmpty(name))
		{
			name = "Gauntlet UI: " + Process.GetCurrentProcess().Id;
		}
		Handle = User32.CreateWindowEx(dwStyle: (parent != IntPtr.Zero) ? (WindowStyle.WS_CHILD | WindowStyle.WS_VISIBLE) : (borderlessWindow ? (WindowStyle.WS_POPUP | WindowStyle.WS_VISIBLE | WindowStyle.WS_SYSMENU) : WindowStyle.OverlappedWindow), dwExStyle: 0, lpClassName: windowClassName, lpWindowName: name, x: x, y: y, nWidth: width, nHeight: height, hWndParent: parent, hMenu: IntPtr.Zero, hInstance: Kernel32.GetModuleHandle(null), lpParam: IntPtr.Zero);
		if (enableWindowBlur)
		{
			DwmBlurBehind ppfd = new DwmBlurBehind
			{
				dwFlags = (BlurBehindConstraints.Enable | BlurBehindConstraints.BlurRegion),
				hRgnBlur = Gdi32.CreateRectRgn(0, 0, -1, -1),
				fEnable = true
			};
			Dwmapi.DwmEnableBlurBehindWindow(Handle, ref ppfd);
		}
	}

	public WindowsForm(int width, int height, ResourceDepot resourceDepot)
		: this(100, 100, width, height, resourceDepot)
	{
	}

	public void SetParent(IntPtr parentHandle)
	{
		User32.SetParent(Handle, parentHandle);
	}

	public void Show()
	{
		User32.ShowWindow(Handle, WindowShowStyle.Show);
	}

	public void Hide()
	{
		User32.ShowWindow(Handle, WindowShowStyle.Hide);
	}

	public void Destroy()
	{
		Hide();
		User32.DestroyWindow(Handle);
		User32.UnregisterClass(windowClassName, IntPtr.Zero);
	}

	public void AddMessageHandler(WindowsFormMessageHandler messageHandler)
	{
		_messageHandlers.Add(messageHandler);
	}

	private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
	{
		long wParam2 = wParam.ToInt64();
		long num = lParam.ToInt64();
		if (message == 5)
		{
			int width = (int)num % 65536;
			int height = (int)(num / 65536);
			Width = width;
			Height = height;
		}
		foreach (WindowsFormMessageHandler messageHandler in _messageHandlers)
		{
			messageHandler((WindowMessage)message, wParam2, num);
		}
		return User32.DefWindowProc(hWnd, message, wParam, lParam);
	}
}
