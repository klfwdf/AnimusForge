using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000F RID: 15
	public class WindowsForm
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000D4 RID: 212 RVA: 0x00004DDC File Offset: 0x00002FDC
		// (set) Token: 0x060000D5 RID: 213 RVA: 0x00004DE4 File Offset: 0x00002FE4
		public int Width { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000D6 RID: 214 RVA: 0x00004DED File Offset: 0x00002FED
		// (set) Token: 0x060000D7 RID: 215 RVA: 0x00004DF5 File Offset: 0x00002FF5
		public int Height { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x00004DFE File Offset: 0x00002FFE
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x00004E06 File Offset: 0x00003006
		public string Text { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000DA RID: 218 RVA: 0x00004E0F File Offset: 0x0000300F
		// (set) Token: 0x060000DB RID: 219 RVA: 0x00004E17 File Offset: 0x00003017
		public IntPtr Handle { get; set; }

		// Token: 0x060000DC RID: 220 RVA: 0x00004E20 File Offset: 0x00003020
		public WindowsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, string name = null)
			: this(x, y, width, height, resourceDepot, IntPtr.Zero, borderlessWindow, enableWindowBlur, name)
		{
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00004E48 File Offset: 0x00003048
		public WindowsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, IntPtr parent, bool borderlessWindow = false, bool enableWindowBlur = false, string name = null)
		{
			this.Handle = IntPtr.Zero;
			WindowsForm.classNameCount++;
			this.Width = width;
			this.Height = height;
			this.Text = "Form";
			this.windowClassName = "Form" + WindowsForm.classNameCount;
			this.wc = default(WindowClass);
			this._windowProcedure = new WndProc(this.WndProc);
			this.wc.style = 0U;
			this.wc.lpfnWndProc = this._windowProcedure;
			this.wc.cbClsExtra = 0;
			this.wc.cbWndExtra = 0;
			this.wc.hCursor = User32.LoadCursorFromFile(resourceDepot.GetFilePath("mb_cursor.cur"));
			this.wc.hInstance = Kernel32.GetModuleHandle(null);
			this.wc.lpszMenuName = null;
			this.wc.lpszClassName = this.windowClassName;
			this.wc.hbrBackground = Gdi32.CreateSolidBrush(IntPtr.Zero);
			User32.RegisterClass(ref this.wc);
			if (string.IsNullOrEmpty(name))
			{
				name = "Gauntlet UI: " + Process.GetCurrentProcess().Id;
			}
			WindowStyle dwStyle;
			if (parent != IntPtr.Zero)
			{
				dwStyle = WindowStyle.WS_CHILD | WindowStyle.WS_VISIBLE;
			}
			else if (!borderlessWindow)
			{
				dwStyle = WindowStyle.OverlappedWindow;
			}
			else
			{
				dwStyle = (WindowStyle)2416443392U;
			}
			this.Handle = User32.CreateWindowEx(0, this.windowClassName, name, dwStyle, x, y, width, height, parent, IntPtr.Zero, Kernel32.GetModuleHandle(null), IntPtr.Zero);
			if (enableWindowBlur)
			{
				DwmBlurBehind dwmBlurBehind = default(DwmBlurBehind);
				dwmBlurBehind.dwFlags = BlurBehindConstraints.Enable | BlurBehindConstraints.BlurRegion;
				dwmBlurBehind.hRgnBlur = Gdi32.CreateRectRgn(0, 0, -1, -1);
				dwmBlurBehind.fEnable = true;
				Dwmapi.DwmEnableBlurBehindWindow(this.Handle, ref dwmBlurBehind);
			}
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00005024 File Offset: 0x00003224
		public WindowsForm(int width, int height, ResourceDepot resourceDepot)
			: this(100, 100, width, height, resourceDepot, false, false, null)
		{
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00005041 File Offset: 0x00003241
		public void SetParent(IntPtr parentHandle)
		{
			User32.SetParent(this.Handle, parentHandle);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00005050 File Offset: 0x00003250
		public void Show()
		{
			User32.ShowWindow(this.Handle, WindowShowStyle.Show);
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000505F File Offset: 0x0000325F
		public void Hide()
		{
			User32.ShowWindow(this.Handle, WindowShowStyle.Hide);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x0000506E File Offset: 0x0000326E
		public void Destroy()
		{
			this.Hide();
			User32.DestroyWindow(this.Handle);
			User32.UnregisterClass(this.windowClassName, IntPtr.Zero);
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00005093 File Offset: 0x00003293
		public void AddMessageHandler(WindowsFormMessageHandler messageHandler)
		{
			this._messageHandlers.Add(messageHandler);
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x000050A4 File Offset: 0x000032A4
		private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
		{
			long wParam2 = wParam.ToInt64();
			long num = lParam.ToInt64();
			if (message == 5U)
			{
				int width = (int)num % 65536;
				int height = (int)(num / 65536L);
				this.Width = width;
				this.Height = height;
			}
			foreach (WindowsFormMessageHandler windowsFormMessageHandler in this._messageHandlers)
			{
				windowsFormMessageHandler((WindowMessage)message, wParam2, num);
			}
			return User32.DefWindowProc(hWnd, message, wParam, lParam);
		}

		// Token: 0x04000047 RID: 71
		private static int classNameCount;

		// Token: 0x04000048 RID: 72
		private WindowClass wc;

		// Token: 0x04000049 RID: 73
		private string windowClassName;

		// Token: 0x0400004A RID: 74
		private WndProc _windowProcedure;

		// Token: 0x0400004E RID: 78
		private List<WindowsFormMessageHandler> _messageHandlers = new List<WindowsFormMessageHandler>();
	}
}
