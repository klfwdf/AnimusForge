using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class D3D11Native
{
	public const int D3D11_SDK_VERSION = 7;

	public const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 32u;

	public const int D3D_FEATURE_LEVEL_11_0 = 45056;

	public const int D3D_FEATURE_LEVEL_10_1 = 41216;

	public const int D3D_FEATURE_LEVEL_10_0 = 40960;

	public const uint D3D11_BIND_VERTEX_BUFFER = 1u;

	public const uint D3D11_BIND_INDEX_BUFFER = 2u;

	public const uint D3D11_BIND_CONSTANT_BUFFER = 4u;

	public const uint D3D11_BIND_SHADER_RESOURCE = 8u;

	public const uint D3D11_BIND_RENDER_TARGET = 32u;

	public const uint D3D11_USAGE_DEFAULT = 0u;

	public const uint D3D11_USAGE_DYNAMIC = 2u;

	public const uint D3D11_USAGE_STAGING = 3u;

	public const uint D3D11_CPU_ACCESS_WRITE = 65536u;

	public const uint D3D11_CPU_ACCESS_READ = 131072u;

	public const uint D3D11_MAP_READ = 1u;

	public const uint D3D11_MAP_WRITE = 2u;

	public const uint D3D11_MAP_READ_WRITE = 3u;

	public const uint D3D11_MAP_WRITE_DISCARD = 4u;

	public const uint D3D11_MAP_WRITE_NO_OVERWRITE = 5u;

	public const uint D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST = 4u;

	public const uint D3D11_BLEND_ZERO = 1u;

	public const uint D3D11_BLEND_ONE = 2u;

	public const uint D3D11_BLEND_SRC_ALPHA = 5u;

	public const uint D3D11_BLEND_INV_SRC_ALPHA = 6u;

	public const uint D3D11_BLEND_OP_ADD = 1u;

	public const uint D3D11_TEXTURE_ADDRESS_WRAP = 1u;

	public const uint D3D11_TEXTURE_ADDRESS_CLAMP = 3u;

	public const uint D3D11_FILTER_MIN_MAG_MIP_LINEAR = 21u;

	public const uint D3D11_FILL_SOLID = 3u;

	public const uint D3D11_CULL_NONE = 1u;

	public const uint DXGI_FORMAT_UNKNOWN = 0u;

	public const uint DXGI_FORMAT_R32G32_FLOAT = 16u;

	public const uint DXGI_FORMAT_R8G8B8A8_UNORM = 28u;

	public const uint DXGI_FORMAT_R32_UINT = 42u;

	public const uint DXGI_FORMAT_R8_UNORM = 61u;

	public const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87u;

	public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 32u;

	public const uint DXGI_SWAP_EFFECT_DISCARD = 0u;

	public const int DXGI_ERROR_DEVICE_REMOVED = -2005270523;

	public const int DXGI_ERROR_DEVICE_RESET = -2005270521;

	[DllImport("d3d11.dll")]
	public static extern int D3D11CreateDevice(IntPtr pAdapter, int driverType, IntPtr software, uint flags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] pFeatureLevels, int featureLevelCount, int sdkVersion, out IntPtr ppDevice, out int pFeatureLevel, out IntPtr ppImmediateContext);

	[DllImport("d3d11.dll")]
	public static extern int D3D11CreateDeviceAndSwapChain(IntPtr pAdapter, int driverType, IntPtr software, uint flags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] int[] pFeatureLevels, int featureLevelCount, int sdkVersion, ref DXGI_SWAP_CHAIN_DESC pSwapChainDesc, out IntPtr ppSwapChain, out IntPtr ppDevice, out int pFeatureLevel, out IntPtr ppImmediateContext);
}
