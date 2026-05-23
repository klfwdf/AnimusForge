using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200002F RID: 47
	internal class ScriptingInterfaceOfITwoDimensionView : ITwoDimensionView
	{
		// Token: 0x06000629 RID: 1577 RVA: 0x00019E23 File Offset: 0x00018023
		public bool AddCachedTextMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData)
		{
			return ScriptingInterfaceOfITwoDimensionView.call_AddCachedTextMeshDelegate(pointer, material, ref meshDrawData);
		}

		// Token: 0x0600062A RID: 1578 RVA: 0x00019E32 File Offset: 0x00018032
		public void AddNewMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData)
		{
			ScriptingInterfaceOfITwoDimensionView.call_AddNewMeshDelegate(pointer, material, ref meshDrawData);
		}

		// Token: 0x0600062B RID: 1579 RVA: 0x00019E41 File Offset: 0x00018041
		public void AddNewQuadMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData)
		{
			ScriptingInterfaceOfITwoDimensionView.call_AddNewQuadMeshDelegate(pointer, material, ref meshDrawData);
		}

		// Token: 0x0600062C RID: 1580 RVA: 0x00019E50 File Offset: 0x00018050
		public void AddNewTextMesh(UIntPtr pointer, float[] vertices, float[] uvs, uint[] indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData)
		{
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(vertices, false);
			IntPtr pointer2 = pinnedArrayData.Pointer;
			PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(uvs, false);
			IntPtr pointer3 = pinnedArrayData2.Pointer;
			PinnedArrayData<uint> pinnedArrayData3 = new PinnedArrayData<uint>(indices, false);
			IntPtr pointer4 = pinnedArrayData3.Pointer;
			ScriptingInterfaceOfITwoDimensionView.call_AddNewTextMeshDelegate(pointer, pointer2, pointer3, pointer4, vertexCount, indexCount, material, ref meshDrawData);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			pinnedArrayData3.Dispose();
		}

		// Token: 0x0600062D RID: 1581 RVA: 0x00019EBE File Offset: 0x000180BE
		public void BeginFrame(UIntPtr pointer)
		{
			ScriptingInterfaceOfITwoDimensionView.call_BeginFrameDelegate(pointer);
		}

		// Token: 0x0600062E RID: 1582 RVA: 0x00019ECB File Offset: 0x000180CB
		public void Clear(UIntPtr pointer)
		{
			ScriptingInterfaceOfITwoDimensionView.call_ClearDelegate(pointer);
		}

		// Token: 0x0600062F RID: 1583 RVA: 0x00019ED8 File Offset: 0x000180D8
		public TwoDimensionView CreateTwoDimensionView(string viewName)
		{
			byte[] array = null;
			if (viewName != null)
			{
				int byteCount = ScriptingInterfaceOfITwoDimensionView._utf8.GetByteCount(viewName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITwoDimensionView._utf8.GetBytes(viewName, 0, viewName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITwoDimensionView.call_CreateTwoDimensionViewDelegate(array);
			TwoDimensionView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new TwoDimensionView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x00019F64 File Offset: 0x00018164
		public void EndFrame(UIntPtr pointer)
		{
			ScriptingInterfaceOfITwoDimensionView.call_EndFrameDelegate(pointer);
		}

		// Token: 0x06000631 RID: 1585 RVA: 0x00019F71 File Offset: 0x00018171
		public UIntPtr GetOrCreateMaterial(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture)
		{
			return ScriptingInterfaceOfITwoDimensionView.call_GetOrCreateMaterialDelegate(pointer, mainTexture, overlayTexture);
		}

		// Token: 0x04000577 RID: 1399
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000578 RID: 1400
		public static ScriptingInterfaceOfITwoDimensionView.AddCachedTextMeshDelegate call_AddCachedTextMeshDelegate;

		// Token: 0x04000579 RID: 1401
		public static ScriptingInterfaceOfITwoDimensionView.AddNewMeshDelegate call_AddNewMeshDelegate;

		// Token: 0x0400057A RID: 1402
		public static ScriptingInterfaceOfITwoDimensionView.AddNewQuadMeshDelegate call_AddNewQuadMeshDelegate;

		// Token: 0x0400057B RID: 1403
		public static ScriptingInterfaceOfITwoDimensionView.AddNewTextMeshDelegate call_AddNewTextMeshDelegate;

		// Token: 0x0400057C RID: 1404
		public static ScriptingInterfaceOfITwoDimensionView.BeginFrameDelegate call_BeginFrameDelegate;

		// Token: 0x0400057D RID: 1405
		public static ScriptingInterfaceOfITwoDimensionView.ClearDelegate call_ClearDelegate;

		// Token: 0x0400057E RID: 1406
		public static ScriptingInterfaceOfITwoDimensionView.CreateTwoDimensionViewDelegate call_CreateTwoDimensionViewDelegate;

		// Token: 0x0400057F RID: 1407
		public static ScriptingInterfaceOfITwoDimensionView.EndFrameDelegate call_EndFrameDelegate;

		// Token: 0x04000580 RID: 1408
		public static ScriptingInterfaceOfITwoDimensionView.GetOrCreateMaterialDelegate call_GetOrCreateMaterialDelegate;

		// Token: 0x020005D7 RID: 1495
		// (Invoke) Token: 0x06001D6F RID: 7535
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool AddCachedTextMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

		// Token: 0x020005D8 RID: 1496
		// (Invoke) Token: 0x06001D73 RID: 7539
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddNewMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

		// Token: 0x020005D9 RID: 1497
		// (Invoke) Token: 0x06001D77 RID: 7543
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddNewQuadMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

		// Token: 0x020005DA RID: 1498
		// (Invoke) Token: 0x06001D7B RID: 7547
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddNewTextMeshDelegate(UIntPtr pointer, IntPtr vertices, IntPtr uvs, IntPtr indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

		// Token: 0x020005DB RID: 1499
		// (Invoke) Token: 0x06001D7F RID: 7551
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BeginFrameDelegate(UIntPtr pointer);

		// Token: 0x020005DC RID: 1500
		// (Invoke) Token: 0x06001D83 RID: 7555
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearDelegate(UIntPtr pointer);

		// Token: 0x020005DD RID: 1501
		// (Invoke) Token: 0x06001D87 RID: 7559
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTwoDimensionViewDelegate(byte[] viewName);

		// Token: 0x020005DE RID: 1502
		// (Invoke) Token: 0x06001D8B RID: 7563
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EndFrameDelegate(UIntPtr pointer);

		// Token: 0x020005DF RID: 1503
		// (Invoke) Token: 0x06001D8F RID: 7567
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetOrCreateMaterialDelegate(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture);
	}
}
