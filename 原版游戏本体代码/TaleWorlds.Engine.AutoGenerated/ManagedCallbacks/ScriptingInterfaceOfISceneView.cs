using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000023 RID: 35
	internal class ScriptingInterfaceOfISceneView : ISceneView
	{
		// Token: 0x0600053A RID: 1338 RVA: 0x00017C35 File Offset: 0x00015E35
		public void AddClearTask(UIntPtr ptr, bool clearOnlySceneview)
		{
			ScriptingInterfaceOfISceneView.call_AddClearTaskDelegate(ptr, clearOnlySceneview);
		}

		// Token: 0x0600053B RID: 1339 RVA: 0x00017C43 File Offset: 0x00015E43
		public bool CheckSceneReadyToRender(UIntPtr ptr)
		{
			return ScriptingInterfaceOfISceneView.call_CheckSceneReadyToRenderDelegate(ptr);
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x00017C50 File Offset: 0x00015E50
		public void ClearAll(UIntPtr pointer, bool clear_scene, bool remove_terrain)
		{
			ScriptingInterfaceOfISceneView.call_ClearAllDelegate(pointer, clear_scene, remove_terrain);
		}

		// Token: 0x0600053D RID: 1341 RVA: 0x00017C60 File Offset: 0x00015E60
		public SceneView CreateSceneView()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfISceneView.call_CreateSceneViewDelegate();
			SceneView result = NativeObject.CreateNativeObjectWrapper<SceneView>(nativeObjectPointer);
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00017CA0 File Offset: 0x00015EA0
		public void DoNotClear(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_DoNotClearDelegate(pointer, value);
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00017CB0 File Offset: 0x00015EB0
		public Scene GetScene(UIntPtr ptr)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfISceneView.call_GetSceneDelegate(ptr);
			Scene result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Scene(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00017CFA File Offset: 0x00015EFA
		public bool ProjectedMousePositionOnGround(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, bool mouseVisible, BodyFlags excludeBodyOwnerFlags, bool checkOccludedSurface)
		{
			return ScriptingInterfaceOfISceneView.call_ProjectedMousePositionOnGroundDelegate(pointer, out groundPosition, out groundNormal, mouseVisible, excludeBodyOwnerFlags, checkOccludedSurface);
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x00017D0F File Offset: 0x00015F0F
		public bool ProjectedMousePositionOnWater(UIntPtr pointer, out Vec3 groundPosition, bool mouseVisible)
		{
			return ScriptingInterfaceOfISceneView.call_ProjectedMousePositionOnWaterDelegate(pointer, out groundPosition, mouseVisible);
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x00017D20 File Offset: 0x00015F20
		public bool RayCastForClosestEntityOrTerrain(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags)
		{
			return ScriptingInterfaceOfISceneView.call_RayCastForClosestEntityOrTerrainDelegate(ptr, ref sourcePoint, ref targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags);
		}

		// Token: 0x06000543 RID: 1347 RVA: 0x00017D44 File Offset: 0x00015F44
		public bool ReadyToRender(UIntPtr pointer)
		{
			return ScriptingInterfaceOfISceneView.call_ReadyToRenderDelegate(pointer);
		}

		// Token: 0x06000544 RID: 1348 RVA: 0x00017D51 File Offset: 0x00015F51
		public Vec2 ScreenPointToViewportPoint(UIntPtr ptr, float position_x, float position_y)
		{
			return ScriptingInterfaceOfISceneView.call_ScreenPointToViewportPointDelegate(ptr, position_x, position_y);
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x00017D60 File Offset: 0x00015F60
		public void SetAcceptGlobalDebugRenderObjects(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetAcceptGlobalDebugRenderObjectsDelegate(ptr, value);
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x00017D6E File Offset: 0x00015F6E
		public void SetCamera(UIntPtr ptr, UIntPtr cameraPtr)
		{
			ScriptingInterfaceOfISceneView.call_SetCameraDelegate(ptr, cameraPtr);
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x00017D7C File Offset: 0x00015F7C
		public void SetCleanScreenUntilLoadingDone(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetCleanScreenUntilLoadingDoneDelegate(pointer, value);
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x00017D8A File Offset: 0x00015F8A
		public void SetClearAndDisableAfterSucessfullRender(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetClearAndDisableAfterSucessfullRenderDelegate(pointer, value);
		}

		// Token: 0x06000549 RID: 1353 RVA: 0x00017D98 File Offset: 0x00015F98
		public void SetClearGbuffer(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetClearGbufferDelegate(pointer, value);
		}

		// Token: 0x0600054A RID: 1354 RVA: 0x00017DA6 File Offset: 0x00015FA6
		public void SetDoQuickExposure(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetDoQuickExposureDelegate(ptr, value);
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x00017DB4 File Offset: 0x00015FB4
		public void SetFocusedShadowmap(UIntPtr ptr, bool enable, ref Vec3 center, float radius)
		{
			ScriptingInterfaceOfISceneView.call_SetFocusedShadowmapDelegate(ptr, enable, ref center, radius);
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x00017DC5 File Offset: 0x00015FC5
		public void SetForceShaderCompilation(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetForceShaderCompilationDelegate(ptr, value);
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x00017DD3 File Offset: 0x00015FD3
		public void SetPointlightResolutionMultiplier(UIntPtr pointer, float value)
		{
			ScriptingInterfaceOfISceneView.call_SetPointlightResolutionMultiplierDelegate(pointer, value);
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x00017DE1 File Offset: 0x00015FE1
		public void SetPostfxConfigParams(UIntPtr ptr, int value)
		{
			ScriptingInterfaceOfISceneView.call_SetPostfxConfigParamsDelegate(ptr, value);
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x00017DEF File Offset: 0x00015FEF
		public void SetPostfxFromConfig(UIntPtr ptr)
		{
			ScriptingInterfaceOfISceneView.call_SetPostfxFromConfigDelegate(ptr);
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x00017DFC File Offset: 0x00015FFC
		public void SetRenderWithPostfx(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetRenderWithPostfxDelegate(ptr, value);
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x00017E0A File Offset: 0x0001600A
		public void SetResolutionScaling(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetResolutionScalingDelegate(ptr, value);
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x00017E18 File Offset: 0x00016018
		public void SetScene(UIntPtr ptr, UIntPtr scenePtr)
		{
			ScriptingInterfaceOfISceneView.call_SetSceneDelegate(ptr, scenePtr);
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x00017E26 File Offset: 0x00016026
		public void SetSceneUsesContour(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetSceneUsesContourDelegate(pointer, value);
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x00017E34 File Offset: 0x00016034
		public void SetSceneUsesShadows(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetSceneUsesShadowsDelegate(pointer, value);
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x00017E42 File Offset: 0x00016042
		public void SetSceneUsesSkybox(UIntPtr pointer, bool value)
		{
			ScriptingInterfaceOfISceneView.call_SetSceneUsesSkyboxDelegate(pointer, value);
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x00017E50 File Offset: 0x00016050
		public void SetShadowmapResolutionMultiplier(UIntPtr pointer, float value)
		{
			ScriptingInterfaceOfISceneView.call_SetShadowmapResolutionMultiplierDelegate(pointer, value);
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00017E5E File Offset: 0x0001605E
		public void TranslateMouse(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance)
		{
			ScriptingInterfaceOfISceneView.call_TranslateMouseDelegate(pointer, ref worldMouseNear, ref worldMouseFar, maxDistance);
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x00017E6F File Offset: 0x0001606F
		public Vec2 WorldPointToScreenPoint(UIntPtr ptr, Vec3 position)
		{
			return ScriptingInterfaceOfISceneView.call_WorldPointToScreenPointDelegate(ptr, position);
		}

		// Token: 0x04000494 RID: 1172
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000495 RID: 1173
		public static ScriptingInterfaceOfISceneView.AddClearTaskDelegate call_AddClearTaskDelegate;

		// Token: 0x04000496 RID: 1174
		public static ScriptingInterfaceOfISceneView.CheckSceneReadyToRenderDelegate call_CheckSceneReadyToRenderDelegate;

		// Token: 0x04000497 RID: 1175
		public static ScriptingInterfaceOfISceneView.ClearAllDelegate call_ClearAllDelegate;

		// Token: 0x04000498 RID: 1176
		public static ScriptingInterfaceOfISceneView.CreateSceneViewDelegate call_CreateSceneViewDelegate;

		// Token: 0x04000499 RID: 1177
		public static ScriptingInterfaceOfISceneView.DoNotClearDelegate call_DoNotClearDelegate;

		// Token: 0x0400049A RID: 1178
		public static ScriptingInterfaceOfISceneView.GetSceneDelegate call_GetSceneDelegate;

		// Token: 0x0400049B RID: 1179
		public static ScriptingInterfaceOfISceneView.ProjectedMousePositionOnGroundDelegate call_ProjectedMousePositionOnGroundDelegate;

		// Token: 0x0400049C RID: 1180
		public static ScriptingInterfaceOfISceneView.ProjectedMousePositionOnWaterDelegate call_ProjectedMousePositionOnWaterDelegate;

		// Token: 0x0400049D RID: 1181
		public static ScriptingInterfaceOfISceneView.RayCastForClosestEntityOrTerrainDelegate call_RayCastForClosestEntityOrTerrainDelegate;

		// Token: 0x0400049E RID: 1182
		public static ScriptingInterfaceOfISceneView.ReadyToRenderDelegate call_ReadyToRenderDelegate;

		// Token: 0x0400049F RID: 1183
		public static ScriptingInterfaceOfISceneView.ScreenPointToViewportPointDelegate call_ScreenPointToViewportPointDelegate;

		// Token: 0x040004A0 RID: 1184
		public static ScriptingInterfaceOfISceneView.SetAcceptGlobalDebugRenderObjectsDelegate call_SetAcceptGlobalDebugRenderObjectsDelegate;

		// Token: 0x040004A1 RID: 1185
		public static ScriptingInterfaceOfISceneView.SetCameraDelegate call_SetCameraDelegate;

		// Token: 0x040004A2 RID: 1186
		public static ScriptingInterfaceOfISceneView.SetCleanScreenUntilLoadingDoneDelegate call_SetCleanScreenUntilLoadingDoneDelegate;

		// Token: 0x040004A3 RID: 1187
		public static ScriptingInterfaceOfISceneView.SetClearAndDisableAfterSucessfullRenderDelegate call_SetClearAndDisableAfterSucessfullRenderDelegate;

		// Token: 0x040004A4 RID: 1188
		public static ScriptingInterfaceOfISceneView.SetClearGbufferDelegate call_SetClearGbufferDelegate;

		// Token: 0x040004A5 RID: 1189
		public static ScriptingInterfaceOfISceneView.SetDoQuickExposureDelegate call_SetDoQuickExposureDelegate;

		// Token: 0x040004A6 RID: 1190
		public static ScriptingInterfaceOfISceneView.SetFocusedShadowmapDelegate call_SetFocusedShadowmapDelegate;

		// Token: 0x040004A7 RID: 1191
		public static ScriptingInterfaceOfISceneView.SetForceShaderCompilationDelegate call_SetForceShaderCompilationDelegate;

		// Token: 0x040004A8 RID: 1192
		public static ScriptingInterfaceOfISceneView.SetPointlightResolutionMultiplierDelegate call_SetPointlightResolutionMultiplierDelegate;

		// Token: 0x040004A9 RID: 1193
		public static ScriptingInterfaceOfISceneView.SetPostfxConfigParamsDelegate call_SetPostfxConfigParamsDelegate;

		// Token: 0x040004AA RID: 1194
		public static ScriptingInterfaceOfISceneView.SetPostfxFromConfigDelegate call_SetPostfxFromConfigDelegate;

		// Token: 0x040004AB RID: 1195
		public static ScriptingInterfaceOfISceneView.SetRenderWithPostfxDelegate call_SetRenderWithPostfxDelegate;

		// Token: 0x040004AC RID: 1196
		public static ScriptingInterfaceOfISceneView.SetResolutionScalingDelegate call_SetResolutionScalingDelegate;

		// Token: 0x040004AD RID: 1197
		public static ScriptingInterfaceOfISceneView.SetSceneDelegate call_SetSceneDelegate;

		// Token: 0x040004AE RID: 1198
		public static ScriptingInterfaceOfISceneView.SetSceneUsesContourDelegate call_SetSceneUsesContourDelegate;

		// Token: 0x040004AF RID: 1199
		public static ScriptingInterfaceOfISceneView.SetSceneUsesShadowsDelegate call_SetSceneUsesShadowsDelegate;

		// Token: 0x040004B0 RID: 1200
		public static ScriptingInterfaceOfISceneView.SetSceneUsesSkyboxDelegate call_SetSceneUsesSkyboxDelegate;

		// Token: 0x040004B1 RID: 1201
		public static ScriptingInterfaceOfISceneView.SetShadowmapResolutionMultiplierDelegate call_SetShadowmapResolutionMultiplierDelegate;

		// Token: 0x040004B2 RID: 1202
		public static ScriptingInterfaceOfISceneView.TranslateMouseDelegate call_TranslateMouseDelegate;

		// Token: 0x040004B3 RID: 1203
		public static ScriptingInterfaceOfISceneView.WorldPointToScreenPointDelegate call_WorldPointToScreenPointDelegate;

		// Token: 0x02000500 RID: 1280
		// (Invoke) Token: 0x06001A13 RID: 6675
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddClearTaskDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool clearOnlySceneview);

		// Token: 0x02000501 RID: 1281
		// (Invoke) Token: 0x06001A17 RID: 6679
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckSceneReadyToRenderDelegate(UIntPtr ptr);

		// Token: 0x02000502 RID: 1282
		// (Invoke) Token: 0x06001A1B RID: 6683
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearAllDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool clear_scene, [MarshalAs(UnmanagedType.U1)] bool remove_terrain);

		// Token: 0x02000503 RID: 1283
		// (Invoke) Token: 0x06001A1F RID: 6687
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateSceneViewDelegate();

		// Token: 0x02000504 RID: 1284
		// (Invoke) Token: 0x06001A23 RID: 6691
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DoNotClearDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000505 RID: 1285
		// (Invoke) Token: 0x06001A27 RID: 6695
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetSceneDelegate(UIntPtr ptr);

		// Token: 0x02000506 RID: 1286
		// (Invoke) Token: 0x06001A2B RID: 6699
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ProjectedMousePositionOnGroundDelegate(UIntPtr pointer, out Vec3 groundPosition, out Vec3 groundNormal, [MarshalAs(UnmanagedType.U1)] bool mouseVisible, BodyFlags excludeBodyOwnerFlags, [MarshalAs(UnmanagedType.U1)] bool checkOccludedSurface);

		// Token: 0x02000507 RID: 1287
		// (Invoke) Token: 0x06001A2F RID: 6703
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ProjectedMousePositionOnWaterDelegate(UIntPtr pointer, out Vec3 groundPosition, [MarshalAs(UnmanagedType.U1)] bool mouseVisible);

		// Token: 0x02000508 RID: 1288
		// (Invoke) Token: 0x06001A33 RID: 6707
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayCastForClosestEntityOrTerrainDelegate(UIntPtr ptr, ref Vec3 sourcePoint, ref Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

		// Token: 0x02000509 RID: 1289
		// (Invoke) Token: 0x06001A37 RID: 6711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ReadyToRenderDelegate(UIntPtr pointer);

		// Token: 0x0200050A RID: 1290
		// (Invoke) Token: 0x06001A3B RID: 6715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 ScreenPointToViewportPointDelegate(UIntPtr ptr, float position_x, float position_y);

		// Token: 0x0200050B RID: 1291
		// (Invoke) Token: 0x06001A3F RID: 6719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAcceptGlobalDebugRenderObjectsDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200050C RID: 1292
		// (Invoke) Token: 0x06001A43 RID: 6723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCameraDelegate(UIntPtr ptr, UIntPtr cameraPtr);

		// Token: 0x0200050D RID: 1293
		// (Invoke) Token: 0x06001A47 RID: 6727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCleanScreenUntilLoadingDoneDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200050E RID: 1294
		// (Invoke) Token: 0x06001A4B RID: 6731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClearAndDisableAfterSucessfullRenderDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200050F RID: 1295
		// (Invoke) Token: 0x06001A4F RID: 6735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClearGbufferDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000510 RID: 1296
		// (Invoke) Token: 0x06001A53 RID: 6739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDoQuickExposureDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000511 RID: 1297
		// (Invoke) Token: 0x06001A57 RID: 6743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFocusedShadowmapDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool enable, ref Vec3 center, float radius);

		// Token: 0x02000512 RID: 1298
		// (Invoke) Token: 0x06001A5B RID: 6747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForceShaderCompilationDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000513 RID: 1299
		// (Invoke) Token: 0x06001A5F RID: 6751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPointlightResolutionMultiplierDelegate(UIntPtr pointer, float value);

		// Token: 0x02000514 RID: 1300
		// (Invoke) Token: 0x06001A63 RID: 6755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPostfxConfigParamsDelegate(UIntPtr ptr, int value);

		// Token: 0x02000515 RID: 1301
		// (Invoke) Token: 0x06001A67 RID: 6759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPostfxFromConfigDelegate(UIntPtr ptr);

		// Token: 0x02000516 RID: 1302
		// (Invoke) Token: 0x06001A6B RID: 6763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRenderWithPostfxDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000517 RID: 1303
		// (Invoke) Token: 0x06001A6F RID: 6767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetResolutionScalingDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000518 RID: 1304
		// (Invoke) Token: 0x06001A73 RID: 6771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneDelegate(UIntPtr ptr, UIntPtr scenePtr);

		// Token: 0x02000519 RID: 1305
		// (Invoke) Token: 0x06001A77 RID: 6775
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneUsesContourDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200051A RID: 1306
		// (Invoke) Token: 0x06001A7B RID: 6779
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneUsesShadowsDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200051B RID: 1307
		// (Invoke) Token: 0x06001A7F RID: 6783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneUsesSkyboxDelegate(UIntPtr pointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200051C RID: 1308
		// (Invoke) Token: 0x06001A83 RID: 6787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetShadowmapResolutionMultiplierDelegate(UIntPtr pointer, float value);

		// Token: 0x0200051D RID: 1309
		// (Invoke) Token: 0x06001A87 RID: 6791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TranslateMouseDelegate(UIntPtr pointer, ref Vec3 worldMouseNear, ref Vec3 worldMouseFar, float maxDistance);

		// Token: 0x0200051E RID: 1310
		// (Invoke) Token: 0x06001A8B RID: 6795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 WorldPointToScreenPointDelegate(UIntPtr ptr, Vec3 position);
	}
}
