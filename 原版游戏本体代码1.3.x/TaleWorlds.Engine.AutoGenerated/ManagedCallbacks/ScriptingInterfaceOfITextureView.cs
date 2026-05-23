using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks
{
	// Token: 0x0200002C RID: 44
	internal class ScriptingInterfaceOfITextureView : ITextureView
	{
		// Token: 0x06000617 RID: 1559 RVA: 0x00019BDC File Offset: 0x00017DDC
		public TextureView CreateTextureView()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITextureView.call_CreateTextureViewDelegate();
			TextureView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new TextureView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x00019C25 File Offset: 0x00017E25
		public void SetTexture(UIntPtr pointer, UIntPtr texture_ptr)
		{
			ScriptingInterfaceOfITextureView.call_SetTextureDelegate(pointer, texture_ptr);
		}

		// Token: 0x04000568 RID: 1384
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000569 RID: 1385
		public static ScriptingInterfaceOfITextureView.CreateTextureViewDelegate call_CreateTextureViewDelegate;

		// Token: 0x0400056A RID: 1386
		public static ScriptingInterfaceOfITextureView.SetTextureDelegate call_SetTextureDelegate;

		// Token: 0x020005CB RID: 1483
		// (Invoke) Token: 0x06001D3F RID: 7487
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTextureViewDelegate();

		// Token: 0x020005CC RID: 1484
		// (Invoke) Token: 0x06001D43 RID: 7491
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTextureDelegate(UIntPtr pointer, UIntPtr texture_ptr);
	}
}
