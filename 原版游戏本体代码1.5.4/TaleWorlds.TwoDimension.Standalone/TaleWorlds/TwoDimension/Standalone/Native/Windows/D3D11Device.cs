using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class D3D11Device
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateBuffer(IntPtr self, ref D3D11_BUFFER_DESC pDesc, IntPtr pInitialData, out IntPtr ppBuffer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateTexture2D(IntPtr self, ref D3D11_TEXTURE2D_DESC pDesc, ref D3D11_SUBRESOURCE_DATA pInitialData, out IntPtr ppTexture2D);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateTexture2DNoData(IntPtr self, ref D3D11_TEXTURE2D_DESC pDesc, IntPtr pInitialData, out IntPtr ppTexture2D);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateShaderResourceView(IntPtr self, IntPtr pResource, IntPtr pDesc, out IntPtr ppSRV);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateRenderTargetView(IntPtr self, IntPtr pResource, IntPtr pDesc, out IntPtr ppRTV);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateVertexShader(IntPtr self, IntPtr pShaderBytecode, UIntPtr bytecodeLength, IntPtr pClassLinkage, out IntPtr ppVertexShader);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreatePixelShader(IntPtr self, IntPtr pShaderBytecode, UIntPtr bytecodeLength, IntPtr pClassLinkage, out IntPtr ppPixelShader);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateInputLayout(IntPtr self, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] D3D11_INPUT_ELEMENT_DESC[] pInputElementDescs, uint numElements, IntPtr pShaderBytecodeWithInputSignature, UIntPtr bytecodeLength, out IntPtr ppInputLayout);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateBlendState(IntPtr self, ref D3D11_BLEND_DESC pBlendStateDesc, out IntPtr ppBlendState);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateRasterizerState(IntPtr self, ref D3D11_RASTERIZER_DESC pRasterizerDesc, out IntPtr ppRasterizerState);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnCreateSamplerState(IntPtr self, ref D3D11_SAMPLER_DESC pSamplerDesc, out IntPtr ppSamplerState);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int FnGetDeviceRemovedReason(IntPtr self);

	private const int Slot_CreateBuffer = 3;

	private const int Slot_CreateTexture1D = 4;

	private const int Slot_CreateTexture2D = 5;

	private const int Slot_CreateTexture3D = 6;

	private const int Slot_CreateShaderResourceView = 7;

	private const int Slot_CreateUnorderedAccessView = 8;

	private const int Slot_CreateRenderTargetView = 9;

	private const int Slot_CreateDepthStencilView = 10;

	private const int Slot_CreateInputLayout = 11;

	private const int Slot_CreateVertexShader = 12;

	private const int Slot_CreateGeometryShader = 13;

	private const int Slot_CreateGSWithSO = 14;

	private const int Slot_CreatePixelShader = 15;

	private const int Slot_CreateHullShader = 16;

	private const int Slot_CreateDomainShader = 17;

	private const int Slot_CreateComputeShader = 18;

	private const int Slot_CreateClassLinkage = 19;

	private const int Slot_CreateBlendState = 20;

	private const int Slot_CreateDepthStencilState = 21;

	private const int Slot_CreateRasterizerState = 22;

	private const int Slot_CreateSamplerState = 23;

	private const int Slot_GetDeviceRemovedReason = 40;

	public static int CreateBuffer(IntPtr device, ref D3D11_BUFFER_DESC desc, out IntPtr buffer)
	{
		return ((FnCreateBuffer)VTable.Get(device, 3, typeof(FnCreateBuffer)))(device, ref desc, IntPtr.Zero, out buffer);
	}

	public static int CreateTexture2D(IntPtr device, ref D3D11_TEXTURE2D_DESC desc, ref D3D11_SUBRESOURCE_DATA initialData, out IntPtr texture)
	{
		return ((FnCreateTexture2D)VTable.Get(device, 5, typeof(FnCreateTexture2D)))(device, ref desc, ref initialData, out texture);
	}

	public static int CreateTexture2DEmpty(IntPtr device, ref D3D11_TEXTURE2D_DESC desc, out IntPtr texture)
	{
		return ((FnCreateTexture2DNoData)VTable.Get(device, 5, typeof(FnCreateTexture2DNoData)))(device, ref desc, IntPtr.Zero, out texture);
	}

	public static int CreateShaderResourceView(IntPtr device, IntPtr resource, out IntPtr srv)
	{
		return ((FnCreateShaderResourceView)VTable.Get(device, 7, typeof(FnCreateShaderResourceView)))(device, resource, IntPtr.Zero, out srv);
	}

	public static int CreateRenderTargetView(IntPtr device, IntPtr resource, out IntPtr rtv)
	{
		return ((FnCreateRenderTargetView)VTable.Get(device, 9, typeof(FnCreateRenderTargetView)))(device, resource, IntPtr.Zero, out rtv);
	}

	public static int CreateVertexShader(IntPtr device, IntPtr bytecode, int bytecodeLen, out IntPtr vs)
	{
		return ((FnCreateVertexShader)VTable.Get(device, 12, typeof(FnCreateVertexShader)))(device, bytecode, (UIntPtr)(ulong)bytecodeLen, IntPtr.Zero, out vs);
	}

	public static int CreatePixelShader(IntPtr device, IntPtr bytecode, int bytecodeLen, out IntPtr ps)
	{
		return ((FnCreatePixelShader)VTable.Get(device, 15, typeof(FnCreatePixelShader)))(device, bytecode, (UIntPtr)(ulong)bytecodeLen, IntPtr.Zero, out ps);
	}

	public static int CreateInputLayout(IntPtr device, D3D11_INPUT_ELEMENT_DESC[] elements, IntPtr vsBytecode, int vsLen, out IntPtr inputLayout)
	{
		return ((FnCreateInputLayout)VTable.Get(device, 11, typeof(FnCreateInputLayout)))(device, elements, (uint)elements.Length, vsBytecode, (UIntPtr)(ulong)vsLen, out inputLayout);
	}

	public static int CreateBlendState(IntPtr device, ref D3D11_BLEND_DESC desc, out IntPtr blendState)
	{
		return ((FnCreateBlendState)VTable.Get(device, 20, typeof(FnCreateBlendState)))(device, ref desc, out blendState);
	}

	public static int CreateRasterizerState(IntPtr device, ref D3D11_RASTERIZER_DESC desc, out IntPtr rasterizerState)
	{
		return ((FnCreateRasterizerState)VTable.Get(device, 22, typeof(FnCreateRasterizerState)))(device, ref desc, out rasterizerState);
	}

	public static int CreateSamplerState(IntPtr device, ref D3D11_SAMPLER_DESC desc, out IntPtr samplerState)
	{
		return ((FnCreateSamplerState)VTable.Get(device, 23, typeof(FnCreateSamplerState)))(device, ref desc, out samplerState);
	}

	public static int GetDeviceRemovedReason(IntPtr device)
	{
		return ((FnGetDeviceRemovedReason)VTable.Get(device, 40, typeof(FnGetDeviceRemovedReason)))(device);
	}
}
