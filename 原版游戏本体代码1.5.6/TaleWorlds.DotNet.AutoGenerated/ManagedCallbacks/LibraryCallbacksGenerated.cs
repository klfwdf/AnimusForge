using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

internal static class LibraryCallbacksGenerated
{
	internal delegate void DotNetObject_DecreaseReferenceCount_delegate(int dotnetObjectId);

	internal delegate int DotNetObject_GetAliveDotNetObjectCount_delegate();

	internal delegate UIntPtr DotNetObject_GetAliveDotNetObjectNames_delegate();

	internal delegate void DotNetObject_IncreaseReferenceCount_delegate(int dotnetObjectId);

	internal delegate void Managed_ApplicationTick_delegate(float dt);

	internal delegate void Managed_ApplicationTickLight_delegate(float dt);

	internal delegate UIntPtr Managed_CallCommandlineFunction_delegate(IntPtr functionName, IntPtr arguments);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Managed_CheckClassNameIsValid_delegate(IntPtr className);

	internal delegate void Managed_CheckSharedStructureSizes_delegate();

	internal delegate int Managed_CreateCustomParameterStringArray_delegate(int length);

	internal delegate int Managed_CreateObjectClassInstanceWithInteger_delegate(IntPtr className, int value);

	internal delegate int Managed_CreateObjectClassInstanceWithPointer_delegate(IntPtr className, IntPtr pointer);

	internal delegate void Managed_EngineApiMethodInterfaceInitializer_delegate(int id, IntPtr pointer);

	internal delegate void Managed_FillEngineApiPointers_delegate();

	internal delegate void Managed_GarbageCollect_delegate([MarshalAs(UnmanagedType.U1)] bool forceTimer);

	internal delegate int Managed_GetClassFields_delegate(IntPtr className, [MarshalAs(UnmanagedType.U1)] bool recursive, [MarshalAs(UnmanagedType.U1)] bool includeInternal, [MarshalAs(UnmanagedType.U1)] bool includeProtected, [MarshalAs(UnmanagedType.U1)] bool includePrivate);

	internal delegate UIntPtr Managed_GetEnumNamesOfField_delegate(uint classNameHash, uint fieldNameHash);

	internal delegate long Managed_GetMemoryUsage_delegate();

	internal delegate UIntPtr Managed_GetModuleList_delegate();

	internal delegate UIntPtr Managed_GetObjectClassName_delegate(IntPtr className);

	internal delegate UIntPtr Managed_GetStackTraceRaw_delegate(int skipCount);

	internal delegate UIntPtr Managed_GetStackTraceStr_delegate(int skipCount);

	internal delegate int Managed_GetStringArrayLength_delegate(int array);

	internal delegate UIntPtr Managed_GetStringArrayValueAtIndex_delegate(int array, int index);

	internal delegate void Managed_GetVersionInts_delegate(ref int major, ref int minor, ref int revision);

	[return: MarshalAs(UnmanagedType.U1)]
	internal delegate bool Managed_IsClassFieldExists_delegate(uint classNameHash, uint fieldNameHash);

	internal delegate void Managed_LoadManagedComponent_delegate(IntPtr assemblyName, IntPtr managedInterface);

	internal delegate void Managed_OnFinalize_delegate();

	internal delegate void Managed_PassCustomCallbackMethodPointers_delegate(IntPtr name, IntPtr initalizer);

	internal delegate void Managed_PreFinalize_delegate();

	internal delegate void Managed_SetClosing_delegate();

	internal delegate void Managed_SetCurrentStringReturnValue_delegate(IntPtr pointer);

	internal delegate void Managed_SetCurrentStringReturnValueAsUnicode_delegate(IntPtr pointer);

	internal delegate void Managed_SetLogsFolder_delegate(IntPtr logFolder);

	internal delegate void Managed_SetStringArrayValueAtIndex_delegate(int array, int index, IntPtr value);

	internal delegate void ManagedDelegate_InvokeAux_delegate(int thisPointer);

	internal delegate int ManagedObject_GetAliveManagedObjectCount_delegate();

	internal delegate UIntPtr ManagedObject_GetAliveManagedObjectNames_delegate();

	internal delegate UIntPtr ManagedObject_GetClassOfObject_delegate(int thisPointer);

	internal delegate UIntPtr ManagedObject_GetCreationCallstack_delegate(IntPtr name);

	internal delegate int NativeObject_GetAliveNativeObjectCount_delegate();

	internal delegate UIntPtr NativeObject_GetAliveNativeObjectNames_delegate();

	internal static Delegate[] Delegates { get; private set; }

	public static void Initialize()
	{
		Delegates = new Delegate[42];
		Delegates[0] = new DotNetObject_DecreaseReferenceCount_delegate(DotNetObject_DecreaseReferenceCount);
		Delegates[1] = new DotNetObject_GetAliveDotNetObjectCount_delegate(DotNetObject_GetAliveDotNetObjectCount);
		Delegates[2] = new DotNetObject_GetAliveDotNetObjectNames_delegate(DotNetObject_GetAliveDotNetObjectNames);
		Delegates[3] = new DotNetObject_IncreaseReferenceCount_delegate(DotNetObject_IncreaseReferenceCount);
		Delegates[4] = new Managed_ApplicationTick_delegate(Managed_ApplicationTick);
		Delegates[5] = new Managed_ApplicationTickLight_delegate(Managed_ApplicationTickLight);
		Delegates[6] = new Managed_CallCommandlineFunction_delegate(Managed_CallCommandlineFunction);
		Delegates[7] = new Managed_CheckClassNameIsValid_delegate(Managed_CheckClassNameIsValid);
		Delegates[8] = new Managed_CheckSharedStructureSizes_delegate(Managed_CheckSharedStructureSizes);
		Delegates[9] = new Managed_CreateCustomParameterStringArray_delegate(Managed_CreateCustomParameterStringArray);
		Delegates[10] = new Managed_CreateObjectClassInstanceWithInteger_delegate(Managed_CreateObjectClassInstanceWithInteger);
		Delegates[11] = new Managed_CreateObjectClassInstanceWithPointer_delegate(Managed_CreateObjectClassInstanceWithPointer);
		Delegates[12] = new Managed_EngineApiMethodInterfaceInitializer_delegate(Managed_EngineApiMethodInterfaceInitializer);
		Delegates[13] = new Managed_FillEngineApiPointers_delegate(Managed_FillEngineApiPointers);
		Delegates[14] = new Managed_GarbageCollect_delegate(Managed_GarbageCollect);
		Delegates[15] = new Managed_GetClassFields_delegate(Managed_GetClassFields);
		Delegates[16] = new Managed_GetEnumNamesOfField_delegate(Managed_GetEnumNamesOfField);
		Delegates[17] = new Managed_GetMemoryUsage_delegate(Managed_GetMemoryUsage);
		Delegates[18] = new Managed_GetModuleList_delegate(Managed_GetModuleList);
		Delegates[19] = new Managed_GetObjectClassName_delegate(Managed_GetObjectClassName);
		Delegates[20] = new Managed_GetStackTraceRaw_delegate(Managed_GetStackTraceRaw);
		Delegates[21] = new Managed_GetStackTraceStr_delegate(Managed_GetStackTraceStr);
		Delegates[22] = new Managed_GetStringArrayLength_delegate(Managed_GetStringArrayLength);
		Delegates[23] = new Managed_GetStringArrayValueAtIndex_delegate(Managed_GetStringArrayValueAtIndex);
		Delegates[24] = new Managed_GetVersionInts_delegate(Managed_GetVersionInts);
		Delegates[25] = new Managed_IsClassFieldExists_delegate(Managed_IsClassFieldExists);
		Delegates[26] = new Managed_LoadManagedComponent_delegate(Managed_LoadManagedComponent);
		Delegates[27] = new Managed_OnFinalize_delegate(Managed_OnFinalize);
		Delegates[28] = new Managed_PassCustomCallbackMethodPointers_delegate(Managed_PassCustomCallbackMethodPointers);
		Delegates[29] = new Managed_PreFinalize_delegate(Managed_PreFinalize);
		Delegates[30] = new Managed_SetClosing_delegate(Managed_SetClosing);
		Delegates[31] = new Managed_SetCurrentStringReturnValue_delegate(Managed_SetCurrentStringReturnValue);
		Delegates[32] = new Managed_SetCurrentStringReturnValueAsUnicode_delegate(Managed_SetCurrentStringReturnValueAsUnicode);
		Delegates[33] = new Managed_SetLogsFolder_delegate(Managed_SetLogsFolder);
		Delegates[34] = new Managed_SetStringArrayValueAtIndex_delegate(Managed_SetStringArrayValueAtIndex);
		Delegates[35] = new ManagedDelegate_InvokeAux_delegate(ManagedDelegate_InvokeAux);
		Delegates[36] = new ManagedObject_GetAliveManagedObjectCount_delegate(ManagedObject_GetAliveManagedObjectCount);
		Delegates[37] = new ManagedObject_GetAliveManagedObjectNames_delegate(ManagedObject_GetAliveManagedObjectNames);
		Delegates[38] = new ManagedObject_GetClassOfObject_delegate(ManagedObject_GetClassOfObject);
		Delegates[39] = new ManagedObject_GetCreationCallstack_delegate(ManagedObject_GetCreationCallstack);
		Delegates[40] = new NativeObject_GetAliveNativeObjectCount_delegate(NativeObject_GetAliveNativeObjectCount);
		Delegates[41] = new NativeObject_GetAliveNativeObjectNames_delegate(NativeObject_GetAliveNativeObjectNames);
	}

	[MonoPInvokeCallback(typeof(DotNetObject_DecreaseReferenceCount_delegate))]
	internal static void DotNetObject_DecreaseReferenceCount(int dotnetObjectId)
	{
		DotNetObject.DecreaseReferenceCount(dotnetObjectId);
	}

	[MonoPInvokeCallback(typeof(DotNetObject_GetAliveDotNetObjectCount_delegate))]
	internal static int DotNetObject_GetAliveDotNetObjectCount()
	{
		return DotNetObject.GetAliveDotNetObjectCount();
	}

	[MonoPInvokeCallback(typeof(DotNetObject_GetAliveDotNetObjectNames_delegate))]
	internal static UIntPtr DotNetObject_GetAliveDotNetObjectNames()
	{
		string aliveDotNetObjectNames = DotNetObject.GetAliveDotNetObjectNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveDotNetObjectNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(DotNetObject_IncreaseReferenceCount_delegate))]
	internal static void DotNetObject_IncreaseReferenceCount(int dotnetObjectId)
	{
		DotNetObject.IncreaseReferenceCount(dotnetObjectId);
	}

	[MonoPInvokeCallback(typeof(Managed_ApplicationTick_delegate))]
	internal static void Managed_ApplicationTick(float dt)
	{
		Managed.ApplicationTick(dt);
	}

	[MonoPInvokeCallback(typeof(Managed_ApplicationTickLight_delegate))]
	internal static void Managed_ApplicationTickLight(float dt)
	{
		Managed.ApplicationTickLight(dt);
	}

	[MonoPInvokeCallback(typeof(Managed_CallCommandlineFunction_delegate))]
	internal static UIntPtr Managed_CallCommandlineFunction(IntPtr functionName, IntPtr arguments)
	{
		string? functionName2 = Marshal.PtrToStringAnsi(functionName);
		string arguments2 = Marshal.PtrToStringAnsi(arguments);
		string text = Managed.CallCommandlineFunction(functionName2, arguments2);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_CheckClassNameIsValid_delegate))]
	internal static bool Managed_CheckClassNameIsValid(IntPtr className)
	{
		return Managed.CheckClassNameIsValid(Marshal.PtrToStringAnsi(className));
	}

	[MonoPInvokeCallback(typeof(Managed_CheckSharedStructureSizes_delegate))]
	internal static void Managed_CheckSharedStructureSizes()
	{
		Managed.CheckSharedStructureSizes();
	}

	[MonoPInvokeCallback(typeof(Managed_CreateCustomParameterStringArray_delegate))]
	internal static int Managed_CreateCustomParameterStringArray(int length)
	{
		return Managed.CreateCustomParameterStringArray(length).GetManagedId();
	}

	[MonoPInvokeCallback(typeof(Managed_CreateObjectClassInstanceWithInteger_delegate))]
	internal static int Managed_CreateObjectClassInstanceWithInteger(IntPtr className, int value)
	{
		return Managed.CreateObjectClassInstanceWithInteger(Marshal.PtrToStringAnsi(className), value)?.GetManagedId() ?? 0;
	}

	[MonoPInvokeCallback(typeof(Managed_CreateObjectClassInstanceWithPointer_delegate))]
	internal static int Managed_CreateObjectClassInstanceWithPointer(IntPtr className, IntPtr pointer)
	{
		return Managed.CreateObjectClassInstanceWithPointer(Marshal.PtrToStringAnsi(className), pointer)?.GetManagedId() ?? 0;
	}

	[MonoPInvokeCallback(typeof(Managed_EngineApiMethodInterfaceInitializer_delegate))]
	internal static void Managed_EngineApiMethodInterfaceInitializer(int id, IntPtr pointer)
	{
		Managed.EngineApiMethodInterfaceInitializer(id, pointer);
	}

	[MonoPInvokeCallback(typeof(Managed_FillEngineApiPointers_delegate))]
	internal static void Managed_FillEngineApiPointers()
	{
		Managed.FillEngineApiPointers();
	}

	[MonoPInvokeCallback(typeof(Managed_GarbageCollect_delegate))]
	internal static void Managed_GarbageCollect(bool forceTimer)
	{
		Managed.GarbageCollect(forceTimer);
	}

	[MonoPInvokeCallback(typeof(Managed_GetClassFields_delegate))]
	internal static int Managed_GetClassFields(IntPtr className, bool recursive, bool includeInternal, bool includeProtected, bool includePrivate)
	{
		return Managed.AddCustomParameter(Managed.GetClassFields(Marshal.PtrToStringAnsi(className), recursive, includeInternal, includeProtected, includePrivate)).GetManagedId();
	}

	[MonoPInvokeCallback(typeof(Managed_GetEnumNamesOfField_delegate))]
	internal static UIntPtr Managed_GetEnumNamesOfField(uint classNameHash, uint fieldNameHash)
	{
		string enumNamesOfField = Managed.GetEnumNamesOfField(classNameHash, fieldNameHash);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, enumNamesOfField);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetMemoryUsage_delegate))]
	internal static long Managed_GetMemoryUsage()
	{
		return Managed.GetMemoryUsage();
	}

	[MonoPInvokeCallback(typeof(Managed_GetModuleList_delegate))]
	internal static UIntPtr Managed_GetModuleList()
	{
		string moduleList = Managed.GetModuleList();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, moduleList);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetObjectClassName_delegate))]
	internal static UIntPtr Managed_GetObjectClassName(IntPtr className)
	{
		string objectClassName = Managed.GetObjectClassName(Marshal.PtrToStringAnsi(className));
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, objectClassName);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetStackTraceRaw_delegate))]
	internal static UIntPtr Managed_GetStackTraceRaw(int skipCount)
	{
		string stackTraceRaw = Managed.GetStackTraceRaw(skipCount);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stackTraceRaw);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetStackTraceStr_delegate))]
	internal static UIntPtr Managed_GetStackTraceStr(int skipCount)
	{
		string stackTraceStr = Managed.GetStackTraceStr(skipCount);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stackTraceStr);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetStringArrayLength_delegate))]
	internal static int Managed_GetStringArrayLength(int array)
	{
		return Managed.GetStringArrayLength((DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target);
	}

	[MonoPInvokeCallback(typeof(Managed_GetStringArrayValueAtIndex_delegate))]
	internal static UIntPtr Managed_GetStringArrayValueAtIndex(int array, int index)
	{
		string stringArrayValueAtIndex = Managed.GetStringArrayValueAtIndex((DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target, index);
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stringArrayValueAtIndex);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(Managed_GetVersionInts_delegate))]
	internal static void Managed_GetVersionInts(ref int major, ref int minor, ref int revision)
	{
		Managed.GetVersionInts(ref major, ref minor, ref revision);
	}

	[MonoPInvokeCallback(typeof(Managed_IsClassFieldExists_delegate))]
	internal static bool Managed_IsClassFieldExists(uint classNameHash, uint fieldNameHash)
	{
		return Managed.IsClassFieldExists(classNameHash, fieldNameHash);
	}

	[MonoPInvokeCallback(typeof(Managed_LoadManagedComponent_delegate))]
	internal static void Managed_LoadManagedComponent(IntPtr assemblyName, IntPtr managedInterface)
	{
		string? assemblyName2 = Marshal.PtrToStringAnsi(assemblyName);
		string managedInterface2 = Marshal.PtrToStringAnsi(managedInterface);
		Managed.LoadManagedComponent(assemblyName2, managedInterface2);
	}

	[MonoPInvokeCallback(typeof(Managed_OnFinalize_delegate))]
	internal static void Managed_OnFinalize()
	{
		Managed.OnFinalize();
	}

	[MonoPInvokeCallback(typeof(Managed_PassCustomCallbackMethodPointers_delegate))]
	internal static void Managed_PassCustomCallbackMethodPointers(IntPtr name, IntPtr initalizer)
	{
		Managed.PassCustomCallbackMethodPointers(Marshal.PtrToStringAnsi(name), initalizer);
	}

	[MonoPInvokeCallback(typeof(Managed_PreFinalize_delegate))]
	internal static void Managed_PreFinalize()
	{
		Managed.PreFinalize();
	}

	[MonoPInvokeCallback(typeof(Managed_SetClosing_delegate))]
	internal static void Managed_SetClosing()
	{
		Managed.SetClosing();
	}

	[MonoPInvokeCallback(typeof(Managed_SetCurrentStringReturnValue_delegate))]
	internal static void Managed_SetCurrentStringReturnValue(IntPtr pointer)
	{
		Managed.SetCurrentStringReturnValue(pointer);
	}

	[MonoPInvokeCallback(typeof(Managed_SetCurrentStringReturnValueAsUnicode_delegate))]
	internal static void Managed_SetCurrentStringReturnValueAsUnicode(IntPtr pointer)
	{
		Managed.SetCurrentStringReturnValueAsUnicode(pointer);
	}

	[MonoPInvokeCallback(typeof(Managed_SetLogsFolder_delegate))]
	internal static void Managed_SetLogsFolder(IntPtr logFolder)
	{
		Managed.SetLogsFolder(Marshal.PtrToStringAnsi(logFolder));
	}

	[MonoPInvokeCallback(typeof(Managed_SetStringArrayValueAtIndex_delegate))]
	internal static void Managed_SetStringArrayValueAtIndex(int array, int index, IntPtr value)
	{
		string[] target = (DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target;
		string value2 = Marshal.PtrToStringAnsi(value);
		Managed.SetStringArrayValueAtIndex(target, index, value2);
	}

	[MonoPInvokeCallback(typeof(ManagedDelegate_InvokeAux_delegate))]
	internal static void ManagedDelegate_InvokeAux(int thisPointer)
	{
		(DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedDelegate).InvokeAux();
	}

	[MonoPInvokeCallback(typeof(ManagedObject_GetAliveManagedObjectCount_delegate))]
	internal static int ManagedObject_GetAliveManagedObjectCount()
	{
		return ManagedObject.GetAliveManagedObjectCount();
	}

	[MonoPInvokeCallback(typeof(ManagedObject_GetAliveManagedObjectNames_delegate))]
	internal static UIntPtr ManagedObject_GetAliveManagedObjectNames()
	{
		string aliveManagedObjectNames = ManagedObject.GetAliveManagedObjectNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveManagedObjectNames);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(ManagedObject_GetClassOfObject_delegate))]
	internal static UIntPtr ManagedObject_GetClassOfObject(int thisPointer)
	{
		string classOfObject = ManagedObjectOwner.GetManagedObjectWithId(thisPointer).GetClassOfObject();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, classOfObject);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(ManagedObject_GetCreationCallstack_delegate))]
	internal static UIntPtr ManagedObject_GetCreationCallstack(IntPtr name)
	{
		string creationCallstack = ManagedObject.GetCreationCallstack(Marshal.PtrToStringAnsi(name));
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, creationCallstack);
		return threadLocalCachedRglVarString;
	}

	[MonoPInvokeCallback(typeof(NativeObject_GetAliveNativeObjectCount_delegate))]
	internal static int NativeObject_GetAliveNativeObjectCount()
	{
		return NativeObject.GetAliveNativeObjectCount();
	}

	[MonoPInvokeCallback(typeof(NativeObject_GetAliveNativeObjectNames_delegate))]
	internal static UIntPtr NativeObject_GetAliveNativeObjectNames()
	{
		string aliveNativeObjectNames = NativeObject.GetAliveNativeObjectNames();
		UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
		NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveNativeObjectNames);
		return threadLocalCachedRglVarString;
	}
}
