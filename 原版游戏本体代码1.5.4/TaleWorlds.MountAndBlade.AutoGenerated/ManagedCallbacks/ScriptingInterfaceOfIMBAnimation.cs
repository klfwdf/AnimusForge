using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBAnimation : IMBAnimation
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AnimationIndexOfActionCodeDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckAnimationClipExistsDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetActionAnimationDurationDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetActionBlendOutStartProgressDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetActionCodeWithNameDelegate(byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetActionNameWithCodeDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Agent.ActionCodeType GetActionTypeDelegate(int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAnimationBlendInPeriodDelegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationBlendsWithActionIndexDelegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationContinueToActionDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetAnimationDisplacementAtProgressDelegate(int animationIndex, float progress);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAnimationDurationDelegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate AnimFlags GetAnimationFlagsDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationNameDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAnimationParameter1Delegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAnimationParameter2Delegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAnimationParameter3Delegate(int animationIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetDisplacementVectorDelegate(int actionSetNo, int actionIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetIDWithIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetIndexWithIDDelegate(byte[] id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumActionCodesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumAnimationsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsAnyAnimationLoadingFromDiskDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PrefetchAnimationClipDelegate(int actionSetNo, int actionIndex);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AnimationIndexOfActionCodeDelegate call_AnimationIndexOfActionCodeDelegate;

	public static CheckAnimationClipExistsDelegate call_CheckAnimationClipExistsDelegate;

	public static GetActionAnimationDurationDelegate call_GetActionAnimationDurationDelegate;

	public static GetActionBlendOutStartProgressDelegate call_GetActionBlendOutStartProgressDelegate;

	public static GetActionCodeWithNameDelegate call_GetActionCodeWithNameDelegate;

	public static GetActionNameWithCodeDelegate call_GetActionNameWithCodeDelegate;

	public static GetActionTypeDelegate call_GetActionTypeDelegate;

	public static GetAnimationBlendInPeriodDelegate call_GetAnimationBlendInPeriodDelegate;

	public static GetAnimationBlendsWithActionIndexDelegate call_GetAnimationBlendsWithActionIndexDelegate;

	public static GetAnimationContinueToActionDelegate call_GetAnimationContinueToActionDelegate;

	public static GetAnimationDisplacementAtProgressDelegate call_GetAnimationDisplacementAtProgressDelegate;

	public static GetAnimationDurationDelegate call_GetAnimationDurationDelegate;

	public static GetAnimationFlagsDelegate call_GetAnimationFlagsDelegate;

	public static GetAnimationNameDelegate call_GetAnimationNameDelegate;

	public static GetAnimationParameter1Delegate call_GetAnimationParameter1Delegate;

	public static GetAnimationParameter2Delegate call_GetAnimationParameter2Delegate;

	public static GetAnimationParameter3Delegate call_GetAnimationParameter3Delegate;

	public static GetDisplacementVectorDelegate call_GetDisplacementVectorDelegate;

	public static GetIDWithIndexDelegate call_GetIDWithIndexDelegate;

	public static GetIndexWithIDDelegate call_GetIndexWithIDDelegate;

	public static GetNumActionCodesDelegate call_GetNumActionCodesDelegate;

	public static GetNumAnimationsDelegate call_GetNumAnimationsDelegate;

	public static IsAnyAnimationLoadingFromDiskDelegate call_IsAnyAnimationLoadingFromDiskDelegate;

	public static PrefetchAnimationClipDelegate call_PrefetchAnimationClipDelegate;

	public int AnimationIndexOfActionCode(int actionSetNo, int actionIndex)
	{
		return call_AnimationIndexOfActionCodeDelegate(actionSetNo, actionIndex);
	}

	public bool CheckAnimationClipExists(int actionSetNo, int actionIndex)
	{
		return call_CheckAnimationClipExistsDelegate(actionSetNo, actionIndex);
	}

	public float GetActionAnimationDuration(int actionSetNo, int actionIndex)
	{
		return call_GetActionAnimationDurationDelegate(actionSetNo, actionIndex);
	}

	public float GetActionBlendOutStartProgress(int actionSetNo, int actionIndex)
	{
		return call_GetActionBlendOutStartProgressDelegate(actionSetNo, actionIndex);
	}

	public int GetActionCodeWithName(string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetActionCodeWithNameDelegate(array);
	}

	public string GetActionNameWithCode(int index)
	{
		if (call_GetActionNameWithCodeDelegate(index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public Agent.ActionCodeType GetActionType(int actionIndex)
	{
		return call_GetActionTypeDelegate(actionIndex);
	}

	public float GetAnimationBlendInPeriod(int animationIndex)
	{
		return call_GetAnimationBlendInPeriodDelegate(animationIndex);
	}

	public int GetAnimationBlendsWithActionIndex(int animationIndex)
	{
		return call_GetAnimationBlendsWithActionIndexDelegate(animationIndex);
	}

	public int GetAnimationContinueToAction(int actionSetNo, int actionIndex)
	{
		return call_GetAnimationContinueToActionDelegate(actionSetNo, actionIndex);
	}

	public Vec3 GetAnimationDisplacementAtProgress(int animationIndex, float progress)
	{
		return call_GetAnimationDisplacementAtProgressDelegate(animationIndex, progress);
	}

	public float GetAnimationDuration(int animationIndex)
	{
		return call_GetAnimationDurationDelegate(animationIndex);
	}

	public AnimFlags GetAnimationFlags(int actionSetNo, int actionIndex)
	{
		return call_GetAnimationFlagsDelegate(actionSetNo, actionIndex);
	}

	public string GetAnimationName(int actionSetNo, int actionIndex)
	{
		if (call_GetAnimationNameDelegate(actionSetNo, actionIndex) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetAnimationParameter1(int animationIndex)
	{
		return call_GetAnimationParameter1Delegate(animationIndex);
	}

	public float GetAnimationParameter2(int animationIndex)
	{
		return call_GetAnimationParameter2Delegate(animationIndex);
	}

	public float GetAnimationParameter3(int animationIndex)
	{
		return call_GetAnimationParameter3Delegate(animationIndex);
	}

	public Vec3 GetDisplacementVector(int actionSetNo, int actionIndex)
	{
		return call_GetDisplacementVectorDelegate(actionSetNo, actionIndex);
	}

	public string GetIDWithIndex(int index)
	{
		if (call_GetIDWithIndexDelegate(index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetIndexWithID(string id)
	{
		byte[] array = null;
		if (id != null)
		{
			int byteCount = _utf8.GetByteCount(id);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(id, 0, id.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetIndexWithIDDelegate(array);
	}

	public int GetNumActionCodes()
	{
		return call_GetNumActionCodesDelegate();
	}

	public int GetNumAnimations()
	{
		return call_GetNumAnimationsDelegate();
	}

	public bool IsAnyAnimationLoadingFromDisk()
	{
		return call_IsAnyAnimationLoadingFromDiskDelegate();
	}

	public void PrefetchAnimationClip(int actionSetNo, int actionIndex)
	{
		call_PrefetchAnimationClipDelegate(actionSetNo, actionIndex);
	}
}
