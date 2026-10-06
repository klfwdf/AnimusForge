using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBActionSet : IMBActionSet
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool AreActionsAlternativesDelegate(int index, int actionNo1, int actionNo2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAnimationNameDelegate(int index, int actionNo);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetBoneHasParentBoneDelegate(byte[] actionSetId, sbyte boneIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate sbyte GetBoneIndexWithIdDelegate(byte[] actionSetId, byte[] boneId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetIndexWithIDDelegate(byte[] id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameWithIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfActionSetsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfMonsterUsageSetsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSkeletonNameDelegate(int index);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AreActionsAlternativesDelegate call_AreActionsAlternativesDelegate;

	public static GetAnimationNameDelegate call_GetAnimationNameDelegate;

	public static GetBoneHasParentBoneDelegate call_GetBoneHasParentBoneDelegate;

	public static GetBoneIndexWithIdDelegate call_GetBoneIndexWithIdDelegate;

	public static GetIndexWithIDDelegate call_GetIndexWithIDDelegate;

	public static GetNameWithIndexDelegate call_GetNameWithIndexDelegate;

	public static GetNumberOfActionSetsDelegate call_GetNumberOfActionSetsDelegate;

	public static GetNumberOfMonsterUsageSetsDelegate call_GetNumberOfMonsterUsageSetsDelegate;

	public static GetSkeletonNameDelegate call_GetSkeletonNameDelegate;

	public bool AreActionsAlternatives(int index, int actionNo1, int actionNo2)
	{
		return call_AreActionsAlternativesDelegate(index, actionNo1, actionNo2);
	}

	public string GetAnimationName(int index, int actionNo)
	{
		if (call_GetAnimationNameDelegate(index, actionNo) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool GetBoneHasParentBone(string actionSetId, sbyte boneIndex)
	{
		byte[] array = null;
		if (actionSetId != null)
		{
			int byteCount = _utf8.GetByteCount(actionSetId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(actionSetId, 0, actionSetId.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetBoneHasParentBoneDelegate(array, boneIndex);
	}

	public sbyte GetBoneIndexWithId(string actionSetId, string boneId)
	{
		byte[] array = null;
		if (actionSetId != null)
		{
			int byteCount = _utf8.GetByteCount(actionSetId);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(actionSetId, 0, actionSetId.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (boneId != null)
		{
			int byteCount2 = _utf8.GetByteCount(boneId);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(boneId, 0, boneId.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		return call_GetBoneIndexWithIdDelegate(array, array2);
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

	public string GetNameWithIndex(int index)
	{
		if (call_GetNameWithIndexDelegate(index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetNumberOfActionSets()
	{
		return call_GetNumberOfActionSetsDelegate();
	}

	public int GetNumberOfMonsterUsageSets()
	{
		return call_GetNumberOfMonsterUsageSetsDelegate();
	}

	public string GetSkeletonName(int index)
	{
		if (call_GetSkeletonNameDelegate(index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}
}
