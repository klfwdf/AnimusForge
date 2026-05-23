using System;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008AB RID: 2219
	[__DynamicallyInvokable]
	public static class RuntimeHelpers
	{
		// Token: 0x06005D82 RID: 23938
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void InitializeArray(Array array, RuntimeFieldHandle fldHandle);

		// Token: 0x06005D83 RID: 23939
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern object GetObjectValue(object obj);

		// Token: 0x06005D84 RID: 23940
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _RunClassConstructor(RuntimeType type);

		// Token: 0x06005D85 RID: 23941 RVA: 0x0014942D File Offset: 0x0014762D
		[__DynamicallyInvokable]
		public static void RunClassConstructor(RuntimeTypeHandle type)
		{
			RuntimeHelpers._RunClassConstructor(type.GetRuntimeType());
		}

		// Token: 0x06005D86 RID: 23942
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _RunModuleConstructor(RuntimeModule module);

		// Token: 0x06005D87 RID: 23943 RVA: 0x0014943B File Offset: 0x0014763B
		public static void RunModuleConstructor(ModuleHandle module)
		{
			RuntimeHelpers._RunModuleConstructor(module.GetRuntimeModule());
		}

		// Token: 0x06005D88 RID: 23944
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void _PrepareMethod(IRuntimeMethodInfo method, IntPtr* pInstantiation, int cInstantiation);

		// Token: 0x06005D89 RID: 23945
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void _CompileMethod(IRuntimeMethodInfo method);

		// Token: 0x06005D8A RID: 23946 RVA: 0x00149449 File Offset: 0x00147649
		[SecurityCritical]
		public static void PrepareMethod(RuntimeMethodHandle method)
		{
			RuntimeHelpers._PrepareMethod(method.GetMethodInfo(), null, 0);
		}

		// Token: 0x06005D8B RID: 23947 RVA: 0x0014945C File Offset: 0x0014765C
		[SecurityCritical]
		public unsafe static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[] instantiation)
		{
			int cInstantiation;
			IntPtr[] array = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out cInstantiation);
			IntPtr[] array2;
			IntPtr* pInstantiation;
			if ((array2 = array) == null || array2.Length == 0)
			{
				pInstantiation = null;
			}
			else
			{
				pInstantiation = &array2[0];
			}
			RuntimeHelpers._PrepareMethod(method.GetMethodInfo(), pInstantiation, cInstantiation);
			GC.KeepAlive(instantiation);
			array2 = null;
		}

		// Token: 0x06005D8C RID: 23948
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void PrepareDelegate(Delegate d);

		// Token: 0x06005D8D RID: 23949
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void PrepareContractedDelegate(Delegate d);

		// Token: 0x06005D8E RID: 23950
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern int GetHashCode(object o);

		// Token: 0x06005D8F RID: 23951
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public new static extern bool Equals(object o1, object o2);

		// Token: 0x1700100F RID: 4111
		// (get) Token: 0x06005D90 RID: 23952 RVA: 0x001494A0 File Offset: 0x001476A0
		[__DynamicallyInvokable]
		public static int OffsetToStringData
		{
			[NonVersionable]
			[__DynamicallyInvokable]
			get
			{
				return 12;
			}
		}

		// Token: 0x06005D91 RID: 23953
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void EnsureSufficientExecutionStack();

		// Token: 0x06005D92 RID: 23954
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void ProbeForSufficientStack();

		// Token: 0x06005D93 RID: 23955 RVA: 0x001494A4 File Offset: 0x001476A4
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static void PrepareConstrainedRegions()
		{
			RuntimeHelpers.ProbeForSufficientStack();
		}

		// Token: 0x06005D94 RID: 23956 RVA: 0x001494AB File Offset: 0x001476AB
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static void PrepareConstrainedRegionsNoOP()
		{
		}

		// Token: 0x06005D95 RID: 23957
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void ExecuteCodeWithGuaranteedCleanup(RuntimeHelpers.TryCode code, RuntimeHelpers.CleanupCode backoutCode, object userData);

		// Token: 0x06005D96 RID: 23958 RVA: 0x001494AD File Offset: 0x001476AD
		[PrePrepareMethod]
		internal static void ExecuteBackoutCodeHelper(object backoutCode, object userData, bool exceptionThrown)
		{
			((RuntimeHelpers.CleanupCode)backoutCode)(userData, exceptionThrown);
		}

		// Token: 0x02000C8A RID: 3210
		// (Invoke) Token: 0x060070E4 RID: 28900
		public delegate void TryCode(object userData);

		// Token: 0x02000C8B RID: 3211
		// (Invoke) Token: 0x060070E8 RID: 28904
		public delegate void CleanupCode(object userData, bool exceptionThrown);
	}
}
