using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class D3D11Context
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnClearRenderTargetView(IntPtr self, IntPtr pRenderTargetView, [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] float[] ColorRGBA);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnOMSetRenderTargets(IntPtr self, uint NumViews, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] ppRTVs, IntPtr pDSV);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnOMSetBlendState(IntPtr self, IntPtr pBlendState, IntPtr BlendFactor, uint SampleMask);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnRSSetViewports(IntPtr self, uint NumViewports, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] D3D11_VIEWPORT[] pViewports);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnRSSetScissorRects(IntPtr self, uint NumRects, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] D3D11_RECT[] pRects);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnRSSetState(IntPtr self, IntPtr pRasterizerState);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnIASetInputLayout(IntPtr self, IntPtr pInputLayout);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnIASetVertexBuffers(IntPtr self, uint StartSlot, uint NumBuffers, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppVertexBuffers, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] pStrides, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] pOffsets);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnIASetIndexBuffer(IntPtr self, IntPtr pIndexBuffer, uint Format, uint Offset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnIASetPrimitiveTopology(IntPtr self, uint Topology);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnVSSetShader(IntPtr self, IntPtr pVertexShader, IntPtr ppClassInstances, uint NumClassInstances);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnPSSetShader(IntPtr self, IntPtr pPixelShader, IntPtr ppClassInstances, uint NumClassInstances);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnPSSetShaderResources(IntPtr self, uint StartSlot, uint NumViews, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppSRVs);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnPSSetSamplers(IntPtr self, uint StartSlot, uint NumSamplers, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppSamplers);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnVSSetConstantBuffers(IntPtr self, uint StartSlot, uint NumBuffers, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppCBs);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnPSSetConstantBuffers(IntPtr self, uint StartSlot, uint NumBuffers, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] IntPtr[] ppCBs);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnDrawIndexed(IntPtr self, uint IndexCount, uint StartIndexLocation, int BaseVertexLocation);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnMap(IntPtr self, IntPtr pResource, uint Subresource, uint MapType, uint MapFlags, out D3D11_MAPPED_SUBRESOURCE pMappedResource);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnUnmap(IntPtr self, IntPtr pResource, uint Subresource);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnCopyResource(IntPtr self, IntPtr pDstResource, IntPtr pSrcResource);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnUpdateSubresource(IntPtr self, IntPtr pDstResource, uint DstSubresource, IntPtr pDstBox, IntPtr pSrcData, uint SrcRowPitch, uint SrcDepthPitch);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnClearState(IntPtr self);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate void FnFlush(IntPtr self);

	private const int Slot_VSSetConstantBuffers = 7;

	private const int Slot_PSSetShaderResources = 8;

	private const int Slot_PSSetShader = 9;

	private const int Slot_PSSetSamplers = 10;

	private const int Slot_VSSetShader = 11;

	private const int Slot_DrawIndexed = 12;

	private const int Slot_Map = 14;

	private const int Slot_Unmap = 15;

	private const int Slot_PSSetConstantBuffers = 16;

	private const int Slot_IASetInputLayout = 17;

	private const int Slot_IASetVertexBuffers = 18;

	private const int Slot_IASetIndexBuffer = 19;

	private const int Slot_IASetPrimitiveTopology = 24;

	private const int Slot_OMSetRenderTargets = 33;

	private const int Slot_OMSetBlendState = 35;

	private const int Slot_RSSetState = 43;

	private const int Slot_RSSetViewports = 44;

	private const int Slot_RSSetScissorRects = 45;

	private const int Slot_CopyResource = 47;

	private const int Slot_UpdateSubresource = 48;

	private const int Slot_ClearRenderTargetView = 50;

	private const int Slot_ClearState = 110;

	private const int Slot_Flush = 111;

	public static void ClearRenderTargetView(IntPtr ctx, IntPtr rtv, float[] color)
	{
		((FnClearRenderTargetView)VTable.Get(ctx, 50, typeof(FnClearRenderTargetView)))(ctx, rtv, color);
	}

	public static void OMSetRenderTargets(IntPtr ctx, IntPtr rtv)
	{
		((FnOMSetRenderTargets)VTable.Get(ctx, 33, typeof(FnOMSetRenderTargets)))(ctx, 1u, new IntPtr[1] { rtv }, IntPtr.Zero);
	}

	public static void OMSetBlendState(IntPtr ctx, IntPtr blendState)
	{
		((FnOMSetBlendState)VTable.Get(ctx, 35, typeof(FnOMSetBlendState)))(ctx, blendState, IntPtr.Zero, uint.MaxValue);
	}

	public static void RSSetViewports(IntPtr ctx, D3D11_VIEWPORT vp)
	{
		((FnRSSetViewports)VTable.Get(ctx, 44, typeof(FnRSSetViewports)))(ctx, 1u, new D3D11_VIEWPORT[1] { vp });
	}

	public static void RSSetScissorRects(IntPtr ctx, D3D11_RECT rect)
	{
		((FnRSSetScissorRects)VTable.Get(ctx, 45, typeof(FnRSSetScissorRects)))(ctx, 1u, new D3D11_RECT[1] { rect });
	}

	public static void RSSetState(IntPtr ctx, IntPtr state)
	{
		((FnRSSetState)VTable.Get(ctx, 43, typeof(FnRSSetState)))(ctx, state);
	}

	public static void IASetInputLayout(IntPtr ctx, IntPtr layout)
	{
		((FnIASetInputLayout)VTable.Get(ctx, 17, typeof(FnIASetInputLayout)))(ctx, layout);
	}

	public static void IASetVertexBuffers(IntPtr ctx, IntPtr buffer, uint stride)
	{
		((FnIASetVertexBuffers)VTable.Get(ctx, 18, typeof(FnIASetVertexBuffers)))(ctx, 0u, 1u, new IntPtr[1] { buffer }, new uint[1] { stride }, new uint[1]);
	}

	public static void IASetIndexBuffer(IntPtr ctx, IntPtr buffer)
	{
		((FnIASetIndexBuffer)VTable.Get(ctx, 19, typeof(FnIASetIndexBuffer)))(ctx, buffer, 42u, 0u);
	}

	public static void IASetPrimitiveTopology(IntPtr ctx)
	{
		((FnIASetPrimitiveTopology)VTable.Get(ctx, 24, typeof(FnIASetPrimitiveTopology)))(ctx, 4u);
	}

	public static void VSSetShader(IntPtr ctx, IntPtr vs)
	{
		((FnVSSetShader)VTable.Get(ctx, 11, typeof(FnVSSetShader)))(ctx, vs, IntPtr.Zero, 0u);
	}

	public static void PSSetShader(IntPtr ctx, IntPtr ps)
	{
		((FnPSSetShader)VTable.Get(ctx, 9, typeof(FnPSSetShader)))(ctx, ps, IntPtr.Zero, 0u);
	}

	public static void PSSetShaderResources(IntPtr ctx, uint slot, IntPtr srv)
	{
		((FnPSSetShaderResources)VTable.Get(ctx, 8, typeof(FnPSSetShaderResources)))(ctx, slot, 1u, new IntPtr[1] { srv });
	}

	public static void PSClearShaderResource(IntPtr ctx, uint slot)
	{
		((FnPSSetShaderResources)VTable.Get(ctx, 8, typeof(FnPSSetShaderResources)))(ctx, slot, 1u, new IntPtr[1] { IntPtr.Zero });
	}

	public static void PSSetSamplers(IntPtr ctx, IntPtr sampler)
	{
		((FnPSSetSamplers)VTable.Get(ctx, 10, typeof(FnPSSetSamplers)))(ctx, 0u, 1u, new IntPtr[1] { sampler });
	}

	public static void VSSetConstantBuffers(IntPtr ctx, uint slot, IntPtr cb)
	{
		((FnVSSetConstantBuffers)VTable.Get(ctx, 7, typeof(FnVSSetConstantBuffers)))(ctx, slot, 1u, new IntPtr[1] { cb });
	}

	public static void PSSetConstantBuffers(IntPtr ctx, uint slot, IntPtr cb)
	{
		((FnPSSetConstantBuffers)VTable.Get(ctx, 16, typeof(FnPSSetConstantBuffers)))(ctx, slot, 1u, new IntPtr[1] { cb });
	}

	public static void DrawIndexed(IntPtr ctx, int indexCount)
	{
		((FnDrawIndexed)VTable.Get(ctx, 12, typeof(FnDrawIndexed)))(ctx, (uint)indexCount, 0u, 0);
	}

	public static int Map(IntPtr ctx, IntPtr resource, uint mapType, out D3D11_MAPPED_SUBRESOURCE mapped)
	{
		return ((FnMap)VTable.Get(ctx, 14, typeof(FnMap)))(ctx, resource, 0u, mapType, 0u, out mapped);
	}

	public static void Unmap(IntPtr ctx, IntPtr resource)
	{
		((FnUnmap)VTable.Get(ctx, 15, typeof(FnUnmap)))(ctx, resource, 0u);
	}

	public static void CopyResource(IntPtr ctx, IntPtr dst, IntPtr src)
	{
		((FnCopyResource)VTable.Get(ctx, 47, typeof(FnCopyResource)))(ctx, dst, src);
	}

	public static void UpdateSubresource(IntPtr ctx, IntPtr resource, IntPtr data, uint rowPitch)
	{
		((FnUpdateSubresource)VTable.Get(ctx, 48, typeof(FnUpdateSubresource)))(ctx, resource, 0u, IntPtr.Zero, data, rowPitch, 0u);
	}

	public static void ClearState(IntPtr ctx)
	{
		((FnClearState)VTable.Get(ctx, 110, typeof(FnClearState)))(ctx);
	}

	public static void Flush(IntPtr ctx)
	{
		((FnFlush)VTable.Get(ctx, 111, typeof(FnFlush)))(ctx);
	}
}
