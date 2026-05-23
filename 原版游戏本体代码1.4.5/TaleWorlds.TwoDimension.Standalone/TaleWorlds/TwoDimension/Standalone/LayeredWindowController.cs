using System;
using System.Drawing;
using System.Runtime.InteropServices;
using TaleWorlds.TwoDimension.Standalone.Native.OpenGL;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class LayeredWindowController
{
	private const int GwlExStyle = -20;

	private const uint WsExLayered = 524288u;

	private readonly IntPtr _windowHandle;

	private readonly IntPtr _screenDC;

	private readonly IntPtr _memoryDC;

	private Size _windowSize;

	private byte[] _pixelData;

	private BlendFunction _blendFunction = BlendFunction.Default;

	private System.Drawing.Point _localOriginPoint = new System.Drawing.Point(0, 0);

	private BitmapInfo _bitmapInfo;

	public LayeredWindowController(IntPtr windowHandle, int width, int height)
	{
		_windowHandle = windowHandle;
		User32.SetWindowLong(_windowHandle, -20, 524288u);
		_screenDC = User32.GetDC(IntPtr.Zero);
		_memoryDC = Gdi32.CreateCompatibleDC(_screenDC);
		SetSize(width, height);
	}

	private void CreateBitmapInfo()
	{
		BitmapInfoHeader bmiHeader = default(BitmapInfoHeader);
		bmiHeader.biWidth = _windowSize.Width;
		bmiHeader.biHeight = _windowSize.Height;
		bmiHeader.biPlanes = 1;
		bmiHeader.biBitCount = 32;
		bmiHeader.biCompression = 0u;
		bmiHeader.biSizeImage = 0u;
		bmiHeader.biXPelsPerMeter = 0;
		bmiHeader.biYPelsPerMeter = 0;
		bmiHeader.biClrUsed = 0u;
		bmiHeader.biClrImportant = 0u;
		bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader));
		_bitmapInfo.bmiHeader = bmiHeader;
		_bitmapInfo.r = 0;
		_bitmapInfo.g = 0;
		_bitmapInfo.b = 0;
		_bitmapInfo.a = 0;
	}

	public void SetSize(int width, int height)
	{
		_windowSize = new Size(width, height);
		if (_windowSize.Width > 0 && _windowSize.Height > 0)
		{
			_pixelData = new byte[_windowSize.Width * _windowSize.Height * 4];
		}
		CreateBitmapInfo();
	}

	public void PostRender()
	{
		if (_windowSize.Width > 0 && _windowSize.Height > 0)
		{
			Opengl32.PixelStore(Target.PACK_ALIGNMENT, 1);
			Opengl32.ReadPixels(0, 0, _windowSize.Width, _windowSize.Height, PixelFormat.BGRA, DataType.UnsignedByte, _pixelData);
			IntPtr intPtr = Gdi32.CreateCompatibleBitmap(_screenDC, _windowSize.Width, _windowSize.Height);
			IntPtr h = Gdi32.SelectObject(_memoryDC, intPtr);
			Gdi32.StretchDIBits(_memoryDC, 0, 0, _windowSize.Width, _windowSize.Height, 0, 0, _windowSize.Width, _windowSize.Height, _pixelData, ref _bitmapInfo, 0u, 13369376);
			User32.GetWindowRect(_windowHandle, out var lpRect);
			System.Drawing.Point pptDst = new System.Drawing.Point(lpRect.Left, lpRect.Top);
			User32.UpdateLayeredWindow(_windowHandle, _screenDC, ref pptDst, ref _windowSize, _memoryDC, ref _localOriginPoint, 0, ref _blendFunction, 2);
			if (intPtr != IntPtr.Zero)
			{
				Gdi32.SelectObject(_memoryDC, h);
				Gdi32.DeleteObject(intPtr);
			}
		}
	}

	public void OnFinalize()
	{
		User32.ReleaseDC(IntPtr.Zero, _screenDC);
		Gdi32.DeleteDC(_memoryDC);
	}
}
