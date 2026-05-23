using System;
using System.Numerics;
using System.Runtime.InteropServices;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class GraphicsForm : IMessageCommunicator
{
	public const int WM_NCLBUTTONDOWN = 161;

	public const int HT_CAPTION = 2;

	private WindowsForm _windowsForm;

	private InputData _currentInputData;

	private InputData _oldInputData;

	private InputData _messageLoopInputData;

	private object _inputDataLocker = new object();

	private bool _mouseOverDragArea = true;

	private bool _isDragging;

	private LayeredWindowController _layeredWindowController;

	public GraphicsContext GraphicsContext { get; private set; }

	public int Width => _windowsForm.Width;

	public int Height => _windowsForm.Height;

	public GraphicsForm(int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, bool layeredWindow = false, string name = null)
	{
		DXGI.RECT rECT = DecideWindowPosition();
		int num = rECT.right - rECT.left;
		int num2 = rECT.bottom - rECT.top;
		int x = rECT.left + (num - width) / 2;
		int y = rECT.top + (num2 - height) / 2;
		_windowsForm = new WindowsForm(x, y, width, height, resourceDepot, borderlessWindow, enableWindowBlur, name);
		Initalize(layeredWindow);
	}

	public GraphicsForm(int x, int y, int width, int height, ResourceDepot resourceDepot, bool borderlessWindow = false, bool enableWindowBlur = false, bool layeredWindow = false, string name = null)
	{
		_windowsForm = new WindowsForm(x, y, width, height, resourceDepot, borderlessWindow, enableWindowBlur, name);
		Initalize(layeredWindow);
	}

	public GraphicsForm(WindowsForm windowsForm)
	{
		_windowsForm = windowsForm;
		Initalize(layeredWindow: false);
	}

	private void Initalize(bool layeredWindow)
	{
		_currentInputData = new InputData();
		_oldInputData = new InputData();
		_messageLoopInputData = new InputData();
		_windowsForm.AddMessageHandler(MessageHandler);
		_windowsForm.Show();
		GraphicsContext = new GraphicsContext();
		if (layeredWindow)
		{
			_layeredWindowController = new LayeredWindowController(_windowsForm.Handle, _windowsForm.Width, _windowsForm.Height);
		}
	}

	public bool CompareRecrangles(DXGI.RECT Rect1, DXGI.RECT Rect2)
	{
		int num = Rect1.right - Rect1.left;
		int num2 = Rect1.bottom - Rect1.top;
		int num3 = Rect2.right - Rect2.left;
		int num4 = Rect2.bottom - Rect2.top;
		if (num > num3 && num2 > num4)
		{
			return true;
		}
		return false;
	}

	public DXGI.RECT DecideWindowPosition()
	{
		User32.GetClientRect(User32.GetDesktopWindow(), out var lpRect);
		DXGI.RECT rECT = new DXGI.RECT
		{
			left = lpRect.Left,
			right = lpRect.Right,
			top = lpRect.Top,
			bottom = lpRect.Bottom
		};
		DXGI.CreateDXGIFactory(ref DXGI.IID_IDXGIFactory, out var factory);
		DXGI.IDXGIFactory iDXGIFactory = (DXGI.IDXGIFactory)Marshal.GetObjectForIUnknown(factory);
		MBList<Tuple<uint, DXGI.DXGI_ADAPTER_DESC>> mBList = new MBList<Tuple<uint, DXGI.DXGI_ADAPTER_DESC>>();
		DXGI.IDXGIAdapter adapter;
		for (uint num = 0u; iDXGIFactory.EnumAdapters(num, out adapter) == 0; num++)
		{
			adapter.GetDesc(out var desc);
			if ((long)(ulong)desc.DedicatedVideoMemory > 0L)
			{
				mBList.Add(new Tuple<uint, DXGI.DXGI_ADAPTER_DESC>(num, desc));
			}
		}
		if (mBList.Count == 0)
		{
			Marshal.FinalReleaseComObject(iDXGIFactory);
			return rECT;
		}
		mBList.Sort((Tuple<uint, DXGI.DXGI_ADAPTER_DESC> x, Tuple<uint, DXGI.DXGI_ADAPTER_DESC> y) => ((ulong)x.Item2.DedicatedVideoMemory <= (ulong)y.Item2.DedicatedVideoMemory) ? 1 : (-1));
		foreach (Tuple<uint, DXGI.DXGI_ADAPTER_DESC> item in mBList)
		{
			iDXGIFactory.EnumAdapters(item.Item1, out var adapter2);
			DXGI.IDXGIOutput ppOutput;
			for (uint num2 = 0u; adapter2.EnumOutputs(num2, out ppOutput) == 0; num2++)
			{
				ppOutput.GetDesc(out var desc2);
				if (desc2.AttachedToDesktop && rECT == desc2.DesktopCoordinates)
				{
					Marshal.FinalReleaseComObject(iDXGIFactory);
					return rECT;
				}
			}
		}
		foreach (Tuple<uint, DXGI.DXGI_ADAPTER_DESC> item2 in mBList)
		{
			iDXGIFactory.EnumAdapters(item2.Item1, out var adapter3);
			DXGI.IDXGIOutput ppOutput2;
			for (uint num3 = 0u; adapter3.EnumOutputs(num3, out ppOutput2) == 0; num3++)
			{
				ppOutput2.GetDesc(out var desc3);
				if (desc3.AttachedToDesktop)
				{
					Marshal.FinalReleaseComObject(iDXGIFactory);
					return desc3.DesktopCoordinates;
				}
			}
		}
		Marshal.FinalReleaseComObject(iDXGIFactory);
		return rECT;
	}

	public void Destroy()
	{
		GraphicsContext.DestroyContext();
		_windowsForm.Destroy();
		_layeredWindowController?.OnFinalize();
	}

	public void MinimizeWindow()
	{
		User32.ShowWindow(_windowsForm.Handle, WindowShowStyle.Minimize);
	}

	public void InitializeGraphicsContext(ResourceDepot resourceDepot)
	{
		GraphicsContext.Control = _windowsForm;
		GraphicsContext.CreateContext(resourceDepot);
		GraphicsContext.ProjectionMatrix = MatrixExtensions.CreateOrthographicOffCenter(0f, _windowsForm.Width, _windowsForm.Height, 0f, 0f, 2f);
	}

	public void BeginFrame()
	{
		if (GraphicsContext != null)
		{
			GraphicsContext.BeginFrame(_windowsForm.Width, _windowsForm.Height);
			GraphicsContext.Resize(_windowsForm.Width, _windowsForm.Height);
			GraphicsContext.ProjectionMatrix = MatrixExtensions.CreateOrthographicOffCenter(0f, _windowsForm.Width, _windowsForm.Height, 0f, 0f, 2f);
		}
	}

	public void Update()
	{
		if (!_isDragging && _mouseOverDragArea && _currentInputData.LeftMouse && !_oldInputData.LeftMouse)
		{
			_isDragging = true;
			MessageHandler(WindowMessage.LeftButtonUp, 0L, 0L);
		}
	}

	public void MessageLoop()
	{
		if (_isDragging)
		{
			User32.ReleaseCapture();
			User32.SendMessage(_windowsForm.Handle, 161u, new IntPtr(2), IntPtr.Zero);
			_isDragging = false;
			User32.SetCapture(_windowsForm.Handle);
		}
	}

	public void UpdateInput(bool mouseOverDragArea = false)
	{
		_mouseOverDragArea = mouseOverDragArea;
		InputData oldInputData = _oldInputData;
		_oldInputData = _currentInputData;
		_currentInputData = oldInputData;
		lock (_inputDataLocker)
		{
			_currentInputData.FillFrom(_messageLoopInputData);
			_messageLoopInputData.Reset();
		}
	}

	public void PostRender()
	{
		if (_layeredWindowController != null)
		{
			_layeredWindowController.PostRender();
		}
	}

	public bool GetKeyDown(InputKey keyCode)
	{
		switch (keyCode)
		{
		case InputKey.LeftMouseButton:
			return LeftMouseDown();
		case InputKey.RightMouseButton:
			return RightMouseDown();
		default:
			if (_currentInputData.KeyData[(int)keyCode])
			{
				return !_oldInputData.KeyData[(int)keyCode];
			}
			return false;
		}
	}

	public bool GetKey(InputKey keyCode)
	{
		return keyCode switch
		{
			InputKey.LeftMouseButton => LeftMouse(), 
			InputKey.RightMouseButton => RightMouse(), 
			_ => _currentInputData.KeyData[(int)keyCode], 
		};
	}

	public bool GetKeyUp(InputKey keyCode)
	{
		switch (keyCode)
		{
		case InputKey.LeftMouseButton:
			return LeftMouseUp();
		case InputKey.RightMouseButton:
			return RightMouseUp();
		default:
			if (!_currentInputData.KeyData[(int)keyCode])
			{
				return _oldInputData.KeyData[(int)keyCode];
			}
			return false;
		}
	}

	public float GetMouseDeltaZ()
	{
		return _currentInputData.MouseScrollDelta;
	}

	public bool LeftMouse()
	{
		return _currentInputData.LeftMouse;
	}

	public bool LeftMouseDown()
	{
		if (_currentInputData.LeftMouse)
		{
			return !_oldInputData.LeftMouse;
		}
		return false;
	}

	public bool LeftMouseUp()
	{
		if (!_currentInputData.LeftMouse)
		{
			return _oldInputData.LeftMouse;
		}
		return false;
	}

	public bool RightMouse()
	{
		return _currentInputData.RightMouse;
	}

	public bool RightMouseDown()
	{
		if (_currentInputData.RightMouse)
		{
			return !_oldInputData.RightMouse;
		}
		return false;
	}

	public bool RightMouseUp()
	{
		if (!_currentInputData.RightMouse)
		{
			return _oldInputData.RightMouse;
		}
		return false;
	}

	public Vector2 MousePosition()
	{
		return new Vector2(_currentInputData.CursorX, _currentInputData.CursorY);
	}

	public bool MouseMove()
	{
		return _currentInputData.MouseMove;
	}

	public void FillInputDataFromCurrent(InputData inputData)
	{
		inputData.FillFrom(_currentInputData);
	}

	private void MessageHandler(WindowMessage message, long wParam, long lParam)
	{
		switch (message)
		{
		case WindowMessage.Close:
			Destroy();
			Environment.Exit(0);
			break;
		case WindowMessage.DisplayChange:
		case WindowMessage.DeviceChange:
		case WindowMessage.DpiChanged:
			if (GraphicsContext != null)
			{
				GraphicsContext.RequestContextReactivation();
			}
			break;
		case WindowMessage.KeyDown:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.KeyData[wParam] = true;
				break;
			}
		case WindowMessage.KeyUp:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.KeyData[wParam] = false;
				break;
			}
		case WindowMessage.RightButtonUp:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.RightMouse = false;
				int cursorX5 = (int)lParam % 65536;
				int cursorY5 = (int)(lParam / 65536);
				_messageLoopInputData.CursorX = cursorX5;
				_messageLoopInputData.CursorY = cursorY5;
				break;
			}
		case WindowMessage.RightButtonDown:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.RightMouse = true;
				int cursorX4 = (int)lParam % 65536;
				int cursorY4 = (int)(lParam / 65536);
				_messageLoopInputData.CursorX = cursorX4;
				_messageLoopInputData.CursorY = cursorY4;
				break;
			}
		case WindowMessage.LeftButtonUp:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.LeftMouse = false;
				int cursorX3 = (int)lParam % 65536;
				int cursorY3 = (int)(lParam / 65536);
				_messageLoopInputData.CursorX = cursorX3;
				_messageLoopInputData.CursorY = cursorY3;
				break;
			}
		case WindowMessage.LeftButtonDown:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.LeftMouse = true;
				int cursorX2 = (int)lParam % 65536;
				int cursorY2 = (int)(lParam / 65536);
				_messageLoopInputData.CursorX = cursorX2;
				_messageLoopInputData.CursorY = cursorY2;
				break;
			}
		case WindowMessage.MouseMove:
			lock (_inputDataLocker)
			{
				_messageLoopInputData.MouseMove = true;
				int cursorX = (int)lParam % 65536;
				int cursorY = (int)(lParam / 65536);
				_messageLoopInputData.CursorX = cursorX;
				_messageLoopInputData.CursorY = cursorY;
				break;
			}
		case WindowMessage.MouseWheel:
			lock (_inputDataLocker)
			{
				short num = (short)(wParam >> 16);
				_messageLoopInputData.MouseScrollDelta = num;
				break;
			}
		case WindowMessage.KillFocus:
			lock (_inputDataLocker)
			{
				for (int i = 0; i < 256; i++)
				{
					_messageLoopInputData.KeyData[i] = false;
					_messageLoopInputData.RightMouse = false;
					_messageLoopInputData.LeftMouse = false;
				}
				break;
			}
		case WindowMessage.SetFocus:
			lock (_inputDataLocker)
			{
				break;
			}
		}
	}
}
