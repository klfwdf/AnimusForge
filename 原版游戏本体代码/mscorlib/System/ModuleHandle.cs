using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x02000139 RID: 313
	[ComVisible(true)]
	public struct ModuleHandle
	{
		// Token: 0x060012A9 RID: 4777 RVA: 0x00037A6C File Offset: 0x00035C6C
		private static ModuleHandle GetEmptyMH()
		{
			return default(ModuleHandle);
		}

		// Token: 0x060012AA RID: 4778 RVA: 0x00037A82 File Offset: 0x00035C82
		internal ModuleHandle(RuntimeModule module)
		{
			this.m_ptr = module;
		}

		// Token: 0x060012AB RID: 4779 RVA: 0x00037A8B File Offset: 0x00035C8B
		internal RuntimeModule GetRuntimeModule()
		{
			return this.m_ptr;
		}

		// Token: 0x060012AC RID: 4780 RVA: 0x00037A93 File Offset: 0x00035C93
		internal bool IsNullHandle()
		{
			return this.m_ptr == null;
		}

		// Token: 0x060012AD RID: 4781 RVA: 0x00037AA1 File Offset: 0x00035CA1
		public override int GetHashCode()
		{
			if (!(this.m_ptr != null))
			{
				return 0;
			}
			return this.m_ptr.GetHashCode();
		}

		// Token: 0x060012AE RID: 4782 RVA: 0x00037AC0 File Offset: 0x00035CC0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public override bool Equals(object obj)
		{
			if (!(obj is ModuleHandle))
			{
				return false;
			}
			ModuleHandle moduleHandle = (ModuleHandle)obj;
			return moduleHandle.m_ptr == this.m_ptr;
		}

		// Token: 0x060012AF RID: 4783 RVA: 0x00037AEF File Offset: 0x00035CEF
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public bool Equals(ModuleHandle handle)
		{
			return handle.m_ptr == this.m_ptr;
		}

		// Token: 0x060012B0 RID: 4784 RVA: 0x00037B02 File Offset: 0x00035D02
		public static bool operator ==(ModuleHandle left, ModuleHandle right)
		{
			return left.Equals(right);
		}

		// Token: 0x060012B1 RID: 4785 RVA: 0x00037B0C File Offset: 0x00035D0C
		public static bool operator !=(ModuleHandle left, ModuleHandle right)
		{
			return !left.Equals(right);
		}

		// Token: 0x060012B2 RID: 4786
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern IRuntimeMethodInfo GetDynamicMethod(DynamicMethod method, RuntimeModule module, string name, byte[] sig, Resolver resolver);

		// Token: 0x060012B3 RID: 4787
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetToken(RuntimeModule module);

		// Token: 0x060012B4 RID: 4788 RVA: 0x00037B19 File Offset: 0x00035D19
		private static void ValidateModulePointer(RuntimeModule module)
		{
			if (module == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullModuleHandle"));
			}
		}

		// Token: 0x060012B5 RID: 4789 RVA: 0x00037B34 File Offset: 0x00035D34
		public RuntimeTypeHandle GetRuntimeTypeHandleFromMetadataToken(int typeToken)
		{
			return this.ResolveTypeHandle(typeToken);
		}

		// Token: 0x060012B6 RID: 4790 RVA: 0x00037B3D File Offset: 0x00035D3D
		public RuntimeTypeHandle ResolveTypeHandle(int typeToken)
		{
			return new RuntimeTypeHandle(ModuleHandle.ResolveTypeHandleInternal(this.GetRuntimeModule(), typeToken, null, null));
		}

		// Token: 0x060012B7 RID: 4791 RVA: 0x00037B52 File Offset: 0x00035D52
		public RuntimeTypeHandle ResolveTypeHandle(int typeToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			return new RuntimeTypeHandle(ModuleHandle.ResolveTypeHandleInternal(this.GetRuntimeModule(), typeToken, typeInstantiationContext, methodInstantiationContext));
		}

		// Token: 0x060012B8 RID: 4792 RVA: 0x00037B68 File Offset: 0x00035D68
		[SecuritySafeCritical]
		internal unsafe static RuntimeType ResolveTypeHandleInternal(RuntimeModule module, int typeToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			ModuleHandle.ValidateModulePointer(module);
			if (!ModuleHandle.GetMetadataImport(module).IsValidToken(typeToken))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[]
				{
					typeToken,
					new ModuleHandle(module)
				}));
			}
			int typeInstCount;
			IntPtr[] array = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
			int methodInstCount;
			IntPtr[] array2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);
			IntPtr[] array3;
			IntPtr* typeInstArgs;
			if ((array3 = array) == null || array3.Length == 0)
			{
				typeInstArgs = null;
			}
			else
			{
				typeInstArgs = &array3[0];
			}
			IntPtr[] array4;
			IntPtr* methodInstArgs;
			if ((array4 = array2) == null || array4.Length == 0)
			{
				methodInstArgs = null;
			}
			else
			{
				methodInstArgs = &array4[0];
			}
			RuntimeType result = null;
			ModuleHandle.ResolveType(module, typeToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, JitHelpers.GetObjectHandleOnStack<RuntimeType>(ref result));
			GC.KeepAlive(typeInstantiationContext);
			GC.KeepAlive(methodInstantiationContext);
			return result;
		}

		// Token: 0x060012B9 RID: 4793
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern void ResolveType(RuntimeModule module, int typeToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount, ObjectHandleOnStack type);

		// Token: 0x060012BA RID: 4794 RVA: 0x00037C30 File Offset: 0x00035E30
		public RuntimeMethodHandle GetRuntimeMethodHandleFromMetadataToken(int methodToken)
		{
			return this.ResolveMethodHandle(methodToken);
		}

		// Token: 0x060012BB RID: 4795 RVA: 0x00037C39 File Offset: 0x00035E39
		public RuntimeMethodHandle ResolveMethodHandle(int methodToken)
		{
			return this.ResolveMethodHandle(methodToken, null, null);
		}

		// Token: 0x060012BC RID: 4796 RVA: 0x00037C44 File Offset: 0x00035E44
		internal static IRuntimeMethodInfo ResolveMethodHandleInternal(RuntimeModule module, int methodToken)
		{
			return ModuleHandle.ResolveMethodHandleInternal(module, methodToken, null, null);
		}

		// Token: 0x060012BD RID: 4797 RVA: 0x00037C4F File Offset: 0x00035E4F
		public RuntimeMethodHandle ResolveMethodHandle(int methodToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			return new RuntimeMethodHandle(ModuleHandle.ResolveMethodHandleInternal(this.GetRuntimeModule(), methodToken, typeInstantiationContext, methodInstantiationContext));
		}

		// Token: 0x060012BE RID: 4798 RVA: 0x00037C64 File Offset: 0x00035E64
		[SecuritySafeCritical]
		internal static IRuntimeMethodInfo ResolveMethodHandleInternal(RuntimeModule module, int methodToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			int typeInstCount;
			IntPtr[] typeInstantiationContext2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
			int methodInstCount;
			IntPtr[] methodInstantiationContext2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);
			RuntimeMethodHandleInternal runtimeMethodHandleInternal = ModuleHandle.ResolveMethodHandleInternalCore(module, methodToken, typeInstantiationContext2, typeInstCount, methodInstantiationContext2, methodInstCount);
			IRuntimeMethodInfo result = new RuntimeMethodInfoStub(runtimeMethodHandleInternal, RuntimeMethodHandle.GetLoaderAllocator(runtimeMethodHandleInternal));
			GC.KeepAlive(typeInstantiationContext);
			GC.KeepAlive(methodInstantiationContext);
			return result;
		}

		// Token: 0x060012BF RID: 4799 RVA: 0x00037CB0 File Offset: 0x00035EB0
		[SecurityCritical]
		internal unsafe static RuntimeMethodHandleInternal ResolveMethodHandleInternalCore(RuntimeModule module, int methodToken, IntPtr[] typeInstantiationContext, int typeInstCount, IntPtr[] methodInstantiationContext, int methodInstCount)
		{
			ModuleHandle.ValidateModulePointer(module);
			if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(methodToken))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[]
				{
					methodToken,
					new ModuleHandle(module)
				}));
			}
			IntPtr* typeInstArgs;
			if (typeInstantiationContext == null || typeInstantiationContext.Length == 0)
			{
				typeInstArgs = null;
			}
			else
			{
				typeInstArgs = &typeInstantiationContext[0];
			}
			IntPtr* methodInstArgs;
			if (methodInstantiationContext == null || methodInstantiationContext.Length == 0)
			{
				methodInstArgs = null;
			}
			else
			{
				methodInstArgs = &methodInstantiationContext[0];
			}
			return ModuleHandle.ResolveMethod(module.GetNativeHandle(), methodToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount);
		}

		// Token: 0x060012C0 RID: 4800
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern RuntimeMethodHandleInternal ResolveMethod(RuntimeModule module, int methodToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount);

		// Token: 0x060012C1 RID: 4801 RVA: 0x00037D50 File Offset: 0x00035F50
		public RuntimeFieldHandle GetRuntimeFieldHandleFromMetadataToken(int fieldToken)
		{
			return this.ResolveFieldHandle(fieldToken);
		}

		// Token: 0x060012C2 RID: 4802 RVA: 0x00037D59 File Offset: 0x00035F59
		public RuntimeFieldHandle ResolveFieldHandle(int fieldToken)
		{
			return new RuntimeFieldHandle(ModuleHandle.ResolveFieldHandleInternal(this.GetRuntimeModule(), fieldToken, null, null));
		}

		// Token: 0x060012C3 RID: 4803 RVA: 0x00037D6E File Offset: 0x00035F6E
		public RuntimeFieldHandle ResolveFieldHandle(int fieldToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			return new RuntimeFieldHandle(ModuleHandle.ResolveFieldHandleInternal(this.GetRuntimeModule(), fieldToken, typeInstantiationContext, methodInstantiationContext));
		}

		// Token: 0x060012C4 RID: 4804 RVA: 0x00037D84 File Offset: 0x00035F84
		[SecuritySafeCritical]
		internal unsafe static IRuntimeFieldInfo ResolveFieldHandleInternal(RuntimeModule module, int fieldToken, RuntimeTypeHandle[] typeInstantiationContext, RuntimeTypeHandle[] methodInstantiationContext)
		{
			ModuleHandle.ValidateModulePointer(module);
			if (!ModuleHandle.GetMetadataImport(module.GetNativeHandle()).IsValidToken(fieldToken))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[]
				{
					fieldToken,
					new ModuleHandle(module)
				}));
			}
			int typeInstCount;
			IntPtr[] array = RuntimeTypeHandle.CopyRuntimeTypeHandles(typeInstantiationContext, out typeInstCount);
			int methodInstCount;
			IntPtr[] array2 = RuntimeTypeHandle.CopyRuntimeTypeHandles(methodInstantiationContext, out methodInstCount);
			IntPtr[] array3;
			IntPtr* typeInstArgs;
			if ((array3 = array) == null || array3.Length == 0)
			{
				typeInstArgs = null;
			}
			else
			{
				typeInstArgs = &array3[0];
			}
			IntPtr[] array4;
			IntPtr* methodInstArgs;
			if ((array4 = array2) == null || array4.Length == 0)
			{
				methodInstArgs = null;
			}
			else
			{
				methodInstArgs = &array4[0];
			}
			IRuntimeFieldInfo result = null;
			ModuleHandle.ResolveField(module.GetNativeHandle(), fieldToken, typeInstArgs, typeInstCount, methodInstArgs, methodInstCount, JitHelpers.GetObjectHandleOnStack<IRuntimeFieldInfo>(ref result));
			GC.KeepAlive(typeInstantiationContext);
			GC.KeepAlive(methodInstantiationContext);
			return result;
		}

		// Token: 0x060012C5 RID: 4805
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern void ResolveField(RuntimeModule module, int fieldToken, IntPtr* typeInstArgs, int typeInstCount, IntPtr* methodInstArgs, int methodInstCount, ObjectHandleOnStack retField);

		// Token: 0x060012C6 RID: 4806
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern bool _ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash);

		// Token: 0x060012C7 RID: 4807 RVA: 0x00037E56 File Offset: 0x00036056
		[SecurityCritical]
		internal static bool ContainsPropertyMatchingHash(RuntimeModule module, int propertyToken, uint hash)
		{
			return ModuleHandle._ContainsPropertyMatchingHash(module.GetNativeHandle(), propertyToken, hash);
		}

		// Token: 0x060012C8 RID: 4808
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetAssembly(RuntimeModule handle, ObjectHandleOnStack retAssembly);

		// Token: 0x060012C9 RID: 4809 RVA: 0x00037E68 File Offset: 0x00036068
		[SecuritySafeCritical]
		internal static RuntimeAssembly GetAssembly(RuntimeModule module)
		{
			RuntimeAssembly result = null;
			ModuleHandle.GetAssembly(module.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<RuntimeAssembly>(ref result));
			return result;
		}

		// Token: 0x060012CA RID: 4810
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void GetModuleType(RuntimeModule handle, ObjectHandleOnStack type);

		// Token: 0x060012CB RID: 4811 RVA: 0x00037E8C File Offset: 0x0003608C
		[SecuritySafeCritical]
		internal static RuntimeType GetModuleType(RuntimeModule module)
		{
			RuntimeType result = null;
			ModuleHandle.GetModuleType(module.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<RuntimeType>(ref result));
			return result;
		}

		// Token: 0x060012CC RID: 4812
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetPEKind(RuntimeModule handle, out int peKind, out int machine);

		// Token: 0x060012CD RID: 4813 RVA: 0x00037EB0 File Offset: 0x000360B0
		[SecuritySafeCritical]
		internal static void GetPEKind(RuntimeModule module, out PortableExecutableKinds peKind, out ImageFileMachine machine)
		{
			int num;
			int num2;
			ModuleHandle.GetPEKind(module.GetNativeHandle(), out num, out num2);
			peKind = (PortableExecutableKinds)num;
			machine = (ImageFileMachine)num2;
		}

		// Token: 0x060012CE RID: 4814
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetMDStreamVersion(RuntimeModule module);

		// Token: 0x170001FA RID: 506
		// (get) Token: 0x060012CF RID: 4815 RVA: 0x00037ED2 File Offset: 0x000360D2
		public int MDStreamVersion
		{
			[SecuritySafeCritical]
			get
			{
				return ModuleHandle.GetMDStreamVersion(this.GetRuntimeModule().GetNativeHandle());
			}
		}

		// Token: 0x060012D0 RID: 4816
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern IntPtr _GetMetadataImport(RuntimeModule module);

		// Token: 0x060012D1 RID: 4817 RVA: 0x00037EE4 File Offset: 0x000360E4
		[SecurityCritical]
		internal static MetadataImport GetMetadataImport(RuntimeModule module)
		{
			return new MetadataImport(ModuleHandle._GetMetadataImport(module.GetNativeHandle()), module);
		}

		// Token: 0x04000675 RID: 1653
		public static readonly ModuleHandle EmptyHandle = ModuleHandle.GetEmptyMH();

		// Token: 0x04000676 RID: 1654
		private RuntimeModule m_ptr;
	}
}
