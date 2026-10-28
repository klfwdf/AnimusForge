using System;
using System.Runtime.InteropServices;
using System.Text;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone;

public class DirectXShader : IDisposable
{
	private IntPtr _vertexShader;

	private IntPtr _pixelShader;

	private IntPtr _inputLayout;

	private DirectXShader()
	{
	}

	public static DirectXShader CreateShader(IntPtr device, string hlslSource, string shaderName)
	{
		IntPtr ppCode = IntPtr.Zero;
		IntPtr ppCode2 = IntPtr.Zero;
		IntPtr ppErrorMsgs = IntPtr.Zero;
		byte[] bytes = Encoding.UTF8.GetBytes(hlslSource);
		GCHandle gCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		try
		{
			IntPtr pSrcData = gCHandle.AddrOfPinnedObject();
			IntPtr srcDataSize = (IntPtr)bytes.Length;
			if (D3DCompiler.D3DCompile(pSrcData, srcDataSize, shaderName + ".hlsl", IntPtr.Zero, IntPtr.Zero, "VSMain", "vs_5_0", 0u, 0u, out ppCode, out ppErrorMsgs) < 0)
			{
				D3DCompiler.GetErrorMessage(ppErrorMsgs);
				return null;
			}
			if (ppErrorMsgs != IntPtr.Zero)
			{
				ID3DBlob.Release(ppErrorMsgs);
				ppErrorMsgs = IntPtr.Zero;
			}
			if (D3DCompiler.D3DCompile(pSrcData, srcDataSize, shaderName + ".hlsl", IntPtr.Zero, IntPtr.Zero, "PSMain", "ps_5_0", 0u, 0u, out ppCode2, out ppErrorMsgs) < 0)
			{
				D3DCompiler.GetErrorMessage(ppErrorMsgs);
				return null;
			}
			if (ppErrorMsgs != IntPtr.Zero)
			{
				ID3DBlob.Release(ppErrorMsgs);
				ppErrorMsgs = IntPtr.Zero;
			}
			IntPtr bufferPointer = ID3DBlob.GetBufferPointer(ppCode);
			int bufferSize = ID3DBlob.GetBufferSize(ppCode);
			IntPtr bufferPointer2 = ID3DBlob.GetBufferPointer(ppCode2);
			int bufferSize2 = ID3DBlob.GetBufferSize(ppCode2);
			if (D3D11Device.CreateVertexShader(device, bufferPointer, bufferSize, out var vs) < 0)
			{
				return null;
			}
			if (D3D11Device.CreatePixelShader(device, bufferPointer2, bufferSize2, out var ps) < 0)
			{
				ComRelease.Release(vs);
				return null;
			}
			D3D11_INPUT_ELEMENT_DESC[] elements = new D3D11_INPUT_ELEMENT_DESC[2]
			{
				new D3D11_INPUT_ELEMENT_DESC
				{
					SemanticName = "POSITION",
					SemanticIndex = 0u,
					Format = 16u,
					InputSlot = 0u,
					AlignedByteOffset = 0u,
					InputSlotClass = 0u,
					InstanceDataStepRate = 0u
				},
				new D3D11_INPUT_ELEMENT_DESC
				{
					SemanticName = "TEXCOORD",
					SemanticIndex = 0u,
					Format = 16u,
					InputSlot = 0u,
					AlignedByteOffset = 8u,
					InputSlotClass = 0u,
					InstanceDataStepRate = 0u
				}
			};
			if (D3D11Device.CreateInputLayout(device, elements, bufferPointer, bufferSize, out var inputLayout) < 0)
			{
				ComRelease.Release(vs);
				ComRelease.Release(ps);
				return null;
			}
			return new DirectXShader
			{
				_vertexShader = vs,
				_pixelShader = ps,
				_inputLayout = inputLayout
			};
		}
		finally
		{
			gCHandle.Free();
			if (ppCode != IntPtr.Zero)
			{
				ID3DBlob.Release(ppCode);
			}
			if (ppCode2 != IntPtr.Zero)
			{
				ID3DBlob.Release(ppCode2);
			}
			if (ppErrorMsgs != IntPtr.Zero)
			{
				ID3DBlob.Release(ppErrorMsgs);
			}
		}
	}

	public void Use(IntPtr context)
	{
		D3D11Context.VSSetShader(context, _vertexShader);
		D3D11Context.PSSetShader(context, _pixelShader);
		D3D11Context.IASetInputLayout(context, _inputLayout);
	}

	public void Dispose()
	{
		ComRelease.Release(_vertexShader);
		_vertexShader = IntPtr.Zero;
		ComRelease.Release(_pixelShader);
		_pixelShader = IntPtr.Zero;
		ComRelease.Release(_inputLayout);
		_inputLayout = IntPtr.Zero;
	}
}
