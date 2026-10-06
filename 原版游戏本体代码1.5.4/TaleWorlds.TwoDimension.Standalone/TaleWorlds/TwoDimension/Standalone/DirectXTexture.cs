using System;
using System.IO;
using System.Runtime.InteropServices;
using StbSharp;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class DirectXTexture : ITexture, IDisposable
{
	private int _width;

	private int _height;

	private string _name;

	private IntPtr _texture;

	private IntPtr _srv;

	private IntPtr _device;

	public bool IsValid => true;

	public int Width => _width;

	public int Height => _height;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public IntPtr ShaderResourceView => _srv;

	public bool ClampToEdge { get; set; }

	public void LoadFromFile(IntPtr device, ResourceDepot resourceDepot, string name)
	{
		string filePath = resourceDepot.GetFilePath(name + ".png");
		LoadFromFile(device, filePath);
	}

	public void LoadFromFile(IntPtr device, string fullPathName)
	{
		_device = device;
		if (!File.Exists(fullPathName))
		{
			return;
		}
		Image image = null;
		using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(fullPathName)))
		{
			image = new ImageReader().Read(stream);
		}
		if (image == null)
		{
			return;
		}
		_width = image.Width;
		_height = image.Height;
		_name = Path.GetFileName(fullPathName);
		uint format;
		byte[] value;
		uint sysMemPitch;
		switch (image.Comp)
		{
		default:
			return;
		case 1:
			format = 61u;
			value = image.Data;
			sysMemPitch = (uint)_width;
			break;
		case 3:
			format = 28u;
			value = ExpandRGBToRGBA(image.Data, _width, _height);
			sysMemPitch = (uint)(_width * 4);
			break;
		case 4:
			format = 28u;
			value = image.Data;
			sysMemPitch = (uint)(_width * 4);
			break;
		case 2:
			return;
		}
		D3D11_TEXTURE2D_DESC desc = new D3D11_TEXTURE2D_DESC
		{
			Width = (uint)_width,
			Height = (uint)_height,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = format,
			SampleDesc = new DXGI_SAMPLE_DESC
			{
				Count = 1u,
				Quality = 0u
			},
			Usage = 0u,
			BindFlags = 8u,
			CPUAccessFlags = 0u,
			MiscFlags = 0u
		};
		GCHandle gCHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
		try
		{
			D3D11_SUBRESOURCE_DATA initialData = new D3D11_SUBRESOURCE_DATA
			{
				pSysMem = gCHandle.AddrOfPinnedObject(),
				SysMemPitch = sysMemPitch,
				SysMemSlicePitch = 0u
			};
			if (D3D11Device.CreateTexture2D(device, ref desc, ref initialData, out _texture) < 0)
			{
				return;
			}
		}
		finally
		{
			gCHandle.Free();
		}
		if (D3D11Device.CreateShaderResourceView(device, _texture, out _srv) < 0)
		{
			ComRelease.Release(_texture);
			_texture = IntPtr.Zero;
		}
	}

	public void CopyFrom(DirectXTexture other)
	{
		ComRelease.Release(_srv);
		_srv = IntPtr.Zero;
		ComRelease.Release(_texture);
		_texture = IntPtr.Zero;
		_width = other._width;
		_height = other._height;
		_name = other._name;
		_device = other._device;
		_texture = other._texture;
		_srv = other._srv;
		if (_texture != IntPtr.Zero)
		{
			ComAddRef.AddRef(_texture);
		}
		if (_srv != IntPtr.Zero)
		{
			ComAddRef.AddRef(_srv);
		}
	}

	public static DirectXTexture FromFile(IntPtr device, ResourceDepot resourceDepot, string name)
	{
		DirectXTexture directXTexture = new DirectXTexture();
		directXTexture.LoadFromFile(device, resourceDepot, name);
		if (!directXTexture.IsLoaded())
		{
			return null;
		}
		return directXTexture;
	}

	public static DirectXTexture FromFile(string fullPath)
	{
		DirectXGraphicsContext active = DirectXGraphicsContext.Active;
		if (active == null)
		{
			return null;
		}
		DirectXTexture directXTexture = new DirectXTexture();
		directXTexture.LoadFromFile(active.DeviceHandle, fullPath);
		if (!directXTexture.IsLoaded())
		{
			return null;
		}
		return directXTexture;
	}

	public bool IsLoaded()
	{
		return _srv != IntPtr.Zero;
	}

	public void Release()
	{
		Dispose();
	}

	public void Dispose()
	{
		ComRelease.Release(_srv);
		_srv = IntPtr.Zero;
		ComRelease.Release(_texture);
		_texture = IntPtr.Zero;
	}

	private static byte[] ExpandRGBToRGBA(byte[] rgb, int width, int height)
	{
		int num = width * height;
		byte[] array = new byte[num * 4];
		for (int i = 0; i < num; i++)
		{
			array[i * 4] = rgb[i * 3];
			array[i * 4 + 1] = rgb[i * 3 + 1];
			array[i * 4 + 2] = rgb[i * 3 + 2];
			array[i * 4 + 3] = byte.MaxValue;
		}
		return array;
	}
}
