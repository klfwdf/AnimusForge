using System;
using System.Runtime.InteropServices;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class DirectXVertexBuffer : IDisposable
{
	private int _floatCapacity = 131072;

	private int _uintCapacity = 131072;

	private IntPtr _device;

	private IntPtr _vertexBuffer;

	private IntPtr _indexBuffer;

	public const uint Stride = 16u;

	private DirectXVertexBuffer()
	{
	}

	public static DirectXVertexBuffer Create(IntPtr device)
	{
		DirectXVertexBuffer directXVertexBuffer = new DirectXVertexBuffer();
		directXVertexBuffer._device = device;
		if (!directXVertexBuffer.CreateVertexBuffer(131072) || !directXVertexBuffer.CreateIndexBuffer(131072))
		{
			return null;
		}
		return directXVertexBuffer;
	}

	private bool CreateVertexBuffer(int floatCapacity)
	{
		ComRelease.Release(_vertexBuffer);
		_vertexBuffer = IntPtr.Zero;
		D3D11_BUFFER_DESC desc = new D3D11_BUFFER_DESC
		{
			ByteWidth = (uint)(floatCapacity * 4),
			Usage = 2u,
			BindFlags = 1u,
			CPUAccessFlags = 65536u,
			MiscFlags = 0u,
			StructureByteStride = 0u
		};
		if (D3D11Device.CreateBuffer(_device, ref desc, out _vertexBuffer) < 0)
		{
			return false;
		}
		_floatCapacity = floatCapacity;
		return true;
	}

	private bool CreateIndexBuffer(int uintCapacity)
	{
		ComRelease.Release(_indexBuffer);
		_indexBuffer = IntPtr.Zero;
		D3D11_BUFFER_DESC desc = new D3D11_BUFFER_DESC
		{
			ByteWidth = (uint)(uintCapacity * 4),
			Usage = 2u,
			BindFlags = 2u,
			CPUAccessFlags = 65536u,
			MiscFlags = 0u,
			StructureByteStride = 0u
		};
		if (D3D11Device.CreateBuffer(_device, ref desc, out _indexBuffer) < 0)
		{
			return false;
		}
		_uintCapacity = uintCapacity;
		return true;
	}

	public void LoadVertexData(IntPtr context, float[] interleavedData)
	{
		if (interleavedData == null || interleavedData.Length == 0)
		{
			return;
		}
		if (interleavedData.Length > _floatCapacity)
		{
			int num;
			for (num = _floatCapacity; num < interleavedData.Length; num *= 2)
			{
			}
			Debug.Print($"[LAUNCHER]: Growing vertex buffer from {_floatCapacity} to {num} floats.");
			if (!CreateVertexBuffer(num))
			{
				return;
			}
		}
		if (D3D11Context.Map(context, _vertexBuffer, 4u, out var mapped) < 0)
		{
			return;
		}
		try
		{
			Marshal.Copy(interleavedData, 0, mapped.pData, interleavedData.Length);
		}
		finally
		{
			D3D11Context.Unmap(context, _vertexBuffer);
		}
	}

	public void LoadIndexData(IntPtr context, uint[] indices)
	{
		if (indices == null || indices.Length == 0)
		{
			return;
		}
		if (indices.Length > _uintCapacity)
		{
			int num;
			for (num = _uintCapacity; num < indices.Length; num *= 2)
			{
			}
			Debug.Print($"[LAUNCHER]: Growing index buffer from {_uintCapacity} to {num} uints.");
			if (!CreateIndexBuffer(num))
			{
				return;
			}
		}
		if (D3D11Context.Map(context, _indexBuffer, 4u, out var mapped) < 0)
		{
			return;
		}
		int[] array = new int[indices.Length];
		for (int i = 0; i < indices.Length; i++)
		{
			array[i] = (int)indices[i];
		}
		try
		{
			Marshal.Copy(array, 0, mapped.pData, array.Length);
		}
		finally
		{
			D3D11Context.Unmap(context, _indexBuffer);
		}
	}

	public void Bind(IntPtr context)
	{
		D3D11Context.IASetVertexBuffers(context, _vertexBuffer, 16u);
		D3D11Context.IASetIndexBuffer(context, _indexBuffer);
		D3D11Context.IASetPrimitiveTopology(context);
	}

	public void Dispose()
	{
		ComRelease.Release(_vertexBuffer);
		_vertexBuffer = IntPtr.Zero;
		ComRelease.Release(_indexBuffer);
		_indexBuffer = IntPtr.Zero;
	}
}
