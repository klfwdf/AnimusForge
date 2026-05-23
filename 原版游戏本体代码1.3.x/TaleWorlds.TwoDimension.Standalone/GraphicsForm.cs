using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x02000006 RID: 6
	public class GraphicsForm : IMessageCommunicator
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600002C RID: 44 RVA: 0x000030C1 File Offset: 0x000012C1
		// (set) Token: 0x0600002D RID: 45 RVA: 0x000030C9 File Offset: 0x000012C9
		public GraphicsContext GraphicsContext { get; private set; }

		// Token: 0x0600002E RID: 46 RVA: 0x000030D4 File Offset: 0x000012D4
		public GraphicsForm(int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, bool layeredWindow = false, string name = null)
		{
			DXGI.RECT rect = this.DecideWindowPosition();
			int num = rect.right - rect.left;
			int num2 = rect.bottom - rect.top;
			int x = rect.left + (num - width) / 2;
			int y = rect.top + (num2 - height) / 2;
			this._windowsForm = new WindowsForm(x, y, width, height, resourceDepot, borderlessWindow, enableWindowBlur, name);
			this.Initalize(layeredWindow);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003158 File Offset: 0x00001358
		public GraphicsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, bool layeredWindow = false, string name = null)
		{
			this._windowsForm = new WindowsForm(x, y, width, height, resourceDepot, borderlessWindow, enableWindowBlur, name);
			this.Initalize(layeredWindow);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000319D File Offset: 0x0000139D
		public GraphicsForm(WindowsForm windowsForm)
		{
			this._windowsForm = windowsForm;
			this.Initalize(false);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000031C8 File Offset: 0x000013C8
		private void Initalize(bool layeredWindow)
		{
			this._currentInputData = new InputData();
			this._oldInputData = new InputData();
			this._messageLoopInputData = new InputData();
			this._windowsForm.AddMessageHandler(new WindowsFormMessageHandler(this.MessageHandler));
			this._windowsForm.Show();
			this.GraphicsContext = new GraphicsContext();
			if (layeredWindow)
			{
				this._layeredWindowController = new LayeredWindowController(this._windowsForm.Handle, this._windowsForm.Width, this._windowsForm.Height);
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00003254 File Offset: 0x00001454
		public bool CompareRecrangles(DXGI.RECT Rect1, DXGI.RECT Rect2)
		{
			int num = Rect1.right - Rect1.left;
			int num2 = Rect1.bottom - Rect1.top;
			int num3 = Rect2.right - Rect2.left;
			int num4 = Rect2.bottom - Rect2.top;
			return num > num3 && num2 > num4;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000032A4 File Offset: 0x000014A4
		public DXGI.RECT DecideWindowPosition()
		{
			Rectangle rectangle;
			User32.GetClientRect(User32.GetDesktopWindow(), out rectangle);
			DXGI.RECT rect = new DXGI.RECT
			{
				left = rectangle.Left,
				right = rectangle.Right,
				top = rectangle.Top,
				bottom = rectangle.Bottom
			};
			DXGI.RECT rect2 = rect;
			IntPtr pUnk;
			DXGI.CreateDXGIFactory(ref DXGI.IID_IDXGIFactory, out pUnk);
			DXGI.IDXGIFactory idxgifactory = (DXGI.IDXGIFactory)Marshal.GetObjectForIUnknown(pUnk);
			MBList<Tuple<uint, DXGI.DXGI_ADAPTER_DESC>> mblist = new MBList<Tuple<uint, DXGI.DXGI_ADAPTER_DESC>>();
			uint num = 0U;
			DXGI.IDXGIAdapter idxgiadapter;
			while (idxgifactory.EnumAdapters(num, out idxgiadapter) == 0)
			{
				DXGI.DXGI_ADAPTER_DESC dxgi_ADAPTER_DESC;
				idxgiadapter.GetDesc(out dxgi_ADAPTER_DESC);
				if ((ulong)dxgi_ADAPTER_DESC.DedicatedVideoMemory > 0UL)
				{
					mblist.Add(new Tuple<uint, DXGI.DXGI_ADAPTER_DESC>(num, dxgi_ADAPTER_DESC));
				}
				num += 1U;
			}
			if (mblist.Count == 0)
			{
				Marshal.FinalReleaseComObject(idxgifactory);
				return rect2;
			}
			mblist.Sort(delegate(Tuple<uint, DXGI.DXGI_ADAPTER_DESC> x, Tuple<uint, DXGI.DXGI_ADAPTER_DESC> y)
			{
				if ((ulong)x.Item2.DedicatedVideoMemory <= (ulong)y.Item2.DedicatedVideoMemory)
				{
					return 1;
				}
				return -1;
			});
			num = 0U;
			foreach (Tuple<uint, DXGI.DXGI_ADAPTER_DESC> tuple in mblist)
			{
				DXGI.IDXGIAdapter idxgiadapter2;
				idxgifactory.EnumAdapters(num, out idxgiadapter2);
				uint num2 = 0U;
				DXGI.IDXGIOutput idxgioutput;
				while (idxgiadapter2.EnumOutputs(num2, out idxgioutput) == 0)
				{
					DXGI.DXGI_OUTPUT_DESC dxgi_OUTPUT_DESC;
					idxgioutput.GetDesc(out dxgi_OUTPUT_DESC);
					if (dxgi_OUTPUT_DESC.AttachedToDesktop && rect2 == dxgi_OUTPUT_DESC.DesktopCoordinates)
					{
						Marshal.FinalReleaseComObject(idxgifactory);
						return rect2;
					}
					num2 += 1U;
				}
			}
			num = 0U;
			foreach (Tuple<uint, DXGI.DXGI_ADAPTER_DESC> tuple2 in mblist)
			{
				DXGI.IDXGIAdapter idxgiadapter3;
				idxgifactory.EnumAdapters(num, out idxgiadapter3);
				uint num3 = 0U;
				DXGI.IDXGIOutput idxgioutput2;
				while (idxgiadapter3.EnumOutputs(num3, out idxgioutput2) == 0)
				{
					DXGI.DXGI_OUTPUT_DESC dxgi_OUTPUT_DESC2;
					idxgioutput2.GetDesc(out dxgi_OUTPUT_DESC2);
					if (dxgi_OUTPUT_DESC2.AttachedToDesktop)
					{
						Marshal.FinalReleaseComObject(idxgifactory);
						return dxgi_OUTPUT_DESC2.DesktopCoordinates;
					}
					num3 += 1U;
				}
			}
			Marshal.FinalReleaseComObject(idxgifactory);
			return rect2;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000034BC File Offset: 0x000016BC
		public void Destroy()
		{
			this.GraphicsContext.DestroyContext();
			this._windowsForm.Destroy();
			LayeredWindowController layeredWindowController = this._layeredWindowController;
			if (layeredWindowController == null)
			{
				return;
			}
			layeredWindowController.OnFinalize();
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000034E4 File Offset: 0x000016E4
		public void MinimizeWindow()
		{
			User32.ShowWindow(this._windowsForm.Handle, WindowShowStyle.Minimize);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000034F8 File Offset: 0x000016F8
		public void InitializeGraphicsContext(ResourceDepot resourceDepot)
		{
			this.GraphicsContext.Control = this._windowsForm;
			this.GraphicsContext.CreateContext(resourceDepot);
			this.GraphicsContext.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, (float)this._windowsForm.Width, (float)this._windowsForm.Height, 0f, 0f, 1f);
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003560 File Offset: 0x00001760
		public void BeginFrame()
		{
			if (this.GraphicsContext != null)
			{
				this.GraphicsContext.BeginFrame(this._windowsForm.Width, this._windowsForm.Height);
				this.GraphicsContext.Resize(this._windowsForm.Width, this._windowsForm.Height);
			}
			this.GraphicsContext.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, (float)this._windowsForm.Width, (float)this._windowsForm.Height, 0f, 0f, 1f);
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000035F4 File Offset: 0x000017F4
		public void Update()
		{
			if (!this._isDragging && this._mouseOverDragArea && this._currentInputData.LeftMouse && !this._oldInputData.LeftMouse)
			{
				this._isDragging = true;
				this.MessageHandler(WindowMessage.LeftButtonUp, 0L, 0L);
			}
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00003644 File Offset: 0x00001844
		public void MessageLoop()
		{
			if (this._isDragging)
			{
				User32.ReleaseCapture();
				User32.SendMessage(this._windowsForm.Handle, 161U, new IntPtr(2), IntPtr.Zero);
				this._isDragging = false;
				User32.SetCapture(this._windowsForm.Handle);
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003698 File Offset: 0x00001898
		public void UpdateInput(bool mouseOverDragArea = false)
		{
			this._mouseOverDragArea = mouseOverDragArea;
			InputData oldInputData = this._oldInputData;
			this._oldInputData = this._currentInputData;
			this._currentInputData = oldInputData;
			object inputDataLocker = this._inputDataLocker;
			lock (inputDataLocker)
			{
				this._currentInputData.FillFrom(this._messageLoopInputData);
				this._messageLoopInputData.Reset();
			}
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003710 File Offset: 0x00001910
		public void PostRender()
		{
			if (this._layeredWindowController != null)
			{
				this._layeredWindowController.PostRender();
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003728 File Offset: 0x00001928
		public bool GetKeyDown(InputKey keyCode)
		{
			if (keyCode == InputKey.LeftMouseButton)
			{
				return this.LeftMouseDown();
			}
			if (keyCode == InputKey.RightMouseButton)
			{
				return this.RightMouseDown();
			}
			return this._currentInputData.KeyData[(int)keyCode] && !this._oldInputData.KeyData[(int)keyCode];
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003774 File Offset: 0x00001974
		public bool GetKey(InputKey keyCode)
		{
			if (keyCode == InputKey.LeftMouseButton)
			{
				return this.LeftMouse();
			}
			if (keyCode == InputKey.RightMouseButton)
			{
				return this.RightMouse();
			}
			return this._currentInputData.KeyData[(int)keyCode];
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000037A1 File Offset: 0x000019A1
		public bool GetKeyUp(InputKey keyCode)
		{
			if (keyCode == InputKey.LeftMouseButton)
			{
				return this.LeftMouseUp();
			}
			if (keyCode == InputKey.RightMouseButton)
			{
				return this.RightMouseUp();
			}
			return !this._currentInputData.KeyData[(int)keyCode] && this._oldInputData.KeyData[(int)keyCode];
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000037DF File Offset: 0x000019DF
		public float GetMouseDeltaZ()
		{
			return this._currentInputData.MouseScrollDelta;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000037EC File Offset: 0x000019EC
		public bool LeftMouse()
		{
			return this._currentInputData.LeftMouse;
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000037F9 File Offset: 0x000019F9
		public bool LeftMouseDown()
		{
			return this._currentInputData.LeftMouse && !this._oldInputData.LeftMouse;
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003818 File Offset: 0x00001A18
		public bool LeftMouseUp()
		{
			return !this._currentInputData.LeftMouse && this._oldInputData.LeftMouse;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003834 File Offset: 0x00001A34
		public bool RightMouse()
		{
			return this._currentInputData.RightMouse;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003841 File Offset: 0x00001A41
		public bool RightMouseDown()
		{
			return this._currentInputData.RightMouse && !this._oldInputData.RightMouse;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003860 File Offset: 0x00001A60
		public bool RightMouseUp()
		{
			return !this._currentInputData.RightMouse && this._oldInputData.RightMouse;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000387C File Offset: 0x00001A7C
		public Vector2 MousePosition()
		{
			return new Vector2((float)this._currentInputData.CursorX, (float)this._currentInputData.CursorY);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x0000389B File Offset: 0x00001A9B
		public bool MouseMove()
		{
			return this._currentInputData.MouseMove;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x000038A8 File Offset: 0x00001AA8
		public void FillInputDataFromCurrent(InputData inputData)
		{
			inputData.FillFrom(this._currentInputData);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000038B8 File Offset: 0x00001AB8
		private void MessageHandler(WindowMessage message, long wParam, long lParam)
		{
			object inputDataLocker;
			if (message <= WindowMessage.Close)
			{
				switch (message)
				{
				case WindowMessage.Size:
				case (WindowMessage)6U:
					return;
				case WindowMessage.SetFocus:
					goto IL_324;
				case WindowMessage.KillFocus:
					break;
				default:
					if (message != WindowMessage.Close)
					{
						return;
					}
					this.Destroy();
					Environment.Exit(0);
					return;
				}
			}
			else
			{
				checked
				{
					if (message != WindowMessage.KeyDown)
					{
						if (message != WindowMessage.KeyUp)
						{
							switch (message)
							{
							case WindowMessage.MouseMove:
								goto IL_23F;
							case WindowMessage.LeftButtonDown:
								goto IL_1E4;
							case WindowMessage.LeftButtonUp:
								goto IL_189;
							case (WindowMessage)515U:
							case (WindowMessage)518U:
							case (WindowMessage)519U:
							case (WindowMessage)520U:
							case (WindowMessage)521U:
								return;
							case WindowMessage.RightButtonDown:
								goto IL_12E;
							case WindowMessage.RightButtonUp:
								goto IL_D7;
							case WindowMessage.MouseWheel:
								goto IL_29A;
							default:
								return;
							}
						}
					}
					else
					{
						inputDataLocker = this._inputDataLocker;
						lock (inputDataLocker)
						{
							this._messageLoopInputData.KeyData[(int)((IntPtr)wParam)] = true;
							return;
						}
					}
					inputDataLocker = this._inputDataLocker;
					lock (inputDataLocker)
					{
						this._messageLoopInputData.KeyData[(int)((IntPtr)wParam)] = false;
						return;
					}
					IL_D7:
					inputDataLocker = this._inputDataLocker;
				}
				lock (inputDataLocker)
				{
					this._messageLoopInputData.RightMouse = false;
					int cursorX = (int)lParam % 65536;
					int cursorY = (int)(lParam / 65536L);
					this._messageLoopInputData.CursorX = cursorX;
					this._messageLoopInputData.CursorY = cursorY;
					return;
				}
				IL_12E:
				inputDataLocker = this._inputDataLocker;
				lock (inputDataLocker)
				{
					this._messageLoopInputData.RightMouse = true;
					int cursorX2 = (int)lParam % 65536;
					int cursorY2 = (int)(lParam / 65536L);
					this._messageLoopInputData.CursorX = cursorX2;
					this._messageLoopInputData.CursorY = cursorY2;
					return;
				}
				IL_189:
				inputDataLocker = this._inputDataLocker;
				lock (inputDataLocker)
				{
					this._messageLoopInputData.LeftMouse = false;
					int cursorX3 = (int)lParam % 65536;
					int cursorY3 = (int)(lParam / 65536L);
					this._messageLoopInputData.CursorX = cursorX3;
					this._messageLoopInputData.CursorY = cursorY3;
					return;
				}
				IL_1E4:
				inputDataLocker = this._inputDataLocker;
				lock (inputDataLocker)
				{
					this._messageLoopInputData.LeftMouse = true;
					int cursorX4 = (int)lParam % 65536;
					int cursorY4 = (int)(lParam / 65536L);
					this._messageLoopInputData.CursorX = cursorX4;
					this._messageLoopInputData.CursorY = cursorY4;
					return;
				}
				IL_23F:
				inputDataLocker = this._inputDataLocker;
				lock (inputDataLocker)
				{
					this._messageLoopInputData.MouseMove = true;
					int cursorX5 = (int)lParam % 65536;
					int cursorY5 = (int)(lParam / 65536L);
					this._messageLoopInputData.CursorX = cursorX5;
					this._messageLoopInputData.CursorY = cursorY5;
					return;
				}
				IL_29A:
				inputDataLocker = this._inputDataLocker;
				lock (inputDataLocker)
				{
					short num = (short)(wParam >> 16);
					this._messageLoopInputData.MouseScrollDelta = (float)num;
					return;
				}
			}
			inputDataLocker = this._inputDataLocker;
			lock (inputDataLocker)
			{
				for (int i = 0; i < 256; i++)
				{
					this._messageLoopInputData.KeyData[i] = false;
					this._messageLoopInputData.RightMouse = false;
					this._messageLoopInputData.LeftMouse = false;
				}
				return;
			}
			IL_324:
			inputDataLocker = this._inputDataLocker;
			lock (inputDataLocker)
			{
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00003C84 File Offset: 0x00001E84
		public int Width
		{
			get
			{
				return this._windowsForm.Width;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00003C91 File Offset: 0x00001E91
		public int Height
		{
			get
			{
				return this._windowsForm.Height;
			}
		}

		// Token: 0x04000019 RID: 25
		public const int WM_NCLBUTTONDOWN = 161;

		// Token: 0x0400001A RID: 26
		public const int HT_CAPTION = 2;

		// Token: 0x0400001B RID: 27
		private WindowsForm _windowsForm;

		// Token: 0x0400001D RID: 29
		private InputData _currentInputData;

		// Token: 0x0400001E RID: 30
		private InputData _oldInputData;

		// Token: 0x0400001F RID: 31
		private InputData _messageLoopInputData;

		// Token: 0x04000020 RID: 32
		private object _inputDataLocker = new object();

		// Token: 0x04000021 RID: 33
		private bool _mouseOverDragArea = true;

		// Token: 0x04000022 RID: 34
		private bool _isDragging;

		// Token: 0x04000023 RID: 35
		private LayeredWindowController _layeredWindowController;
	}
}
