using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBVoiceManager : IMBVoiceManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate(byte[] className);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate(byte[] className, IntPtr definitionIndices);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVoiceTypeIndexDelegate(byte[] voiceType);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate call_GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate;

	public static GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate call_GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate;

	public static GetVoiceTypeIndexDelegate call_GetVoiceTypeIndexDelegate;

	public int GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassName(string className)
	{
		byte[] array = null;
		if (className != null)
		{
			int byteCount = _utf8.GetByteCount(className);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(className, 0, className.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassNameDelegate(array);
	}

	public void GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassName(string className, int[] definitionIndices)
	{
		byte[] array = null;
		if (className != null)
		{
			int byteCount = _utf8.GetByteCount(className);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(className, 0, className.Length, array, 0);
			array[byteCount] = 0;
		}
		PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(definitionIndices);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassNameDelegate(array, pointer);
		pinnedArrayData.Dispose();
	}

	public int GetVoiceTypeIndex(string voiceType)
	{
		byte[] array = null;
		if (voiceType != null)
		{
			int byteCount = _utf8.GetByteCount(voiceType);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(voiceType, 0, voiceType.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetVoiceTypeIndexDelegate(array);
	}
}
