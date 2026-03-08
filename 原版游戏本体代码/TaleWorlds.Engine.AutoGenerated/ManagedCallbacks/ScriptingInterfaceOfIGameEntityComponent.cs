using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x02000012 RID: 18
	internal class ScriptingInterfaceOfIGameEntityComponent : IGameEntityComponent
	{
		// Token: 0x0600024C RID: 588 RVA: 0x00011C3C File Offset: 0x0000FE3C
		public GameEntity GetEntity(GameEntityComponent entityComponent)
		{
			UIntPtr entityComponent2 = ((entityComponent != null) ? entityComponent.Pointer : UIntPtr.Zero);
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntityComponent.call_GetEntityDelegate(entityComponent2);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600024D RID: 589 RVA: 0x00011C9D File Offset: 0x0000FE9D
		public UIntPtr GetEntityPointer(UIntPtr componentPointer)
		{
			return ScriptingInterfaceOfIGameEntityComponent.call_GetEntityPointerDelegate(componentPointer);
		}

		// Token: 0x0600024E RID: 590 RVA: 0x00011CAC File Offset: 0x0000FEAC
		public MetaMesh GetFirstMetaMesh(GameEntityComponent entityComponent)
		{
			UIntPtr entityComponent2 = ((entityComponent != null) ? entityComponent.Pointer : UIntPtr.Zero);
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntityComponent.call_GetFirstMetaMeshDelegate(entityComponent2);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x040001C9 RID: 457
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040001CA RID: 458
		public static ScriptingInterfaceOfIGameEntityComponent.GetEntityDelegate call_GetEntityDelegate;

		// Token: 0x040001CB RID: 459
		public static ScriptingInterfaceOfIGameEntityComponent.GetEntityPointerDelegate call_GetEntityPointerDelegate;

		// Token: 0x040001CC RID: 460
		public static ScriptingInterfaceOfIGameEntityComponent.GetFirstMetaMeshDelegate call_GetFirstMetaMeshDelegate;

		// Token: 0x02000246 RID: 582
		// (Invoke) Token: 0x06000F2B RID: 3883
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetEntityDelegate(UIntPtr entityComponent);

		// Token: 0x02000247 RID: 583
		// (Invoke) Token: 0x06000F2F RID: 3887
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetEntityPointerDelegate(UIntPtr componentPointer);

		// Token: 0x02000248 RID: 584
		// (Invoke) Token: 0x06000F33 RID: 3891
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFirstMetaMeshDelegate(UIntPtr entityComponent);
	}
}
