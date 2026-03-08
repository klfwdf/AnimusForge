using System;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A13 RID: 2579
	internal sealed class WinRTClassActivator : MarshalByRefObject, IWinRTClassActivator
	{
		// Token: 0x060065BA RID: 26042 RVA: 0x00159C08 File Offset: 0x00157E08
		[SecurityCritical]
		public object ActivateInstance(string activatableClassId)
		{
			ManagedActivationFactory managedActivationFactory = WindowsRuntimeMarshal.GetManagedActivationFactory(this.LoadWinRTType(activatableClassId));
			return managedActivationFactory.ActivateInstance();
		}

		// Token: 0x060065BB RID: 26043 RVA: 0x00159C28 File Offset: 0x00157E28
		[SecurityCritical]
		public IntPtr GetActivationFactory(string activatableClassId, ref Guid iid)
		{
			IntPtr intPtr = IntPtr.Zero;
			IntPtr result;
			try
			{
				intPtr = WindowsRuntimeMarshal.GetActivationFactoryForType(this.LoadWinRTType(activatableClassId));
				IntPtr zero = IntPtr.Zero;
				int num = Marshal.QueryInterface(intPtr, ref iid, out zero);
				if (num < 0)
				{
					Marshal.ThrowExceptionForHR(num);
				}
				result = zero;
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.Release(intPtr);
				}
			}
			return result;
		}

		// Token: 0x060065BC RID: 26044 RVA: 0x00159C8C File Offset: 0x00157E8C
		private Type LoadWinRTType(string acid)
		{
			Type type = Type.GetType(acid + ", Windows, ContentType=WindowsRuntime");
			if (type == null)
			{
				throw new COMException(-2147221164);
			}
			return type;
		}

		// Token: 0x060065BD RID: 26045 RVA: 0x00159CBF File Offset: 0x00157EBF
		[SecurityCritical]
		internal IntPtr GetIWinRTClassActivator()
		{
			return Marshal.GetComInterfaceForObject(this, typeof(IWinRTClassActivator));
		}
	}
}
