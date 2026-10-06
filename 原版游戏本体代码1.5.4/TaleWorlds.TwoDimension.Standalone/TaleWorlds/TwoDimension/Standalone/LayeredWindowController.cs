using System;
using System.Drawing;
using System.Runtime.InteropServices;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class LayeredWindowController
{
	private const int GwlExStyle = -20;

	private const uint WsExLayered = 524288u;

	private readonly IntPtr _windowHandle;

	private readonly IntPtr _screenDC;

	private readonly IntPtr _memoryDC;

	private DirectXGraphicsContext _context;

	private IntPtr _stagingTexture;

	private IntPtr _hDib;

	private IntPtr _dibBits;

	private IntPtr _hOldBitmap;

	private int _width;

	private int _height;

	private byte[] _rowBuffer;

	private BlendFunction _blendFunction = BlendFunction.Default;

	private System.Drawing.Point _localOriginPoint = new System.Drawing.Point(0, 0);

	public LayeredWindowController(IntPtr windowHandle, int width, int height, DirectXGraphicsContext context)
	{
		_windowHandle = windowHandle;
		_context = context;
		User32.SetWindowLong(_windowHandle, -20, 524288u);
		_screenDC = User32.GetDC(IntPtr.Zero);
		_memoryDC = Gdi32.CreateCompatibleDC(_screenDC);
		SetSize(width, height);
	}

	public void SetSize(int width, int height)
	{
		if (width > 0 && height > 0 && (width != _width || height != _height))
		{
			_width = width;
			_height = height;
			ReleaseDibResources();
			ReleaseStaging();
			_rowBuffer = new byte[_width * 4];
			CreateStagingTexture();
			CreateDib();
		}
	}

	private void CreateStagingTexture()
	{
		if (_context != null && !(_context.DeviceHandle == IntPtr.Zero))
		{
			D3D11_TEXTURE2D_DESC desc = new D3D11_TEXTURE2D_DESC
			{
				Width = (uint)_width,
				Height = (uint)_height,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = 87u,
				SampleDesc = new DXGI_SAMPLE_DESC
				{
					Count = 1u,
					Quality = 0u
				},
				Usage = 3u,
				BindFlags = 0u,
				CPUAccessFlags = 131072u,
				MiscFlags = 0u
			};
			if (D3D11Device.CreateTexture2DEmpty(_context.DeviceHandle, ref desc, out _stagingTexture) < 0)
			{
				_stagingTexture = IntPtr.Zero;
			}
		}
	}

	private void ReleaseStaging()
	{
		if (_stagingTexture != IntPtr.Zero)
		{
			ComRelease.Release(_stagingTexture);
			_stagingTexture = IntPtr.Zero;
		}
	}

	private void CreateDib()
	{
		BitmapInfo pbmi = new BitmapInfo
		{
			bmiHeader = 
			{
				biSize = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader)),
				biWidth = _width,
				biHeight = -_height,
				biPlanes = 1,
				biBitCount = 32,
				biCompression = 0u,
				biSizeImage = 0u,
				biXPelsPerMeter = 0,
				biYPelsPerMeter = 0,
				biClrUsed = 0u,
				biClrImportant = 0u
			},
			r = 0,
			g = 0,
			b = 0,
			a = 0
		};
		_hDib = Gdi32.CreateDIBSection(_screenDC, ref pbmi, 0u, out _dibBits, IntPtr.Zero, 0u);
		if (_hDib == IntPtr.Zero)
		{
			_dibBits = IntPtr.Zero;
		}
		else
		{
			_hOldBitmap = Gdi32.SelectObject(_memoryDC, _hDib);
		}
	}

	private void ReleaseDibResources()
	{
		if (_hDib != IntPtr.Zero)
		{
			if (_hOldBitmap != IntPtr.Zero)
			{
				Gdi32.SelectObject(_memoryDC, _hOldBitmap);
				_hOldBitmap = IntPtr.Zero;
			}
			Gdi32.DeleteObject(_hDib);
			_hDib = IntPtr.Zero;
			_dibBits = IntPtr.Zero;
		}
	}

	public void PostRender()
	{
		if (_width <= 0 || _height <= 0 || _context == null || _stagingTexture == IntPtr.Zero || _context.DeviceContextHandle == IntPtr.Zero || _context.IsDeviceLost || _hDib == IntPtr.Zero || _dibBits == IntPtr.Zero)
		{
			return;
		}
		IntPtr currentBackBuffer = _context.GetCurrentBackBuffer();
		if (currentBackBuffer == IntPtr.Zero)
		{
			return;
		}
		D3D11Context.CopyResource(_context.DeviceContextHandle, _stagingTexture, currentBackBuffer);
		ComRelease.Release(currentBackBuffer);
		D3D11_MAPPED_SUBRESOURCE mapped;
		int num = D3D11Context.Map(_context.DeviceContextHandle, _stagingTexture, 1u, out mapped);
		if (num < 0)
		{
			_context.ReportDeviceLost(num);
			return;
		}
		try
		{
			int num2 = _width * 4;
			for (int i = 0; i < _height; i++)
			{
				IntPtr source = mapped.pData + i * (int)mapped.RowPitch;
				IntPtr destination = _dibBits + i * num2;
				Marshal.Copy(source, _rowBuffer, 0, num2);
				Marshal.Copy(_rowBuffer, 0, destination, num2);
			}
		}
		finally
		{
			D3D11Context.Unmap(_context.DeviceContextHandle, _stagingTexture);
		}
		User32.GetWindowRect(_windowHandle, out var lpRect);
		System.Drawing.Point pptDst = new System.Drawing.Point(lpRect.Left, lpRect.Top);
		Size psize = new Size(_width, _height);
		User32.UpdateLayeredWindow(_windowHandle, _screenDC, ref pptDst, ref psize, _memoryDC, ref _localOriginPoint, 0, ref _blendFunction, 2);
	}

	public void OnFinalize()
	{
		ReleaseDibResources();
		ReleaseStaging();
		User32.ReleaseDC(IntPtr.Zero, _screenDC);
		Gdi32.DeleteDC(_memoryDC);
	}
}
