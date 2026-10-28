using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIPath : IPath
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddPathPointDelegate(UIntPtr ptr, int newNodeIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeletePathPointDelegate(UIntPtr ptr, int newNodeIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetArcLengthDelegate(UIntPtr ptr, int firstPoint);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetHermiteFrameAndColorForDistanceDelegate(UIntPtr ptr, out MatrixFrame frame, out Vec3 color, float distance);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetHermiteFrameForDistanceDelegate(UIntPtr ptr, ref MatrixFrame frame, float distance);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetHermiteFrameForDtDelegate(UIntPtr ptr, ref MatrixFrame frame, float phase, int firstPoint);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNearestHermiteFrameWithValidAlphaForDistanceDelegate(UIntPtr ptr, ref MatrixFrame frame, float distance, [MarshalAs(UnmanagedType.U1)] bool searchForward, float alphaThreshold);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfPointsDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetPointsDelegate(UIntPtr ptr, IntPtr points);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetTotalLengthDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVersionDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasValidAlphaAtPathPointDelegate(UIntPtr ptr, int nodeIndex, float alphaThreshold);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameOfPointDelegate(UIntPtr ptr, int pointIndex, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTangentPositionOfPointDelegate(UIntPtr ptr, int pointIndex, int tangentIndex, ref Vec3 position);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddPathPointDelegate call_AddPathPointDelegate;

	public static DeletePathPointDelegate call_DeletePathPointDelegate;

	public static GetArcLengthDelegate call_GetArcLengthDelegate;

	public static GetHermiteFrameAndColorForDistanceDelegate call_GetHermiteFrameAndColorForDistanceDelegate;

	public static GetHermiteFrameForDistanceDelegate call_GetHermiteFrameForDistanceDelegate;

	public static GetHermiteFrameForDtDelegate call_GetHermiteFrameForDtDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetNearestHermiteFrameWithValidAlphaForDistanceDelegate call_GetNearestHermiteFrameWithValidAlphaForDistanceDelegate;

	public static GetNumberOfPointsDelegate call_GetNumberOfPointsDelegate;

	public static GetPointsDelegate call_GetPointsDelegate;

	public static GetTotalLengthDelegate call_GetTotalLengthDelegate;

	public static GetVersionDelegate call_GetVersionDelegate;

	public static HasValidAlphaAtPathPointDelegate call_HasValidAlphaAtPathPointDelegate;

	public static SetFrameOfPointDelegate call_SetFrameOfPointDelegate;

	public static SetTangentPositionOfPointDelegate call_SetTangentPositionOfPointDelegate;

	public int AddPathPoint(UIntPtr ptr, int newNodeIndex)
	{
		return call_AddPathPointDelegate(ptr, newNodeIndex);
	}

	public void DeletePathPoint(UIntPtr ptr, int newNodeIndex)
	{
		call_DeletePathPointDelegate(ptr, newNodeIndex);
	}

	public float GetArcLength(UIntPtr ptr, int firstPoint)
	{
		return call_GetArcLengthDelegate(ptr, firstPoint);
	}

	public void GetHermiteFrameAndColorForDistance(UIntPtr ptr, out MatrixFrame frame, out Vec3 color, float distance)
	{
		call_GetHermiteFrameAndColorForDistanceDelegate(ptr, out frame, out color, distance);
	}

	public void GetHermiteFrameForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance)
	{
		call_GetHermiteFrameForDistanceDelegate(ptr, ref frame, distance);
	}

	public void GetHermiteFrameForDt(UIntPtr ptr, ref MatrixFrame frame, float phase, int firstPoint)
	{
		call_GetHermiteFrameForDtDelegate(ptr, ref frame, phase, firstPoint);
	}

	public string GetName(UIntPtr ptr)
	{
		if (call_GetNameDelegate(ptr) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetNearestHermiteFrameWithValidAlphaForDistance(UIntPtr ptr, ref MatrixFrame frame, float distance, bool searchForward, float alphaThreshold)
	{
		call_GetNearestHermiteFrameWithValidAlphaForDistanceDelegate(ptr, ref frame, distance, searchForward, alphaThreshold);
	}

	public int GetNumberOfPoints(UIntPtr ptr)
	{
		return call_GetNumberOfPointsDelegate(ptr);
	}

	public void GetPoints(UIntPtr ptr, MatrixFrame[] points)
	{
		PinnedArrayData<MatrixFrame> pinnedArrayData = new PinnedArrayData<MatrixFrame>(points);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetPointsDelegate(ptr, pointer);
		pinnedArrayData.Dispose();
	}

	public float GetTotalLength(UIntPtr ptr)
	{
		return call_GetTotalLengthDelegate(ptr);
	}

	public int GetVersion(UIntPtr ptr)
	{
		return call_GetVersionDelegate(ptr);
	}

	public bool HasValidAlphaAtPathPoint(UIntPtr ptr, int nodeIndex, float alphaThreshold)
	{
		return call_HasValidAlphaAtPathPointDelegate(ptr, nodeIndex, alphaThreshold);
	}

	public void SetFrameOfPoint(UIntPtr ptr, int pointIndex, ref MatrixFrame frame)
	{
		call_SetFrameOfPointDelegate(ptr, pointIndex, ref frame);
	}

	public void SetTangentPositionOfPoint(UIntPtr ptr, int pointIndex, int tangentIndex, ref Vec3 position)
	{
		call_SetTangentPositionOfPointDelegate(ptr, pointIndex, tangentIndex, ref position);
	}
}
