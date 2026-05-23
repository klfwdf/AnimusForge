using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000004 RID: 4
	internal static class LibraryCallbacksGenerated
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000C RID: 12 RVA: 0x00002099 File Offset: 0x00000299
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000020A0 File Offset: 0x000002A0
		internal static Delegate[] Delegates { get; private set; }

		// Token: 0x0600000E RID: 14 RVA: 0x000020A8 File Offset: 0x000002A8
		public static void Initialize()
		{
			LibraryCallbacksGenerated.Delegates = new Delegate[42];
			LibraryCallbacksGenerated.Delegates[0] = new LibraryCallbacksGenerated.DotNetObject_DecreaseReferenceCount_delegate(LibraryCallbacksGenerated.DotNetObject_DecreaseReferenceCount);
			LibraryCallbacksGenerated.Delegates[1] = new LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectCount_delegate(LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectCount);
			LibraryCallbacksGenerated.Delegates[2] = new LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectNames_delegate(LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectNames);
			LibraryCallbacksGenerated.Delegates[3] = new LibraryCallbacksGenerated.DotNetObject_IncreaseReferenceCount_delegate(LibraryCallbacksGenerated.DotNetObject_IncreaseReferenceCount);
			LibraryCallbacksGenerated.Delegates[4] = new LibraryCallbacksGenerated.Managed_ApplicationTick_delegate(LibraryCallbacksGenerated.Managed_ApplicationTick);
			LibraryCallbacksGenerated.Delegates[5] = new LibraryCallbacksGenerated.Managed_ApplicationTickLight_delegate(LibraryCallbacksGenerated.Managed_ApplicationTickLight);
			LibraryCallbacksGenerated.Delegates[6] = new LibraryCallbacksGenerated.Managed_CallCommandlineFunction_delegate(LibraryCallbacksGenerated.Managed_CallCommandlineFunction);
			LibraryCallbacksGenerated.Delegates[7] = new LibraryCallbacksGenerated.Managed_CheckClassNameIsValid_delegate(LibraryCallbacksGenerated.Managed_CheckClassNameIsValid);
			LibraryCallbacksGenerated.Delegates[8] = new LibraryCallbacksGenerated.Managed_CheckSharedStructureSizes_delegate(LibraryCallbacksGenerated.Managed_CheckSharedStructureSizes);
			LibraryCallbacksGenerated.Delegates[9] = new LibraryCallbacksGenerated.Managed_CreateCustomParameterStringArray_delegate(LibraryCallbacksGenerated.Managed_CreateCustomParameterStringArray);
			LibraryCallbacksGenerated.Delegates[10] = new LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithInteger_delegate(LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithInteger);
			LibraryCallbacksGenerated.Delegates[11] = new LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithPointer_delegate(LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithPointer);
			LibraryCallbacksGenerated.Delegates[12] = new LibraryCallbacksGenerated.Managed_EngineApiMethodInterfaceInitializer_delegate(LibraryCallbacksGenerated.Managed_EngineApiMethodInterfaceInitializer);
			LibraryCallbacksGenerated.Delegates[13] = new LibraryCallbacksGenerated.Managed_FillEngineApiPointers_delegate(LibraryCallbacksGenerated.Managed_FillEngineApiPointers);
			LibraryCallbacksGenerated.Delegates[14] = new LibraryCallbacksGenerated.Managed_GarbageCollect_delegate(LibraryCallbacksGenerated.Managed_GarbageCollect);
			LibraryCallbacksGenerated.Delegates[15] = new LibraryCallbacksGenerated.Managed_GetClassFields_delegate(LibraryCallbacksGenerated.Managed_GetClassFields);
			LibraryCallbacksGenerated.Delegates[16] = new LibraryCallbacksGenerated.Managed_GetEnumNamesOfField_delegate(LibraryCallbacksGenerated.Managed_GetEnumNamesOfField);
			LibraryCallbacksGenerated.Delegates[17] = new LibraryCallbacksGenerated.Managed_GetMemoryUsage_delegate(LibraryCallbacksGenerated.Managed_GetMemoryUsage);
			LibraryCallbacksGenerated.Delegates[18] = new LibraryCallbacksGenerated.Managed_GetModuleList_delegate(LibraryCallbacksGenerated.Managed_GetModuleList);
			LibraryCallbacksGenerated.Delegates[19] = new LibraryCallbacksGenerated.Managed_GetObjectClassName_delegate(LibraryCallbacksGenerated.Managed_GetObjectClassName);
			LibraryCallbacksGenerated.Delegates[20] = new LibraryCallbacksGenerated.Managed_GetStackTraceRaw_delegate(LibraryCallbacksGenerated.Managed_GetStackTraceRaw);
			LibraryCallbacksGenerated.Delegates[21] = new LibraryCallbacksGenerated.Managed_GetStackTraceStr_delegate(LibraryCallbacksGenerated.Managed_GetStackTraceStr);
			LibraryCallbacksGenerated.Delegates[22] = new LibraryCallbacksGenerated.Managed_GetStringArrayLength_delegate(LibraryCallbacksGenerated.Managed_GetStringArrayLength);
			LibraryCallbacksGenerated.Delegates[23] = new LibraryCallbacksGenerated.Managed_GetStringArrayValueAtIndex_delegate(LibraryCallbacksGenerated.Managed_GetStringArrayValueAtIndex);
			LibraryCallbacksGenerated.Delegates[24] = new LibraryCallbacksGenerated.Managed_GetVersionInts_delegate(LibraryCallbacksGenerated.Managed_GetVersionInts);
			LibraryCallbacksGenerated.Delegates[25] = new LibraryCallbacksGenerated.Managed_IsClassFieldExists_delegate(LibraryCallbacksGenerated.Managed_IsClassFieldExists);
			LibraryCallbacksGenerated.Delegates[26] = new LibraryCallbacksGenerated.Managed_LoadManagedComponent_delegate(LibraryCallbacksGenerated.Managed_LoadManagedComponent);
			LibraryCallbacksGenerated.Delegates[27] = new LibraryCallbacksGenerated.Managed_OnFinalize_delegate(LibraryCallbacksGenerated.Managed_OnFinalize);
			LibraryCallbacksGenerated.Delegates[28] = new LibraryCallbacksGenerated.Managed_PassCustomCallbackMethodPointers_delegate(LibraryCallbacksGenerated.Managed_PassCustomCallbackMethodPointers);
			LibraryCallbacksGenerated.Delegates[29] = new LibraryCallbacksGenerated.Managed_PreFinalize_delegate(LibraryCallbacksGenerated.Managed_PreFinalize);
			LibraryCallbacksGenerated.Delegates[30] = new LibraryCallbacksGenerated.Managed_SetClosing_delegate(LibraryCallbacksGenerated.Managed_SetClosing);
			LibraryCallbacksGenerated.Delegates[31] = new LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValue_delegate(LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValue);
			LibraryCallbacksGenerated.Delegates[32] = new LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValueAsUnicode_delegate(LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValueAsUnicode);
			LibraryCallbacksGenerated.Delegates[33] = new LibraryCallbacksGenerated.Managed_SetLogsFolder_delegate(LibraryCallbacksGenerated.Managed_SetLogsFolder);
			LibraryCallbacksGenerated.Delegates[34] = new LibraryCallbacksGenerated.Managed_SetStringArrayValueAtIndex_delegate(LibraryCallbacksGenerated.Managed_SetStringArrayValueAtIndex);
			LibraryCallbacksGenerated.Delegates[35] = new LibraryCallbacksGenerated.ManagedDelegate_InvokeAux_delegate(LibraryCallbacksGenerated.ManagedDelegate_InvokeAux);
			LibraryCallbacksGenerated.Delegates[36] = new LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectCount_delegate(LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectCount);
			LibraryCallbacksGenerated.Delegates[37] = new LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectNames_delegate(LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectNames);
			LibraryCallbacksGenerated.Delegates[38] = new LibraryCallbacksGenerated.ManagedObject_GetClassOfObject_delegate(LibraryCallbacksGenerated.ManagedObject_GetClassOfObject);
			LibraryCallbacksGenerated.Delegates[39] = new LibraryCallbacksGenerated.ManagedObject_GetCreationCallstack_delegate(LibraryCallbacksGenerated.ManagedObject_GetCreationCallstack);
			LibraryCallbacksGenerated.Delegates[40] = new LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectCount_delegate(LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectCount);
			LibraryCallbacksGenerated.Delegates[41] = new LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectNames_delegate(LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectNames);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002400 File Offset: 0x00000600
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.DotNetObject_DecreaseReferenceCount_delegate))]
		internal static void DotNetObject_DecreaseReferenceCount(int dotnetObjectId)
		{
			DotNetObject.DecreaseReferenceCount(dotnetObjectId);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002408 File Offset: 0x00000608
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectCount_delegate))]
		internal static int DotNetObject_GetAliveDotNetObjectCount()
		{
			return DotNetObject.GetAliveDotNetObjectCount();
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002410 File Offset: 0x00000610
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.DotNetObject_GetAliveDotNetObjectNames_delegate))]
		internal static UIntPtr DotNetObject_GetAliveDotNetObjectNames()
		{
			string aliveDotNetObjectNames = DotNetObject.GetAliveDotNetObjectNames();
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveDotNetObjectNames);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000242F File Offset: 0x0000062F
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.DotNetObject_IncreaseReferenceCount_delegate))]
		internal static void DotNetObject_IncreaseReferenceCount(int dotnetObjectId)
		{
			DotNetObject.IncreaseReferenceCount(dotnetObjectId);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002437 File Offset: 0x00000637
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_ApplicationTick_delegate))]
		internal static void Managed_ApplicationTick(float dt)
		{
			Managed.ApplicationTick(dt);
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000243F File Offset: 0x0000063F
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_ApplicationTickLight_delegate))]
		internal static void Managed_ApplicationTickLight(float dt)
		{
			Managed.ApplicationTickLight(dt);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002448 File Offset: 0x00000648
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CallCommandlineFunction_delegate))]
		internal static UIntPtr Managed_CallCommandlineFunction(IntPtr functionName, IntPtr arguments)
		{
			string functionName2 = Marshal.PtrToStringAnsi(functionName);
			string arguments2 = Marshal.PtrToStringAnsi(arguments);
			string text = Managed.CallCommandlineFunction(functionName2, arguments2);
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, text);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002475 File Offset: 0x00000675
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CheckClassNameIsValid_delegate))]
		internal static bool Managed_CheckClassNameIsValid(IntPtr className)
		{
			return Managed.CheckClassNameIsValid(Marshal.PtrToStringAnsi(className));
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002482 File Offset: 0x00000682
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CheckSharedStructureSizes_delegate))]
		internal static void Managed_CheckSharedStructureSizes()
		{
			Managed.CheckSharedStructureSizes();
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002489 File Offset: 0x00000689
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CreateCustomParameterStringArray_delegate))]
		internal static int Managed_CreateCustomParameterStringArray(int length)
		{
			return Managed.CreateCustomParameterStringArray(length).GetManagedId();
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002498 File Offset: 0x00000698
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithInteger_delegate))]
		internal static int Managed_CreateObjectClassInstanceWithInteger(IntPtr className, int value)
		{
			ManagedObject managedObject = Managed.CreateObjectClassInstanceWithInteger(Marshal.PtrToStringAnsi(className), value);
			if (managedObject == null)
			{
				return 0;
			}
			return managedObject.GetManagedId();
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000024C0 File Offset: 0x000006C0
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_CreateObjectClassInstanceWithPointer_delegate))]
		internal static int Managed_CreateObjectClassInstanceWithPointer(IntPtr className, IntPtr pointer)
		{
			ManagedObject managedObject = Managed.CreateObjectClassInstanceWithPointer(Marshal.PtrToStringAnsi(className), pointer);
			if (managedObject == null)
			{
				return 0;
			}
			return managedObject.GetManagedId();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000024E5 File Offset: 0x000006E5
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_EngineApiMethodInterfaceInitializer_delegate))]
		internal static void Managed_EngineApiMethodInterfaceInitializer(int id, IntPtr pointer)
		{
			Managed.EngineApiMethodInterfaceInitializer(id, pointer);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000024EE File Offset: 0x000006EE
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_FillEngineApiPointers_delegate))]
		internal static void Managed_FillEngineApiPointers()
		{
			Managed.FillEngineApiPointers();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000024F5 File Offset: 0x000006F5
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GarbageCollect_delegate))]
		internal static void Managed_GarbageCollect(bool forceTimer)
		{
			Managed.GarbageCollect(forceTimer);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000024FD File Offset: 0x000006FD
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetClassFields_delegate))]
		internal static int Managed_GetClassFields(IntPtr className, bool recursive, bool includeInternal, bool includeProtected, bool includePrivate)
		{
			return Managed.AddCustomParameter<string[]>(Managed.GetClassFields(Marshal.PtrToStringAnsi(className), recursive, includeInternal, includeProtected, includePrivate)).GetManagedId();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000251C File Offset: 0x0000071C
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetEnumNamesOfField_delegate))]
		internal static UIntPtr Managed_GetEnumNamesOfField(uint classNameHash, uint fieldNameHash)
		{
			string enumNamesOfField = Managed.GetEnumNamesOfField(classNameHash, fieldNameHash);
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, enumNamesOfField);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000253D File Offset: 0x0000073D
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetMemoryUsage_delegate))]
		internal static long Managed_GetMemoryUsage()
		{
			return Managed.GetMemoryUsage();
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002544 File Offset: 0x00000744
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetModuleList_delegate))]
		internal static UIntPtr Managed_GetModuleList()
		{
			string moduleList = Managed.GetModuleList();
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, moduleList);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002564 File Offset: 0x00000764
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetObjectClassName_delegate))]
		internal static UIntPtr Managed_GetObjectClassName(IntPtr className)
		{
			string objectClassName = Managed.GetObjectClassName(Marshal.PtrToStringAnsi(className));
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, objectClassName);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000258C File Offset: 0x0000078C
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetStackTraceRaw_delegate))]
		internal static UIntPtr Managed_GetStackTraceRaw(int skipCount)
		{
			string stackTraceRaw = Managed.GetStackTraceRaw(skipCount);
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stackTraceRaw);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000025AC File Offset: 0x000007AC
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetStackTraceStr_delegate))]
		internal static UIntPtr Managed_GetStackTraceStr(int skipCount)
		{
			string stackTraceStr = Managed.GetStackTraceStr(skipCount);
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stackTraceStr);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000025CC File Offset: 0x000007CC
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetStringArrayLength_delegate))]
		internal static int Managed_GetStringArrayLength(int array)
		{
			return Managed.GetStringArrayLength((DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000025E4 File Offset: 0x000007E4
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetStringArrayValueAtIndex_delegate))]
		internal static UIntPtr Managed_GetStringArrayValueAtIndex(int array, int index)
		{
			string stringArrayValueAtIndex = Managed.GetStringArrayValueAtIndex((DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target, index);
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, stringArrayValueAtIndex);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002614 File Offset: 0x00000814
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_GetVersionInts_delegate))]
		internal static void Managed_GetVersionInts(ref int major, ref int minor, ref int revision)
		{
			Managed.GetVersionInts(ref major, ref minor, ref revision);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000261E File Offset: 0x0000081E
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_IsClassFieldExists_delegate))]
		internal static bool Managed_IsClassFieldExists(uint classNameHash, uint fieldNameHash)
		{
			return Managed.IsClassFieldExists(classNameHash, fieldNameHash);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002628 File Offset: 0x00000828
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_LoadManagedComponent_delegate))]
		internal static void Managed_LoadManagedComponent(IntPtr assemblyName, IntPtr managedInterface)
		{
			string assemblyName2 = Marshal.PtrToStringAnsi(assemblyName);
			string managedInterface2 = Marshal.PtrToStringAnsi(managedInterface);
			Managed.LoadManagedComponent(assemblyName2, managedInterface2);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002648 File Offset: 0x00000848
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_OnFinalize_delegate))]
		internal static void Managed_OnFinalize()
		{
			Managed.OnFinalize();
		}

		// Token: 0x0600002B RID: 43 RVA: 0x0000264F File Offset: 0x0000084F
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_PassCustomCallbackMethodPointers_delegate))]
		internal static void Managed_PassCustomCallbackMethodPointers(IntPtr name, IntPtr initalizer)
		{
			Managed.PassCustomCallbackMethodPointers(Marshal.PtrToStringAnsi(name), initalizer);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000265D File Offset: 0x0000085D
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_PreFinalize_delegate))]
		internal static void Managed_PreFinalize()
		{
			Managed.PreFinalize();
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002664 File Offset: 0x00000864
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_SetClosing_delegate))]
		internal static void Managed_SetClosing()
		{
			Managed.SetClosing();
		}

		// Token: 0x0600002E RID: 46 RVA: 0x0000266B File Offset: 0x0000086B
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValue_delegate))]
		internal static void Managed_SetCurrentStringReturnValue(IntPtr pointer)
		{
			Managed.SetCurrentStringReturnValue(pointer);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002673 File Offset: 0x00000873
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_SetCurrentStringReturnValueAsUnicode_delegate))]
		internal static void Managed_SetCurrentStringReturnValueAsUnicode(IntPtr pointer)
		{
			Managed.SetCurrentStringReturnValueAsUnicode(pointer);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000267B File Offset: 0x0000087B
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_SetLogsFolder_delegate))]
		internal static void Managed_SetLogsFolder(IntPtr logFolder)
		{
			Managed.SetLogsFolder(Marshal.PtrToStringAnsi(logFolder));
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002688 File Offset: 0x00000888
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.Managed_SetStringArrayValueAtIndex_delegate))]
		internal static void Managed_SetStringArrayValueAtIndex(int array, int index, IntPtr value)
		{
			string[] target = (DotNetObject.GetManagedObjectWithId(array) as CustomParameter<string[]>).Target;
			string value2 = Marshal.PtrToStringAnsi(value);
			Managed.SetStringArrayValueAtIndex(target, index, value2);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000026B3 File Offset: 0x000008B3
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.ManagedDelegate_InvokeAux_delegate))]
		internal static void ManagedDelegate_InvokeAux(int thisPointer)
		{
			(DotNetObject.GetManagedObjectWithId(thisPointer) as ManagedDelegate).InvokeAux();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000026C5 File Offset: 0x000008C5
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectCount_delegate))]
		internal static int ManagedObject_GetAliveManagedObjectCount()
		{
			return ManagedObject.GetAliveManagedObjectCount();
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000026CC File Offset: 0x000008CC
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.ManagedObject_GetAliveManagedObjectNames_delegate))]
		internal static UIntPtr ManagedObject_GetAliveManagedObjectNames()
		{
			string aliveManagedObjectNames = ManagedObject.GetAliveManagedObjectNames();
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveManagedObjectNames);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000026EC File Offset: 0x000008EC
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.ManagedObject_GetClassOfObject_delegate))]
		internal static UIntPtr ManagedObject_GetClassOfObject(int thisPointer)
		{
			string classOfObject = ManagedObjectOwner.GetManagedObjectWithId(thisPointer).GetClassOfObject();
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, classOfObject);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002714 File Offset: 0x00000914
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.ManagedObject_GetCreationCallstack_delegate))]
		internal static UIntPtr ManagedObject_GetCreationCallstack(IntPtr name)
		{
			string creationCallstack = ManagedObject.GetCreationCallstack(Marshal.PtrToStringAnsi(name));
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, creationCallstack);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002739 File Offset: 0x00000939
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectCount_delegate))]
		internal static int NativeObject_GetAliveNativeObjectCount()
		{
			return NativeObject.GetAliveNativeObjectCount();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002740 File Offset: 0x00000940
		[MonoPInvokeCallback(typeof(LibraryCallbacksGenerated.NativeObject_GetAliveNativeObjectNames_delegate))]
		internal static UIntPtr NativeObject_GetAliveNativeObjectNames()
		{
			string aliveNativeObjectNames = NativeObject.GetAliveNativeObjectNames();
			UIntPtr threadLocalCachedRglVarString = NativeStringHelper.GetThreadLocalCachedRglVarString();
			NativeStringHelper.SetRglVarString(threadLocalCachedRglVarString, aliveNativeObjectNames);
			return threadLocalCachedRglVarString;
		}

		// Token: 0x0200000D RID: 13
		// (Invoke) Token: 0x0600006B RID: 107
		internal delegate void DotNetObject_DecreaseReferenceCount_delegate(int dotnetObjectId);

		// Token: 0x0200000E RID: 14
		// (Invoke) Token: 0x0600006F RID: 111
		internal delegate int DotNetObject_GetAliveDotNetObjectCount_delegate();

		// Token: 0x0200000F RID: 15
		// (Invoke) Token: 0x06000073 RID: 115
		internal delegate UIntPtr DotNetObject_GetAliveDotNetObjectNames_delegate();

		// Token: 0x02000010 RID: 16
		// (Invoke) Token: 0x06000077 RID: 119
		internal delegate void DotNetObject_IncreaseReferenceCount_delegate(int dotnetObjectId);

		// Token: 0x02000011 RID: 17
		// (Invoke) Token: 0x0600007B RID: 123
		internal delegate void Managed_ApplicationTick_delegate(float dt);

		// Token: 0x02000012 RID: 18
		// (Invoke) Token: 0x0600007F RID: 127
		internal delegate void Managed_ApplicationTickLight_delegate(float dt);

		// Token: 0x02000013 RID: 19
		// (Invoke) Token: 0x06000083 RID: 131
		internal delegate UIntPtr Managed_CallCommandlineFunction_delegate(IntPtr functionName, IntPtr arguments);

		// Token: 0x02000014 RID: 20
		// (Invoke) Token: 0x06000087 RID: 135
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool Managed_CheckClassNameIsValid_delegate(IntPtr className);

		// Token: 0x02000015 RID: 21
		// (Invoke) Token: 0x0600008B RID: 139
		internal delegate void Managed_CheckSharedStructureSizes_delegate();

		// Token: 0x02000016 RID: 22
		// (Invoke) Token: 0x0600008F RID: 143
		internal delegate int Managed_CreateCustomParameterStringArray_delegate(int length);

		// Token: 0x02000017 RID: 23
		// (Invoke) Token: 0x06000093 RID: 147
		internal delegate int Managed_CreateObjectClassInstanceWithInteger_delegate(IntPtr className, int value);

		// Token: 0x02000018 RID: 24
		// (Invoke) Token: 0x06000097 RID: 151
		internal delegate int Managed_CreateObjectClassInstanceWithPointer_delegate(IntPtr className, IntPtr pointer);

		// Token: 0x02000019 RID: 25
		// (Invoke) Token: 0x0600009B RID: 155
		internal delegate void Managed_EngineApiMethodInterfaceInitializer_delegate(int id, IntPtr pointer);

		// Token: 0x0200001A RID: 26
		// (Invoke) Token: 0x0600009F RID: 159
		internal delegate void Managed_FillEngineApiPointers_delegate();

		// Token: 0x0200001B RID: 27
		// (Invoke) Token: 0x060000A3 RID: 163
		internal delegate void Managed_GarbageCollect_delegate([MarshalAs(UnmanagedType.U1)] bool forceTimer);

		// Token: 0x0200001C RID: 28
		// (Invoke) Token: 0x060000A7 RID: 167
		internal delegate int Managed_GetClassFields_delegate(IntPtr className, [MarshalAs(UnmanagedType.U1)] bool recursive, [MarshalAs(UnmanagedType.U1)] bool includeInternal, [MarshalAs(UnmanagedType.U1)] bool includeProtected, [MarshalAs(UnmanagedType.U1)] bool includePrivate);

		// Token: 0x0200001D RID: 29
		// (Invoke) Token: 0x060000AB RID: 171
		internal delegate UIntPtr Managed_GetEnumNamesOfField_delegate(uint classNameHash, uint fieldNameHash);

		// Token: 0x0200001E RID: 30
		// (Invoke) Token: 0x060000AF RID: 175
		internal delegate long Managed_GetMemoryUsage_delegate();

		// Token: 0x0200001F RID: 31
		// (Invoke) Token: 0x060000B3 RID: 179
		internal delegate UIntPtr Managed_GetModuleList_delegate();

		// Token: 0x02000020 RID: 32
		// (Invoke) Token: 0x060000B7 RID: 183
		internal delegate UIntPtr Managed_GetObjectClassName_delegate(IntPtr className);

		// Token: 0x02000021 RID: 33
		// (Invoke) Token: 0x060000BB RID: 187
		internal delegate UIntPtr Managed_GetStackTraceRaw_delegate(int skipCount);

		// Token: 0x02000022 RID: 34
		// (Invoke) Token: 0x060000BF RID: 191
		internal delegate UIntPtr Managed_GetStackTraceStr_delegate(int skipCount);

		// Token: 0x02000023 RID: 35
		// (Invoke) Token: 0x060000C3 RID: 195
		internal delegate int Managed_GetStringArrayLength_delegate(int array);

		// Token: 0x02000024 RID: 36
		// (Invoke) Token: 0x060000C7 RID: 199
		internal delegate UIntPtr Managed_GetStringArrayValueAtIndex_delegate(int array, int index);

		// Token: 0x02000025 RID: 37
		// (Invoke) Token: 0x060000CB RID: 203
		internal delegate void Managed_GetVersionInts_delegate(ref int major, ref int minor, ref int revision);

		// Token: 0x02000026 RID: 38
		// (Invoke) Token: 0x060000CF RID: 207
		[return: MarshalAs(UnmanagedType.U1)]
		internal delegate bool Managed_IsClassFieldExists_delegate(uint classNameHash, uint fieldNameHash);

		// Token: 0x02000027 RID: 39
		// (Invoke) Token: 0x060000D3 RID: 211
		internal delegate void Managed_LoadManagedComponent_delegate(IntPtr assemblyName, IntPtr managedInterface);

		// Token: 0x02000028 RID: 40
		// (Invoke) Token: 0x060000D7 RID: 215
		internal delegate void Managed_OnFinalize_delegate();

		// Token: 0x02000029 RID: 41
		// (Invoke) Token: 0x060000DB RID: 219
		internal delegate void Managed_PassCustomCallbackMethodPointers_delegate(IntPtr name, IntPtr initalizer);

		// Token: 0x0200002A RID: 42
		// (Invoke) Token: 0x060000DF RID: 223
		internal delegate void Managed_PreFinalize_delegate();

		// Token: 0x0200002B RID: 43
		// (Invoke) Token: 0x060000E3 RID: 227
		internal delegate void Managed_SetClosing_delegate();

		// Token: 0x0200002C RID: 44
		// (Invoke) Token: 0x060000E7 RID: 231
		internal delegate void Managed_SetCurrentStringReturnValue_delegate(IntPtr pointer);

		// Token: 0x0200002D RID: 45
		// (Invoke) Token: 0x060000EB RID: 235
		internal delegate void Managed_SetCurrentStringReturnValueAsUnicode_delegate(IntPtr pointer);

		// Token: 0x0200002E RID: 46
		// (Invoke) Token: 0x060000EF RID: 239
		internal delegate void Managed_SetLogsFolder_delegate(IntPtr logFolder);

		// Token: 0x0200002F RID: 47
		// (Invoke) Token: 0x060000F3 RID: 243
		internal delegate void Managed_SetStringArrayValueAtIndex_delegate(int array, int index, IntPtr value);

		// Token: 0x02000030 RID: 48
		// (Invoke) Token: 0x060000F7 RID: 247
		internal delegate void ManagedDelegate_InvokeAux_delegate(int thisPointer);

		// Token: 0x02000031 RID: 49
		// (Invoke) Token: 0x060000FB RID: 251
		internal delegate int ManagedObject_GetAliveManagedObjectCount_delegate();

		// Token: 0x02000032 RID: 50
		// (Invoke) Token: 0x060000FF RID: 255
		internal delegate UIntPtr ManagedObject_GetAliveManagedObjectNames_delegate();

		// Token: 0x02000033 RID: 51
		// (Invoke) Token: 0x06000103 RID: 259
		internal delegate UIntPtr ManagedObject_GetClassOfObject_delegate(int thisPointer);

		// Token: 0x02000034 RID: 52
		// (Invoke) Token: 0x06000107 RID: 263
		internal delegate UIntPtr ManagedObject_GetCreationCallstack_delegate(IntPtr name);

		// Token: 0x02000035 RID: 53
		// (Invoke) Token: 0x0600010B RID: 267
		internal delegate int NativeObject_GetAliveNativeObjectCount_delegate();

		// Token: 0x02000036 RID: 54
		// (Invoke) Token: 0x0600010F RID: 271
		internal delegate UIntPtr NativeObject_GetAliveNativeObjectNames_delegate();
	}
}
