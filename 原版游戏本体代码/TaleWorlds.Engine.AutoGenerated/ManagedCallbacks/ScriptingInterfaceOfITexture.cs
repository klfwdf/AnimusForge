using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200002B RID: 43
	internal class ScriptingInterfaceOfITexture : ITexture
	{
		// Token: 0x060005F8 RID: 1528 RVA: 0x000194E4 File Offset: 0x000176E4
		public Texture CheckAndGetFromResource(string textureName)
		{
			byte[] array = null;
			if (textureName != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(textureName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CheckAndGetFromResourceDelegate(array);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00019570 File Offset: 0x00017770
		public Texture CreateDepthTarget(string name, int width, int height)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CreateDepthTargetDelegate(array, width, height);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00019600 File Offset: 0x00017800
		public Texture CreateFromByteArray(byte[] data, int width, int height)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CreateFromByteArrayDelegate(data2, width, height);
			pinnedArrayData.Dispose();
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x00019678 File Offset: 0x00017878
		public Texture CreateFromMemory(byte[] data)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CreateFromMemoryDelegate(data2);
			pinnedArrayData.Dispose();
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x000196F0 File Offset: 0x000178F0
		public Texture CreateRenderTarget(string name, int width, int height, bool autoMipmaps, bool isTableau, bool createUninitialized, bool always_valid)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CreateRenderTargetDelegate(array, width, height, autoMipmaps, isTableau, createUninitialized, always_valid);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x00019788 File Offset: 0x00017988
		public Texture CreateTextureFromPath(PlatformFilePath filePath)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_CreateTextureFromPathDelegate(filePath);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x000197D2 File Offset: 0x000179D2
		public void GetCurObject(UIntPtr texturePointer, bool blocking)
		{
			ScriptingInterfaceOfITexture.call_GetCurObjectDelegate(texturePointer, blocking);
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x000197E0 File Offset: 0x000179E0
		public Texture GetFromResource(string textureName)
		{
			byte[] array = null;
			if (textureName != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(textureName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_GetFromResourceDelegate(array);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x0001986C File Offset: 0x00017A6C
		public int GetHeight(UIntPtr texturePointer)
		{
			return ScriptingInterfaceOfITexture.call_GetHeightDelegate(texturePointer);
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x00019879 File Offset: 0x00017A79
		public int GetMemorySize(UIntPtr texturePointer)
		{
			return ScriptingInterfaceOfITexture.call_GetMemorySizeDelegate(texturePointer);
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x00019886 File Offset: 0x00017A86
		public string GetName(UIntPtr texturePointer)
		{
			if (ScriptingInterfaceOfITexture.call_GetNameDelegate(texturePointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x000198A0 File Offset: 0x00017AA0
		public void GetPixelData(UIntPtr texturePointer, byte[] bytes)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(bytes, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray bytes2 = new ManagedArray(pointer, (bytes != null) ? bytes.Length : 0);
			ScriptingInterfaceOfITexture.call_GetPixelDataDelegate(texturePointer, bytes2);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x000198E2 File Offset: 0x00017AE2
		public RenderTargetComponent GetRenderTargetComponent(UIntPtr texturePointer)
		{
			return DotNetObject.GetManagedObjectWithId(ScriptingInterfaceOfITexture.call_GetRenderTargetComponentDelegate(texturePointer)) as RenderTargetComponent;
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x000198F9 File Offset: 0x00017AF9
		public void GetSDFBoundingBoxData(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max)
		{
			ScriptingInterfaceOfITexture.call_GetSDFBoundingBoxDataDelegate(texturePointer, ref min, ref max);
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00019908 File Offset: 0x00017B08
		public TableauView GetTableauView(UIntPtr texturePointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_GetTableauViewDelegate(texturePointer);
			TableauView result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new TableauView(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00019952 File Offset: 0x00017B52
		public int GetWidth(UIntPtr texturePointer)
		{
			return ScriptingInterfaceOfITexture.call_GetWidthDelegate(texturePointer);
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x0001995F File Offset: 0x00017B5F
		public bool IsLoaded(UIntPtr texturePointer)
		{
			return ScriptingInterfaceOfITexture.call_IsLoadedDelegate(texturePointer);
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x0001996C File Offset: 0x00017B6C
		public bool IsRenderTarget(UIntPtr texturePointer)
		{
			return ScriptingInterfaceOfITexture.call_IsRenderTargetDelegate(texturePointer);
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x0001997C File Offset: 0x00017B7C
		public Texture LoadTextureFromPath(string fileName, string folder)
		{
			byte[] array = null;
			if (fileName != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(fileName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (folder != null)
			{
				int byteCount2 = ScriptingInterfaceOfITexture._utf8.GetByteCount(folder);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(folder, 0, folder.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfITexture.call_LoadTextureFromPathDelegate(array, array2);
			Texture result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Texture(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00019A53 File Offset: 0x00017C53
		public void Release(UIntPtr texturePointer)
		{
			ScriptingInterfaceOfITexture.call_ReleaseDelegate(texturePointer);
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x00019A60 File Offset: 0x00017C60
		public void ReleaseAfterNumberOfFrames(UIntPtr texturePointer, int numberOfFrames)
		{
			ScriptingInterfaceOfITexture.call_ReleaseAfterNumberOfFramesDelegate(texturePointer, numberOfFrames);
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x00019A6E File Offset: 0x00017C6E
		public void ReleaseGpuMemories()
		{
			ScriptingInterfaceOfITexture.call_ReleaseGpuMemoriesDelegate();
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x00019A7A File Offset: 0x00017C7A
		public void ReleaseNextFrame(UIntPtr texturePointer)
		{
			ScriptingInterfaceOfITexture.call_ReleaseNextFrameDelegate(texturePointer);
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00019A87 File Offset: 0x00017C87
		public void RemoveContinousTableauTexture(UIntPtr texturePointer)
		{
			ScriptingInterfaceOfITexture.call_RemoveContinousTableauTextureDelegate(texturePointer);
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00019A94 File Offset: 0x00017C94
		public void SaveTextureAsAlwaysValid(UIntPtr texturePointer)
		{
			ScriptingInterfaceOfITexture.call_SaveTextureAsAlwaysValidDelegate(texturePointer);
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x00019AA4 File Offset: 0x00017CA4
		public void SaveToFile(UIntPtr texturePointer, string fileName, bool isRelativePath)
		{
			byte[] array = null;
			if (fileName != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(fileName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(fileName, 0, fileName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfITexture.call_SaveToFileDelegate(texturePointer, array, isRelativePath);
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x00019B00 File Offset: 0x00017D00
		public void SetName(UIntPtr texturePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfITexture.call_SetNameDelegate(texturePointer, array);
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x00019B5B File Offset: 0x00017D5B
		public void SetTableauView(UIntPtr texturePointer, UIntPtr tableauView)
		{
			ScriptingInterfaceOfITexture.call_SetTableauViewDelegate(texturePointer, tableauView);
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x00019B6C File Offset: 0x00017D6C
		public void TransformRenderTargetToResourceTexture(UIntPtr texturePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfITexture._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfITexture._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfITexture.call_TransformRenderTargetToResourceTextureDelegate(texturePointer, array);
		}

		// Token: 0x0400054A RID: 1354
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400054B RID: 1355
		public static ScriptingInterfaceOfITexture.CheckAndGetFromResourceDelegate call_CheckAndGetFromResourceDelegate;

		// Token: 0x0400054C RID: 1356
		public static ScriptingInterfaceOfITexture.CreateDepthTargetDelegate call_CreateDepthTargetDelegate;

		// Token: 0x0400054D RID: 1357
		public static ScriptingInterfaceOfITexture.CreateFromByteArrayDelegate call_CreateFromByteArrayDelegate;

		// Token: 0x0400054E RID: 1358
		public static ScriptingInterfaceOfITexture.CreateFromMemoryDelegate call_CreateFromMemoryDelegate;

		// Token: 0x0400054F RID: 1359
		public static ScriptingInterfaceOfITexture.CreateRenderTargetDelegate call_CreateRenderTargetDelegate;

		// Token: 0x04000550 RID: 1360
		public static ScriptingInterfaceOfITexture.CreateTextureFromPathDelegate call_CreateTextureFromPathDelegate;

		// Token: 0x04000551 RID: 1361
		public static ScriptingInterfaceOfITexture.GetCurObjectDelegate call_GetCurObjectDelegate;

		// Token: 0x04000552 RID: 1362
		public static ScriptingInterfaceOfITexture.GetFromResourceDelegate call_GetFromResourceDelegate;

		// Token: 0x04000553 RID: 1363
		public static ScriptingInterfaceOfITexture.GetHeightDelegate call_GetHeightDelegate;

		// Token: 0x04000554 RID: 1364
		public static ScriptingInterfaceOfITexture.GetMemorySizeDelegate call_GetMemorySizeDelegate;

		// Token: 0x04000555 RID: 1365
		public static ScriptingInterfaceOfITexture.GetNameDelegate call_GetNameDelegate;

		// Token: 0x04000556 RID: 1366
		public static ScriptingInterfaceOfITexture.GetPixelDataDelegate call_GetPixelDataDelegate;

		// Token: 0x04000557 RID: 1367
		public static ScriptingInterfaceOfITexture.GetRenderTargetComponentDelegate call_GetRenderTargetComponentDelegate;

		// Token: 0x04000558 RID: 1368
		public static ScriptingInterfaceOfITexture.GetSDFBoundingBoxDataDelegate call_GetSDFBoundingBoxDataDelegate;

		// Token: 0x04000559 RID: 1369
		public static ScriptingInterfaceOfITexture.GetTableauViewDelegate call_GetTableauViewDelegate;

		// Token: 0x0400055A RID: 1370
		public static ScriptingInterfaceOfITexture.GetWidthDelegate call_GetWidthDelegate;

		// Token: 0x0400055B RID: 1371
		public static ScriptingInterfaceOfITexture.IsLoadedDelegate call_IsLoadedDelegate;

		// Token: 0x0400055C RID: 1372
		public static ScriptingInterfaceOfITexture.IsRenderTargetDelegate call_IsRenderTargetDelegate;

		// Token: 0x0400055D RID: 1373
		public static ScriptingInterfaceOfITexture.LoadTextureFromPathDelegate call_LoadTextureFromPathDelegate;

		// Token: 0x0400055E RID: 1374
		public static ScriptingInterfaceOfITexture.ReleaseDelegate call_ReleaseDelegate;

		// Token: 0x0400055F RID: 1375
		public static ScriptingInterfaceOfITexture.ReleaseAfterNumberOfFramesDelegate call_ReleaseAfterNumberOfFramesDelegate;

		// Token: 0x04000560 RID: 1376
		public static ScriptingInterfaceOfITexture.ReleaseGpuMemoriesDelegate call_ReleaseGpuMemoriesDelegate;

		// Token: 0x04000561 RID: 1377
		public static ScriptingInterfaceOfITexture.ReleaseNextFrameDelegate call_ReleaseNextFrameDelegate;

		// Token: 0x04000562 RID: 1378
		public static ScriptingInterfaceOfITexture.RemoveContinousTableauTextureDelegate call_RemoveContinousTableauTextureDelegate;

		// Token: 0x04000563 RID: 1379
		public static ScriptingInterfaceOfITexture.SaveTextureAsAlwaysValidDelegate call_SaveTextureAsAlwaysValidDelegate;

		// Token: 0x04000564 RID: 1380
		public static ScriptingInterfaceOfITexture.SaveToFileDelegate call_SaveToFileDelegate;

		// Token: 0x04000565 RID: 1381
		public static ScriptingInterfaceOfITexture.SetNameDelegate call_SetNameDelegate;

		// Token: 0x04000566 RID: 1382
		public static ScriptingInterfaceOfITexture.SetTableauViewDelegate call_SetTableauViewDelegate;

		// Token: 0x04000567 RID: 1383
		public static ScriptingInterfaceOfITexture.TransformRenderTargetToResourceTextureDelegate call_TransformRenderTargetToResourceTextureDelegate;

		// Token: 0x020005AE RID: 1454
		// (Invoke) Token: 0x06001CCB RID: 7371
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CheckAndGetFromResourceDelegate(byte[] textureName);

		// Token: 0x020005AF RID: 1455
		// (Invoke) Token: 0x06001CCF RID: 7375
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDepthTargetDelegate(byte[] name, int width, int height);

		// Token: 0x020005B0 RID: 1456
		// (Invoke) Token: 0x06001CD3 RID: 7379
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateFromByteArrayDelegate(ManagedArray data, int width, int height);

		// Token: 0x020005B1 RID: 1457
		// (Invoke) Token: 0x06001CD7 RID: 7383
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateFromMemoryDelegate(ManagedArray data);

		// Token: 0x020005B2 RID: 1458
		// (Invoke) Token: 0x06001CDB RID: 7387
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateRenderTargetDelegate(byte[] name, int width, int height, [MarshalAs(UnmanagedType.U1)] bool autoMipmaps, [MarshalAs(UnmanagedType.U1)] bool isTableau, [MarshalAs(UnmanagedType.U1)] bool createUninitialized, [MarshalAs(UnmanagedType.U1)] bool always_valid);

		// Token: 0x020005B3 RID: 1459
		// (Invoke) Token: 0x06001CDF RID: 7391
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTextureFromPathDelegate(PlatformFilePath filePath);

		// Token: 0x020005B4 RID: 1460
		// (Invoke) Token: 0x06001CE3 RID: 7395
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetCurObjectDelegate(UIntPtr texturePointer, [MarshalAs(UnmanagedType.U1)] bool blocking);

		// Token: 0x020005B5 RID: 1461
		// (Invoke) Token: 0x06001CE7 RID: 7399
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFromResourceDelegate(byte[] textureName);

		// Token: 0x020005B6 RID: 1462
		// (Invoke) Token: 0x06001CEB RID: 7403
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetHeightDelegate(UIntPtr texturePointer);

		// Token: 0x020005B7 RID: 1463
		// (Invoke) Token: 0x06001CEF RID: 7407
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMemorySizeDelegate(UIntPtr texturePointer);

		// Token: 0x020005B8 RID: 1464
		// (Invoke) Token: 0x06001CF3 RID: 7411
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr texturePointer);

		// Token: 0x020005B9 RID: 1465
		// (Invoke) Token: 0x06001CF7 RID: 7415
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPixelDataDelegate(UIntPtr texturePointer, ManagedArray bytes);

		// Token: 0x020005BA RID: 1466
		// (Invoke) Token: 0x06001CFB RID: 7419
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetRenderTargetComponentDelegate(UIntPtr texturePointer);

		// Token: 0x020005BB RID: 1467
		// (Invoke) Token: 0x06001CFF RID: 7423
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetSDFBoundingBoxDataDelegate(UIntPtr texturePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x020005BC RID: 1468
		// (Invoke) Token: 0x06001D03 RID: 7427
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetTableauViewDelegate(UIntPtr texturePointer);

		// Token: 0x020005BD RID: 1469
		// (Invoke) Token: 0x06001D07 RID: 7431
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetWidthDelegate(UIntPtr texturePointer);

		// Token: 0x020005BE RID: 1470
		// (Invoke) Token: 0x06001D0B RID: 7435
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsLoadedDelegate(UIntPtr texturePointer);

		// Token: 0x020005BF RID: 1471
		// (Invoke) Token: 0x06001D0F RID: 7439
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsRenderTargetDelegate(UIntPtr texturePointer);

		// Token: 0x020005C0 RID: 1472
		// (Invoke) Token: 0x06001D13 RID: 7443
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer LoadTextureFromPathDelegate(byte[] fileName, byte[] folder);

		// Token: 0x020005C1 RID: 1473
		// (Invoke) Token: 0x06001D17 RID: 7447
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseDelegate(UIntPtr texturePointer);

		// Token: 0x020005C2 RID: 1474
		// (Invoke) Token: 0x06001D1B RID: 7451
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseAfterNumberOfFramesDelegate(UIntPtr texturePointer, int numberOfFrames);

		// Token: 0x020005C3 RID: 1475
		// (Invoke) Token: 0x06001D1F RID: 7455
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseGpuMemoriesDelegate();

		// Token: 0x020005C4 RID: 1476
		// (Invoke) Token: 0x06001D23 RID: 7459
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseNextFrameDelegate(UIntPtr texturePointer);

		// Token: 0x020005C5 RID: 1477
		// (Invoke) Token: 0x06001D27 RID: 7463
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveContinousTableauTextureDelegate(UIntPtr texturePointer);

		// Token: 0x020005C6 RID: 1478
		// (Invoke) Token: 0x06001D2B RID: 7467
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SaveTextureAsAlwaysValidDelegate(UIntPtr texturePointer);

		// Token: 0x020005C7 RID: 1479
		// (Invoke) Token: 0x06001D2F RID: 7471
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SaveToFileDelegate(UIntPtr texturePointer, byte[] fileName, [MarshalAs(UnmanagedType.U1)] bool isRelativePath);

		// Token: 0x020005C8 RID: 1480
		// (Invoke) Token: 0x06001D33 RID: 7475
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNameDelegate(UIntPtr texturePointer, byte[] name);

		// Token: 0x020005C9 RID: 1481
		// (Invoke) Token: 0x06001D37 RID: 7479
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTableauViewDelegate(UIntPtr texturePointer, UIntPtr tableauView);

		// Token: 0x020005CA RID: 1482
		// (Invoke) Token: 0x06001D3B RID: 7483
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TransformRenderTargetToResourceTextureDelegate(UIntPtr texturePointer, byte[] name);
	}
}
