using System;
using System.Numerics;
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

	private bool _layeredWindow;

	private bool _isFinalized;

	public DirectXGraphicsContext GraphicsContext { get; private set; }

	public int Width => _windowsForm.Width;

	public int Height => _windowsForm.Height;

	public bool IsMinimized => _windowsForm.IsMinimized;

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
		GraphicsContext = new DirectXGraphicsContext();
		_layeredWindow = layeredWindow;
	}

	public DXGI.RECT DecideWindowPosition()
	{
		IntPtr intPtr = User32.MonitorFromWindow(User32.GetDesktopWindow(), 1u);
		User32.GetClientRect(User32.GetDesktopWindow(), out var lpRect);
		DXGI.RECT result = new DXGI.RECT
		{
			left = lpRect.Left,
			right = lpRect.Right,
			top = lpRect.Top,
			bottom = lpRect.Bottom
		};
		IntPtr factory = IntPtr.Zero;
		DXGI.CreateDXGIFactory(ref DXGI.IID_IDXGIFactory, out factory);
		if (factory == IntPtr.Zero)
		{
			return result;
		}
		MBList<Tuple<uint, ulong>> mBList = new MBList<Tuple<uint, ulong>>();
		IntPtr adapter;
		for (uint num = 0u; DXGIFactory.EnumAdapters(factory, num, out adapter) == 0; num++)
		{
			DXGIAdapter.GetDesc(adapter, out var desc);
			ulong num2 = (ulong)desc.DedicatedVideoMemory;
			if (num2 != 0)
			{
				mBList.Add(new Tuple<uint, ulong>(num, num2));
			}
			ComRelease.Release(adapter);
		}
		if (mBList.Count == 0)
		{
			ComRelease.Release(factory);
			return result;
		}
		mBList.Sort((Tuple<uint, ulong> x, Tuple<uint, ulong> y) => y.Item2.CompareTo(x.Item2));
		foreach (Tuple<uint, ulong> item in mBList)
		{
			if (DXGIFactory.EnumAdapters(factory, item.Item1, out var adapter2) != 0)
			{
				continue;
			}
			IntPtr output;
			for (uint num3 = 0u; DXGIAdapter.EnumOutputs(adapter2, num3, out output) == 0; num3++)
			{
				DXGIOutput.GetDesc(output, out var desc2);
				ComRelease.Release(output);
				if (desc2.AttachedToDesktop && desc2.Monitor == intPtr)
				{
					ComRelease.Release(adapter2);
					ComRelease.Release(factory);
					return desc2.DesktopCoordinates;
				}
			}
			ComRelease.Release(adapter2);
		}
		foreach (Tuple<uint, ulong> item2 in mBList)
		{
			if (DXGIFactory.EnumAdapters(factory, item2.Item1, out var adapter3) != 0)
			{
				continue;
			}
			IntPtr output2;
			for (uint num4 = 0u; DXGIAdapter.EnumOutputs(adapter3, num4, out output2) == 0; num4++)
			{
				DXGIOutput.GetDesc(output2, out var desc3);
				ComRelease.Release(output2);
				if (desc3.AttachedToDesktop)
				{
					ComRelease.Release(adapter3);
					ComRelease.Release(factory);
					return desc3.DesktopCoordinates;
				}
			}
			ComRelease.Release(adapter3);
		}
		ComRelease.Release(factory);
		return result;
	}

	public void Destroy()
	{
		if (!_isFinalized)
		{
			_isFinalized = true;
			_layeredWindowController?.OnFinalize();
			_windowsForm.Destroy();
		}
	}

	public void MinimizeWindow()
	{
		User32.ShowWindow(_windowsForm.Handle, WindowShowStyle.Minimize);
	}

	public void InitializeGraphicsContext(ResourceDepot resourceDepot)
	{
		if (_layeredWindow)
		{
			GraphicsContext.IsLayeredWindow = true;
		}
		GraphicsContext.CreateContext(_windowsForm.Handle, resourceDepot);
		GraphicsContext.ProjectionMatrix = MatrixExtensions.CreateOrthographicOffCenter(0f, _windowsForm.Width, _windowsForm.Height, 0f, 0f, 2f);
		if (_layeredWindow)
		{
			_layeredWindowController = new LayeredWindowController(_windowsForm.Handle, _windowsForm.Width, _windowsForm.Height, GraphicsContext);
		}
	}

	public void BeginFrame()
	{
		if (GraphicsContext != null)
		{
			GraphicsContext.BeginFrame(_windowsForm.Width, _windowsForm.Height);
			GraphicsContext.ProjectionMatrix = MatrixExtensions.CreateOrthographicOffCenter(0f, _windowsForm.Width, _windowsForm.Height, 0f, 0f, 2f);
			_layeredWindowController?.SetSize(_windowsForm.Width, _windowsForm.Height);
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
		if (message <= WindowMessage.KeyDown)
		{
			switch (message)
			{
			case WindowMessage.Close:
				Destroy();
				Environment.Exit(0);
				break;
			case WindowMessage.KeyDown:
				lock (_inputDataLocker)
				{
					_messageLoopInputData.KeyData[wParam] = true;
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
		else if (message <= WindowMessage.MouseWheel)
		{
			switch (message)
			{
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
			}
		}
		else if (message != WindowMessage.DeviceChange)
		{
			_ = 736;
		}
	}
}
