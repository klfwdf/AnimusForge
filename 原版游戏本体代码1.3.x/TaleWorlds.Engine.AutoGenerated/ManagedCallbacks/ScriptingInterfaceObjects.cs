using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ManagedCallbacks
{
	// Token: 0x02000007 RID: 7
	internal static class ScriptingInterfaceObjects
	{
		// Token: 0x06000066 RID: 102 RVA: 0x00002ECC File Offset: 0x000010CC
		public static Dictionary<string, object> GetObjects()
		{
			return new Dictionary<string, object>
			{
				{
					"TaleWorlds.Engine.IAsyncTask",
					new ScriptingInterfaceOfIAsyncTask()
				},
				{
					"TaleWorlds.Engine.IBodyPart",
					new ScriptingInterfaceOfIBodyPart()
				},
				{
					"TaleWorlds.Engine.ICamera",
					new ScriptingInterfaceOfICamera()
				},
				{
					"TaleWorlds.Engine.IClothSimulatorComponent",
					new ScriptingInterfaceOfIClothSimulatorComponent()
				},
				{
					"TaleWorlds.Engine.ICompositeComponent",
					new ScriptingInterfaceOfICompositeComponent()
				},
				{
					"TaleWorlds.Engine.IConfig",
					new ScriptingInterfaceOfIConfig()
				},
				{
					"TaleWorlds.Engine.IDebug",
					new ScriptingInterfaceOfIDebug()
				},
				{
					"TaleWorlds.Engine.IDecal",
					new ScriptingInterfaceOfIDecal()
				},
				{
					"TaleWorlds.Engine.IEngineSizeChecker",
					new ScriptingInterfaceOfIEngineSizeChecker()
				},
				{
					"TaleWorlds.Engine.IGameEntity",
					new ScriptingInterfaceOfIGameEntity()
				},
				{
					"TaleWorlds.Engine.IGameEntityComponent",
					new ScriptingInterfaceOfIGameEntityComponent()
				},
				{
					"TaleWorlds.Engine.IHighlights",
					new ScriptingInterfaceOfIHighlights()
				},
				{
					"TaleWorlds.Engine.IImgui",
					new ScriptingInterfaceOfIImgui()
				},
				{
					"TaleWorlds.Engine.IInput",
					new ScriptingInterfaceOfIInput()
				},
				{
					"TaleWorlds.Engine.ILight",
					new ScriptingInterfaceOfILight()
				},
				{
					"TaleWorlds.Engine.IManagedMeshEditOperations",
					new ScriptingInterfaceOfIManagedMeshEditOperations()
				},
				{
					"TaleWorlds.Engine.IMaterial",
					new ScriptingInterfaceOfIMaterial()
				},
				{
					"TaleWorlds.Engine.IMesh",
					new ScriptingInterfaceOfIMesh()
				},
				{
					"TaleWorlds.Engine.IMeshBuilder",
					new ScriptingInterfaceOfIMeshBuilder()
				},
				{
					"TaleWorlds.Engine.IMetaMesh",
					new ScriptingInterfaceOfIMetaMesh()
				},
				{
					"TaleWorlds.Engine.IMouseManager",
					new ScriptingInterfaceOfIMouseManager()
				},
				{
					"TaleWorlds.Engine.IMusic",
					new ScriptingInterfaceOfIMusic()
				},
				{
					"TaleWorlds.Engine.IParticleSystem",
					new ScriptingInterfaceOfIParticleSystem()
				},
				{
					"TaleWorlds.Engine.IPath",
					new ScriptingInterfaceOfIPath()
				},
				{
					"TaleWorlds.Engine.IPhysicsMaterial",
					new ScriptingInterfaceOfIPhysicsMaterial()
				},
				{
					"TaleWorlds.Engine.IPhysicsShape",
					new ScriptingInterfaceOfIPhysicsShape()
				},
				{
					"TaleWorlds.Engine.IScene",
					new ScriptingInterfaceOfIScene()
				},
				{
					"TaleWorlds.Engine.ISceneView",
					new ScriptingInterfaceOfISceneView()
				},
				{
					"TaleWorlds.Engine.IScreen",
					new ScriptingInterfaceOfIScreen()
				},
				{
					"TaleWorlds.Engine.IScriptComponent",
					new ScriptingInterfaceOfIScriptComponent()
				},
				{
					"TaleWorlds.Engine.IShader",
					new ScriptingInterfaceOfIShader()
				},
				{
					"TaleWorlds.Engine.ISkeleton",
					new ScriptingInterfaceOfISkeleton()
				},
				{
					"TaleWorlds.Engine.ISoundEvent",
					new ScriptingInterfaceOfISoundEvent()
				},
				{
					"TaleWorlds.Engine.ISoundManager",
					new ScriptingInterfaceOfISoundManager()
				},
				{
					"TaleWorlds.Engine.ITableauView",
					new ScriptingInterfaceOfITableauView()
				},
				{
					"TaleWorlds.Engine.ITexture",
					new ScriptingInterfaceOfITexture()
				},
				{
					"TaleWorlds.Engine.ITextureView",
					new ScriptingInterfaceOfITextureView()
				},
				{
					"TaleWorlds.Engine.IThumbnailCreatorView",
					new ScriptingInterfaceOfIThumbnailCreatorView()
				},
				{
					"TaleWorlds.Engine.ITime",
					new ScriptingInterfaceOfITime()
				},
				{
					"TaleWorlds.Engine.ITwoDimensionView",
					new ScriptingInterfaceOfITwoDimensionView()
				},
				{
					"TaleWorlds.Engine.IUtil",
					new ScriptingInterfaceOfIUtil()
				},
				{
					"TaleWorlds.Engine.IVideoPlayerView",
					new ScriptingInterfaceOfIVideoPlayerView()
				},
				{
					"TaleWorlds.Engine.IView",
					new ScriptingInterfaceOfIView()
				}
			};
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003190 File Offset: 0x00001390
		public static void SetFunctionPointer(int id, IntPtr pointer)
		{
			switch (id)
			{
			case 0:
				ScriptingInterfaceOfIAsyncTask.call_CreateWithDelegateDelegate = (ScriptingInterfaceOfIAsyncTask.CreateWithDelegateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIAsyncTask.CreateWithDelegateDelegate));
				return;
			case 1:
				ScriptingInterfaceOfIAsyncTask.call_InvokeDelegate = (ScriptingInterfaceOfIAsyncTask.InvokeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIAsyncTask.InvokeDelegate));
				return;
			case 2:
				ScriptingInterfaceOfIAsyncTask.call_WaitDelegate = (ScriptingInterfaceOfIAsyncTask.WaitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIAsyncTask.WaitDelegate));
				return;
			case 3:
				ScriptingInterfaceOfIBodyPart.call_DoSegmentsIntersectDelegate = (ScriptingInterfaceOfIBodyPart.DoSegmentsIntersectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIBodyPart.DoSegmentsIntersectDelegate));
				return;
			case 4:
				ScriptingInterfaceOfICamera.call_CheckEntityVisibilityDelegate = (ScriptingInterfaceOfICamera.CheckEntityVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.CheckEntityVisibilityDelegate));
				return;
			case 5:
				ScriptingInterfaceOfICamera.call_ConstructCameraFromPositionElevationBearingDelegate = (ScriptingInterfaceOfICamera.ConstructCameraFromPositionElevationBearingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.ConstructCameraFromPositionElevationBearingDelegate));
				return;
			case 6:
				ScriptingInterfaceOfICamera.call_CreateCameraDelegate = (ScriptingInterfaceOfICamera.CreateCameraDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.CreateCameraDelegate));
				return;
			case 7:
				ScriptingInterfaceOfICamera.call_EnclosesPointDelegate = (ScriptingInterfaceOfICamera.EnclosesPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.EnclosesPointDelegate));
				return;
			case 8:
				ScriptingInterfaceOfICamera.call_FillParametersFromDelegate = (ScriptingInterfaceOfICamera.FillParametersFromDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.FillParametersFromDelegate));
				return;
			case 9:
				ScriptingInterfaceOfICamera.call_GetAspectRatioDelegate = (ScriptingInterfaceOfICamera.GetAspectRatioDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetAspectRatioDelegate));
				return;
			case 10:
				ScriptingInterfaceOfICamera.call_GetEntityDelegate = (ScriptingInterfaceOfICamera.GetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetEntityDelegate));
				return;
			case 11:
				ScriptingInterfaceOfICamera.call_GetFarDelegate = (ScriptingInterfaceOfICamera.GetFarDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetFarDelegate));
				return;
			case 12:
				ScriptingInterfaceOfICamera.call_GetFovHorizontalDelegate = (ScriptingInterfaceOfICamera.GetFovHorizontalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetFovHorizontalDelegate));
				return;
			case 13:
				ScriptingInterfaceOfICamera.call_GetFovVerticalDelegate = (ScriptingInterfaceOfICamera.GetFovVerticalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetFovVerticalDelegate));
				return;
			case 14:
				ScriptingInterfaceOfICamera.call_GetFrameDelegate = (ScriptingInterfaceOfICamera.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetFrameDelegate));
				return;
			case 15:
				ScriptingInterfaceOfICamera.call_GetHorizontalFovDelegate = (ScriptingInterfaceOfICamera.GetHorizontalFovDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetHorizontalFovDelegate));
				return;
			case 16:
				ScriptingInterfaceOfICamera.call_GetNearDelegate = (ScriptingInterfaceOfICamera.GetNearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetNearDelegate));
				return;
			case 17:
				ScriptingInterfaceOfICamera.call_GetNearPlanePointsDelegate = (ScriptingInterfaceOfICamera.GetNearPlanePointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetNearPlanePointsDelegate));
				return;
			case 18:
				ScriptingInterfaceOfICamera.call_GetNearPlanePointsStaticDelegate = (ScriptingInterfaceOfICamera.GetNearPlanePointsStaticDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetNearPlanePointsStaticDelegate));
				return;
			case 19:
				ScriptingInterfaceOfICamera.call_GetViewProjMatrixDelegate = (ScriptingInterfaceOfICamera.GetViewProjMatrixDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.GetViewProjMatrixDelegate));
				return;
			case 20:
				ScriptingInterfaceOfICamera.call_LookAtDelegate = (ScriptingInterfaceOfICamera.LookAtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.LookAtDelegate));
				return;
			case 21:
				ScriptingInterfaceOfICamera.call_ReleaseDelegate = (ScriptingInterfaceOfICamera.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.ReleaseDelegate));
				return;
			case 22:
				ScriptingInterfaceOfICamera.call_ReleaseCameraEntityDelegate = (ScriptingInterfaceOfICamera.ReleaseCameraEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.ReleaseCameraEntityDelegate));
				return;
			case 23:
				ScriptingInterfaceOfICamera.call_RenderFrustrumDelegate = (ScriptingInterfaceOfICamera.RenderFrustrumDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.RenderFrustrumDelegate));
				return;
			case 24:
				ScriptingInterfaceOfICamera.call_ScreenSpaceRayProjectionDelegate = (ScriptingInterfaceOfICamera.ScreenSpaceRayProjectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.ScreenSpaceRayProjectionDelegate));
				return;
			case 25:
				ScriptingInterfaceOfICamera.call_SetEntityDelegate = (ScriptingInterfaceOfICamera.SetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetEntityDelegate));
				return;
			case 26:
				ScriptingInterfaceOfICamera.call_SetFovHorizontalDelegate = (ScriptingInterfaceOfICamera.SetFovHorizontalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetFovHorizontalDelegate));
				return;
			case 27:
				ScriptingInterfaceOfICamera.call_SetFovVerticalDelegate = (ScriptingInterfaceOfICamera.SetFovVerticalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetFovVerticalDelegate));
				return;
			case 28:
				ScriptingInterfaceOfICamera.call_SetFrameDelegate = (ScriptingInterfaceOfICamera.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetFrameDelegate));
				return;
			case 29:
				ScriptingInterfaceOfICamera.call_SetPositionDelegate = (ScriptingInterfaceOfICamera.SetPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetPositionDelegate));
				return;
			case 30:
				ScriptingInterfaceOfICamera.call_SetViewVolumeDelegate = (ScriptingInterfaceOfICamera.SetViewVolumeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.SetViewVolumeDelegate));
				return;
			case 31:
				ScriptingInterfaceOfICamera.call_ViewportPointToWorldRayDelegate = (ScriptingInterfaceOfICamera.ViewportPointToWorldRayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.ViewportPointToWorldRayDelegate));
				return;
			case 32:
				ScriptingInterfaceOfICamera.call_WorldPointToViewportPointDelegate = (ScriptingInterfaceOfICamera.WorldPointToViewportPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICamera.WorldPointToViewportPointDelegate));
				return;
			case 33:
				ScriptingInterfaceOfIClothSimulatorComponent.call_DisableForcedWindDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.DisableForcedWindDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.DisableForcedWindDelegate));
				return;
			case 34:
				ScriptingInterfaceOfIClothSimulatorComponent.call_DisableMorphAnimationDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.DisableMorphAnimationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.DisableMorphAnimationDelegate));
				return;
			case 35:
				ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimCenterPointsDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimCenterPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimCenterPointsDelegate));
				return;
			case 36:
				ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimLeftPointsDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimLeftPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimLeftPointsDelegate));
				return;
			case 37:
				ScriptingInterfaceOfIClothSimulatorComponent.call_GetMorphAnimRightPointsDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimRightPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.GetMorphAnimRightPointsDelegate));
				return;
			case 38:
				ScriptingInterfaceOfIClothSimulatorComponent.call_GetNumberOfMorphKeysDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.GetNumberOfMorphKeysDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.GetNumberOfMorphKeysDelegate));
				return;
			case 39:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedGustStrengthDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetForcedGustStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetForcedGustStrengthDelegate));
				return;
			case 40:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedVelocityDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetForcedVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetForcedVelocityDelegate));
				return;
			case 41:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetForcedWindDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetForcedWindDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetForcedWindDelegate));
				return;
			case 42:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetMaxDistanceMultiplierDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetMaxDistanceMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetMaxDistanceMultiplierDelegate));
				return;
			case 43:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetMorphAnimationDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetMorphAnimationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetMorphAnimationDelegate));
				return;
			case 44:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetResetRequiredDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetResetRequiredDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetResetRequiredDelegate));
				return;
			case 45:
				ScriptingInterfaceOfIClothSimulatorComponent.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfIClothSimulatorComponent.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIClothSimulatorComponent.SetVectorArgumentDelegate));
				return;
			case 46:
				ScriptingInterfaceOfICompositeComponent.call_AddComponentDelegate = (ScriptingInterfaceOfICompositeComponent.AddComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.AddComponentDelegate));
				return;
			case 47:
				ScriptingInterfaceOfICompositeComponent.call_AddMultiMeshDelegate = (ScriptingInterfaceOfICompositeComponent.AddMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.AddMultiMeshDelegate));
				return;
			case 48:
				ScriptingInterfaceOfICompositeComponent.call_AddPrefabEntityDelegate = (ScriptingInterfaceOfICompositeComponent.AddPrefabEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.AddPrefabEntityDelegate));
				return;
			case 49:
				ScriptingInterfaceOfICompositeComponent.call_CreateCompositeComponentDelegate = (ScriptingInterfaceOfICompositeComponent.CreateCompositeComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.CreateCompositeComponentDelegate));
				return;
			case 50:
				ScriptingInterfaceOfICompositeComponent.call_CreateCopyDelegate = (ScriptingInterfaceOfICompositeComponent.CreateCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.CreateCopyDelegate));
				return;
			case 51:
				ScriptingInterfaceOfICompositeComponent.call_GetBoundingBoxDelegate = (ScriptingInterfaceOfICompositeComponent.GetBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetBoundingBoxDelegate));
				return;
			case 52:
				ScriptingInterfaceOfICompositeComponent.call_GetFactor1Delegate = (ScriptingInterfaceOfICompositeComponent.GetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetFactor1Delegate));
				return;
			case 53:
				ScriptingInterfaceOfICompositeComponent.call_GetFactor2Delegate = (ScriptingInterfaceOfICompositeComponent.GetFactor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetFactor2Delegate));
				return;
			case 54:
				ScriptingInterfaceOfICompositeComponent.call_GetFirstMetaMeshDelegate = (ScriptingInterfaceOfICompositeComponent.GetFirstMetaMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetFirstMetaMeshDelegate));
				return;
			case 55:
				ScriptingInterfaceOfICompositeComponent.call_GetFrameDelegate = (ScriptingInterfaceOfICompositeComponent.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetFrameDelegate));
				return;
			case 56:
				ScriptingInterfaceOfICompositeComponent.call_GetVectorUserDataDelegate = (ScriptingInterfaceOfICompositeComponent.GetVectorUserDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.GetVectorUserDataDelegate));
				return;
			case 57:
				ScriptingInterfaceOfICompositeComponent.call_IsVisibleDelegate = (ScriptingInterfaceOfICompositeComponent.IsVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.IsVisibleDelegate));
				return;
			case 58:
				ScriptingInterfaceOfICompositeComponent.call_ReleaseDelegate = (ScriptingInterfaceOfICompositeComponent.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.ReleaseDelegate));
				return;
			case 59:
				ScriptingInterfaceOfICompositeComponent.call_SetFactor1Delegate = (ScriptingInterfaceOfICompositeComponent.SetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetFactor1Delegate));
				return;
			case 60:
				ScriptingInterfaceOfICompositeComponent.call_SetFactor2Delegate = (ScriptingInterfaceOfICompositeComponent.SetFactor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetFactor2Delegate));
				return;
			case 61:
				ScriptingInterfaceOfICompositeComponent.call_SetFrameDelegate = (ScriptingInterfaceOfICompositeComponent.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetFrameDelegate));
				return;
			case 62:
				ScriptingInterfaceOfICompositeComponent.call_SetMaterialDelegate = (ScriptingInterfaceOfICompositeComponent.SetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetMaterialDelegate));
				return;
			case 63:
				ScriptingInterfaceOfICompositeComponent.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfICompositeComponent.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetVectorArgumentDelegate));
				return;
			case 64:
				ScriptingInterfaceOfICompositeComponent.call_SetVectorUserDataDelegate = (ScriptingInterfaceOfICompositeComponent.SetVectorUserDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetVectorUserDataDelegate));
				return;
			case 65:
				ScriptingInterfaceOfICompositeComponent.call_SetVisibilityMaskDelegate = (ScriptingInterfaceOfICompositeComponent.SetVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetVisibilityMaskDelegate));
				return;
			case 66:
				ScriptingInterfaceOfICompositeComponent.call_SetVisibleDelegate = (ScriptingInterfaceOfICompositeComponent.SetVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfICompositeComponent.SetVisibleDelegate));
				return;
			case 67:
				ScriptingInterfaceOfIConfig.call_ApplyDelegate = (ScriptingInterfaceOfIConfig.ApplyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.ApplyDelegate));
				return;
			case 68:
				ScriptingInterfaceOfIConfig.call_ApplyConfigChangesDelegate = (ScriptingInterfaceOfIConfig.ApplyConfigChangesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.ApplyConfigChangesDelegate));
				return;
			case 69:
				ScriptingInterfaceOfIConfig.call_AutoSaveInMinutesDelegate = (ScriptingInterfaceOfIConfig.AutoSaveInMinutesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.AutoSaveInMinutesDelegate));
				return;
			case 70:
				ScriptingInterfaceOfIConfig.call_CheckGFXSupportStatusDelegate = (ScriptingInterfaceOfIConfig.CheckGFXSupportStatusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.CheckGFXSupportStatusDelegate));
				return;
			case 71:
				ScriptingInterfaceOfIConfig.call_GetAutoGFXQualityDelegate = (ScriptingInterfaceOfIConfig.GetAutoGFXQualityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetAutoGFXQualityDelegate));
				return;
			case 72:
				ScriptingInterfaceOfIConfig.call_GetCharacterDetailDelegate = (ScriptingInterfaceOfIConfig.GetCharacterDetailDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetCharacterDetailDelegate));
				return;
			case 73:
				ScriptingInterfaceOfIConfig.call_GetCheatModeDelegate = (ScriptingInterfaceOfIConfig.GetCheatModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetCheatModeDelegate));
				return;
			case 74:
				ScriptingInterfaceOfIConfig.call_GetCurrentSoundDeviceIndexDelegate = (ScriptingInterfaceOfIConfig.GetCurrentSoundDeviceIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetCurrentSoundDeviceIndexDelegate));
				return;
			case 75:
				ScriptingInterfaceOfIConfig.call_GetDebugLoginPasswordDelegate = (ScriptingInterfaceOfIConfig.GetDebugLoginPasswordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDebugLoginPasswordDelegate));
				return;
			case 76:
				ScriptingInterfaceOfIConfig.call_GetDebugLoginUserNameDelegate = (ScriptingInterfaceOfIConfig.GetDebugLoginUserNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDebugLoginUserNameDelegate));
				return;
			case 77:
				ScriptingInterfaceOfIConfig.call_GetDefaultRGLConfigDelegate = (ScriptingInterfaceOfIConfig.GetDefaultRGLConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDefaultRGLConfigDelegate));
				return;
			case 78:
				ScriptingInterfaceOfIConfig.call_GetDesktopResolutionDelegate = (ScriptingInterfaceOfIConfig.GetDesktopResolutionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDesktopResolutionDelegate));
				return;
			case 79:
				ScriptingInterfaceOfIConfig.call_GetDevelopmentModeDelegate = (ScriptingInterfaceOfIConfig.GetDevelopmentModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDevelopmentModeDelegate));
				return;
			case 80:
				ScriptingInterfaceOfIConfig.call_GetDisableGuiMessagesDelegate = (ScriptingInterfaceOfIConfig.GetDisableGuiMessagesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDisableGuiMessagesDelegate));
				return;
			case 81:
				ScriptingInterfaceOfIConfig.call_GetDisableSoundDelegate = (ScriptingInterfaceOfIConfig.GetDisableSoundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDisableSoundDelegate));
				return;
			case 82:
				ScriptingInterfaceOfIConfig.call_GetDlssOptionCountDelegate = (ScriptingInterfaceOfIConfig.GetDlssOptionCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDlssOptionCountDelegate));
				return;
			case 83:
				ScriptingInterfaceOfIConfig.call_GetDlssTechniqueDelegate = (ScriptingInterfaceOfIConfig.GetDlssTechniqueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDlssTechniqueDelegate));
				return;
			case 84:
				ScriptingInterfaceOfIConfig.call_GetDoLocalizationCheckAtStartupDelegate = (ScriptingInterfaceOfIConfig.GetDoLocalizationCheckAtStartupDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetDoLocalizationCheckAtStartupDelegate));
				return;
			case 85:
				ScriptingInterfaceOfIConfig.call_GetEnableClothSimulationDelegate = (ScriptingInterfaceOfIConfig.GetEnableClothSimulationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetEnableClothSimulationDelegate));
				return;
			case 86:
				ScriptingInterfaceOfIConfig.call_GetEnableEditModeDelegate = (ScriptingInterfaceOfIConfig.GetEnableEditModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetEnableEditModeDelegate));
				return;
			case 87:
				ScriptingInterfaceOfIConfig.call_GetInvertMouseDelegate = (ScriptingInterfaceOfIConfig.GetInvertMouseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetInvertMouseDelegate));
				return;
			case 88:
				ScriptingInterfaceOfIConfig.call_GetLastOpenedSceneDelegate = (ScriptingInterfaceOfIConfig.GetLastOpenedSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetLastOpenedSceneDelegate));
				return;
			case 89:
				ScriptingInterfaceOfIConfig.call_GetLocalizationDebugModeDelegate = (ScriptingInterfaceOfIConfig.GetLocalizationDebugModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetLocalizationDebugModeDelegate));
				return;
			case 90:
				ScriptingInterfaceOfIConfig.call_GetMonitorDeviceCountDelegate = (ScriptingInterfaceOfIConfig.GetMonitorDeviceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetMonitorDeviceCountDelegate));
				return;
			case 91:
				ScriptingInterfaceOfIConfig.call_GetMonitorDeviceNameDelegate = (ScriptingInterfaceOfIConfig.GetMonitorDeviceNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetMonitorDeviceNameDelegate));
				return;
			case 92:
				ScriptingInterfaceOfIConfig.call_GetRefreshRateAtIndexDelegate = (ScriptingInterfaceOfIConfig.GetRefreshRateAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetRefreshRateAtIndexDelegate));
				return;
			case 93:
				ScriptingInterfaceOfIConfig.call_GetRefreshRateCountDelegate = (ScriptingInterfaceOfIConfig.GetRefreshRateCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetRefreshRateCountDelegate));
				return;
			case 94:
				ScriptingInterfaceOfIConfig.call_GetResolutionDelegate = (ScriptingInterfaceOfIConfig.GetResolutionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetResolutionDelegate));
				return;
			case 95:
				ScriptingInterfaceOfIConfig.call_GetResolutionAtIndexDelegate = (ScriptingInterfaceOfIConfig.GetResolutionAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetResolutionAtIndexDelegate));
				return;
			case 96:
				ScriptingInterfaceOfIConfig.call_GetResolutionCountDelegate = (ScriptingInterfaceOfIConfig.GetResolutionCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetResolutionCountDelegate));
				return;
			case 97:
				ScriptingInterfaceOfIConfig.call_GetRGLConfigDelegate = (ScriptingInterfaceOfIConfig.GetRGLConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetRGLConfigDelegate));
				return;
			case 98:
				ScriptingInterfaceOfIConfig.call_GetRGLConfigForDefaultSettingsDelegate = (ScriptingInterfaceOfIConfig.GetRGLConfigForDefaultSettingsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetRGLConfigForDefaultSettingsDelegate));
				return;
			case 99:
				ScriptingInterfaceOfIConfig.call_GetSoundDeviceCountDelegate = (ScriptingInterfaceOfIConfig.GetSoundDeviceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetSoundDeviceCountDelegate));
				return;
			case 100:
				ScriptingInterfaceOfIConfig.call_GetSoundDeviceNameDelegate = (ScriptingInterfaceOfIConfig.GetSoundDeviceNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetSoundDeviceNameDelegate));
				return;
			case 101:
				ScriptingInterfaceOfIConfig.call_GetTableauCacheModeDelegate = (ScriptingInterfaceOfIConfig.GetTableauCacheModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetTableauCacheModeDelegate));
				return;
			case 102:
				ScriptingInterfaceOfIConfig.call_GetUIDebugModeDelegate = (ScriptingInterfaceOfIConfig.GetUIDebugModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetUIDebugModeDelegate));
				return;
			case 103:
				ScriptingInterfaceOfIConfig.call_GetUIDoNotUseGeneratedPrefabsDelegate = (ScriptingInterfaceOfIConfig.GetUIDoNotUseGeneratedPrefabsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetUIDoNotUseGeneratedPrefabsDelegate));
				return;
			case 104:
				ScriptingInterfaceOfIConfig.call_GetVideoDeviceCountDelegate = (ScriptingInterfaceOfIConfig.GetVideoDeviceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetVideoDeviceCountDelegate));
				return;
			case 105:
				ScriptingInterfaceOfIConfig.call_GetVideoDeviceNameDelegate = (ScriptingInterfaceOfIConfig.GetVideoDeviceNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.GetVideoDeviceNameDelegate));
				return;
			case 106:
				ScriptingInterfaceOfIConfig.call_Is120HzAvailableDelegate = (ScriptingInterfaceOfIConfig.Is120HzAvailableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.Is120HzAvailableDelegate));
				return;
			case 107:
				ScriptingInterfaceOfIConfig.call_IsDlssAvailableDelegate = (ScriptingInterfaceOfIConfig.IsDlssAvailableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.IsDlssAvailableDelegate));
				return;
			case 108:
				ScriptingInterfaceOfIConfig.call_ReadRGLConfigFilesDelegate = (ScriptingInterfaceOfIConfig.ReadRGLConfigFilesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.ReadRGLConfigFilesDelegate));
				return;
			case 109:
				ScriptingInterfaceOfIConfig.call_RefreshOptionsDataDelegate = (ScriptingInterfaceOfIConfig.RefreshOptionsDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.RefreshOptionsDataDelegate));
				return;
			case 110:
				ScriptingInterfaceOfIConfig.call_SaveRGLConfigDelegate = (ScriptingInterfaceOfIConfig.SaveRGLConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SaveRGLConfigDelegate));
				return;
			case 111:
				ScriptingInterfaceOfIConfig.call_SetAutoConfigWrtHardwareDelegate = (ScriptingInterfaceOfIConfig.SetAutoConfigWrtHardwareDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetAutoConfigWrtHardwareDelegate));
				return;
			case 112:
				ScriptingInterfaceOfIConfig.call_SetBrightnessDelegate = (ScriptingInterfaceOfIConfig.SetBrightnessDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetBrightnessDelegate));
				return;
			case 113:
				ScriptingInterfaceOfIConfig.call_SetCustomResolutionDelegate = (ScriptingInterfaceOfIConfig.SetCustomResolutionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetCustomResolutionDelegate));
				return;
			case 114:
				ScriptingInterfaceOfIConfig.call_SetDefaultGameConfigDelegate = (ScriptingInterfaceOfIConfig.SetDefaultGameConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetDefaultGameConfigDelegate));
				return;
			case 115:
				ScriptingInterfaceOfIConfig.call_SetRGLConfigDelegate = (ScriptingInterfaceOfIConfig.SetRGLConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetRGLConfigDelegate));
				return;
			case 116:
				ScriptingInterfaceOfIConfig.call_SetSharpenAmountDelegate = (ScriptingInterfaceOfIConfig.SetSharpenAmountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetSharpenAmountDelegate));
				return;
			case 117:
				ScriptingInterfaceOfIConfig.call_SetSoundDeviceDelegate = (ScriptingInterfaceOfIConfig.SetSoundDeviceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetSoundDeviceDelegate));
				return;
			case 118:
				ScriptingInterfaceOfIConfig.call_SetSoundPresetDelegate = (ScriptingInterfaceOfIConfig.SetSoundPresetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIConfig.SetSoundPresetDelegate));
				return;
			case 119:
				ScriptingInterfaceOfIDebug.call_AbortGameDelegate = (ScriptingInterfaceOfIDebug.AbortGameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.AbortGameDelegate));
				return;
			case 120:
				ScriptingInterfaceOfIDebug.call_AssertMemoryUsageDelegate = (ScriptingInterfaceOfIDebug.AssertMemoryUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.AssertMemoryUsageDelegate));
				return;
			case 121:
				ScriptingInterfaceOfIDebug.call_ClearAllDebugRenderObjectsDelegate = (ScriptingInterfaceOfIDebug.ClearAllDebugRenderObjectsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.ClearAllDebugRenderObjectsDelegate));
				return;
			case 122:
				ScriptingInterfaceOfIDebug.call_ContentWarningDelegate = (ScriptingInterfaceOfIDebug.ContentWarningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.ContentWarningDelegate));
				return;
			case 123:
				ScriptingInterfaceOfIDebug.call_EchoCommandWindowDelegate = (ScriptingInterfaceOfIDebug.EchoCommandWindowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.EchoCommandWindowDelegate));
				return;
			case 124:
				ScriptingInterfaceOfIDebug.call_ErrorDelegate = (ScriptingInterfaceOfIDebug.ErrorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.ErrorDelegate));
				return;
			case 125:
				ScriptingInterfaceOfIDebug.call_FailedAssertDelegate = (ScriptingInterfaceOfIDebug.FailedAssertDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.FailedAssertDelegate));
				return;
			case 126:
				ScriptingInterfaceOfIDebug.call_GetDebugVectorDelegate = (ScriptingInterfaceOfIDebug.GetDebugVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.GetDebugVectorDelegate));
				return;
			case 127:
				ScriptingInterfaceOfIDebug.call_GetShowDebugInfoDelegate = (ScriptingInterfaceOfIDebug.GetShowDebugInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.GetShowDebugInfoDelegate));
				return;
			case 128:
				ScriptingInterfaceOfIDebug.call_IsErrorReportModeActiveDelegate = (ScriptingInterfaceOfIDebug.IsErrorReportModeActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.IsErrorReportModeActiveDelegate));
				return;
			case 129:
				ScriptingInterfaceOfIDebug.call_IsErrorReportModePauseMissionDelegate = (ScriptingInterfaceOfIDebug.IsErrorReportModePauseMissionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.IsErrorReportModePauseMissionDelegate));
				return;
			case 130:
				ScriptingInterfaceOfIDebug.call_IsTestModeDelegate = (ScriptingInterfaceOfIDebug.IsTestModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.IsTestModeDelegate));
				return;
			case 131:
				ScriptingInterfaceOfIDebug.call_MessageBoxDelegate = (ScriptingInterfaceOfIDebug.MessageBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.MessageBoxDelegate));
				return;
			case 132:
				ScriptingInterfaceOfIDebug.call_PostWarningLineDelegate = (ScriptingInterfaceOfIDebug.PostWarningLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.PostWarningLineDelegate));
				return;
			case 133:
				ScriptingInterfaceOfIDebug.call_RenderDebugBoxObjectDelegate = (ScriptingInterfaceOfIDebug.RenderDebugBoxObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugBoxObjectDelegate));
				return;
			case 134:
				ScriptingInterfaceOfIDebug.call_RenderDebugBoxObjectWithFrameDelegate = (ScriptingInterfaceOfIDebug.RenderDebugBoxObjectWithFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugBoxObjectWithFrameDelegate));
				return;
			case 135:
				ScriptingInterfaceOfIDebug.call_RenderDebugCapsuleDelegate = (ScriptingInterfaceOfIDebug.RenderDebugCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugCapsuleDelegate));
				return;
			case 136:
				ScriptingInterfaceOfIDebug.call_RenderDebugDirectionArrowDelegate = (ScriptingInterfaceOfIDebug.RenderDebugDirectionArrowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugDirectionArrowDelegate));
				return;
			case 137:
				ScriptingInterfaceOfIDebug.call_RenderDebugFrameDelegate = (ScriptingInterfaceOfIDebug.RenderDebugFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugFrameDelegate));
				return;
			case 138:
				ScriptingInterfaceOfIDebug.call_RenderDebugLineDelegate = (ScriptingInterfaceOfIDebug.RenderDebugLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugLineDelegate));
				return;
			case 139:
				ScriptingInterfaceOfIDebug.call_RenderDebugRectDelegate = (ScriptingInterfaceOfIDebug.RenderDebugRectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugRectDelegate));
				return;
			case 140:
				ScriptingInterfaceOfIDebug.call_RenderDebugRectWithColorDelegate = (ScriptingInterfaceOfIDebug.RenderDebugRectWithColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugRectWithColorDelegate));
				return;
			case 141:
				ScriptingInterfaceOfIDebug.call_RenderDebugSphereDelegate = (ScriptingInterfaceOfIDebug.RenderDebugSphereDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugSphereDelegate));
				return;
			case 142:
				ScriptingInterfaceOfIDebug.call_RenderDebugTextDelegate = (ScriptingInterfaceOfIDebug.RenderDebugTextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugTextDelegate));
				return;
			case 143:
				ScriptingInterfaceOfIDebug.call_RenderDebugText3dDelegate = (ScriptingInterfaceOfIDebug.RenderDebugText3dDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.RenderDebugText3dDelegate));
				return;
			case 144:
				ScriptingInterfaceOfIDebug.call_SetDebugVectorDelegate = (ScriptingInterfaceOfIDebug.SetDebugVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.SetDebugVectorDelegate));
				return;
			case 145:
				ScriptingInterfaceOfIDebug.call_SetDumpGenerationDisabledDelegate = (ScriptingInterfaceOfIDebug.SetDumpGenerationDisabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.SetDumpGenerationDisabledDelegate));
				return;
			case 146:
				ScriptingInterfaceOfIDebug.call_SetErrorReportSceneDelegate = (ScriptingInterfaceOfIDebug.SetErrorReportSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.SetErrorReportSceneDelegate));
				return;
			case 147:
				ScriptingInterfaceOfIDebug.call_SetShowDebugInfoDelegate = (ScriptingInterfaceOfIDebug.SetShowDebugInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.SetShowDebugInfoDelegate));
				return;
			case 148:
				ScriptingInterfaceOfIDebug.call_SilentAssertDelegate = (ScriptingInterfaceOfIDebug.SilentAssertDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.SilentAssertDelegate));
				return;
			case 149:
				ScriptingInterfaceOfIDebug.call_WarningDelegate = (ScriptingInterfaceOfIDebug.WarningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.WarningDelegate));
				return;
			case 150:
				ScriptingInterfaceOfIDebug.call_WriteDebugLineOnScreenDelegate = (ScriptingInterfaceOfIDebug.WriteDebugLineOnScreenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.WriteDebugLineOnScreenDelegate));
				return;
			case 151:
				ScriptingInterfaceOfIDebug.call_WriteLineDelegate = (ScriptingInterfaceOfIDebug.WriteLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDebug.WriteLineDelegate));
				return;
			case 152:
				ScriptingInterfaceOfIDecal.call_CheckAndRegisterToDecalSetDelegate = (ScriptingInterfaceOfIDecal.CheckAndRegisterToDecalSetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.CheckAndRegisterToDecalSetDelegate));
				return;
			case 153:
				ScriptingInterfaceOfIDecal.call_CreateCopyDelegate = (ScriptingInterfaceOfIDecal.CreateCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.CreateCopyDelegate));
				return;
			case 154:
				ScriptingInterfaceOfIDecal.call_CreateDecalDelegate = (ScriptingInterfaceOfIDecal.CreateDecalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.CreateDecalDelegate));
				return;
			case 155:
				ScriptingInterfaceOfIDecal.call_GetFactor1Delegate = (ScriptingInterfaceOfIDecal.GetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.GetFactor1Delegate));
				return;
			case 156:
				ScriptingInterfaceOfIDecal.call_GetFrameDelegate = (ScriptingInterfaceOfIDecal.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.GetFrameDelegate));
				return;
			case 157:
				ScriptingInterfaceOfIDecal.call_GetMaterialDelegate = (ScriptingInterfaceOfIDecal.GetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.GetMaterialDelegate));
				return;
			case 158:
				ScriptingInterfaceOfIDecal.call_OverrideRoadBoundaryP0Delegate = (ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP0Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP0Delegate));
				return;
			case 159:
				ScriptingInterfaceOfIDecal.call_OverrideRoadBoundaryP1Delegate = (ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.OverrideRoadBoundaryP1Delegate));
				return;
			case 160:
				ScriptingInterfaceOfIDecal.call_SetAlphaDelegate = (ScriptingInterfaceOfIDecal.SetAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetAlphaDelegate));
				return;
			case 161:
				ScriptingInterfaceOfIDecal.call_SetFactor1Delegate = (ScriptingInterfaceOfIDecal.SetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetFactor1Delegate));
				return;
			case 162:
				ScriptingInterfaceOfIDecal.call_SetFactor1LinearDelegate = (ScriptingInterfaceOfIDecal.SetFactor1LinearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetFactor1LinearDelegate));
				return;
			case 163:
				ScriptingInterfaceOfIDecal.call_SetFrameDelegate = (ScriptingInterfaceOfIDecal.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetFrameDelegate));
				return;
			case 164:
				ScriptingInterfaceOfIDecal.call_SetIsVisibleDelegate = (ScriptingInterfaceOfIDecal.SetIsVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetIsVisibleDelegate));
				return;
			case 165:
				ScriptingInterfaceOfIDecal.call_SetMaterialDelegate = (ScriptingInterfaceOfIDecal.SetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetMaterialDelegate));
				return;
			case 166:
				ScriptingInterfaceOfIDecal.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfIDecal.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetVectorArgumentDelegate));
				return;
			case 167:
				ScriptingInterfaceOfIDecal.call_SetVectorArgument2Delegate = (ScriptingInterfaceOfIDecal.SetVectorArgument2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIDecal.SetVectorArgument2Delegate));
				return;
			case 168:
				ScriptingInterfaceOfIEngineSizeChecker.call_GetEngineStructMemberOffsetDelegate = (ScriptingInterfaceOfIEngineSizeChecker.GetEngineStructMemberOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIEngineSizeChecker.GetEngineStructMemberOffsetDelegate));
				return;
			case 169:
				ScriptingInterfaceOfIEngineSizeChecker.call_GetEngineStructSizeDelegate = (ScriptingInterfaceOfIEngineSizeChecker.GetEngineStructSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIEngineSizeChecker.GetEngineStructSizeDelegate));
				return;
			case 170:
				ScriptingInterfaceOfIGameEntity.call_ActivateRagdollDelegate = (ScriptingInterfaceOfIGameEntity.ActivateRagdollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ActivateRagdollDelegate));
				return;
			case 171:
				ScriptingInterfaceOfIGameEntity.call_AddAllMeshesOfGameEntityDelegate = (ScriptingInterfaceOfIGameEntity.AddAllMeshesOfGameEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddAllMeshesOfGameEntityDelegate));
				return;
			case 172:
				ScriptingInterfaceOfIGameEntity.call_AddCapsuleAsBodyDelegate = (ScriptingInterfaceOfIGameEntity.AddCapsuleAsBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddCapsuleAsBodyDelegate));
				return;
			case 173:
				ScriptingInterfaceOfIGameEntity.call_AddChildDelegate = (ScriptingInterfaceOfIGameEntity.AddChildDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddChildDelegate));
				return;
			case 174:
				ScriptingInterfaceOfIGameEntity.call_AddComponentDelegate = (ScriptingInterfaceOfIGameEntity.AddComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddComponentDelegate));
				return;
			case 175:
				ScriptingInterfaceOfIGameEntity.call_AddDistanceJointDelegate = (ScriptingInterfaceOfIGameEntity.AddDistanceJointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddDistanceJointDelegate));
				return;
			case 176:
				ScriptingInterfaceOfIGameEntity.call_AddDistanceJointWithFramesDelegate = (ScriptingInterfaceOfIGameEntity.AddDistanceJointWithFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddDistanceJointWithFramesDelegate));
				return;
			case 177:
				ScriptingInterfaceOfIGameEntity.call_AddEditDataUserToAllMeshesDelegate = (ScriptingInterfaceOfIGameEntity.AddEditDataUserToAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddEditDataUserToAllMeshesDelegate));
				return;
			case 178:
				ScriptingInterfaceOfIGameEntity.call_AddLightDelegate = (ScriptingInterfaceOfIGameEntity.AddLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddLightDelegate));
				return;
			case 179:
				ScriptingInterfaceOfIGameEntity.call_AddMeshDelegate = (ScriptingInterfaceOfIGameEntity.AddMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddMeshDelegate));
				return;
			case 180:
				ScriptingInterfaceOfIGameEntity.call_AddMeshToBoneDelegate = (ScriptingInterfaceOfIGameEntity.AddMeshToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddMeshToBoneDelegate));
				return;
			case 181:
				ScriptingInterfaceOfIGameEntity.call_AddMultiMeshDelegate = (ScriptingInterfaceOfIGameEntity.AddMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddMultiMeshDelegate));
				return;
			case 182:
				ScriptingInterfaceOfIGameEntity.call_AddMultiMeshToSkeletonDelegate = (ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonDelegate));
				return;
			case 183:
				ScriptingInterfaceOfIGameEntity.call_AddMultiMeshToSkeletonBoneDelegate = (ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonBoneDelegate));
				return;
			case 184:
				ScriptingInterfaceOfIGameEntity.call_AddParticleSystemComponentDelegate = (ScriptingInterfaceOfIGameEntity.AddParticleSystemComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddParticleSystemComponentDelegate));
				return;
			case 185:
				ScriptingInterfaceOfIGameEntity.call_AddPhysicsDelegate = (ScriptingInterfaceOfIGameEntity.AddPhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddPhysicsDelegate));
				return;
			case 186:
				ScriptingInterfaceOfIGameEntity.call_AddSphereAsBodyDelegate = (ScriptingInterfaceOfIGameEntity.AddSphereAsBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddSphereAsBodyDelegate));
				return;
			case 187:
				ScriptingInterfaceOfIGameEntity.call_AddSplashPositionToWaterVisualRecordDelegate = (ScriptingInterfaceOfIGameEntity.AddSplashPositionToWaterVisualRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddSplashPositionToWaterVisualRecordDelegate));
				return;
			case 188:
				ScriptingInterfaceOfIGameEntity.call_AddTagDelegate = (ScriptingInterfaceOfIGameEntity.AddTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AddTagDelegate));
				return;
			case 189:
				ScriptingInterfaceOfIGameEntity.call_ApplyAccelerationToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyAccelerationToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyAccelerationToDynamicBodyDelegate));
				return;
			case 190:
				ScriptingInterfaceOfIGameEntity.call_ApplyForceToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyForceToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyForceToDynamicBodyDelegate));
				return;
			case 191:
				ScriptingInterfaceOfIGameEntity.call_ApplyGlobalForceAtLocalPosToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyGlobalForceAtLocalPosToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyGlobalForceAtLocalPosToDynamicBodyDelegate));
				return;
			case 192:
				ScriptingInterfaceOfIGameEntity.call_ApplyLocalForceAtLocalPosToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyLocalForceAtLocalPosToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyLocalForceAtLocalPosToDynamicBodyDelegate));
				return;
			case 193:
				ScriptingInterfaceOfIGameEntity.call_ApplyLocalImpulseToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyLocalImpulseToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyLocalImpulseToDynamicBodyDelegate));
				return;
			case 194:
				ScriptingInterfaceOfIGameEntity.call_ApplyTorqueToDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.ApplyTorqueToDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ApplyTorqueToDynamicBodyDelegate));
				return;
			case 195:
				ScriptingInterfaceOfIGameEntity.call_AttachNavigationMeshFacesDelegate = (ScriptingInterfaceOfIGameEntity.AttachNavigationMeshFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.AttachNavigationMeshFacesDelegate));
				return;
			case 196:
				ScriptingInterfaceOfIGameEntity.call_BreakPrefabDelegate = (ScriptingInterfaceOfIGameEntity.BreakPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.BreakPrefabDelegate));
				return;
			case 197:
				ScriptingInterfaceOfIGameEntity.call_BurstEntityParticleDelegate = (ScriptingInterfaceOfIGameEntity.BurstEntityParticleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.BurstEntityParticleDelegate));
				return;
			case 198:
				ScriptingInterfaceOfIGameEntity.call_CallScriptCallbacksDelegate = (ScriptingInterfaceOfIGameEntity.CallScriptCallbacksDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CallScriptCallbacksDelegate));
				return;
			case 199:
				ScriptingInterfaceOfIGameEntity.call_ChangeMetaMeshOrRemoveItIfNotExistsDelegate = (ScriptingInterfaceOfIGameEntity.ChangeMetaMeshOrRemoveItIfNotExistsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ChangeMetaMeshOrRemoveItIfNotExistsDelegate));
				return;
			case 200:
				ScriptingInterfaceOfIGameEntity.call_ChangeResolutionMultiplierOfWaterVisualDelegate = (ScriptingInterfaceOfIGameEntity.ChangeResolutionMultiplierOfWaterVisualDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ChangeResolutionMultiplierOfWaterVisualDelegate));
				return;
			case 201:
				ScriptingInterfaceOfIGameEntity.call_CheckIsPrefabLinkRootPrefabDelegate = (ScriptingInterfaceOfIGameEntity.CheckIsPrefabLinkRootPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CheckIsPrefabLinkRootPrefabDelegate));
				return;
			case 202:
				ScriptingInterfaceOfIGameEntity.call_CheckPointWithOrientedBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.CheckPointWithOrientedBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CheckPointWithOrientedBoundingBoxDelegate));
				return;
			case 203:
				ScriptingInterfaceOfIGameEntity.call_CheckResourcesDelegate = (ScriptingInterfaceOfIGameEntity.CheckResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CheckResourcesDelegate));
				return;
			case 204:
				ScriptingInterfaceOfIGameEntity.call_ClearComponentsDelegate = (ScriptingInterfaceOfIGameEntity.ClearComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ClearComponentsDelegate));
				return;
			case 205:
				ScriptingInterfaceOfIGameEntity.call_ClearEntityComponentsDelegate = (ScriptingInterfaceOfIGameEntity.ClearEntityComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ClearEntityComponentsDelegate));
				return;
			case 206:
				ScriptingInterfaceOfIGameEntity.call_ClearOnlyOwnComponentsDelegate = (ScriptingInterfaceOfIGameEntity.ClearOnlyOwnComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ClearOnlyOwnComponentsDelegate));
				return;
			case 207:
				ScriptingInterfaceOfIGameEntity.call_ComputeTrajectoryVolumeDelegate = (ScriptingInterfaceOfIGameEntity.ComputeTrajectoryVolumeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ComputeTrajectoryVolumeDelegate));
				return;
			case 208:
				ScriptingInterfaceOfIGameEntity.call_ComputeVelocityDeltaFromImpulseDelegate = (ScriptingInterfaceOfIGameEntity.ComputeVelocityDeltaFromImpulseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ComputeVelocityDeltaFromImpulseDelegate));
				return;
			case 209:
				ScriptingInterfaceOfIGameEntity.call_ConvertDynamicBodyToRayCastDelegate = (ScriptingInterfaceOfIGameEntity.ConvertDynamicBodyToRayCastDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ConvertDynamicBodyToRayCastDelegate));
				return;
			case 210:
				ScriptingInterfaceOfIGameEntity.call_CookTrianglePhysxMeshDelegate = (ScriptingInterfaceOfIGameEntity.CookTrianglePhysxMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CookTrianglePhysxMeshDelegate));
				return;
			case 211:
				ScriptingInterfaceOfIGameEntity.call_CopyComponentsToSkeletonDelegate = (ScriptingInterfaceOfIGameEntity.CopyComponentsToSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CopyComponentsToSkeletonDelegate));
				return;
			case 212:
				ScriptingInterfaceOfIGameEntity.call_CopyFromPrefabDelegate = (ScriptingInterfaceOfIGameEntity.CopyFromPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CopyFromPrefabDelegate));
				return;
			case 213:
				ScriptingInterfaceOfIGameEntity.call_CopyScriptComponentFromAnotherEntityDelegate = (ScriptingInterfaceOfIGameEntity.CopyScriptComponentFromAnotherEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CopyScriptComponentFromAnotherEntityDelegate));
				return;
			case 214:
				ScriptingInterfaceOfIGameEntity.call_CreateAndAddScriptComponentDelegate = (ScriptingInterfaceOfIGameEntity.CreateAndAddScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateAndAddScriptComponentDelegate));
				return;
			case 215:
				ScriptingInterfaceOfIGameEntity.call_CreateEmptyDelegate = (ScriptingInterfaceOfIGameEntity.CreateEmptyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateEmptyDelegate));
				return;
			case 216:
				ScriptingInterfaceOfIGameEntity.call_CreateEmptyPhysxShapeDelegate = (ScriptingInterfaceOfIGameEntity.CreateEmptyPhysxShapeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateEmptyPhysxShapeDelegate));
				return;
			case 217:
				ScriptingInterfaceOfIGameEntity.call_CreateEmptyWithoutSceneDelegate = (ScriptingInterfaceOfIGameEntity.CreateEmptyWithoutSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateEmptyWithoutSceneDelegate));
				return;
			case 218:
				ScriptingInterfaceOfIGameEntity.call_CreateFromPrefabDelegate = (ScriptingInterfaceOfIGameEntity.CreateFromPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateFromPrefabDelegate));
				return;
			case 219:
				ScriptingInterfaceOfIGameEntity.call_CreateFromPrefabWithInitialFrameDelegate = (ScriptingInterfaceOfIGameEntity.CreateFromPrefabWithInitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateFromPrefabWithInitialFrameDelegate));
				return;
			case 220:
				ScriptingInterfaceOfIGameEntity.call_CreatePhysxCookingInstanceDelegate = (ScriptingInterfaceOfIGameEntity.CreatePhysxCookingInstanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreatePhysxCookingInstanceDelegate));
				return;
			case 221:
				ScriptingInterfaceOfIGameEntity.call_CreateVariableRatePhysicsDelegate = (ScriptingInterfaceOfIGameEntity.CreateVariableRatePhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.CreateVariableRatePhysicsDelegate));
				return;
			case 222:
				ScriptingInterfaceOfIGameEntity.call_DeleteEmptyShapeDelegate = (ScriptingInterfaceOfIGameEntity.DeleteEmptyShapeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DeleteEmptyShapeDelegate));
				return;
			case 223:
				ScriptingInterfaceOfIGameEntity.call_DeletePhysxCookingInstanceDelegate = (ScriptingInterfaceOfIGameEntity.DeletePhysxCookingInstanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DeletePhysxCookingInstanceDelegate));
				return;
			case 224:
				ScriptingInterfaceOfIGameEntity.call_DeRegisterWaterMeshMaterialsDelegate = (ScriptingInterfaceOfIGameEntity.DeRegisterWaterMeshMaterialsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DeRegisterWaterMeshMaterialsDelegate));
				return;
			case 225:
				ScriptingInterfaceOfIGameEntity.call_DeRegisterWaterSDFClipDelegate = (ScriptingInterfaceOfIGameEntity.DeRegisterWaterSDFClipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DeRegisterWaterSDFClipDelegate));
				return;
			case 226:
				ScriptingInterfaceOfIGameEntity.call_DeselectEntityOnEditorDelegate = (ScriptingInterfaceOfIGameEntity.DeselectEntityOnEditorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DeselectEntityOnEditorDelegate));
				return;
			case 227:
				ScriptingInterfaceOfIGameEntity.call_DetachAllAttachedNavigationMeshFacesDelegate = (ScriptingInterfaceOfIGameEntity.DetachAllAttachedNavigationMeshFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DetachAllAttachedNavigationMeshFacesDelegate));
				return;
			case 228:
				ScriptingInterfaceOfIGameEntity.call_DisableContourDelegate = (ScriptingInterfaceOfIGameEntity.DisableContourDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DisableContourDelegate));
				return;
			case 229:
				ScriptingInterfaceOfIGameEntity.call_DisableDynamicBodySimulationDelegate = (ScriptingInterfaceOfIGameEntity.DisableDynamicBodySimulationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DisableDynamicBodySimulationDelegate));
				return;
			case 230:
				ScriptingInterfaceOfIGameEntity.call_DisableGravityDelegate = (ScriptingInterfaceOfIGameEntity.DisableGravityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.DisableGravityDelegate));
				return;
			case 231:
				ScriptingInterfaceOfIGameEntity.call_EnableDynamicBodyDelegate = (ScriptingInterfaceOfIGameEntity.EnableDynamicBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.EnableDynamicBodyDelegate));
				return;
			case 232:
				ScriptingInterfaceOfIGameEntity.call_FindWithNameDelegate = (ScriptingInterfaceOfIGameEntity.FindWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.FindWithNameDelegate));
				return;
			case 233:
				ScriptingInterfaceOfIGameEntity.call_FreezeDelegate = (ScriptingInterfaceOfIGameEntity.FreezeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.FreezeDelegate));
				return;
			case 234:
				ScriptingInterfaceOfIGameEntity.call_GetAngularVelocityDelegate = (ScriptingInterfaceOfIGameEntity.GetAngularVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetAngularVelocityDelegate));
				return;
			case 235:
				ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceCountDelegate = (ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceCountDelegate));
				return;
			case 236:
				ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceRecordsDelegate = (ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceRecordsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceRecordsDelegate));
				return;
			case 237:
				ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceVertexIndicesDelegate = (ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceVertexIndicesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceVertexIndicesDelegate));
				return;
			case 238:
				ScriptingInterfaceOfIGameEntity.call_GetBodyFlagsDelegate = (ScriptingInterfaceOfIGameEntity.GetBodyFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBodyFlagsDelegate));
				return;
			case 239:
				ScriptingInterfaceOfIGameEntity.call_GetBodyShapeDelegate = (ScriptingInterfaceOfIGameEntity.GetBodyShapeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBodyShapeDelegate));
				return;
			case 240:
				ScriptingInterfaceOfIGameEntity.call_GetBodyVisualWorldTransformDelegate = (ScriptingInterfaceOfIGameEntity.GetBodyVisualWorldTransformDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBodyVisualWorldTransformDelegate));
				return;
			case 241:
				ScriptingInterfaceOfIGameEntity.call_GetBodyWorldTransformDelegate = (ScriptingInterfaceOfIGameEntity.GetBodyWorldTransformDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBodyWorldTransformDelegate));
				return;
			case 242:
				ScriptingInterfaceOfIGameEntity.call_GetBoneCountDelegate = (ScriptingInterfaceOfIGameEntity.GetBoneCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBoneCountDelegate));
				return;
			case 243:
				ScriptingInterfaceOfIGameEntity.call_GetBoneEntitialFrameWithIndexDelegate = (ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithIndexDelegate));
				return;
			case 244:
				ScriptingInterfaceOfIGameEntity.call_GetBoneEntitialFrameWithNameDelegate = (ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithNameDelegate));
				return;
			case 245:
				ScriptingInterfaceOfIGameEntity.call_GetBoundingBoxMaxDelegate = (ScriptingInterfaceOfIGameEntity.GetBoundingBoxMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBoundingBoxMaxDelegate));
				return;
			case 246:
				ScriptingInterfaceOfIGameEntity.call_GetBoundingBoxMinDelegate = (ScriptingInterfaceOfIGameEntity.GetBoundingBoxMinDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetBoundingBoxMinDelegate));
				return;
			case 247:
				ScriptingInterfaceOfIGameEntity.call_GetCameraParamsFromCameraScriptDelegate = (ScriptingInterfaceOfIGameEntity.GetCameraParamsFromCameraScriptDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetCameraParamsFromCameraScriptDelegate));
				return;
			case 248:
				ScriptingInterfaceOfIGameEntity.call_GetCenterOfMassDelegate = (ScriptingInterfaceOfIGameEntity.GetCenterOfMassDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetCenterOfMassDelegate));
				return;
			case 249:
				ScriptingInterfaceOfIGameEntity.call_GetChildDelegate = (ScriptingInterfaceOfIGameEntity.GetChildDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetChildDelegate));
				return;
			case 250:
				ScriptingInterfaceOfIGameEntity.call_GetChildCountDelegate = (ScriptingInterfaceOfIGameEntity.GetChildCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetChildCountDelegate));
				return;
			case 251:
				ScriptingInterfaceOfIGameEntity.call_GetChildPointerDelegate = (ScriptingInterfaceOfIGameEntity.GetChildPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetChildPointerDelegate));
				return;
			case 252:
				ScriptingInterfaceOfIGameEntity.call_GetComponentAtIndexDelegate = (ScriptingInterfaceOfIGameEntity.GetComponentAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetComponentAtIndexDelegate));
				return;
			case 253:
				ScriptingInterfaceOfIGameEntity.call_GetComponentCountDelegate = (ScriptingInterfaceOfIGameEntity.GetComponentCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetComponentCountDelegate));
				return;
			case 254:
				ScriptingInterfaceOfIGameEntity.call_GetEditModeLevelVisibilityDelegate = (ScriptingInterfaceOfIGameEntity.GetEditModeLevelVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetEditModeLevelVisibilityDelegate));
				return;
			case 255:
				ScriptingInterfaceOfIGameEntity.call_GetEntityFlagsDelegate = (ScriptingInterfaceOfIGameEntity.GetEntityFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetEntityFlagsDelegate));
				return;
			case 256:
				ScriptingInterfaceOfIGameEntity.call_GetEntityVisibilityFlagsDelegate = (ScriptingInterfaceOfIGameEntity.GetEntityVisibilityFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetEntityVisibilityFlagsDelegate));
				return;
			case 257:
				ScriptingInterfaceOfIGameEntity.call_GetFactorColorDelegate = (ScriptingInterfaceOfIGameEntity.GetFactorColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetFactorColorDelegate));
				return;
			case 258:
				ScriptingInterfaceOfIGameEntity.call_GetFirstChildWithTagRecursiveDelegate = (ScriptingInterfaceOfIGameEntity.GetFirstChildWithTagRecursiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetFirstChildWithTagRecursiveDelegate));
				return;
			case 259:
				ScriptingInterfaceOfIGameEntity.call_GetFirstEntityWithTagDelegate = (ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagDelegate));
				return;
			case 260:
				ScriptingInterfaceOfIGameEntity.call_GetFirstEntityWithTagExpressionDelegate = (ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagExpressionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagExpressionDelegate));
				return;
			case 261:
				ScriptingInterfaceOfIGameEntity.call_GetFirstMeshDelegate = (ScriptingInterfaceOfIGameEntity.GetFirstMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetFirstMeshDelegate));
				return;
			case 262:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalBoundingBoxDelegate));
				return;
			case 263:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalBoxMaxDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalBoxMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalBoxMaxDelegate));
				return;
			case 264:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalBoxMinDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalBoxMinDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalBoxMinDelegate));
				return;
			case 265:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalFrameDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalFrameDelegate));
				return;
			case 266:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalFrameImpreciseForFixedTickDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalFrameImpreciseForFixedTickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalFrameImpreciseForFixedTickDelegate));
				return;
			case 267:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalScaleDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalScaleDelegate));
				return;
			case 268:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalWindStrengthVectorOfSceneDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalWindStrengthVectorOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalWindStrengthVectorOfSceneDelegate));
				return;
			case 269:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalWindVelocityOfSceneDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityOfSceneDelegate));
				return;
			case 270:
				ScriptingInterfaceOfIGameEntity.call_GetGlobalWindVelocityWithGustNoiseOfSceneDelegate = (ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityWithGustNoiseOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityWithGustNoiseOfSceneDelegate));
				return;
			case 271:
				ScriptingInterfaceOfIGameEntity.call_GetGuidDelegate = (ScriptingInterfaceOfIGameEntity.GetGuidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetGuidDelegate));
				return;
			case 272:
				ScriptingInterfaceOfIGameEntity.call_GetLastFinalRenderCameraPositionOfSceneDelegate = (ScriptingInterfaceOfIGameEntity.GetLastFinalRenderCameraPositionOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLastFinalRenderCameraPositionOfSceneDelegate));
				return;
			case 273:
				ScriptingInterfaceOfIGameEntity.call_GetLightDelegate = (ScriptingInterfaceOfIGameEntity.GetLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLightDelegate));
				return;
			case 274:
				ScriptingInterfaceOfIGameEntity.call_GetLinearVelocityDelegate = (ScriptingInterfaceOfIGameEntity.GetLinearVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLinearVelocityDelegate));
				return;
			case 275:
				ScriptingInterfaceOfIGameEntity.call_GetLocalBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.GetLocalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLocalBoundingBoxDelegate));
				return;
			case 276:
				ScriptingInterfaceOfIGameEntity.call_GetLocalFrameDelegate = (ScriptingInterfaceOfIGameEntity.GetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLocalFrameDelegate));
				return;
			case 277:
				ScriptingInterfaceOfIGameEntity.call_GetLocalPhysicsBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.GetLocalPhysicsBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLocalPhysicsBoundingBoxDelegate));
				return;
			case 278:
				ScriptingInterfaceOfIGameEntity.call_GetLodLevelForDistanceSqDelegate = (ScriptingInterfaceOfIGameEntity.GetLodLevelForDistanceSqDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetLodLevelForDistanceSqDelegate));
				return;
			case 279:
				ScriptingInterfaceOfIGameEntity.call_GetMassDelegate = (ScriptingInterfaceOfIGameEntity.GetMassDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetMassDelegate));
				return;
			case 280:
				ScriptingInterfaceOfIGameEntity.call_GetMassSpaceInertiaDelegate = (ScriptingInterfaceOfIGameEntity.GetMassSpaceInertiaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetMassSpaceInertiaDelegate));
				return;
			case 281:
				ScriptingInterfaceOfIGameEntity.call_GetMassSpaceInverseInertiaDelegate = (ScriptingInterfaceOfIGameEntity.GetMassSpaceInverseInertiaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetMassSpaceInverseInertiaDelegate));
				return;
			case 282:
				ScriptingInterfaceOfIGameEntity.call_GetMeshBendedPositionDelegate = (ScriptingInterfaceOfIGameEntity.GetMeshBendedPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetMeshBendedPositionDelegate));
				return;
			case 283:
				ScriptingInterfaceOfIGameEntity.call_GetMobilityDelegate = (ScriptingInterfaceOfIGameEntity.GetMobilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetMobilityDelegate));
				return;
			case 284:
				ScriptingInterfaceOfIGameEntity.call_GetNameDelegate = (ScriptingInterfaceOfIGameEntity.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetNameDelegate));
				return;
			case 285:
				ScriptingInterfaceOfIGameEntity.call_GetNativeScriptComponentVariableDelegate = (ScriptingInterfaceOfIGameEntity.GetNativeScriptComponentVariableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetNativeScriptComponentVariableDelegate));
				return;
			case 286:
				ScriptingInterfaceOfIGameEntity.call_GetNextEntityWithTagDelegate = (ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagDelegate));
				return;
			case 287:
				ScriptingInterfaceOfIGameEntity.call_GetNextEntityWithTagExpressionDelegate = (ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagExpressionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagExpressionDelegate));
				return;
			case 288:
				ScriptingInterfaceOfIGameEntity.call_GetNextPrefabDelegate = (ScriptingInterfaceOfIGameEntity.GetNextPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetNextPrefabDelegate));
				return;
			case 289:
				ScriptingInterfaceOfIGameEntity.call_GetOldPrefabNameDelegate = (ScriptingInterfaceOfIGameEntity.GetOldPrefabNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetOldPrefabNameDelegate));
				return;
			case 290:
				ScriptingInterfaceOfIGameEntity.call_GetParentDelegate = (ScriptingInterfaceOfIGameEntity.GetParentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetParentDelegate));
				return;
			case 291:
				ScriptingInterfaceOfIGameEntity.call_GetParentPointerDelegate = (ScriptingInterfaceOfIGameEntity.GetParentPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetParentPointerDelegate));
				return;
			case 292:
				ScriptingInterfaceOfIGameEntity.call_GetPhysicsDescBodyFlagsDelegate = (ScriptingInterfaceOfIGameEntity.GetPhysicsDescBodyFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPhysicsDescBodyFlagsDelegate));
				return;
			case 293:
				ScriptingInterfaceOfIGameEntity.call_GetPhysicsMaterialIndexDelegate = (ScriptingInterfaceOfIGameEntity.GetPhysicsMaterialIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPhysicsMaterialIndexDelegate));
				return;
			case 294:
				ScriptingInterfaceOfIGameEntity.call_GetPhysicsMinMaxDelegate = (ScriptingInterfaceOfIGameEntity.GetPhysicsMinMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPhysicsMinMaxDelegate));
				return;
			case 295:
				ScriptingInterfaceOfIGameEntity.call_GetPhysicsStateDelegate = (ScriptingInterfaceOfIGameEntity.GetPhysicsStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPhysicsStateDelegate));
				return;
			case 296:
				ScriptingInterfaceOfIGameEntity.call_GetPhysicsTriangleCountDelegate = (ScriptingInterfaceOfIGameEntity.GetPhysicsTriangleCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPhysicsTriangleCountDelegate));
				return;
			case 297:
				ScriptingInterfaceOfIGameEntity.call_GetPrefabNameDelegate = (ScriptingInterfaceOfIGameEntity.GetPrefabNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPrefabNameDelegate));
				return;
			case 298:
				ScriptingInterfaceOfIGameEntity.call_GetPreviousGlobalFrameDelegate = (ScriptingInterfaceOfIGameEntity.GetPreviousGlobalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetPreviousGlobalFrameDelegate));
				return;
			case 299:
				ScriptingInterfaceOfIGameEntity.call_GetQuickBoneEntitialFrameDelegate = (ScriptingInterfaceOfIGameEntity.GetQuickBoneEntitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetQuickBoneEntitialFrameDelegate));
				return;
			case 300:
				ScriptingInterfaceOfIGameEntity.call_GetRadiusDelegate = (ScriptingInterfaceOfIGameEntity.GetRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetRadiusDelegate));
				return;
			case 301:
				ScriptingInterfaceOfIGameEntity.call_GetRootParentPointerDelegate = (ScriptingInterfaceOfIGameEntity.GetRootParentPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetRootParentPointerDelegate));
				return;
			case 302:
				ScriptingInterfaceOfIGameEntity.call_GetSceneDelegate = (ScriptingInterfaceOfIGameEntity.GetSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetSceneDelegate));
				return;
			case 303:
				ScriptingInterfaceOfIGameEntity.call_GetScenePointerDelegate = (ScriptingInterfaceOfIGameEntity.GetScenePointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetScenePointerDelegate));
				return;
			case 304:
				ScriptingInterfaceOfIGameEntity.call_GetScriptComponentDelegate = (ScriptingInterfaceOfIGameEntity.GetScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetScriptComponentDelegate));
				return;
			case 305:
				ScriptingInterfaceOfIGameEntity.call_GetScriptComponentAtIndexDelegate = (ScriptingInterfaceOfIGameEntity.GetScriptComponentAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetScriptComponentAtIndexDelegate));
				return;
			case 306:
				ScriptingInterfaceOfIGameEntity.call_GetScriptComponentCountDelegate = (ScriptingInterfaceOfIGameEntity.GetScriptComponentCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetScriptComponentCountDelegate));
				return;
			case 307:
				ScriptingInterfaceOfIGameEntity.call_GetScriptComponentIndexDelegate = (ScriptingInterfaceOfIGameEntity.GetScriptComponentIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetScriptComponentIndexDelegate));
				return;
			case 308:
				ScriptingInterfaceOfIGameEntity.call_GetSkeletonDelegate = (ScriptingInterfaceOfIGameEntity.GetSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetSkeletonDelegate));
				return;
			case 309:
				ScriptingInterfaceOfIGameEntity.call_GetTagsDelegate = (ScriptingInterfaceOfIGameEntity.GetTagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetTagsDelegate));
				return;
			case 310:
				ScriptingInterfaceOfIGameEntity.call_GetUpgradeLevelMaskDelegate = (ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskDelegate));
				return;
			case 311:
				ScriptingInterfaceOfIGameEntity.call_GetUpgradeLevelMaskCumulativeDelegate = (ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskCumulativeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskCumulativeDelegate));
				return;
			case 312:
				ScriptingInterfaceOfIGameEntity.call_GetVisibilityExcludeParentsDelegate = (ScriptingInterfaceOfIGameEntity.GetVisibilityExcludeParentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetVisibilityExcludeParentsDelegate));
				return;
			case 313:
				ScriptingInterfaceOfIGameEntity.call_GetVisibilityLevelMaskIncludingParentsDelegate = (ScriptingInterfaceOfIGameEntity.GetVisibilityLevelMaskIncludingParentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetVisibilityLevelMaskIncludingParentsDelegate));
				return;
			case 314:
				ScriptingInterfaceOfIGameEntity.call_GetWaterLevelAtPositionDelegate = (ScriptingInterfaceOfIGameEntity.GetWaterLevelAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.GetWaterLevelAtPositionDelegate));
				return;
			case 315:
				ScriptingInterfaceOfIGameEntity.call_HasBatchedKinematicPhysicsFlagDelegate = (ScriptingInterfaceOfIGameEntity.HasBatchedKinematicPhysicsFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasBatchedKinematicPhysicsFlagDelegate));
				return;
			case 316:
				ScriptingInterfaceOfIGameEntity.call_HasBatchedRayCastPhysicsFlagDelegate = (ScriptingInterfaceOfIGameEntity.HasBatchedRayCastPhysicsFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasBatchedRayCastPhysicsFlagDelegate));
				return;
			case 317:
				ScriptingInterfaceOfIGameEntity.call_HasBodyDelegate = (ScriptingInterfaceOfIGameEntity.HasBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasBodyDelegate));
				return;
			case 318:
				ScriptingInterfaceOfIGameEntity.call_HasComplexAnimTreeDelegate = (ScriptingInterfaceOfIGameEntity.HasComplexAnimTreeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasComplexAnimTreeDelegate));
				return;
			case 319:
				ScriptingInterfaceOfIGameEntity.call_HasComponentDelegate = (ScriptingInterfaceOfIGameEntity.HasComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasComponentDelegate));
				return;
			case 320:
				ScriptingInterfaceOfIGameEntity.call_HasDynamicRigidBodyDelegate = (ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyDelegate));
				return;
			case 321:
				ScriptingInterfaceOfIGameEntity.call_HasDynamicRigidBodyAndActiveSimulationDelegate = (ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyAndActiveSimulationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyAndActiveSimulationDelegate));
				return;
			case 322:
				ScriptingInterfaceOfIGameEntity.call_HasFrameChangedDelegate = (ScriptingInterfaceOfIGameEntity.HasFrameChangedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasFrameChangedDelegate));
				return;
			case 323:
				ScriptingInterfaceOfIGameEntity.call_HasKinematicRigidBodyDelegate = (ScriptingInterfaceOfIGameEntity.HasKinematicRigidBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasKinematicRigidBodyDelegate));
				return;
			case 324:
				ScriptingInterfaceOfIGameEntity.call_HasPhysicsBodyDelegate = (ScriptingInterfaceOfIGameEntity.HasPhysicsBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasPhysicsBodyDelegate));
				return;
			case 325:
				ScriptingInterfaceOfIGameEntity.call_HasPhysicsDefinitionDelegate = (ScriptingInterfaceOfIGameEntity.HasPhysicsDefinitionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasPhysicsDefinitionDelegate));
				return;
			case 326:
				ScriptingInterfaceOfIGameEntity.call_HasSceneDelegate = (ScriptingInterfaceOfIGameEntity.HasSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasSceneDelegate));
				return;
			case 327:
				ScriptingInterfaceOfIGameEntity.call_HasScriptComponentDelegate = (ScriptingInterfaceOfIGameEntity.HasScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasScriptComponentDelegate));
				return;
			case 328:
				ScriptingInterfaceOfIGameEntity.call_HasScriptComponentHashDelegate = (ScriptingInterfaceOfIGameEntity.HasScriptComponentHashDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasScriptComponentHashDelegate));
				return;
			case 329:
				ScriptingInterfaceOfIGameEntity.call_HasStaticPhysicsBodyDelegate = (ScriptingInterfaceOfIGameEntity.HasStaticPhysicsBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasStaticPhysicsBodyDelegate));
				return;
			case 330:
				ScriptingInterfaceOfIGameEntity.call_HasTagDelegate = (ScriptingInterfaceOfIGameEntity.HasTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.HasTagDelegate));
				return;
			case 331:
				ScriptingInterfaceOfIGameEntity.call_IsDynamicBodyStationaryDelegate = (ScriptingInterfaceOfIGameEntity.IsDynamicBodyStationaryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsDynamicBodyStationaryDelegate));
				return;
			case 332:
				ScriptingInterfaceOfIGameEntity.call_IsEngineBodySleepingDelegate = (ScriptingInterfaceOfIGameEntity.IsEngineBodySleepingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsEngineBodySleepingDelegate));
				return;
			case 333:
				ScriptingInterfaceOfIGameEntity.call_IsEntitySelectedOnEditorDelegate = (ScriptingInterfaceOfIGameEntity.IsEntitySelectedOnEditorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsEntitySelectedOnEditorDelegate));
				return;
			case 334:
				ScriptingInterfaceOfIGameEntity.call_IsFrozenDelegate = (ScriptingInterfaceOfIGameEntity.IsFrozenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsFrozenDelegate));
				return;
			case 335:
				ScriptingInterfaceOfIGameEntity.call_IsGhostObjectDelegate = (ScriptingInterfaceOfIGameEntity.IsGhostObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsGhostObjectDelegate));
				return;
			case 336:
				ScriptingInterfaceOfIGameEntity.call_IsGravityDisabledDelegate = (ScriptingInterfaceOfIGameEntity.IsGravityDisabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsGravityDisabledDelegate));
				return;
			case 337:
				ScriptingInterfaceOfIGameEntity.call_IsGuidValidDelegate = (ScriptingInterfaceOfIGameEntity.IsGuidValidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsGuidValidDelegate));
				return;
			case 338:
				ScriptingInterfaceOfIGameEntity.call_IsInEditorSceneDelegate = (ScriptingInterfaceOfIGameEntity.IsInEditorSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsInEditorSceneDelegate));
				return;
			case 339:
				ScriptingInterfaceOfIGameEntity.call_IsVisibleIncludeParentsDelegate = (ScriptingInterfaceOfIGameEntity.IsVisibleIncludeParentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.IsVisibleIncludeParentsDelegate));
				return;
			case 340:
				ScriptingInterfaceOfIGameEntity.call_PauseParticleSystemDelegate = (ScriptingInterfaceOfIGameEntity.PauseParticleSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.PauseParticleSystemDelegate));
				return;
			case 341:
				ScriptingInterfaceOfIGameEntity.call_PopCapsuleShapeFromEntityBodyDelegate = (ScriptingInterfaceOfIGameEntity.PopCapsuleShapeFromEntityBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.PopCapsuleShapeFromEntityBodyDelegate));
				return;
			case 342:
				ScriptingInterfaceOfIGameEntity.call_PrefabExistsDelegate = (ScriptingInterfaceOfIGameEntity.PrefabExistsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.PrefabExistsDelegate));
				return;
			case 343:
				ScriptingInterfaceOfIGameEntity.call_PushCapsuleShapeToEntityBodyDelegate = (ScriptingInterfaceOfIGameEntity.PushCapsuleShapeToEntityBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.PushCapsuleShapeToEntityBodyDelegate));
				return;
			case 344:
				ScriptingInterfaceOfIGameEntity.call_RayHitEntityDelegate = (ScriptingInterfaceOfIGameEntity.RayHitEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RayHitEntityDelegate));
				return;
			case 345:
				ScriptingInterfaceOfIGameEntity.call_RayHitEntityWithNormalDelegate = (ScriptingInterfaceOfIGameEntity.RayHitEntityWithNormalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RayHitEntityWithNormalDelegate));
				return;
			case 346:
				ScriptingInterfaceOfIGameEntity.call_RecomputeBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.RecomputeBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RecomputeBoundingBoxDelegate));
				return;
			case 347:
				ScriptingInterfaceOfIGameEntity.call_RefreshMeshesToRenderToHullWaterDelegate = (ScriptingInterfaceOfIGameEntity.RefreshMeshesToRenderToHullWaterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RefreshMeshesToRenderToHullWaterDelegate));
				return;
			case 348:
				ScriptingInterfaceOfIGameEntity.call_RegisterWaterSDFClipDelegate = (ScriptingInterfaceOfIGameEntity.RegisterWaterSDFClipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RegisterWaterSDFClipDelegate));
				return;
			case 349:
				ScriptingInterfaceOfIGameEntity.call_RelaxLocalBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.RelaxLocalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RelaxLocalBoundingBoxDelegate));
				return;
			case 350:
				ScriptingInterfaceOfIGameEntity.call_ReleaseEditDataUserToAllMeshesDelegate = (ScriptingInterfaceOfIGameEntity.ReleaseEditDataUserToAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ReleaseEditDataUserToAllMeshesDelegate));
				return;
			case 351:
				ScriptingInterfaceOfIGameEntity.call_RemoveDelegate = (ScriptingInterfaceOfIGameEntity.RemoveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveDelegate));
				return;
			case 352:
				ScriptingInterfaceOfIGameEntity.call_RemoveAllChildrenDelegate = (ScriptingInterfaceOfIGameEntity.RemoveAllChildrenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveAllChildrenDelegate));
				return;
			case 353:
				ScriptingInterfaceOfIGameEntity.call_RemoveAllParticleSystemsDelegate = (ScriptingInterfaceOfIGameEntity.RemoveAllParticleSystemsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveAllParticleSystemsDelegate));
				return;
			case 354:
				ScriptingInterfaceOfIGameEntity.call_RemoveChildDelegate = (ScriptingInterfaceOfIGameEntity.RemoveChildDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveChildDelegate));
				return;
			case 355:
				ScriptingInterfaceOfIGameEntity.call_RemoveComponentDelegate = (ScriptingInterfaceOfIGameEntity.RemoveComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveComponentDelegate));
				return;
			case 356:
				ScriptingInterfaceOfIGameEntity.call_RemoveComponentWithMeshDelegate = (ScriptingInterfaceOfIGameEntity.RemoveComponentWithMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveComponentWithMeshDelegate));
				return;
			case 357:
				ScriptingInterfaceOfIGameEntity.call_RemoveEnginePhysicsDelegate = (ScriptingInterfaceOfIGameEntity.RemoveEnginePhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveEnginePhysicsDelegate));
				return;
			case 358:
				ScriptingInterfaceOfIGameEntity.call_RemoveFromPredisplayEntityDelegate = (ScriptingInterfaceOfIGameEntity.RemoveFromPredisplayEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveFromPredisplayEntityDelegate));
				return;
			case 359:
				ScriptingInterfaceOfIGameEntity.call_RemoveJointDelegate = (ScriptingInterfaceOfIGameEntity.RemoveJointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveJointDelegate));
				return;
			case 360:
				ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshDelegate = (ScriptingInterfaceOfIGameEntity.RemoveMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveMultiMeshDelegate));
				return;
			case 361:
				ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshFromSkeletonDelegate = (ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonDelegate));
				return;
			case 362:
				ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshFromSkeletonBoneDelegate = (ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonBoneDelegate));
				return;
			case 363:
				ScriptingInterfaceOfIGameEntity.call_RemovePhysicsDelegate = (ScriptingInterfaceOfIGameEntity.RemovePhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemovePhysicsDelegate));
				return;
			case 364:
				ScriptingInterfaceOfIGameEntity.call_RemoveScriptComponentDelegate = (ScriptingInterfaceOfIGameEntity.RemoveScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveScriptComponentDelegate));
				return;
			case 365:
				ScriptingInterfaceOfIGameEntity.call_RemoveTagDelegate = (ScriptingInterfaceOfIGameEntity.RemoveTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.RemoveTagDelegate));
				return;
			case 366:
				ScriptingInterfaceOfIGameEntity.call_ReplacePhysicsBodyWithQuadPhysicsBodyDelegate = (ScriptingInterfaceOfIGameEntity.ReplacePhysicsBodyWithQuadPhysicsBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ReplacePhysicsBodyWithQuadPhysicsBodyDelegate));
				return;
			case 367:
				ScriptingInterfaceOfIGameEntity.call_ResetHullWaterDelegate = (ScriptingInterfaceOfIGameEntity.ResetHullWaterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ResetHullWaterDelegate));
				return;
			case 368:
				ScriptingInterfaceOfIGameEntity.call_ResumeParticleSystemDelegate = (ScriptingInterfaceOfIGameEntity.ResumeParticleSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ResumeParticleSystemDelegate));
				return;
			case 369:
				ScriptingInterfaceOfIGameEntity.call_SelectEntityOnEditorDelegate = (ScriptingInterfaceOfIGameEntity.SelectEntityOnEditorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SelectEntityOnEditorDelegate));
				return;
			case 370:
				ScriptingInterfaceOfIGameEntity.call_SetAlphaDelegate = (ScriptingInterfaceOfIGameEntity.SetAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAlphaDelegate));
				return;
			case 371:
				ScriptingInterfaceOfIGameEntity.call_SetAngularVelocityDelegate = (ScriptingInterfaceOfIGameEntity.SetAngularVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAngularVelocityDelegate));
				return;
			case 372:
				ScriptingInterfaceOfIGameEntity.call_SetAnimationSoundActivationDelegate = (ScriptingInterfaceOfIGameEntity.SetAnimationSoundActivationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAnimationSoundActivationDelegate));
				return;
			case 373:
				ScriptingInterfaceOfIGameEntity.call_SetAnimTreeChannelParameterDelegate = (ScriptingInterfaceOfIGameEntity.SetAnimTreeChannelParameterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAnimTreeChannelParameterDelegate));
				return;
			case 374:
				ScriptingInterfaceOfIGameEntity.call_SetAsContourEntityDelegate = (ScriptingInterfaceOfIGameEntity.SetAsContourEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAsContourEntityDelegate));
				return;
			case 375:
				ScriptingInterfaceOfIGameEntity.call_SetAsPredisplayEntityDelegate = (ScriptingInterfaceOfIGameEntity.SetAsPredisplayEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAsPredisplayEntityDelegate));
				return;
			case 376:
				ScriptingInterfaceOfIGameEntity.call_SetAsReplayEntityDelegate = (ScriptingInterfaceOfIGameEntity.SetAsReplayEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetAsReplayEntityDelegate));
				return;
			case 377:
				ScriptingInterfaceOfIGameEntity.call_SetBodyFlagsDelegate = (ScriptingInterfaceOfIGameEntity.SetBodyFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetBodyFlagsDelegate));
				return;
			case 378:
				ScriptingInterfaceOfIGameEntity.call_SetBodyFlagsRecursiveDelegate = (ScriptingInterfaceOfIGameEntity.SetBodyFlagsRecursiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetBodyFlagsRecursiveDelegate));
				return;
			case 379:
				ScriptingInterfaceOfIGameEntity.call_SetBodyShapeDelegate = (ScriptingInterfaceOfIGameEntity.SetBodyShapeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetBodyShapeDelegate));
				return;
			case 380:
				ScriptingInterfaceOfIGameEntity.call_SetBoneFrameToAllMeshesDelegate = (ScriptingInterfaceOfIGameEntity.SetBoneFrameToAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetBoneFrameToAllMeshesDelegate));
				return;
			case 381:
				ScriptingInterfaceOfIGameEntity.call_SetBoundingboxDirtyDelegate = (ScriptingInterfaceOfIGameEntity.SetBoundingboxDirtyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetBoundingboxDirtyDelegate));
				return;
			case 382:
				ScriptingInterfaceOfIGameEntity.call_SetCenterOfMassDelegate = (ScriptingInterfaceOfIGameEntity.SetCenterOfMassDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetCenterOfMassDelegate));
				return;
			case 383:
				ScriptingInterfaceOfIGameEntity.call_SetClothComponentKeepStateDelegate = (ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateDelegate));
				return;
			case 384:
				ScriptingInterfaceOfIGameEntity.call_SetClothComponentKeepStateOfAllMeshesDelegate = (ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateOfAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateOfAllMeshesDelegate));
				return;
			case 385:
				ScriptingInterfaceOfIGameEntity.call_SetClothMaxDistanceMultiplierDelegate = (ScriptingInterfaceOfIGameEntity.SetClothMaxDistanceMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetClothMaxDistanceMultiplierDelegate));
				return;
			case 386:
				ScriptingInterfaceOfIGameEntity.call_SetColorToAllMeshesWithTagRecursiveDelegate = (ScriptingInterfaceOfIGameEntity.SetColorToAllMeshesWithTagRecursiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetColorToAllMeshesWithTagRecursiveDelegate));
				return;
			case 387:
				ScriptingInterfaceOfIGameEntity.call_SetContourStateDelegate = (ScriptingInterfaceOfIGameEntity.SetContourStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetContourStateDelegate));
				return;
			case 388:
				ScriptingInterfaceOfIGameEntity.call_SetCostAdderForAttachedFacesDelegate = (ScriptingInterfaceOfIGameEntity.SetCostAdderForAttachedFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetCostAdderForAttachedFacesDelegate));
				return;
			case 389:
				ScriptingInterfaceOfIGameEntity.call_SetCullModeDelegate = (ScriptingInterfaceOfIGameEntity.SetCullModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetCullModeDelegate));
				return;
			case 390:
				ScriptingInterfaceOfIGameEntity.call_SetCustomClipPlaneDelegate = (ScriptingInterfaceOfIGameEntity.SetCustomClipPlaneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetCustomClipPlaneDelegate));
				return;
			case 391:
				ScriptingInterfaceOfIGameEntity.call_SetCustomVertexPositionEnabledDelegate = (ScriptingInterfaceOfIGameEntity.SetCustomVertexPositionEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetCustomVertexPositionEnabledDelegate));
				return;
			case 392:
				ScriptingInterfaceOfIGameEntity.call_SetDampingDelegate = (ScriptingInterfaceOfIGameEntity.SetDampingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetDampingDelegate));
				return;
			case 393:
				ScriptingInterfaceOfIGameEntity.call_SetDoNotCheckVisibilityDelegate = (ScriptingInterfaceOfIGameEntity.SetDoNotCheckVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetDoNotCheckVisibilityDelegate));
				return;
			case 394:
				ScriptingInterfaceOfIGameEntity.call_SetEnforcedMaximumLodLevelDelegate = (ScriptingInterfaceOfIGameEntity.SetEnforcedMaximumLodLevelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetEnforcedMaximumLodLevelDelegate));
				return;
			case 395:
				ScriptingInterfaceOfIGameEntity.call_SetEntityEnvMapVisibilityDelegate = (ScriptingInterfaceOfIGameEntity.SetEntityEnvMapVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetEntityEnvMapVisibilityDelegate));
				return;
			case 396:
				ScriptingInterfaceOfIGameEntity.call_SetEntityFlagsDelegate = (ScriptingInterfaceOfIGameEntity.SetEntityFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetEntityFlagsDelegate));
				return;
			case 397:
				ScriptingInterfaceOfIGameEntity.call_SetEntityVisibilityFlagsDelegate = (ScriptingInterfaceOfIGameEntity.SetEntityVisibilityFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetEntityVisibilityFlagsDelegate));
				return;
			case 398:
				ScriptingInterfaceOfIGameEntity.call_SetExternalReferencesUsageDelegate = (ScriptingInterfaceOfIGameEntity.SetExternalReferencesUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetExternalReferencesUsageDelegate));
				return;
			case 399:
				ScriptingInterfaceOfIGameEntity.call_SetFactor2ColorDelegate = (ScriptingInterfaceOfIGameEntity.SetFactor2ColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetFactor2ColorDelegate));
				return;
			case 400:
				ScriptingInterfaceOfIGameEntity.call_SetFactorColorDelegate = (ScriptingInterfaceOfIGameEntity.SetFactorColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetFactorColorDelegate));
				return;
			case 401:
				ScriptingInterfaceOfIGameEntity.call_SetForceDecalsToRenderDelegate = (ScriptingInterfaceOfIGameEntity.SetForceDecalsToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetForceDecalsToRenderDelegate));
				return;
			case 402:
				ScriptingInterfaceOfIGameEntity.call_SetForceNotAffectedBySeasonDelegate = (ScriptingInterfaceOfIGameEntity.SetForceNotAffectedBySeasonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetForceNotAffectedBySeasonDelegate));
				return;
			case 403:
				ScriptingInterfaceOfIGameEntity.call_SetFrameChangedDelegate = (ScriptingInterfaceOfIGameEntity.SetFrameChangedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetFrameChangedDelegate));
				return;
			case 404:
				ScriptingInterfaceOfIGameEntity.call_SetGlobalFrameDelegate = (ScriptingInterfaceOfIGameEntity.SetGlobalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetGlobalFrameDelegate));
				return;
			case 405:
				ScriptingInterfaceOfIGameEntity.call_SetGlobalPositionDelegate = (ScriptingInterfaceOfIGameEntity.SetGlobalPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetGlobalPositionDelegate));
				return;
			case 406:
				ScriptingInterfaceOfIGameEntity.call_SetHasCustomBoundingBoxValidationSystemDelegate = (ScriptingInterfaceOfIGameEntity.SetHasCustomBoundingBoxValidationSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetHasCustomBoundingBoxValidationSystemDelegate));
				return;
			case 407:
				ScriptingInterfaceOfIGameEntity.call_SetLinearVelocityDelegate = (ScriptingInterfaceOfIGameEntity.SetLinearVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetLinearVelocityDelegate));
				return;
			case 408:
				ScriptingInterfaceOfIGameEntity.call_SetLocalFrameDelegate = (ScriptingInterfaceOfIGameEntity.SetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetLocalFrameDelegate));
				return;
			case 409:
				ScriptingInterfaceOfIGameEntity.call_SetLocalPositionDelegate = (ScriptingInterfaceOfIGameEntity.SetLocalPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetLocalPositionDelegate));
				return;
			case 410:
				ScriptingInterfaceOfIGameEntity.call_SetManualGlobalBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.SetManualGlobalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetManualGlobalBoundingBoxDelegate));
				return;
			case 411:
				ScriptingInterfaceOfIGameEntity.call_SetManualLocalBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.SetManualLocalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetManualLocalBoundingBoxDelegate));
				return;
			case 412:
				ScriptingInterfaceOfIGameEntity.call_SetMassAndUpdateInertiaAndCenterOfMassDelegate = (ScriptingInterfaceOfIGameEntity.SetMassAndUpdateInertiaAndCenterOfMassDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMassAndUpdateInertiaAndCenterOfMassDelegate));
				return;
			case 413:
				ScriptingInterfaceOfIGameEntity.call_SetMassSpaceInertiaDelegate = (ScriptingInterfaceOfIGameEntity.SetMassSpaceInertiaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMassSpaceInertiaDelegate));
				return;
			case 414:
				ScriptingInterfaceOfIGameEntity.call_SetMaterialForAllMeshesDelegate = (ScriptingInterfaceOfIGameEntity.SetMaterialForAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMaterialForAllMeshesDelegate));
				return;
			case 415:
				ScriptingInterfaceOfIGameEntity.call_SetMaxDepenetrationVelocityDelegate = (ScriptingInterfaceOfIGameEntity.SetMaxDepenetrationVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMaxDepenetrationVelocityDelegate));
				return;
			case 416:
				ScriptingInterfaceOfIGameEntity.call_SetMobilityDelegate = (ScriptingInterfaceOfIGameEntity.SetMobilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMobilityDelegate));
				return;
			case 417:
				ScriptingInterfaceOfIGameEntity.call_SetMorphFrameOfComponentsDelegate = (ScriptingInterfaceOfIGameEntity.SetMorphFrameOfComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetMorphFrameOfComponentsDelegate));
				return;
			case 418:
				ScriptingInterfaceOfIGameEntity.call_SetNameDelegate = (ScriptingInterfaceOfIGameEntity.SetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetNameDelegate));
				return;
			case 419:
				ScriptingInterfaceOfIGameEntity.call_SetNativeScriptComponentVariableDelegate = (ScriptingInterfaceOfIGameEntity.SetNativeScriptComponentVariableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetNativeScriptComponentVariableDelegate));
				return;
			case 420:
				ScriptingInterfaceOfIGameEntity.call_SetPhysicsMoveToBatchedDelegate = (ScriptingInterfaceOfIGameEntity.SetPhysicsMoveToBatchedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetPhysicsMoveToBatchedDelegate));
				return;
			case 421:
				ScriptingInterfaceOfIGameEntity.call_SetPhysicsStateDelegate = (ScriptingInterfaceOfIGameEntity.SetPhysicsStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetPhysicsStateDelegate));
				return;
			case 422:
				ScriptingInterfaceOfIGameEntity.call_SetPhysicsStateOnlyVariableDelegate = (ScriptingInterfaceOfIGameEntity.SetPhysicsStateOnlyVariableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetPhysicsStateOnlyVariableDelegate));
				return;
			case 423:
				ScriptingInterfaceOfIGameEntity.call_SetPositionsForAttachedNavmeshVerticesDelegate = (ScriptingInterfaceOfIGameEntity.SetPositionsForAttachedNavmeshVerticesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetPositionsForAttachedNavmeshVerticesDelegate));
				return;
			case 424:
				ScriptingInterfaceOfIGameEntity.call_SetPreviousFrameInvalidDelegate = (ScriptingInterfaceOfIGameEntity.SetPreviousFrameInvalidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetPreviousFrameInvalidDelegate));
				return;
			case 425:
				ScriptingInterfaceOfIGameEntity.call_SetReadyToRenderDelegate = (ScriptingInterfaceOfIGameEntity.SetReadyToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetReadyToRenderDelegate));
				return;
			case 426:
				ScriptingInterfaceOfIGameEntity.call_SetRuntimeEmissionRateMultiplierDelegate = (ScriptingInterfaceOfIGameEntity.SetRuntimeEmissionRateMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetRuntimeEmissionRateMultiplierDelegate));
				return;
			case 427:
				ScriptingInterfaceOfIGameEntity.call_SetSkeletonDelegate = (ScriptingInterfaceOfIGameEntity.SetSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetSkeletonDelegate));
				return;
			case 428:
				ScriptingInterfaceOfIGameEntity.call_SetSolverIterationCountsDelegate = (ScriptingInterfaceOfIGameEntity.SetSolverIterationCountsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetSolverIterationCountsDelegate));
				return;
			case 429:
				ScriptingInterfaceOfIGameEntity.call_SetupAdditionalBoneBufferForMeshesDelegate = (ScriptingInterfaceOfIGameEntity.SetupAdditionalBoneBufferForMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetupAdditionalBoneBufferForMeshesDelegate));
				return;
			case 430:
				ScriptingInterfaceOfIGameEntity.call_SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate = (ScriptingInterfaceOfIGameEntity.SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate));
				return;
			case 431:
				ScriptingInterfaceOfIGameEntity.call_SetUpgradeLevelMaskDelegate = (ScriptingInterfaceOfIGameEntity.SetUpgradeLevelMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetUpgradeLevelMaskDelegate));
				return;
			case 432:
				ScriptingInterfaceOfIGameEntity.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfIGameEntity.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetVectorArgumentDelegate));
				return;
			case 433:
				ScriptingInterfaceOfIGameEntity.call_SetVelocityLimitsDelegate = (ScriptingInterfaceOfIGameEntity.SetVelocityLimitsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetVelocityLimitsDelegate));
				return;
			case 434:
				ScriptingInterfaceOfIGameEntity.call_SetVisibilityExcludeParentsDelegate = (ScriptingInterfaceOfIGameEntity.SetVisibilityExcludeParentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetVisibilityExcludeParentsDelegate));
				return;
			case 435:
				ScriptingInterfaceOfIGameEntity.call_SetVisualRecordWakeParamsDelegate = (ScriptingInterfaceOfIGameEntity.SetVisualRecordWakeParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetVisualRecordWakeParamsDelegate));
				return;
			case 436:
				ScriptingInterfaceOfIGameEntity.call_SetWaterSDFClipDataDelegate = (ScriptingInterfaceOfIGameEntity.SetWaterSDFClipDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetWaterSDFClipDataDelegate));
				return;
			case 437:
				ScriptingInterfaceOfIGameEntity.call_SetWaterVisualRecordFrameAndDtDelegate = (ScriptingInterfaceOfIGameEntity.SetWaterVisualRecordFrameAndDtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SetWaterVisualRecordFrameAndDtDelegate));
				return;
			case 438:
				ScriptingInterfaceOfIGameEntity.call_SwapPhysxShapeInEntityDelegate = (ScriptingInterfaceOfIGameEntity.SwapPhysxShapeInEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.SwapPhysxShapeInEntityDelegate));
				return;
			case 439:
				ScriptingInterfaceOfIGameEntity.call_UpdateAttachedNavigationMeshFacesDelegate = (ScriptingInterfaceOfIGameEntity.UpdateAttachedNavigationMeshFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.UpdateAttachedNavigationMeshFacesDelegate));
				return;
			case 440:
				ScriptingInterfaceOfIGameEntity.call_UpdateGlobalBoundsDelegate = (ScriptingInterfaceOfIGameEntity.UpdateGlobalBoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.UpdateGlobalBoundsDelegate));
				return;
			case 441:
				ScriptingInterfaceOfIGameEntity.call_UpdateHullWaterEffectFramesDelegate = (ScriptingInterfaceOfIGameEntity.UpdateHullWaterEffectFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.UpdateHullWaterEffectFramesDelegate));
				return;
			case 442:
				ScriptingInterfaceOfIGameEntity.call_UpdateTriadFrameForEditorDelegate = (ScriptingInterfaceOfIGameEntity.UpdateTriadFrameForEditorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.UpdateTriadFrameForEditorDelegate));
				return;
			case 443:
				ScriptingInterfaceOfIGameEntity.call_UpdateVisibilityMaskDelegate = (ScriptingInterfaceOfIGameEntity.UpdateVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.UpdateVisibilityMaskDelegate));
				return;
			case 444:
				ScriptingInterfaceOfIGameEntity.call_ValidateBoundingBoxDelegate = (ScriptingInterfaceOfIGameEntity.ValidateBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntity.ValidateBoundingBoxDelegate));
				return;
			case 445:
				ScriptingInterfaceOfIGameEntityComponent.call_GetEntityDelegate = (ScriptingInterfaceOfIGameEntityComponent.GetEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntityComponent.GetEntityDelegate));
				return;
			case 446:
				ScriptingInterfaceOfIGameEntityComponent.call_GetEntityPointerDelegate = (ScriptingInterfaceOfIGameEntityComponent.GetEntityPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntityComponent.GetEntityPointerDelegate));
				return;
			case 447:
				ScriptingInterfaceOfIGameEntityComponent.call_GetFirstMetaMeshDelegate = (ScriptingInterfaceOfIGameEntityComponent.GetFirstMetaMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIGameEntityComponent.GetFirstMetaMeshDelegate));
				return;
			case 448:
				ScriptingInterfaceOfIHighlights.call_AddHighlightDelegate = (ScriptingInterfaceOfIHighlights.AddHighlightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.AddHighlightDelegate));
				return;
			case 449:
				ScriptingInterfaceOfIHighlights.call_CloseGroupDelegate = (ScriptingInterfaceOfIHighlights.CloseGroupDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.CloseGroupDelegate));
				return;
			case 450:
				ScriptingInterfaceOfIHighlights.call_InitializeDelegate = (ScriptingInterfaceOfIHighlights.InitializeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.InitializeDelegate));
				return;
			case 451:
				ScriptingInterfaceOfIHighlights.call_OpenGroupDelegate = (ScriptingInterfaceOfIHighlights.OpenGroupDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.OpenGroupDelegate));
				return;
			case 452:
				ScriptingInterfaceOfIHighlights.call_OpenSummaryDelegate = (ScriptingInterfaceOfIHighlights.OpenSummaryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.OpenSummaryDelegate));
				return;
			case 453:
				ScriptingInterfaceOfIHighlights.call_RemoveHighlightDelegate = (ScriptingInterfaceOfIHighlights.RemoveHighlightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.RemoveHighlightDelegate));
				return;
			case 454:
				ScriptingInterfaceOfIHighlights.call_SaveScreenshotDelegate = (ScriptingInterfaceOfIHighlights.SaveScreenshotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.SaveScreenshotDelegate));
				return;
			case 455:
				ScriptingInterfaceOfIHighlights.call_SaveVideoDelegate = (ScriptingInterfaceOfIHighlights.SaveVideoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIHighlights.SaveVideoDelegate));
				return;
			case 456:
				ScriptingInterfaceOfIImgui.call_BeginDelegate = (ScriptingInterfaceOfIImgui.BeginDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.BeginDelegate));
				return;
			case 457:
				ScriptingInterfaceOfIImgui.call_BeginMainThreadScopeDelegate = (ScriptingInterfaceOfIImgui.BeginMainThreadScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.BeginMainThreadScopeDelegate));
				return;
			case 458:
				ScriptingInterfaceOfIImgui.call_BeginWithCloseButtonDelegate = (ScriptingInterfaceOfIImgui.BeginWithCloseButtonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.BeginWithCloseButtonDelegate));
				return;
			case 459:
				ScriptingInterfaceOfIImgui.call_ButtonDelegate = (ScriptingInterfaceOfIImgui.ButtonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.ButtonDelegate));
				return;
			case 460:
				ScriptingInterfaceOfIImgui.call_CheckboxDelegate = (ScriptingInterfaceOfIImgui.CheckboxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.CheckboxDelegate));
				return;
			case 461:
				ScriptingInterfaceOfIImgui.call_CollapsingHeaderDelegate = (ScriptingInterfaceOfIImgui.CollapsingHeaderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.CollapsingHeaderDelegate));
				return;
			case 462:
				ScriptingInterfaceOfIImgui.call_ColumnsDelegate = (ScriptingInterfaceOfIImgui.ColumnsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.ColumnsDelegate));
				return;
			case 463:
				ScriptingInterfaceOfIImgui.call_ComboDelegate = (ScriptingInterfaceOfIImgui.ComboDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.ComboDelegate));
				return;
			case 464:
				ScriptingInterfaceOfIImgui.call_ComboCustomSeperatorDelegate = (ScriptingInterfaceOfIImgui.ComboCustomSeperatorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.ComboCustomSeperatorDelegate));
				return;
			case 465:
				ScriptingInterfaceOfIImgui.call_EndDelegate = (ScriptingInterfaceOfIImgui.EndDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.EndDelegate));
				return;
			case 466:
				ScriptingInterfaceOfIImgui.call_EndMainThreadScopeDelegate = (ScriptingInterfaceOfIImgui.EndMainThreadScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.EndMainThreadScopeDelegate));
				return;
			case 467:
				ScriptingInterfaceOfIImgui.call_InputFloatDelegate = (ScriptingInterfaceOfIImgui.InputFloatDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputFloatDelegate));
				return;
			case 468:
				ScriptingInterfaceOfIImgui.call_InputFloat2Delegate = (ScriptingInterfaceOfIImgui.InputFloat2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputFloat2Delegate));
				return;
			case 469:
				ScriptingInterfaceOfIImgui.call_InputFloat3Delegate = (ScriptingInterfaceOfIImgui.InputFloat3Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputFloat3Delegate));
				return;
			case 470:
				ScriptingInterfaceOfIImgui.call_InputFloat4Delegate = (ScriptingInterfaceOfIImgui.InputFloat4Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputFloat4Delegate));
				return;
			case 471:
				ScriptingInterfaceOfIImgui.call_InputIntDelegate = (ScriptingInterfaceOfIImgui.InputIntDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputIntDelegate));
				return;
			case 472:
				ScriptingInterfaceOfIImgui.call_InputTextDelegate = (ScriptingInterfaceOfIImgui.InputTextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputTextDelegate));
				return;
			case 473:
				ScriptingInterfaceOfIImgui.call_InputTextMultilineCopyPasteDelegate = (ScriptingInterfaceOfIImgui.InputTextMultilineCopyPasteDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.InputTextMultilineCopyPasteDelegate));
				return;
			case 474:
				ScriptingInterfaceOfIImgui.call_IsItemHoveredDelegate = (ScriptingInterfaceOfIImgui.IsItemHoveredDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.IsItemHoveredDelegate));
				return;
			case 475:
				ScriptingInterfaceOfIImgui.call_NewFrameDelegate = (ScriptingInterfaceOfIImgui.NewFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.NewFrameDelegate));
				return;
			case 476:
				ScriptingInterfaceOfIImgui.call_NewLineDelegate = (ScriptingInterfaceOfIImgui.NewLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.NewLineDelegate));
				return;
			case 477:
				ScriptingInterfaceOfIImgui.call_NextColumnDelegate = (ScriptingInterfaceOfIImgui.NextColumnDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.NextColumnDelegate));
				return;
			case 478:
				ScriptingInterfaceOfIImgui.call_PlotLinesDelegate = (ScriptingInterfaceOfIImgui.PlotLinesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.PlotLinesDelegate));
				return;
			case 479:
				ScriptingInterfaceOfIImgui.call_PopStyleColorDelegate = (ScriptingInterfaceOfIImgui.PopStyleColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.PopStyleColorDelegate));
				return;
			case 480:
				ScriptingInterfaceOfIImgui.call_ProgressBarDelegate = (ScriptingInterfaceOfIImgui.ProgressBarDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.ProgressBarDelegate));
				return;
			case 481:
				ScriptingInterfaceOfIImgui.call_PushStyleColorDelegate = (ScriptingInterfaceOfIImgui.PushStyleColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.PushStyleColorDelegate));
				return;
			case 482:
				ScriptingInterfaceOfIImgui.call_RadioButtonDelegate = (ScriptingInterfaceOfIImgui.RadioButtonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.RadioButtonDelegate));
				return;
			case 483:
				ScriptingInterfaceOfIImgui.call_RenderDelegate = (ScriptingInterfaceOfIImgui.RenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.RenderDelegate));
				return;
			case 484:
				ScriptingInterfaceOfIImgui.call_SameLineDelegate = (ScriptingInterfaceOfIImgui.SameLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.SameLineDelegate));
				return;
			case 485:
				ScriptingInterfaceOfIImgui.call_SeparatorDelegate = (ScriptingInterfaceOfIImgui.SeparatorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.SeparatorDelegate));
				return;
			case 486:
				ScriptingInterfaceOfIImgui.call_SetTooltipDelegate = (ScriptingInterfaceOfIImgui.SetTooltipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.SetTooltipDelegate));
				return;
			case 487:
				ScriptingInterfaceOfIImgui.call_SliderFloatDelegate = (ScriptingInterfaceOfIImgui.SliderFloatDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.SliderFloatDelegate));
				return;
			case 488:
				ScriptingInterfaceOfIImgui.call_SmallButtonDelegate = (ScriptingInterfaceOfIImgui.SmallButtonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.SmallButtonDelegate));
				return;
			case 489:
				ScriptingInterfaceOfIImgui.call_TextDelegate = (ScriptingInterfaceOfIImgui.TextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.TextDelegate));
				return;
			case 490:
				ScriptingInterfaceOfIImgui.call_TreeNodeDelegate = (ScriptingInterfaceOfIImgui.TreeNodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.TreeNodeDelegate));
				return;
			case 491:
				ScriptingInterfaceOfIImgui.call_TreePopDelegate = (ScriptingInterfaceOfIImgui.TreePopDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIImgui.TreePopDelegate));
				return;
			case 492:
				ScriptingInterfaceOfIInput.call_ClearKeysDelegate = (ScriptingInterfaceOfIInput.ClearKeysDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.ClearKeysDelegate));
				return;
			case 493:
				ScriptingInterfaceOfIInput.call_GetClipboardTextDelegate = (ScriptingInterfaceOfIInput.GetClipboardTextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetClipboardTextDelegate));
				return;
			case 494:
				ScriptingInterfaceOfIInput.call_GetControllerTypeDelegate = (ScriptingInterfaceOfIInput.GetControllerTypeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetControllerTypeDelegate));
				return;
			case 495:
				ScriptingInterfaceOfIInput.call_GetGyroXDelegate = (ScriptingInterfaceOfIInput.GetGyroXDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetGyroXDelegate));
				return;
			case 496:
				ScriptingInterfaceOfIInput.call_GetGyroYDelegate = (ScriptingInterfaceOfIInput.GetGyroYDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetGyroYDelegate));
				return;
			case 497:
				ScriptingInterfaceOfIInput.call_GetGyroZDelegate = (ScriptingInterfaceOfIInput.GetGyroZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetGyroZDelegate));
				return;
			case 498:
				ScriptingInterfaceOfIInput.call_GetKeyStateDelegate = (ScriptingInterfaceOfIInput.GetKeyStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetKeyStateDelegate));
				return;
			case 499:
				ScriptingInterfaceOfIInput.call_GetMouseDeltaZDelegate = (ScriptingInterfaceOfIInput.GetMouseDeltaZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMouseDeltaZDelegate));
				return;
			case 500:
				ScriptingInterfaceOfIInput.call_GetMouseMoveXDelegate = (ScriptingInterfaceOfIInput.GetMouseMoveXDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMouseMoveXDelegate));
				return;
			case 501:
				ScriptingInterfaceOfIInput.call_GetMouseMoveYDelegate = (ScriptingInterfaceOfIInput.GetMouseMoveYDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMouseMoveYDelegate));
				return;
			case 502:
				ScriptingInterfaceOfIInput.call_GetMousePositionXDelegate = (ScriptingInterfaceOfIInput.GetMousePositionXDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMousePositionXDelegate));
				return;
			case 503:
				ScriptingInterfaceOfIInput.call_GetMousePositionYDelegate = (ScriptingInterfaceOfIInput.GetMousePositionYDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMousePositionYDelegate));
				return;
			case 504:
				ScriptingInterfaceOfIInput.call_GetMouseScrollValueDelegate = (ScriptingInterfaceOfIInput.GetMouseScrollValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMouseScrollValueDelegate));
				return;
			case 505:
				ScriptingInterfaceOfIInput.call_GetMouseSensitivityDelegate = (ScriptingInterfaceOfIInput.GetMouseSensitivityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetMouseSensitivityDelegate));
				return;
			case 506:
				ScriptingInterfaceOfIInput.call_GetVirtualKeyCodeDelegate = (ScriptingInterfaceOfIInput.GetVirtualKeyCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.GetVirtualKeyCodeDelegate));
				return;
			case 507:
				ScriptingInterfaceOfIInput.call_IsAnyTouchActiveDelegate = (ScriptingInterfaceOfIInput.IsAnyTouchActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsAnyTouchActiveDelegate));
				return;
			case 508:
				ScriptingInterfaceOfIInput.call_IsControllerConnectedDelegate = (ScriptingInterfaceOfIInput.IsControllerConnectedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsControllerConnectedDelegate));
				return;
			case 509:
				ScriptingInterfaceOfIInput.call_IsKeyDownDelegate = (ScriptingInterfaceOfIInput.IsKeyDownDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsKeyDownDelegate));
				return;
			case 510:
				ScriptingInterfaceOfIInput.call_IsKeyDownImmediateDelegate = (ScriptingInterfaceOfIInput.IsKeyDownImmediateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsKeyDownImmediateDelegate));
				return;
			case 511:
				ScriptingInterfaceOfIInput.call_IsKeyPressedDelegate = (ScriptingInterfaceOfIInput.IsKeyPressedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsKeyPressedDelegate));
				return;
			case 512:
				ScriptingInterfaceOfIInput.call_IsKeyReleasedDelegate = (ScriptingInterfaceOfIInput.IsKeyReleasedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsKeyReleasedDelegate));
				return;
			case 513:
				ScriptingInterfaceOfIInput.call_IsMouseActiveDelegate = (ScriptingInterfaceOfIInput.IsMouseActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.IsMouseActiveDelegate));
				return;
			case 514:
				ScriptingInterfaceOfIInput.call_PressKeyDelegate = (ScriptingInterfaceOfIInput.PressKeyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.PressKeyDelegate));
				return;
			case 515:
				ScriptingInterfaceOfIInput.call_SetClipboardTextDelegate = (ScriptingInterfaceOfIInput.SetClipboardTextDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetClipboardTextDelegate));
				return;
			case 516:
				ScriptingInterfaceOfIInput.call_SetCursorFrictionValueDelegate = (ScriptingInterfaceOfIInput.SetCursorFrictionValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetCursorFrictionValueDelegate));
				return;
			case 517:
				ScriptingInterfaceOfIInput.call_SetCursorPositionDelegate = (ScriptingInterfaceOfIInput.SetCursorPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetCursorPositionDelegate));
				return;
			case 518:
				ScriptingInterfaceOfIInput.call_SetLightbarColorDelegate = (ScriptingInterfaceOfIInput.SetLightbarColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetLightbarColorDelegate));
				return;
			case 519:
				ScriptingInterfaceOfIInput.call_SetRumbleEffectDelegate = (ScriptingInterfaceOfIInput.SetRumbleEffectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetRumbleEffectDelegate));
				return;
			case 520:
				ScriptingInterfaceOfIInput.call_SetTriggerFeedbackDelegate = (ScriptingInterfaceOfIInput.SetTriggerFeedbackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetTriggerFeedbackDelegate));
				return;
			case 521:
				ScriptingInterfaceOfIInput.call_SetTriggerVibrationDelegate = (ScriptingInterfaceOfIInput.SetTriggerVibrationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetTriggerVibrationDelegate));
				return;
			case 522:
				ScriptingInterfaceOfIInput.call_SetTriggerWeaponEffectDelegate = (ScriptingInterfaceOfIInput.SetTriggerWeaponEffectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.SetTriggerWeaponEffectDelegate));
				return;
			case 523:
				ScriptingInterfaceOfIInput.call_UpdateKeyDataDelegate = (ScriptingInterfaceOfIInput.UpdateKeyDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIInput.UpdateKeyDataDelegate));
				return;
			case 524:
				ScriptingInterfaceOfILight.call_CreatePointLightDelegate = (ScriptingInterfaceOfILight.CreatePointLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.CreatePointLightDelegate));
				return;
			case 525:
				ScriptingInterfaceOfILight.call_EnableShadowDelegate = (ScriptingInterfaceOfILight.EnableShadowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.EnableShadowDelegate));
				return;
			case 526:
				ScriptingInterfaceOfILight.call_GetFrameDelegate = (ScriptingInterfaceOfILight.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.GetFrameDelegate));
				return;
			case 527:
				ScriptingInterfaceOfILight.call_GetIntensityDelegate = (ScriptingInterfaceOfILight.GetIntensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.GetIntensityDelegate));
				return;
			case 528:
				ScriptingInterfaceOfILight.call_GetLightColorDelegate = (ScriptingInterfaceOfILight.GetLightColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.GetLightColorDelegate));
				return;
			case 529:
				ScriptingInterfaceOfILight.call_GetRadiusDelegate = (ScriptingInterfaceOfILight.GetRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.GetRadiusDelegate));
				return;
			case 530:
				ScriptingInterfaceOfILight.call_IsShadowEnabledDelegate = (ScriptingInterfaceOfILight.IsShadowEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.IsShadowEnabledDelegate));
				return;
			case 531:
				ScriptingInterfaceOfILight.call_ReleaseDelegate = (ScriptingInterfaceOfILight.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.ReleaseDelegate));
				return;
			case 532:
				ScriptingInterfaceOfILight.call_SetFrameDelegate = (ScriptingInterfaceOfILight.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetFrameDelegate));
				return;
			case 533:
				ScriptingInterfaceOfILight.call_SetIntensityDelegate = (ScriptingInterfaceOfILight.SetIntensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetIntensityDelegate));
				return;
			case 534:
				ScriptingInterfaceOfILight.call_SetLightColorDelegate = (ScriptingInterfaceOfILight.SetLightColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetLightColorDelegate));
				return;
			case 535:
				ScriptingInterfaceOfILight.call_SetLightFlickerDelegate = (ScriptingInterfaceOfILight.SetLightFlickerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetLightFlickerDelegate));
				return;
			case 536:
				ScriptingInterfaceOfILight.call_SetRadiusDelegate = (ScriptingInterfaceOfILight.SetRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetRadiusDelegate));
				return;
			case 537:
				ScriptingInterfaceOfILight.call_SetShadowsDelegate = (ScriptingInterfaceOfILight.SetShadowsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetShadowsDelegate));
				return;
			case 538:
				ScriptingInterfaceOfILight.call_SetVisibilityDelegate = (ScriptingInterfaceOfILight.SetVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetVisibilityDelegate));
				return;
			case 539:
				ScriptingInterfaceOfILight.call_SetVolumetricPropertiesDelegate = (ScriptingInterfaceOfILight.SetVolumetricPropertiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILight.SetVolumetricPropertiesDelegate));
				return;
			case 540:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceDelegate));
				return;
			case 541:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceCorner1Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner1Delegate));
				return;
			case 542:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceCorner2Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner2Delegate));
				return;
			case 543:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddLineDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddLineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddLineDelegate));
				return;
			case 544:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshDelegate));
				return;
			case 545:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshAuxDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshAuxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshAuxDelegate));
				return;
			case 546:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshToBoneDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshToBoneDelegate));
				return;
			case 547:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithColorDelegate));
				return;
			case 548:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithFixedNormalsDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsDelegate));
				return;
			case 549:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithFixedNormalsWithHeightGradientColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsWithHeightGradientColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsWithHeightGradientColorDelegate));
				return;
			case 550:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithSkinDataDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithSkinDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithSkinDataDelegate));
				return;
			case 551:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddRectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddRectDelegate));
				return;
			case 552:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectangle3Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangle3Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangle3Delegate));
				return;
			case 553:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectangleWithInverseUVDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangleWithInverseUVDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangleWithInverseUVDelegate));
				return;
			case 554:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectWithZUpDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddRectWithZUpDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddRectWithZUpDelegate));
				return;
			case 555:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddSkinnedMeshWithColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddSkinnedMeshWithColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddSkinnedMeshWithColorDelegate));
				return;
			case 556:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddTriangle1Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle1Delegate));
				return;
			case 557:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddTriangle2Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle2Delegate));
				return;
			case 558:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_AddVertexDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.AddVertexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.AddVertexDelegate));
				return;
			case 559:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ApplyCPUSkinningDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ApplyCPUSkinningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ApplyCPUSkinningDelegate));
				return;
			case 560:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ClearAllDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ClearAllDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ClearAllDelegate));
				return;
			case 561:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeCornerNormalsDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsDelegate));
				return;
			case 562:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeCornerNormalsWithSmoothingDataDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsWithSmoothingDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsWithSmoothingDataDelegate));
				return;
			case 563:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeTangentsDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ComputeTangentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ComputeTangentsDelegate));
				return;
			case 564:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_CreateDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.CreateDelegate));
				return;
			case 565:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_EnsureTransformedVerticesDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.EnsureTransformedVerticesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.EnsureTransformedVerticesDelegate));
				return;
			case 566:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_FinalizeEditingDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.FinalizeEditingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.FinalizeEditingDelegate));
				return;
			case 567:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_GenerateGridDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.GenerateGridDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.GenerateGridDelegate));
				return;
			case 568:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_GetPositionOfVertexDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.GetPositionOfVertexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.GetPositionOfVertexDelegate));
				return;
			case 569:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_GetVertexColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorDelegate));
				return;
			case 570:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_GetVertexColorAlphaDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorAlphaDelegate));
				return;
			case 571:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_InvertFacesWindingOrderDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.InvertFacesWindingOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.InvertFacesWindingOrderDelegate));
				return;
			case 572:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_MoveVerticesAlongNormalDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.MoveVerticesAlongNormalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.MoveVerticesAlongNormalDelegate));
				return;
			case 573:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RemoveDuplicatedCornersDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RemoveDuplicatedCornersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RemoveDuplicatedCornersDelegate));
				return;
			case 574:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RemoveFaceDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RemoveFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RemoveFaceDelegate));
				return;
			case 575:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dDelegate));
				return;
			case 576:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatXDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXDelegate));
				return;
			case 577:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatXWithTilingDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXWithTilingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXWithTilingDelegate));
				return;
			case 578:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatYDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYDelegate));
				return;
			case 579:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatYWithTilingDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYWithTilingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYWithTilingDelegate));
				return;
			case 580:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dWithoutChangingUVDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dWithoutChangingUVDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dWithoutChangingUVDelegate));
				return;
			case 581:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveFaceCornersDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFaceCornersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFaceCornersDelegate));
				return;
			case 582:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveFacesDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFacesDelegate));
				return;
			case 583:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveVerticesDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ReserveVerticesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ReserveVerticesDelegate));
				return;
			case 584:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ScaleVertices1Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices1Delegate));
				return;
			case 585:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_ScaleVertices2Delegate = (ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices2Delegate));
				return;
			case 586:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetCornerUVDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerUVDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerUVDelegate));
				return;
			case 587:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetCornerVertexColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerVertexColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerVertexColorDelegate));
				return;
			case 588:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetPositionOfVertexDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetPositionOfVertexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetPositionOfVertexDelegate));
				return;
			case 589:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetTangentsOfFaceCornerDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetTangentsOfFaceCornerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetTangentsOfFaceCornerDelegate));
				return;
			case 590:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetVertexColorDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorDelegate));
				return;
			case 591:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_SetVertexColorAlphaDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorAlphaDelegate));
				return;
			case 592:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_TransformVerticesToLocalDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToLocalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToLocalDelegate));
				return;
			case 593:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_TransformVerticesToParentDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToParentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToParentDelegate));
				return;
			case 594:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_TranslateVerticesDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.TranslateVerticesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.TranslateVerticesDelegate));
				return;
			case 595:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_UpdateOverlappedVertexNormalsDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.UpdateOverlappedVertexNormalsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.UpdateOverlappedVertexNormalsDelegate));
				return;
			case 596:
				ScriptingInterfaceOfIManagedMeshEditOperations.call_WeldDelegate = (ScriptingInterfaceOfIManagedMeshEditOperations.WeldDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManagedMeshEditOperations.WeldDelegate));
				return;
			case 597:
				ScriptingInterfaceOfIMaterial.call_AddMaterialShaderFlagDelegate = (ScriptingInterfaceOfIMaterial.AddMaterialShaderFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.AddMaterialShaderFlagDelegate));
				return;
			case 598:
				ScriptingInterfaceOfIMaterial.call_CreateCopyDelegate = (ScriptingInterfaceOfIMaterial.CreateCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.CreateCopyDelegate));
				return;
			case 599:
				ScriptingInterfaceOfIMaterial.call_GetAlphaBlendModeDelegate = (ScriptingInterfaceOfIMaterial.GetAlphaBlendModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetAlphaBlendModeDelegate));
				return;
			case 600:
				ScriptingInterfaceOfIMaterial.call_GetAlphaTestValueDelegate = (ScriptingInterfaceOfIMaterial.GetAlphaTestValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetAlphaTestValueDelegate));
				return;
			case 601:
				ScriptingInterfaceOfIMaterial.call_GetDefaultMaterialDelegate = (ScriptingInterfaceOfIMaterial.GetDefaultMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetDefaultMaterialDelegate));
				return;
			case 602:
				ScriptingInterfaceOfIMaterial.call_GetFlagsDelegate = (ScriptingInterfaceOfIMaterial.GetFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetFlagsDelegate));
				return;
			case 603:
				ScriptingInterfaceOfIMaterial.call_GetFromResourceDelegate = (ScriptingInterfaceOfIMaterial.GetFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetFromResourceDelegate));
				return;
			case 604:
				ScriptingInterfaceOfIMaterial.call_GetNameDelegate = (ScriptingInterfaceOfIMaterial.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetNameDelegate));
				return;
			case 605:
				ScriptingInterfaceOfIMaterial.call_GetOutlineMaterialDelegate = (ScriptingInterfaceOfIMaterial.GetOutlineMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetOutlineMaterialDelegate));
				return;
			case 606:
				ScriptingInterfaceOfIMaterial.call_GetShaderDelegate = (ScriptingInterfaceOfIMaterial.GetShaderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetShaderDelegate));
				return;
			case 607:
				ScriptingInterfaceOfIMaterial.call_GetShaderFlagsDelegate = (ScriptingInterfaceOfIMaterial.GetShaderFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetShaderFlagsDelegate));
				return;
			case 608:
				ScriptingInterfaceOfIMaterial.call_GetTextureDelegate = (ScriptingInterfaceOfIMaterial.GetTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.GetTextureDelegate));
				return;
			case 609:
				ScriptingInterfaceOfIMaterial.call_ReleaseDelegate = (ScriptingInterfaceOfIMaterial.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.ReleaseDelegate));
				return;
			case 610:
				ScriptingInterfaceOfIMaterial.call_RemoveMaterialShaderFlagDelegate = (ScriptingInterfaceOfIMaterial.RemoveMaterialShaderFlagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.RemoveMaterialShaderFlagDelegate));
				return;
			case 611:
				ScriptingInterfaceOfIMaterial.call_SetAlphaBlendModeDelegate = (ScriptingInterfaceOfIMaterial.SetAlphaBlendModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetAlphaBlendModeDelegate));
				return;
			case 612:
				ScriptingInterfaceOfIMaterial.call_SetAlphaTestValueDelegate = (ScriptingInterfaceOfIMaterial.SetAlphaTestValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetAlphaTestValueDelegate));
				return;
			case 613:
				ScriptingInterfaceOfIMaterial.call_SetAreaMapScaleDelegate = (ScriptingInterfaceOfIMaterial.SetAreaMapScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetAreaMapScaleDelegate));
				return;
			case 614:
				ScriptingInterfaceOfIMaterial.call_SetEnableSkinningDelegate = (ScriptingInterfaceOfIMaterial.SetEnableSkinningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetEnableSkinningDelegate));
				return;
			case 615:
				ScriptingInterfaceOfIMaterial.call_SetFlagsDelegate = (ScriptingInterfaceOfIMaterial.SetFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetFlagsDelegate));
				return;
			case 616:
				ScriptingInterfaceOfIMaterial.call_SetMeshVectorArgumentDelegate = (ScriptingInterfaceOfIMaterial.SetMeshVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetMeshVectorArgumentDelegate));
				return;
			case 617:
				ScriptingInterfaceOfIMaterial.call_SetNameDelegate = (ScriptingInterfaceOfIMaterial.SetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetNameDelegate));
				return;
			case 618:
				ScriptingInterfaceOfIMaterial.call_SetShaderDelegate = (ScriptingInterfaceOfIMaterial.SetShaderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetShaderDelegate));
				return;
			case 619:
				ScriptingInterfaceOfIMaterial.call_SetShaderFlagsDelegate = (ScriptingInterfaceOfIMaterial.SetShaderFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetShaderFlagsDelegate));
				return;
			case 620:
				ScriptingInterfaceOfIMaterial.call_SetTextureDelegate = (ScriptingInterfaceOfIMaterial.SetTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetTextureDelegate));
				return;
			case 621:
				ScriptingInterfaceOfIMaterial.call_SetTextureAtSlotDelegate = (ScriptingInterfaceOfIMaterial.SetTextureAtSlotDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.SetTextureAtSlotDelegate));
				return;
			case 622:
				ScriptingInterfaceOfIMaterial.call_UsingSkinningDelegate = (ScriptingInterfaceOfIMaterial.UsingSkinningDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMaterial.UsingSkinningDelegate));
				return;
			case 623:
				ScriptingInterfaceOfIMesh.call_AddEditDataUserDelegate = (ScriptingInterfaceOfIMesh.AddEditDataUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddEditDataUserDelegate));
				return;
			case 624:
				ScriptingInterfaceOfIMesh.call_AddFaceDelegate = (ScriptingInterfaceOfIMesh.AddFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddFaceDelegate));
				return;
			case 625:
				ScriptingInterfaceOfIMesh.call_AddFaceCornerDelegate = (ScriptingInterfaceOfIMesh.AddFaceCornerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddFaceCornerDelegate));
				return;
			case 626:
				ScriptingInterfaceOfIMesh.call_AddMeshToMeshDelegate = (ScriptingInterfaceOfIMesh.AddMeshToMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddMeshToMeshDelegate));
				return;
			case 627:
				ScriptingInterfaceOfIMesh.call_AddTriangleDelegate = (ScriptingInterfaceOfIMesh.AddTriangleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddTriangleDelegate));
				return;
			case 628:
				ScriptingInterfaceOfIMesh.call_AddTriangleWithVertexColorsDelegate = (ScriptingInterfaceOfIMesh.AddTriangleWithVertexColorsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.AddTriangleWithVertexColorsDelegate));
				return;
			case 629:
				ScriptingInterfaceOfIMesh.call_ClearMeshDelegate = (ScriptingInterfaceOfIMesh.ClearMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.ClearMeshDelegate));
				return;
			case 630:
				ScriptingInterfaceOfIMesh.call_ComputeNormalsDelegate = (ScriptingInterfaceOfIMesh.ComputeNormalsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.ComputeNormalsDelegate));
				return;
			case 631:
				ScriptingInterfaceOfIMesh.call_ComputeTangentsDelegate = (ScriptingInterfaceOfIMesh.ComputeTangentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.ComputeTangentsDelegate));
				return;
			case 632:
				ScriptingInterfaceOfIMesh.call_CreateMeshDelegate = (ScriptingInterfaceOfIMesh.CreateMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.CreateMeshDelegate));
				return;
			case 633:
				ScriptingInterfaceOfIMesh.call_CreateMeshCopyDelegate = (ScriptingInterfaceOfIMesh.CreateMeshCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.CreateMeshCopyDelegate));
				return;
			case 634:
				ScriptingInterfaceOfIMesh.call_CreateMeshWithMaterialDelegate = (ScriptingInterfaceOfIMesh.CreateMeshWithMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.CreateMeshWithMaterialDelegate));
				return;
			case 635:
				ScriptingInterfaceOfIMesh.call_DisableContourDelegate = (ScriptingInterfaceOfIMesh.DisableContourDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.DisableContourDelegate));
				return;
			case 636:
				ScriptingInterfaceOfIMesh.call_GetBaseMeshDelegate = (ScriptingInterfaceOfIMesh.GetBaseMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBaseMeshDelegate));
				return;
			case 637:
				ScriptingInterfaceOfIMesh.call_GetBillboardDelegate = (ScriptingInterfaceOfIMesh.GetBillboardDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBillboardDelegate));
				return;
			case 638:
				ScriptingInterfaceOfIMesh.call_GetBoundingBoxHeightDelegate = (ScriptingInterfaceOfIMesh.GetBoundingBoxHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBoundingBoxHeightDelegate));
				return;
			case 639:
				ScriptingInterfaceOfIMesh.call_GetBoundingBoxMaxDelegate = (ScriptingInterfaceOfIMesh.GetBoundingBoxMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBoundingBoxMaxDelegate));
				return;
			case 640:
				ScriptingInterfaceOfIMesh.call_GetBoundingBoxMinDelegate = (ScriptingInterfaceOfIMesh.GetBoundingBoxMinDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBoundingBoxMinDelegate));
				return;
			case 641:
				ScriptingInterfaceOfIMesh.call_GetBoundingBoxWidthDelegate = (ScriptingInterfaceOfIMesh.GetBoundingBoxWidthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetBoundingBoxWidthDelegate));
				return;
			case 642:
				ScriptingInterfaceOfIMesh.call_GetClothLinearVelocityMultiplierDelegate = (ScriptingInterfaceOfIMesh.GetClothLinearVelocityMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetClothLinearVelocityMultiplierDelegate));
				return;
			case 643:
				ScriptingInterfaceOfIMesh.call_GetColorDelegate = (ScriptingInterfaceOfIMesh.GetColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetColorDelegate));
				return;
			case 644:
				ScriptingInterfaceOfIMesh.call_GetColor2Delegate = (ScriptingInterfaceOfIMesh.GetColor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetColor2Delegate));
				return;
			case 645:
				ScriptingInterfaceOfIMesh.call_GetEditDataFaceCornerCountDelegate = (ScriptingInterfaceOfIMesh.GetEditDataFaceCornerCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetEditDataFaceCornerCountDelegate));
				return;
			case 646:
				ScriptingInterfaceOfIMesh.call_GetEditDataFaceCornerVertexColorDelegate = (ScriptingInterfaceOfIMesh.GetEditDataFaceCornerVertexColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetEditDataFaceCornerVertexColorDelegate));
				return;
			case 647:
				ScriptingInterfaceOfIMesh.call_GetFaceCornerCountDelegate = (ScriptingInterfaceOfIMesh.GetFaceCornerCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetFaceCornerCountDelegate));
				return;
			case 648:
				ScriptingInterfaceOfIMesh.call_GetFaceCountDelegate = (ScriptingInterfaceOfIMesh.GetFaceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetFaceCountDelegate));
				return;
			case 649:
				ScriptingInterfaceOfIMesh.call_GetLocalFrameDelegate = (ScriptingInterfaceOfIMesh.GetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetLocalFrameDelegate));
				return;
			case 650:
				ScriptingInterfaceOfIMesh.call_GetMaterialDelegate = (ScriptingInterfaceOfIMesh.GetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetMaterialDelegate));
				return;
			case 651:
				ScriptingInterfaceOfIMesh.call_GetMeshFromResourceDelegate = (ScriptingInterfaceOfIMesh.GetMeshFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetMeshFromResourceDelegate));
				return;
			case 652:
				ScriptingInterfaceOfIMesh.call_GetNameDelegate = (ScriptingInterfaceOfIMesh.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetNameDelegate));
				return;
			case 653:
				ScriptingInterfaceOfIMesh.call_GetRandomMeshWithVdeclDelegate = (ScriptingInterfaceOfIMesh.GetRandomMeshWithVdeclDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetRandomMeshWithVdeclDelegate));
				return;
			case 654:
				ScriptingInterfaceOfIMesh.call_GetSecondMaterialDelegate = (ScriptingInterfaceOfIMesh.GetSecondMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetSecondMaterialDelegate));
				return;
			case 655:
				ScriptingInterfaceOfIMesh.call_GetVectorArgumentDelegate = (ScriptingInterfaceOfIMesh.GetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetVectorArgumentDelegate));
				return;
			case 656:
				ScriptingInterfaceOfIMesh.call_GetVectorArgument2Delegate = (ScriptingInterfaceOfIMesh.GetVectorArgument2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetVectorArgument2Delegate));
				return;
			case 657:
				ScriptingInterfaceOfIMesh.call_GetVisibilityMaskDelegate = (ScriptingInterfaceOfIMesh.GetVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.GetVisibilityMaskDelegate));
				return;
			case 658:
				ScriptingInterfaceOfIMesh.call_HasClothDelegate = (ScriptingInterfaceOfIMesh.HasClothDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.HasClothDelegate));
				return;
			case 659:
				ScriptingInterfaceOfIMesh.call_HasTagDelegate = (ScriptingInterfaceOfIMesh.HasTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.HasTagDelegate));
				return;
			case 660:
				ScriptingInterfaceOfIMesh.call_HintIndicesDynamicDelegate = (ScriptingInterfaceOfIMesh.HintIndicesDynamicDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.HintIndicesDynamicDelegate));
				return;
			case 661:
				ScriptingInterfaceOfIMesh.call_HintVerticesDynamicDelegate = (ScriptingInterfaceOfIMesh.HintVerticesDynamicDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.HintVerticesDynamicDelegate));
				return;
			case 662:
				ScriptingInterfaceOfIMesh.call_LockEditDataWriteDelegate = (ScriptingInterfaceOfIMesh.LockEditDataWriteDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.LockEditDataWriteDelegate));
				return;
			case 663:
				ScriptingInterfaceOfIMesh.call_PreloadForRenderingDelegate = (ScriptingInterfaceOfIMesh.PreloadForRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.PreloadForRenderingDelegate));
				return;
			case 664:
				ScriptingInterfaceOfIMesh.call_RecomputeBoundingBoxDelegate = (ScriptingInterfaceOfIMesh.RecomputeBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.RecomputeBoundingBoxDelegate));
				return;
			case 665:
				ScriptingInterfaceOfIMesh.call_ReleaseEditDataUserDelegate = (ScriptingInterfaceOfIMesh.ReleaseEditDataUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.ReleaseEditDataUserDelegate));
				return;
			case 666:
				ScriptingInterfaceOfIMesh.call_ReleaseResourcesDelegate = (ScriptingInterfaceOfIMesh.ReleaseResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.ReleaseResourcesDelegate));
				return;
			case 667:
				ScriptingInterfaceOfIMesh.call_SetAdditionalBoneFrameDelegate = (ScriptingInterfaceOfIMesh.SetAdditionalBoneFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetAdditionalBoneFrameDelegate));
				return;
			case 668:
				ScriptingInterfaceOfIMesh.call_SetAsNotEffectedBySeasonDelegate = (ScriptingInterfaceOfIMesh.SetAsNotEffectedBySeasonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetAsNotEffectedBySeasonDelegate));
				return;
			case 669:
				ScriptingInterfaceOfIMesh.call_SetBillboardDelegate = (ScriptingInterfaceOfIMesh.SetBillboardDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetBillboardDelegate));
				return;
			case 670:
				ScriptingInterfaceOfIMesh.call_SetColorDelegate = (ScriptingInterfaceOfIMesh.SetColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetColorDelegate));
				return;
			case 671:
				ScriptingInterfaceOfIMesh.call_SetColor2Delegate = (ScriptingInterfaceOfIMesh.SetColor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetColor2Delegate));
				return;
			case 672:
				ScriptingInterfaceOfIMesh.call_SetColorAlphaDelegate = (ScriptingInterfaceOfIMesh.SetColorAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetColorAlphaDelegate));
				return;
			case 673:
				ScriptingInterfaceOfIMesh.call_SetColorAndStrokeDelegate = (ScriptingInterfaceOfIMesh.SetColorAndStrokeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetColorAndStrokeDelegate));
				return;
			case 674:
				ScriptingInterfaceOfIMesh.call_SetContourColorDelegate = (ScriptingInterfaceOfIMesh.SetContourColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetContourColorDelegate));
				return;
			case 675:
				ScriptingInterfaceOfIMesh.call_SetCullingModeDelegate = (ScriptingInterfaceOfIMesh.SetCullingModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetCullingModeDelegate));
				return;
			case 676:
				ScriptingInterfaceOfIMesh.call_SetCustomClipPlaneDelegate = (ScriptingInterfaceOfIMesh.SetCustomClipPlaneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetCustomClipPlaneDelegate));
				return;
			case 677:
				ScriptingInterfaceOfIMesh.call_SetEditDataFaceCornerVertexColorDelegate = (ScriptingInterfaceOfIMesh.SetEditDataFaceCornerVertexColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetEditDataFaceCornerVertexColorDelegate));
				return;
			case 678:
				ScriptingInterfaceOfIMesh.call_SetEditDataPolicyDelegate = (ScriptingInterfaceOfIMesh.SetEditDataPolicyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetEditDataPolicyDelegate));
				return;
			case 679:
				ScriptingInterfaceOfIMesh.call_SetExternalBoundingBoxDelegate = (ScriptingInterfaceOfIMesh.SetExternalBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetExternalBoundingBoxDelegate));
				return;
			case 680:
				ScriptingInterfaceOfIMesh.call_SetLocalFrameDelegate = (ScriptingInterfaceOfIMesh.SetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetLocalFrameDelegate));
				return;
			case 681:
				ScriptingInterfaceOfIMesh.call_SetMaterialDelegate = (ScriptingInterfaceOfIMesh.SetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetMaterialDelegate));
				return;
			case 682:
				ScriptingInterfaceOfIMesh.call_SetMaterialByNameDelegate = (ScriptingInterfaceOfIMesh.SetMaterialByNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetMaterialByNameDelegate));
				return;
			case 683:
				ScriptingInterfaceOfIMesh.call_SetMeshRenderOrderDelegate = (ScriptingInterfaceOfIMesh.SetMeshRenderOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetMeshRenderOrderDelegate));
				return;
			case 684:
				ScriptingInterfaceOfIMesh.call_SetMorphTimeDelegate = (ScriptingInterfaceOfIMesh.SetMorphTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetMorphTimeDelegate));
				return;
			case 685:
				ScriptingInterfaceOfIMesh.call_SetNameDelegate = (ScriptingInterfaceOfIMesh.SetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetNameDelegate));
				return;
			case 686:
				ScriptingInterfaceOfIMesh.call_SetupAdditionalBoneBufferDelegate = (ScriptingInterfaceOfIMesh.SetupAdditionalBoneBufferDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetupAdditionalBoneBufferDelegate));
				return;
			case 687:
				ScriptingInterfaceOfIMesh.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfIMesh.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetVectorArgumentDelegate));
				return;
			case 688:
				ScriptingInterfaceOfIMesh.call_SetVectorArgument2Delegate = (ScriptingInterfaceOfIMesh.SetVectorArgument2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetVectorArgument2Delegate));
				return;
			case 689:
				ScriptingInterfaceOfIMesh.call_SetVisibilityMaskDelegate = (ScriptingInterfaceOfIMesh.SetVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.SetVisibilityMaskDelegate));
				return;
			case 690:
				ScriptingInterfaceOfIMesh.call_UnlockEditDataWriteDelegate = (ScriptingInterfaceOfIMesh.UnlockEditDataWriteDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.UnlockEditDataWriteDelegate));
				return;
			case 691:
				ScriptingInterfaceOfIMesh.call_UpdateBoundingBoxDelegate = (ScriptingInterfaceOfIMesh.UpdateBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMesh.UpdateBoundingBoxDelegate));
				return;
			case 692:
				ScriptingInterfaceOfIMeshBuilder.call_CreateTilingButtonMeshDelegate = (ScriptingInterfaceOfIMeshBuilder.CreateTilingButtonMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMeshBuilder.CreateTilingButtonMeshDelegate));
				return;
			case 693:
				ScriptingInterfaceOfIMeshBuilder.call_CreateTilingWindowMeshDelegate = (ScriptingInterfaceOfIMeshBuilder.CreateTilingWindowMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMeshBuilder.CreateTilingWindowMeshDelegate));
				return;
			case 694:
				ScriptingInterfaceOfIMeshBuilder.call_FinalizeMeshBuilderDelegate = (ScriptingInterfaceOfIMeshBuilder.FinalizeMeshBuilderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMeshBuilder.FinalizeMeshBuilderDelegate));
				return;
			case 695:
				ScriptingInterfaceOfIMetaMesh.call_AddEditDataUserDelegate = (ScriptingInterfaceOfIMetaMesh.AddEditDataUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.AddEditDataUserDelegate));
				return;
			case 696:
				ScriptingInterfaceOfIMetaMesh.call_AddMeshDelegate = (ScriptingInterfaceOfIMetaMesh.AddMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.AddMeshDelegate));
				return;
			case 697:
				ScriptingInterfaceOfIMetaMesh.call_AddMetaMeshDelegate = (ScriptingInterfaceOfIMetaMesh.AddMetaMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.AddMetaMeshDelegate));
				return;
			case 698:
				ScriptingInterfaceOfIMetaMesh.call_AssignClothBodyFromDelegate = (ScriptingInterfaceOfIMetaMesh.AssignClothBodyFromDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.AssignClothBodyFromDelegate));
				return;
			case 699:
				ScriptingInterfaceOfIMetaMesh.call_BatchMultiMeshesDelegate = (ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesDelegate));
				return;
			case 700:
				ScriptingInterfaceOfIMetaMesh.call_BatchMultiMeshesMultipleDelegate = (ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesMultipleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesMultipleDelegate));
				return;
			case 701:
				ScriptingInterfaceOfIMetaMesh.call_CheckMetaMeshExistenceDelegate = (ScriptingInterfaceOfIMetaMesh.CheckMetaMeshExistenceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CheckMetaMeshExistenceDelegate));
				return;
			case 702:
				ScriptingInterfaceOfIMetaMesh.call_CheckResourcesDelegate = (ScriptingInterfaceOfIMetaMesh.CheckResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CheckResourcesDelegate));
				return;
			case 703:
				ScriptingInterfaceOfIMetaMesh.call_ClearEditDataDelegate = (ScriptingInterfaceOfIMetaMesh.ClearEditDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ClearEditDataDelegate));
				return;
			case 704:
				ScriptingInterfaceOfIMetaMesh.call_ClearMeshesDelegate = (ScriptingInterfaceOfIMetaMesh.ClearMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ClearMeshesDelegate));
				return;
			case 705:
				ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForLodDelegate = (ScriptingInterfaceOfIMetaMesh.ClearMeshesForLodDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ClearMeshesForLodDelegate));
				return;
			case 706:
				ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForLowerLodsDelegate = (ScriptingInterfaceOfIMetaMesh.ClearMeshesForLowerLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ClearMeshesForLowerLodsDelegate));
				return;
			case 707:
				ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForOtherLodsDelegate = (ScriptingInterfaceOfIMetaMesh.ClearMeshesForOtherLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ClearMeshesForOtherLodsDelegate));
				return;
			case 708:
				ScriptingInterfaceOfIMetaMesh.call_CopyToDelegate = (ScriptingInterfaceOfIMetaMesh.CopyToDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CopyToDelegate));
				return;
			case 709:
				ScriptingInterfaceOfIMetaMesh.call_CreateCopyDelegate = (ScriptingInterfaceOfIMetaMesh.CreateCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CreateCopyDelegate));
				return;
			case 710:
				ScriptingInterfaceOfIMetaMesh.call_CreateCopyFromNameDelegate = (ScriptingInterfaceOfIMetaMesh.CreateCopyFromNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CreateCopyFromNameDelegate));
				return;
			case 711:
				ScriptingInterfaceOfIMetaMesh.call_CreateMetaMeshDelegate = (ScriptingInterfaceOfIMetaMesh.CreateMetaMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.CreateMetaMeshDelegate));
				return;
			case 712:
				ScriptingInterfaceOfIMetaMesh.call_DrawTextWithDefaultFontDelegate = (ScriptingInterfaceOfIMetaMesh.DrawTextWithDefaultFontDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.DrawTextWithDefaultFontDelegate));
				return;
			case 713:
				ScriptingInterfaceOfIMetaMesh.call_GetAllMultiMeshesDelegate = (ScriptingInterfaceOfIMetaMesh.GetAllMultiMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetAllMultiMeshesDelegate));
				return;
			case 714:
				ScriptingInterfaceOfIMetaMesh.call_GetBoundingBoxDelegate = (ScriptingInterfaceOfIMetaMesh.GetBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetBoundingBoxDelegate));
				return;
			case 715:
				ScriptingInterfaceOfIMetaMesh.call_GetFactor1Delegate = (ScriptingInterfaceOfIMetaMesh.GetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetFactor1Delegate));
				return;
			case 716:
				ScriptingInterfaceOfIMetaMesh.call_GetFactor2Delegate = (ScriptingInterfaceOfIMetaMesh.GetFactor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetFactor2Delegate));
				return;
			case 717:
				ScriptingInterfaceOfIMetaMesh.call_GetFrameDelegate = (ScriptingInterfaceOfIMetaMesh.GetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetFrameDelegate));
				return;
			case 718:
				ScriptingInterfaceOfIMetaMesh.call_GetLodMaskForMeshAtIndexDelegate = (ScriptingInterfaceOfIMetaMesh.GetLodMaskForMeshAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetLodMaskForMeshAtIndexDelegate));
				return;
			case 719:
				ScriptingInterfaceOfIMetaMesh.call_GetMeshAtIndexDelegate = (ScriptingInterfaceOfIMetaMesh.GetMeshAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMeshAtIndexDelegate));
				return;
			case 720:
				ScriptingInterfaceOfIMetaMesh.call_GetMeshCountDelegate = (ScriptingInterfaceOfIMetaMesh.GetMeshCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMeshCountDelegate));
				return;
			case 721:
				ScriptingInterfaceOfIMetaMesh.call_GetMeshCountWithTagDelegate = (ScriptingInterfaceOfIMetaMesh.GetMeshCountWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMeshCountWithTagDelegate));
				return;
			case 722:
				ScriptingInterfaceOfIMetaMesh.call_GetMorphedCopyDelegate = (ScriptingInterfaceOfIMetaMesh.GetMorphedCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMorphedCopyDelegate));
				return;
			case 723:
				ScriptingInterfaceOfIMetaMesh.call_GetMultiMeshDelegate = (ScriptingInterfaceOfIMetaMesh.GetMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMultiMeshDelegate));
				return;
			case 724:
				ScriptingInterfaceOfIMetaMesh.call_GetMultiMeshCountDelegate = (ScriptingInterfaceOfIMetaMesh.GetMultiMeshCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetMultiMeshCountDelegate));
				return;
			case 725:
				ScriptingInterfaceOfIMetaMesh.call_GetNameDelegate = (ScriptingInterfaceOfIMetaMesh.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetNameDelegate));
				return;
			case 726:
				ScriptingInterfaceOfIMetaMesh.call_GetTotalGpuSizeDelegate = (ScriptingInterfaceOfIMetaMesh.GetTotalGpuSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetTotalGpuSizeDelegate));
				return;
			case 727:
				ScriptingInterfaceOfIMetaMesh.call_GetVectorArgument2Delegate = (ScriptingInterfaceOfIMetaMesh.GetVectorArgument2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetVectorArgument2Delegate));
				return;
			case 728:
				ScriptingInterfaceOfIMetaMesh.call_GetVectorUserDataDelegate = (ScriptingInterfaceOfIMetaMesh.GetVectorUserDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetVectorUserDataDelegate));
				return;
			case 729:
				ScriptingInterfaceOfIMetaMesh.call_GetVisibilityMaskDelegate = (ScriptingInterfaceOfIMetaMesh.GetVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.GetVisibilityMaskDelegate));
				return;
			case 730:
				ScriptingInterfaceOfIMetaMesh.call_HasAnyGeneratedLodsDelegate = (ScriptingInterfaceOfIMetaMesh.HasAnyGeneratedLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.HasAnyGeneratedLodsDelegate));
				return;
			case 731:
				ScriptingInterfaceOfIMetaMesh.call_HasAnyLodsDelegate = (ScriptingInterfaceOfIMetaMesh.HasAnyLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.HasAnyLodsDelegate));
				return;
			case 732:
				ScriptingInterfaceOfIMetaMesh.call_HasClothDataDelegate = (ScriptingInterfaceOfIMetaMesh.HasClothDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.HasClothDataDelegate));
				return;
			case 733:
				ScriptingInterfaceOfIMetaMesh.call_HasVertexBufferOrEditDataOrPackageItemDelegate = (ScriptingInterfaceOfIMetaMesh.HasVertexBufferOrEditDataOrPackageItemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.HasVertexBufferOrEditDataOrPackageItemDelegate));
				return;
			case 734:
				ScriptingInterfaceOfIMetaMesh.call_MergeMultiMeshesDelegate = (ScriptingInterfaceOfIMetaMesh.MergeMultiMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.MergeMultiMeshesDelegate));
				return;
			case 735:
				ScriptingInterfaceOfIMetaMesh.call_PreloadForRenderingDelegate = (ScriptingInterfaceOfIMetaMesh.PreloadForRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.PreloadForRenderingDelegate));
				return;
			case 736:
				ScriptingInterfaceOfIMetaMesh.call_PreloadShadersDelegate = (ScriptingInterfaceOfIMetaMesh.PreloadShadersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.PreloadShadersDelegate));
				return;
			case 737:
				ScriptingInterfaceOfIMetaMesh.call_RecomputeBoundingBoxDelegate = (ScriptingInterfaceOfIMetaMesh.RecomputeBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.RecomputeBoundingBoxDelegate));
				return;
			case 738:
				ScriptingInterfaceOfIMetaMesh.call_ReleaseDelegate = (ScriptingInterfaceOfIMetaMesh.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ReleaseDelegate));
				return;
			case 739:
				ScriptingInterfaceOfIMetaMesh.call_ReleaseEditDataUserDelegate = (ScriptingInterfaceOfIMetaMesh.ReleaseEditDataUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.ReleaseEditDataUserDelegate));
				return;
			case 740:
				ScriptingInterfaceOfIMetaMesh.call_RemoveMeshesWithoutTagDelegate = (ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithoutTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithoutTagDelegate));
				return;
			case 741:
				ScriptingInterfaceOfIMetaMesh.call_RemoveMeshesWithTagDelegate = (ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithTagDelegate));
				return;
			case 742:
				ScriptingInterfaceOfIMetaMesh.call_SetBillboardingDelegate = (ScriptingInterfaceOfIMetaMesh.SetBillboardingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetBillboardingDelegate));
				return;
			case 743:
				ScriptingInterfaceOfIMetaMesh.call_SetContourColorDelegate = (ScriptingInterfaceOfIMetaMesh.SetContourColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetContourColorDelegate));
				return;
			case 744:
				ScriptingInterfaceOfIMetaMesh.call_SetContourStateDelegate = (ScriptingInterfaceOfIMetaMesh.SetContourStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetContourStateDelegate));
				return;
			case 745:
				ScriptingInterfaceOfIMetaMesh.call_SetCullModeDelegate = (ScriptingInterfaceOfIMetaMesh.SetCullModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetCullModeDelegate));
				return;
			case 746:
				ScriptingInterfaceOfIMetaMesh.call_SetEditDataPolicyDelegate = (ScriptingInterfaceOfIMetaMesh.SetEditDataPolicyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetEditDataPolicyDelegate));
				return;
			case 747:
				ScriptingInterfaceOfIMetaMesh.call_SetFactor1Delegate = (ScriptingInterfaceOfIMetaMesh.SetFactor1Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFactor1Delegate));
				return;
			case 748:
				ScriptingInterfaceOfIMetaMesh.call_SetFactor1LinearDelegate = (ScriptingInterfaceOfIMetaMesh.SetFactor1LinearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFactor1LinearDelegate));
				return;
			case 749:
				ScriptingInterfaceOfIMetaMesh.call_SetFactor2Delegate = (ScriptingInterfaceOfIMetaMesh.SetFactor2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFactor2Delegate));
				return;
			case 750:
				ScriptingInterfaceOfIMetaMesh.call_SetFactor2LinearDelegate = (ScriptingInterfaceOfIMetaMesh.SetFactor2LinearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFactor2LinearDelegate));
				return;
			case 751:
				ScriptingInterfaceOfIMetaMesh.call_SetFactorColorToSubMeshesWithTagDelegate = (ScriptingInterfaceOfIMetaMesh.SetFactorColorToSubMeshesWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFactorColorToSubMeshesWithTagDelegate));
				return;
			case 752:
				ScriptingInterfaceOfIMetaMesh.call_SetFrameDelegate = (ScriptingInterfaceOfIMetaMesh.SetFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetFrameDelegate));
				return;
			case 753:
				ScriptingInterfaceOfIMetaMesh.call_SetGlossMultiplierDelegate = (ScriptingInterfaceOfIMetaMesh.SetGlossMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetGlossMultiplierDelegate));
				return;
			case 754:
				ScriptingInterfaceOfIMetaMesh.call_SetLodBiasDelegate = (ScriptingInterfaceOfIMetaMesh.SetLodBiasDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetLodBiasDelegate));
				return;
			case 755:
				ScriptingInterfaceOfIMetaMesh.call_SetMaterialDelegate = (ScriptingInterfaceOfIMetaMesh.SetMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetMaterialDelegate));
				return;
			case 756:
				ScriptingInterfaceOfIMetaMesh.call_SetMaterialToSubMeshesWithTagDelegate = (ScriptingInterfaceOfIMetaMesh.SetMaterialToSubMeshesWithTagDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetMaterialToSubMeshesWithTagDelegate));
				return;
			case 757:
				ScriptingInterfaceOfIMetaMesh.call_SetNumLodsDelegate = (ScriptingInterfaceOfIMetaMesh.SetNumLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetNumLodsDelegate));
				return;
			case 758:
				ScriptingInterfaceOfIMetaMesh.call_SetShaderToMaterialDelegate = (ScriptingInterfaceOfIMetaMesh.SetShaderToMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetShaderToMaterialDelegate));
				return;
			case 759:
				ScriptingInterfaceOfIMetaMesh.call_SetVectorArgumentDelegate = (ScriptingInterfaceOfIMetaMesh.SetVectorArgumentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetVectorArgumentDelegate));
				return;
			case 760:
				ScriptingInterfaceOfIMetaMesh.call_SetVectorArgument2Delegate = (ScriptingInterfaceOfIMetaMesh.SetVectorArgument2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetVectorArgument2Delegate));
				return;
			case 761:
				ScriptingInterfaceOfIMetaMesh.call_SetVectorUserDataDelegate = (ScriptingInterfaceOfIMetaMesh.SetVectorUserDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetVectorUserDataDelegate));
				return;
			case 762:
				ScriptingInterfaceOfIMetaMesh.call_SetVisibilityMaskDelegate = (ScriptingInterfaceOfIMetaMesh.SetVisibilityMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.SetVisibilityMaskDelegate));
				return;
			case 763:
				ScriptingInterfaceOfIMetaMesh.call_UseHeadBoneFaceGenScalingDelegate = (ScriptingInterfaceOfIMetaMesh.UseHeadBoneFaceGenScalingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMetaMesh.UseHeadBoneFaceGenScalingDelegate));
				return;
			case 764:
				ScriptingInterfaceOfIMouseManager.call_ActivateMouseCursorDelegate = (ScriptingInterfaceOfIMouseManager.ActivateMouseCursorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.ActivateMouseCursorDelegate));
				return;
			case 765:
				ScriptingInterfaceOfIMouseManager.call_LockCursorAtCurrentPositionDelegate = (ScriptingInterfaceOfIMouseManager.LockCursorAtCurrentPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.LockCursorAtCurrentPositionDelegate));
				return;
			case 766:
				ScriptingInterfaceOfIMouseManager.call_LockCursorAtPositionDelegate = (ScriptingInterfaceOfIMouseManager.LockCursorAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.LockCursorAtPositionDelegate));
				return;
			case 767:
				ScriptingInterfaceOfIMouseManager.call_SetMouseCursorDelegate = (ScriptingInterfaceOfIMouseManager.SetMouseCursorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.SetMouseCursorDelegate));
				return;
			case 768:
				ScriptingInterfaceOfIMouseManager.call_ShowCursorDelegate = (ScriptingInterfaceOfIMouseManager.ShowCursorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.ShowCursorDelegate));
				return;
			case 769:
				ScriptingInterfaceOfIMouseManager.call_UnlockCursorDelegate = (ScriptingInterfaceOfIMouseManager.UnlockCursorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMouseManager.UnlockCursorDelegate));
				return;
			case 770:
				ScriptingInterfaceOfIMusic.call_GetFreeMusicChannelIndexDelegate = (ScriptingInterfaceOfIMusic.GetFreeMusicChannelIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.GetFreeMusicChannelIndexDelegate));
				return;
			case 771:
				ScriptingInterfaceOfIMusic.call_IsClipLoadedDelegate = (ScriptingInterfaceOfIMusic.IsClipLoadedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.IsClipLoadedDelegate));
				return;
			case 772:
				ScriptingInterfaceOfIMusic.call_IsMusicPlayingDelegate = (ScriptingInterfaceOfIMusic.IsMusicPlayingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.IsMusicPlayingDelegate));
				return;
			case 773:
				ScriptingInterfaceOfIMusic.call_LoadClipDelegate = (ScriptingInterfaceOfIMusic.LoadClipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.LoadClipDelegate));
				return;
			case 774:
				ScriptingInterfaceOfIMusic.call_PauseMusicDelegate = (ScriptingInterfaceOfIMusic.PauseMusicDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.PauseMusicDelegate));
				return;
			case 775:
				ScriptingInterfaceOfIMusic.call_PlayDelayedDelegate = (ScriptingInterfaceOfIMusic.PlayDelayedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.PlayDelayedDelegate));
				return;
			case 776:
				ScriptingInterfaceOfIMusic.call_PlayMusicDelegate = (ScriptingInterfaceOfIMusic.PlayMusicDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.PlayMusicDelegate));
				return;
			case 777:
				ScriptingInterfaceOfIMusic.call_SetVolumeDelegate = (ScriptingInterfaceOfIMusic.SetVolumeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.SetVolumeDelegate));
				return;
			case 778:
				ScriptingInterfaceOfIMusic.call_StopMusicDelegate = (ScriptingInterfaceOfIMusic.StopMusicDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.StopMusicDelegate));
				return;
			case 779:
				ScriptingInterfaceOfIMusic.call_UnloadClipDelegate = (ScriptingInterfaceOfIMusic.UnloadClipDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIMusic.UnloadClipDelegate));
				return;
			case 780:
				ScriptingInterfaceOfIParticleSystem.call_CreateParticleSystemAttachedToBoneDelegate = (ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToBoneDelegate));
				return;
			case 781:
				ScriptingInterfaceOfIParticleSystem.call_CreateParticleSystemAttachedToEntityDelegate = (ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.CreateParticleSystemAttachedToEntityDelegate));
				return;
			case 782:
				ScriptingInterfaceOfIParticleSystem.call_GetLocalFrameDelegate = (ScriptingInterfaceOfIParticleSystem.GetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.GetLocalFrameDelegate));
				return;
			case 783:
				ScriptingInterfaceOfIParticleSystem.call_GetRuntimeIdByNameDelegate = (ScriptingInterfaceOfIParticleSystem.GetRuntimeIdByNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.GetRuntimeIdByNameDelegate));
				return;
			case 784:
				ScriptingInterfaceOfIParticleSystem.call_HasAliveParticlesDelegate = (ScriptingInterfaceOfIParticleSystem.HasAliveParticlesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.HasAliveParticlesDelegate));
				return;
			case 785:
				ScriptingInterfaceOfIParticleSystem.call_RestartDelegate = (ScriptingInterfaceOfIParticleSystem.RestartDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.RestartDelegate));
				return;
			case 786:
				ScriptingInterfaceOfIParticleSystem.call_SetDontRemoveFromEntityDelegate = (ScriptingInterfaceOfIParticleSystem.SetDontRemoveFromEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetDontRemoveFromEntityDelegate));
				return;
			case 787:
				ScriptingInterfaceOfIParticleSystem.call_SetEnableDelegate = (ScriptingInterfaceOfIParticleSystem.SetEnableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetEnableDelegate));
				return;
			case 788:
				ScriptingInterfaceOfIParticleSystem.call_SetLocalFrameDelegate = (ScriptingInterfaceOfIParticleSystem.SetLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetLocalFrameDelegate));
				return;
			case 789:
				ScriptingInterfaceOfIParticleSystem.call_SetParticleEffectByNameDelegate = (ScriptingInterfaceOfIParticleSystem.SetParticleEffectByNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetParticleEffectByNameDelegate));
				return;
			case 790:
				ScriptingInterfaceOfIParticleSystem.call_SetPreviousGlobalFrameDelegate = (ScriptingInterfaceOfIParticleSystem.SetPreviousGlobalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetPreviousGlobalFrameDelegate));
				return;
			case 791:
				ScriptingInterfaceOfIParticleSystem.call_SetRuntimeEmissionRateMultiplierDelegate = (ScriptingInterfaceOfIParticleSystem.SetRuntimeEmissionRateMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIParticleSystem.SetRuntimeEmissionRateMultiplierDelegate));
				return;
			case 792:
				ScriptingInterfaceOfIPath.call_AddPathPointDelegate = (ScriptingInterfaceOfIPath.AddPathPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.AddPathPointDelegate));
				return;
			case 793:
				ScriptingInterfaceOfIPath.call_DeletePathPointDelegate = (ScriptingInterfaceOfIPath.DeletePathPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.DeletePathPointDelegate));
				return;
			case 794:
				ScriptingInterfaceOfIPath.call_GetArcLengthDelegate = (ScriptingInterfaceOfIPath.GetArcLengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetArcLengthDelegate));
				return;
			case 795:
				ScriptingInterfaceOfIPath.call_GetHermiteFrameAndColorForDistanceDelegate = (ScriptingInterfaceOfIPath.GetHermiteFrameAndColorForDistanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetHermiteFrameAndColorForDistanceDelegate));
				return;
			case 796:
				ScriptingInterfaceOfIPath.call_GetHermiteFrameForDistanceDelegate = (ScriptingInterfaceOfIPath.GetHermiteFrameForDistanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetHermiteFrameForDistanceDelegate));
				return;
			case 797:
				ScriptingInterfaceOfIPath.call_GetHermiteFrameForDtDelegate = (ScriptingInterfaceOfIPath.GetHermiteFrameForDtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetHermiteFrameForDtDelegate));
				return;
			case 798:
				ScriptingInterfaceOfIPath.call_GetNameDelegate = (ScriptingInterfaceOfIPath.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetNameDelegate));
				return;
			case 799:
				ScriptingInterfaceOfIPath.call_GetNearestHermiteFrameWithValidAlphaForDistanceDelegate = (ScriptingInterfaceOfIPath.GetNearestHermiteFrameWithValidAlphaForDistanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetNearestHermiteFrameWithValidAlphaForDistanceDelegate));
				return;
			case 800:
				ScriptingInterfaceOfIPath.call_GetNumberOfPointsDelegate = (ScriptingInterfaceOfIPath.GetNumberOfPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetNumberOfPointsDelegate));
				return;
			case 801:
				ScriptingInterfaceOfIPath.call_GetPointsDelegate = (ScriptingInterfaceOfIPath.GetPointsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetPointsDelegate));
				return;
			case 802:
				ScriptingInterfaceOfIPath.call_GetTotalLengthDelegate = (ScriptingInterfaceOfIPath.GetTotalLengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetTotalLengthDelegate));
				return;
			case 803:
				ScriptingInterfaceOfIPath.call_GetVersionDelegate = (ScriptingInterfaceOfIPath.GetVersionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.GetVersionDelegate));
				return;
			case 804:
				ScriptingInterfaceOfIPath.call_HasValidAlphaAtPathPointDelegate = (ScriptingInterfaceOfIPath.HasValidAlphaAtPathPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.HasValidAlphaAtPathPointDelegate));
				return;
			case 805:
				ScriptingInterfaceOfIPath.call_SetFrameOfPointDelegate = (ScriptingInterfaceOfIPath.SetFrameOfPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.SetFrameOfPointDelegate));
				return;
			case 806:
				ScriptingInterfaceOfIPath.call_SetTangentPositionOfPointDelegate = (ScriptingInterfaceOfIPath.SetTangentPositionOfPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPath.SetTangentPositionOfPointDelegate));
				return;
			case 807:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetAngularDampingAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetAngularDampingAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetAngularDampingAtIndexDelegate));
				return;
			case 808:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetDynamicFrictionAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetDynamicFrictionAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetDynamicFrictionAtIndexDelegate));
				return;
			case 809:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetFlagsAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetFlagsAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetFlagsAtIndexDelegate));
				return;
			case 810:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetIndexWithNameDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetIndexWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetIndexWithNameDelegate));
				return;
			case 811:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetLinearDampingAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetLinearDampingAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetLinearDampingAtIndexDelegate));
				return;
			case 812:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetMaterialCountDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetMaterialCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetMaterialCountDelegate));
				return;
			case 813:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetMaterialNameAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetMaterialNameAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetMaterialNameAtIndexDelegate));
				return;
			case 814:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetRestitutionAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetRestitutionAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetRestitutionAtIndexDelegate));
				return;
			case 815:
				ScriptingInterfaceOfIPhysicsMaterial.call_GetStaticFrictionAtIndexDelegate = (ScriptingInterfaceOfIPhysicsMaterial.GetStaticFrictionAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsMaterial.GetStaticFrictionAtIndexDelegate));
				return;
			case 816:
				ScriptingInterfaceOfIPhysicsShape.call_AddCapsuleDelegate = (ScriptingInterfaceOfIPhysicsShape.AddCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.AddCapsuleDelegate));
				return;
			case 817:
				ScriptingInterfaceOfIPhysicsShape.call_AddPreloadQueueWithNameDelegate = (ScriptingInterfaceOfIPhysicsShape.AddPreloadQueueWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.AddPreloadQueueWithNameDelegate));
				return;
			case 818:
				ScriptingInterfaceOfIPhysicsShape.call_AddSphereDelegate = (ScriptingInterfaceOfIPhysicsShape.AddSphereDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.AddSphereDelegate));
				return;
			case 819:
				ScriptingInterfaceOfIPhysicsShape.call_CapsuleCountDelegate = (ScriptingInterfaceOfIPhysicsShape.CapsuleCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.CapsuleCountDelegate));
				return;
			case 820:
				ScriptingInterfaceOfIPhysicsShape.call_clearDelegate = (ScriptingInterfaceOfIPhysicsShape.clearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.clearDelegate));
				return;
			case 821:
				ScriptingInterfaceOfIPhysicsShape.call_CreateBodyCopyDelegate = (ScriptingInterfaceOfIPhysicsShape.CreateBodyCopyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.CreateBodyCopyDelegate));
				return;
			case 822:
				ScriptingInterfaceOfIPhysicsShape.call_GetBoundingBoxDelegate = (ScriptingInterfaceOfIPhysicsShape.GetBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetBoundingBoxDelegate));
				return;
			case 823:
				ScriptingInterfaceOfIPhysicsShape.call_GetBoundingBoxCenterDelegate = (ScriptingInterfaceOfIPhysicsShape.GetBoundingBoxCenterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetBoundingBoxCenterDelegate));
				return;
			case 824:
				ScriptingInterfaceOfIPhysicsShape.call_GetCapsuleDelegate = (ScriptingInterfaceOfIPhysicsShape.GetCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetCapsuleDelegate));
				return;
			case 825:
				ScriptingInterfaceOfIPhysicsShape.call_GetCapsuleWithMaterialDelegate = (ScriptingInterfaceOfIPhysicsShape.GetCapsuleWithMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetCapsuleWithMaterialDelegate));
				return;
			case 826:
				ScriptingInterfaceOfIPhysicsShape.call_GetDominantMaterialForTriangleMeshDelegate = (ScriptingInterfaceOfIPhysicsShape.GetDominantMaterialForTriangleMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetDominantMaterialForTriangleMeshDelegate));
				return;
			case 827:
				ScriptingInterfaceOfIPhysicsShape.call_GetFromResourceDelegate = (ScriptingInterfaceOfIPhysicsShape.GetFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetFromResourceDelegate));
				return;
			case 828:
				ScriptingInterfaceOfIPhysicsShape.call_GetNameDelegate = (ScriptingInterfaceOfIPhysicsShape.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetNameDelegate));
				return;
			case 829:
				ScriptingInterfaceOfIPhysicsShape.call_GetSphereDelegate = (ScriptingInterfaceOfIPhysicsShape.GetSphereDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetSphereDelegate));
				return;
			case 830:
				ScriptingInterfaceOfIPhysicsShape.call_GetSphereWithMaterialDelegate = (ScriptingInterfaceOfIPhysicsShape.GetSphereWithMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetSphereWithMaterialDelegate));
				return;
			case 831:
				ScriptingInterfaceOfIPhysicsShape.call_GetTriangleDelegate = (ScriptingInterfaceOfIPhysicsShape.GetTriangleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.GetTriangleDelegate));
				return;
			case 832:
				ScriptingInterfaceOfIPhysicsShape.call_InitDescriptionDelegate = (ScriptingInterfaceOfIPhysicsShape.InitDescriptionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.InitDescriptionDelegate));
				return;
			case 833:
				ScriptingInterfaceOfIPhysicsShape.call_PrepareDelegate = (ScriptingInterfaceOfIPhysicsShape.PrepareDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.PrepareDelegate));
				return;
			case 834:
				ScriptingInterfaceOfIPhysicsShape.call_ProcessPreloadQueueDelegate = (ScriptingInterfaceOfIPhysicsShape.ProcessPreloadQueueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.ProcessPreloadQueueDelegate));
				return;
			case 835:
				ScriptingInterfaceOfIPhysicsShape.call_SetCapsuleDelegate = (ScriptingInterfaceOfIPhysicsShape.SetCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.SetCapsuleDelegate));
				return;
			case 836:
				ScriptingInterfaceOfIPhysicsShape.call_SphereCountDelegate = (ScriptingInterfaceOfIPhysicsShape.SphereCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.SphereCountDelegate));
				return;
			case 837:
				ScriptingInterfaceOfIPhysicsShape.call_TransformDelegate = (ScriptingInterfaceOfIPhysicsShape.TransformDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.TransformDelegate));
				return;
			case 838:
				ScriptingInterfaceOfIPhysicsShape.call_TriangleCountInTriangleMeshDelegate = (ScriptingInterfaceOfIPhysicsShape.TriangleCountInTriangleMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.TriangleCountInTriangleMeshDelegate));
				return;
			case 839:
				ScriptingInterfaceOfIPhysicsShape.call_TriangleMeshCountDelegate = (ScriptingInterfaceOfIPhysicsShape.TriangleMeshCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.TriangleMeshCountDelegate));
				return;
			case 840:
				ScriptingInterfaceOfIPhysicsShape.call_UnloadDynamicBodiesDelegate = (ScriptingInterfaceOfIPhysicsShape.UnloadDynamicBodiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIPhysicsShape.UnloadDynamicBodiesDelegate));
				return;
			case 841:
				ScriptingInterfaceOfIScene.call_AddAlwaysRenderedSkeletonDelegate = (ScriptingInterfaceOfIScene.AddAlwaysRenderedSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddAlwaysRenderedSkeletonDelegate));
				return;
			case 842:
				ScriptingInterfaceOfIScene.call_AddDecalInstanceDelegate = (ScriptingInterfaceOfIScene.AddDecalInstanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddDecalInstanceDelegate));
				return;
			case 843:
				ScriptingInterfaceOfIScene.call_AddDirectionalLightDelegate = (ScriptingInterfaceOfIScene.AddDirectionalLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddDirectionalLightDelegate));
				return;
			case 844:
				ScriptingInterfaceOfIScene.call_AddEntityWithMeshDelegate = (ScriptingInterfaceOfIScene.AddEntityWithMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddEntityWithMeshDelegate));
				return;
			case 845:
				ScriptingInterfaceOfIScene.call_AddEntityWithMultiMeshDelegate = (ScriptingInterfaceOfIScene.AddEntityWithMultiMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddEntityWithMultiMeshDelegate));
				return;
			case 846:
				ScriptingInterfaceOfIScene.call_AddItemEntityDelegate = (ScriptingInterfaceOfIScene.AddItemEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddItemEntityDelegate));
				return;
			case 847:
				ScriptingInterfaceOfIScene.call_AddPathDelegate = (ScriptingInterfaceOfIScene.AddPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddPathDelegate));
				return;
			case 848:
				ScriptingInterfaceOfIScene.call_AddPathPointDelegate = (ScriptingInterfaceOfIScene.AddPathPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddPathPointDelegate));
				return;
			case 849:
				ScriptingInterfaceOfIScene.call_AddPointLightDelegate = (ScriptingInterfaceOfIScene.AddPointLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddPointLightDelegate));
				return;
			case 850:
				ScriptingInterfaceOfIScene.call_AddWaterWakeWithCapsuleDelegate = (ScriptingInterfaceOfIScene.AddWaterWakeWithCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AddWaterWakeWithCapsuleDelegate));
				return;
			case 851:
				ScriptingInterfaceOfIScene.call_AttachEntityDelegate = (ScriptingInterfaceOfIScene.AttachEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.AttachEntityDelegate));
				return;
			case 852:
				ScriptingInterfaceOfIScene.call_BoxCastDelegate = (ScriptingInterfaceOfIScene.BoxCastDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.BoxCastDelegate));
				return;
			case 853:
				ScriptingInterfaceOfIScene.call_BoxCastOnlyForCameraDelegate = (ScriptingInterfaceOfIScene.BoxCastOnlyForCameraDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.BoxCastOnlyForCameraDelegate));
				return;
			case 854:
				ScriptingInterfaceOfIScene.call_CalculateEffectiveLightingDelegate = (ScriptingInterfaceOfIScene.CalculateEffectiveLightingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CalculateEffectiveLightingDelegate));
				return;
			case 855:
				ScriptingInterfaceOfIScene.call_CheckPathEntitiesFrameChangedDelegate = (ScriptingInterfaceOfIScene.CheckPathEntitiesFrameChangedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CheckPathEntitiesFrameChangedDelegate));
				return;
			case 856:
				ScriptingInterfaceOfIScene.call_CheckPointCanSeePointDelegate = (ScriptingInterfaceOfIScene.CheckPointCanSeePointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CheckPointCanSeePointDelegate));
				return;
			case 857:
				ScriptingInterfaceOfIScene.call_CheckResourcesDelegate = (ScriptingInterfaceOfIScene.CheckResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CheckResourcesDelegate));
				return;
			case 858:
				ScriptingInterfaceOfIScene.call_ClearAllDelegate = (ScriptingInterfaceOfIScene.ClearAllDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ClearAllDelegate));
				return;
			case 859:
				ScriptingInterfaceOfIScene.call_ClearCurrentFrameTickEntitiesDelegate = (ScriptingInterfaceOfIScene.ClearCurrentFrameTickEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ClearCurrentFrameTickEntitiesDelegate));
				return;
			case 860:
				ScriptingInterfaceOfIScene.call_ClearDecalsDelegate = (ScriptingInterfaceOfIScene.ClearDecalsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ClearDecalsDelegate));
				return;
			case 861:
				ScriptingInterfaceOfIScene.call_ClearNavMeshDelegate = (ScriptingInterfaceOfIScene.ClearNavMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ClearNavMeshDelegate));
				return;
			case 862:
				ScriptingInterfaceOfIScene.call_ContainsTerrainDelegate = (ScriptingInterfaceOfIScene.ContainsTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ContainsTerrainDelegate));
				return;
			case 863:
				ScriptingInterfaceOfIScene.call_CreateBurstParticleDelegate = (ScriptingInterfaceOfIScene.CreateBurstParticleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CreateBurstParticleDelegate));
				return;
			case 864:
				ScriptingInterfaceOfIScene.call_CreateDynamicRainTextureDelegate = (ScriptingInterfaceOfIScene.CreateDynamicRainTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CreateDynamicRainTextureDelegate));
				return;
			case 865:
				ScriptingInterfaceOfIScene.call_CreateNewSceneDelegate = (ScriptingInterfaceOfIScene.CreateNewSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CreateNewSceneDelegate));
				return;
			case 866:
				ScriptingInterfaceOfIScene.call_CreatePathMeshDelegate = (ScriptingInterfaceOfIScene.CreatePathMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CreatePathMeshDelegate));
				return;
			case 867:
				ScriptingInterfaceOfIScene.call_CreatePathMesh2Delegate = (ScriptingInterfaceOfIScene.CreatePathMesh2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.CreatePathMesh2Delegate));
				return;
			case 868:
				ScriptingInterfaceOfIScene.call_DeletePathWithNameDelegate = (ScriptingInterfaceOfIScene.DeletePathWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DeletePathWithNameDelegate));
				return;
			case 869:
				ScriptingInterfaceOfIScene.call_DeleteWaterWakeRendererDelegate = (ScriptingInterfaceOfIScene.DeleteWaterWakeRendererDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DeleteWaterWakeRendererDelegate));
				return;
			case 870:
				ScriptingInterfaceOfIScene.call_DeRegisterShipVisualDelegate = (ScriptingInterfaceOfIScene.DeRegisterShipVisualDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DeRegisterShipVisualDelegate));
				return;
			case 871:
				ScriptingInterfaceOfIScene.call_DisableStaticShadowsDelegate = (ScriptingInterfaceOfIScene.DisableStaticShadowsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DisableStaticShadowsDelegate));
				return;
			case 872:
				ScriptingInterfaceOfIScene.call_DoesPathExistBetweenFacesDelegate = (ScriptingInterfaceOfIScene.DoesPathExistBetweenFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DoesPathExistBetweenFacesDelegate));
				return;
			case 873:
				ScriptingInterfaceOfIScene.call_DoesPathExistBetweenPositionsDelegate = (ScriptingInterfaceOfIScene.DoesPathExistBetweenPositionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.DoesPathExistBetweenPositionsDelegate));
				return;
			case 874:
				ScriptingInterfaceOfIScene.call_EnableFixedTickDelegate = (ScriptingInterfaceOfIScene.EnableFixedTickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.EnableFixedTickDelegate));
				return;
			case 875:
				ScriptingInterfaceOfIScene.call_EnableInclusiveAsyncPhysxDelegate = (ScriptingInterfaceOfIScene.EnableInclusiveAsyncPhysxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.EnableInclusiveAsyncPhysxDelegate));
				return;
			case 876:
				ScriptingInterfaceOfIScene.call_EnsurePostfxSystemDelegate = (ScriptingInterfaceOfIScene.EnsurePostfxSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.EnsurePostfxSystemDelegate));
				return;
			case 877:
				ScriptingInterfaceOfIScene.call_EnsureWaterWakeRendererDelegate = (ScriptingInterfaceOfIScene.EnsureWaterWakeRendererDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.EnsureWaterWakeRendererDelegate));
				return;
			case 878:
				ScriptingInterfaceOfIScene.call_FillEntityWithHardBorderPhysicsBarrierDelegate = (ScriptingInterfaceOfIScene.FillEntityWithHardBorderPhysicsBarrierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FillEntityWithHardBorderPhysicsBarrierDelegate));
				return;
			case 879:
				ScriptingInterfaceOfIScene.call_FillTerrainHeightDataDelegate = (ScriptingInterfaceOfIScene.FillTerrainHeightDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FillTerrainHeightDataDelegate));
				return;
			case 880:
				ScriptingInterfaceOfIScene.call_FillTerrainPhysicsMaterialIndexDataDelegate = (ScriptingInterfaceOfIScene.FillTerrainPhysicsMaterialIndexDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FillTerrainPhysicsMaterialIndexDataDelegate));
				return;
			case 881:
				ScriptingInterfaceOfIScene.call_FindClosestExitPositionForPositionOnABoundaryFaceDelegate = (ScriptingInterfaceOfIScene.FindClosestExitPositionForPositionOnABoundaryFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FindClosestExitPositionForPositionOnABoundaryFaceDelegate));
				return;
			case 882:
				ScriptingInterfaceOfIScene.call_FinishSceneSoundsDelegate = (ScriptingInterfaceOfIScene.FinishSceneSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FinishSceneSoundsDelegate));
				return;
			case 883:
				ScriptingInterfaceOfIScene.call_FocusRayCastForFixedPhysicsDelegate = (ScriptingInterfaceOfIScene.FocusRayCastForFixedPhysicsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.FocusRayCastForFixedPhysicsDelegate));
				return;
			case 884:
				ScriptingInterfaceOfIScene.call_ForceLoadResourcesDelegate = (ScriptingInterfaceOfIScene.ForceLoadResourcesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ForceLoadResourcesDelegate));
				return;
			case 885:
				ScriptingInterfaceOfIScene.call_GenerateContactsWithCapsuleDelegate = (ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleDelegate));
				return;
			case 886:
				ScriptingInterfaceOfIScene.call_GenerateContactsWithCapsuleAgainstEntityDelegate = (ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleAgainstEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleAgainstEntityDelegate));
				return;
			case 887:
				ScriptingInterfaceOfIScene.call_GetAllColorGradeNamesDelegate = (ScriptingInterfaceOfIScene.GetAllColorGradeNamesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetAllColorGradeNamesDelegate));
				return;
			case 888:
				ScriptingInterfaceOfIScene.call_GetAllEntitiesWithScriptComponentDelegate = (ScriptingInterfaceOfIScene.GetAllEntitiesWithScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetAllEntitiesWithScriptComponentDelegate));
				return;
			case 889:
				ScriptingInterfaceOfIScene.call_GetAllFilterNamesDelegate = (ScriptingInterfaceOfIScene.GetAllFilterNamesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetAllFilterNamesDelegate));
				return;
			case 890:
				ScriptingInterfaceOfIScene.call_GetAllNavmeshFaceRecordsDelegate = (ScriptingInterfaceOfIScene.GetAllNavmeshFaceRecordsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetAllNavmeshFaceRecordsDelegate));
				return;
			case 891:
				ScriptingInterfaceOfIScene.call_GetBoundingBoxDelegate = (ScriptingInterfaceOfIScene.GetBoundingBoxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetBoundingBoxDelegate));
				return;
			case 892:
				ScriptingInterfaceOfIScene.call_GetBulkWaterLevelAtPositionsDelegate = (ScriptingInterfaceOfIScene.GetBulkWaterLevelAtPositionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetBulkWaterLevelAtPositionsDelegate));
				return;
			case 893:
				ScriptingInterfaceOfIScene.call_GetBulkWaterLevelAtVolumesDelegate = (ScriptingInterfaceOfIScene.GetBulkWaterLevelAtVolumesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetBulkWaterLevelAtVolumesDelegate));
				return;
			case 894:
				ScriptingInterfaceOfIScene.call_GetCampaignEntityWithNameDelegate = (ScriptingInterfaceOfIScene.GetCampaignEntityWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetCampaignEntityWithNameDelegate));
				return;
			case 895:
				ScriptingInterfaceOfIScene.call_GetEnginePhysicsEnabledDelegate = (ScriptingInterfaceOfIScene.GetEnginePhysicsEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetEnginePhysicsEnabledDelegate));
				return;
			case 896:
				ScriptingInterfaceOfIScene.call_GetEntitiesDelegate = (ScriptingInterfaceOfIScene.GetEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetEntitiesDelegate));
				return;
			case 897:
				ScriptingInterfaceOfIScene.call_GetEntityCountDelegate = (ScriptingInterfaceOfIScene.GetEntityCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetEntityCountDelegate));
				return;
			case 898:
				ScriptingInterfaceOfIScene.call_GetEntityWithGuidDelegate = (ScriptingInterfaceOfIScene.GetEntityWithGuidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetEntityWithGuidDelegate));
				return;
			case 899:
				ScriptingInterfaceOfIScene.call_GetFallDensityDelegate = (ScriptingInterfaceOfIScene.GetFallDensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFallDensityDelegate));
				return;
			case 900:
				ScriptingInterfaceOfIScene.call_GetFirstEntityWithNameDelegate = (ScriptingInterfaceOfIScene.GetFirstEntityWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFirstEntityWithNameDelegate));
				return;
			case 901:
				ScriptingInterfaceOfIScene.call_GetFirstEntityWithScriptComponentDelegate = (ScriptingInterfaceOfIScene.GetFirstEntityWithScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFirstEntityWithScriptComponentDelegate));
				return;
			case 902:
				ScriptingInterfaceOfIScene.call_GetFloraInstanceCountDelegate = (ScriptingInterfaceOfIScene.GetFloraInstanceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFloraInstanceCountDelegate));
				return;
			case 903:
				ScriptingInterfaceOfIScene.call_GetFloraRendererTextureUsageDelegate = (ScriptingInterfaceOfIScene.GetFloraRendererTextureUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFloraRendererTextureUsageDelegate));
				return;
			case 904:
				ScriptingInterfaceOfIScene.call_GetFogDelegate = (ScriptingInterfaceOfIScene.GetFogDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetFogDelegate));
				return;
			case 905:
				ScriptingInterfaceOfIScene.call_GetGlobalWindStrengthVectorDelegate = (ScriptingInterfaceOfIScene.GetGlobalWindStrengthVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetGlobalWindStrengthVectorDelegate));
				return;
			case 906:
				ScriptingInterfaceOfIScene.call_GetGlobalWindVelocityDelegate = (ScriptingInterfaceOfIScene.GetGlobalWindVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetGlobalWindVelocityDelegate));
				return;
			case 907:
				ScriptingInterfaceOfIScene.call_GetGroundHeightAndBodyFlagsAtPositionDelegate = (ScriptingInterfaceOfIScene.GetGroundHeightAndBodyFlagsAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetGroundHeightAndBodyFlagsAtPositionDelegate));
				return;
			case 908:
				ScriptingInterfaceOfIScene.call_GetGroundHeightAndNormalAtPositionDelegate = (ScriptingInterfaceOfIScene.GetGroundHeightAndNormalAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetGroundHeightAndNormalAtPositionDelegate));
				return;
			case 909:
				ScriptingInterfaceOfIScene.call_GetGroundHeightAtPositionDelegate = (ScriptingInterfaceOfIScene.GetGroundHeightAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetGroundHeightAtPositionDelegate));
				return;
			case 910:
				ScriptingInterfaceOfIScene.call_GetHardBoundaryVertexDelegate = (ScriptingInterfaceOfIScene.GetHardBoundaryVertexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetHardBoundaryVertexDelegate));
				return;
			case 911:
				ScriptingInterfaceOfIScene.call_GetHardBoundaryVertexCountDelegate = (ScriptingInterfaceOfIScene.GetHardBoundaryVertexCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetHardBoundaryVertexCountDelegate));
				return;
			case 912:
				ScriptingInterfaceOfIScene.call_GetHeightAtPointDelegate = (ScriptingInterfaceOfIScene.GetHeightAtPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetHeightAtPointDelegate));
				return;
			case 913:
				ScriptingInterfaceOfIScene.call_GetIdOfNavMeshFaceDelegate = (ScriptingInterfaceOfIScene.GetIdOfNavMeshFaceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetIdOfNavMeshFaceDelegate));
				return;
			case 914:
				ScriptingInterfaceOfIScene.call_GetInterpolationFactorForBodyWorldTransformSmoothingDelegate = (ScriptingInterfaceOfIScene.GetInterpolationFactorForBodyWorldTransformSmoothingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetInterpolationFactorForBodyWorldTransformSmoothingDelegate));
				return;
			case 915:
				ScriptingInterfaceOfIScene.call_GetLastFinalRenderCameraFrameDelegate = (ScriptingInterfaceOfIScene.GetLastFinalRenderCameraFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLastFinalRenderCameraFrameDelegate));
				return;
			case 916:
				ScriptingInterfaceOfIScene.call_GetLastFinalRenderCameraPositionDelegate = (ScriptingInterfaceOfIScene.GetLastFinalRenderCameraPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLastFinalRenderCameraPositionDelegate));
				return;
			case 917:
				ScriptingInterfaceOfIScene.call_GetLastPointOnNavigationMeshFromPositionToDestinationDelegate = (ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromPositionToDestinationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromPositionToDestinationDelegate));
				return;
			case 918:
				ScriptingInterfaceOfIScene.call_GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate = (ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate));
				return;
			case 919:
				ScriptingInterfaceOfIScene.call_GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate = (ScriptingInterfaceOfIScene.GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate));
				return;
			case 920:
				ScriptingInterfaceOfIScene.call_GetLoadingStateNameDelegate = (ScriptingInterfaceOfIScene.GetLoadingStateNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetLoadingStateNameDelegate));
				return;
			case 921:
				ScriptingInterfaceOfIScene.call_GetModulePathDelegate = (ScriptingInterfaceOfIScene.GetModulePathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetModulePathDelegate));
				return;
			case 922:
				ScriptingInterfaceOfIScene.call_GetNameDelegate = (ScriptingInterfaceOfIScene.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNameDelegate));
				return;
			case 923:
				ScriptingInterfaceOfIScene.call_GetNavigationMeshCRCDelegate = (ScriptingInterfaceOfIScene.GetNavigationMeshCRCDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavigationMeshCRCDelegate));
				return;
			case 924:
				ScriptingInterfaceOfIScene.call_GetNavigationMeshForPositionDelegate = (ScriptingInterfaceOfIScene.GetNavigationMeshForPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavigationMeshForPositionDelegate));
				return;
			case 925:
				ScriptingInterfaceOfIScene.call_GetNavMeshFaceCenterPositionDelegate = (ScriptingInterfaceOfIScene.GetNavMeshFaceCenterPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshFaceCenterPositionDelegate));
				return;
			case 926:
				ScriptingInterfaceOfIScene.call_GetNavMeshFaceCountDelegate = (ScriptingInterfaceOfIScene.GetNavMeshFaceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshFaceCountDelegate));
				return;
			case 927:
				ScriptingInterfaceOfIScene.call_GetNavmeshFaceCountBetweenTwoIdsDelegate = (ScriptingInterfaceOfIScene.GetNavmeshFaceCountBetweenTwoIdsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavmeshFaceCountBetweenTwoIdsDelegate));
				return;
			case 928:
				ScriptingInterfaceOfIScene.call_GetNavMeshFaceFirstVertexZDelegate = (ScriptingInterfaceOfIScene.GetNavMeshFaceFirstVertexZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshFaceFirstVertexZDelegate));
				return;
			case 929:
				ScriptingInterfaceOfIScene.call_GetNavMeshFaceIndexDelegate = (ScriptingInterfaceOfIScene.GetNavMeshFaceIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshFaceIndexDelegate));
				return;
			case 930:
				ScriptingInterfaceOfIScene.call_GetNavMeshFaceIndex3Delegate = (ScriptingInterfaceOfIScene.GetNavMeshFaceIndex3Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshFaceIndex3Delegate));
				return;
			case 931:
				ScriptingInterfaceOfIScene.call_GetNavmeshFaceRecordsBetweenTwoIdsDelegate = (ScriptingInterfaceOfIScene.GetNavmeshFaceRecordsBetweenTwoIdsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavmeshFaceRecordsBetweenTwoIdsDelegate));
				return;
			case 932:
				ScriptingInterfaceOfIScene.call_GetNavMeshPathFaceRecordDelegate = (ScriptingInterfaceOfIScene.GetNavMeshPathFaceRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNavMeshPathFaceRecordDelegate));
				return;
			case 933:
				ScriptingInterfaceOfIScene.call_GetNearestNavigationMeshForPositionDelegate = (ScriptingInterfaceOfIScene.GetNearestNavigationMeshForPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNearestNavigationMeshForPositionDelegate));
				return;
			case 934:
				ScriptingInterfaceOfIScene.call_GetNodeDataCountDelegate = (ScriptingInterfaceOfIScene.GetNodeDataCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNodeDataCountDelegate));
				return;
			case 935:
				ScriptingInterfaceOfIScene.call_GetNormalAtDelegate = (ScriptingInterfaceOfIScene.GetNormalAtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNormalAtDelegate));
				return;
			case 936:
				ScriptingInterfaceOfIScene.call_GetNorthAngleDelegate = (ScriptingInterfaceOfIScene.GetNorthAngleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNorthAngleDelegate));
				return;
			case 937:
				ScriptingInterfaceOfIScene.call_GetNumberOfPathsWithNamePrefixDelegate = (ScriptingInterfaceOfIScene.GetNumberOfPathsWithNamePrefixDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetNumberOfPathsWithNamePrefixDelegate));
				return;
			case 938:
				ScriptingInterfaceOfIScene.call_GetPathBetweenAIFaceIndicesDelegate = (ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesDelegate));
				return;
			case 939:
				ScriptingInterfaceOfIScene.call_GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate = (ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate));
				return;
			case 940:
				ScriptingInterfaceOfIScene.call_GetPathBetweenAIFacePointersDelegate = (ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersDelegate));
				return;
			case 941:
				ScriptingInterfaceOfIScene.call_GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate = (ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate));
				return;
			case 942:
				ScriptingInterfaceOfIScene.call_GetPathDistanceBetweenAIFacesDelegate = (ScriptingInterfaceOfIScene.GetPathDistanceBetweenAIFacesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathDistanceBetweenAIFacesDelegate));
				return;
			case 943:
				ScriptingInterfaceOfIScene.call_GetPathDistanceBetweenPositionsDelegate = (ScriptingInterfaceOfIScene.GetPathDistanceBetweenPositionsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathDistanceBetweenPositionsDelegate));
				return;
			case 944:
				ScriptingInterfaceOfIScene.call_GetPathFaceRecordFromNavMeshFacePointerDelegate = (ScriptingInterfaceOfIScene.GetPathFaceRecordFromNavMeshFacePointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathFaceRecordFromNavMeshFacePointerDelegate));
				return;
			case 945:
				ScriptingInterfaceOfIScene.call_GetPathsWithNamePrefixDelegate = (ScriptingInterfaceOfIScene.GetPathsWithNamePrefixDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathsWithNamePrefixDelegate));
				return;
			case 946:
				ScriptingInterfaceOfIScene.call_GetPathWithNameDelegate = (ScriptingInterfaceOfIScene.GetPathWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPathWithNameDelegate));
				return;
			case 947:
				ScriptingInterfaceOfIScene.call_GetPhotoModeFocusDelegate = (ScriptingInterfaceOfIScene.GetPhotoModeFocusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhotoModeFocusDelegate));
				return;
			case 948:
				ScriptingInterfaceOfIScene.call_GetPhotoModeFovDelegate = (ScriptingInterfaceOfIScene.GetPhotoModeFovDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhotoModeFovDelegate));
				return;
			case 949:
				ScriptingInterfaceOfIScene.call_GetPhotoModeOnDelegate = (ScriptingInterfaceOfIScene.GetPhotoModeOnDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhotoModeOnDelegate));
				return;
			case 950:
				ScriptingInterfaceOfIScene.call_GetPhotoModeOrbitDelegate = (ScriptingInterfaceOfIScene.GetPhotoModeOrbitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhotoModeOrbitDelegate));
				return;
			case 951:
				ScriptingInterfaceOfIScene.call_GetPhotoModeRollDelegate = (ScriptingInterfaceOfIScene.GetPhotoModeRollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhotoModeRollDelegate));
				return;
			case 952:
				ScriptingInterfaceOfIScene.call_GetPhysicsMinMaxDelegate = (ScriptingInterfaceOfIScene.GetPhysicsMinMaxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetPhysicsMinMaxDelegate));
				return;
			case 953:
				ScriptingInterfaceOfIScene.call_GetRainDensityDelegate = (ScriptingInterfaceOfIScene.GetRainDensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetRainDensityDelegate));
				return;
			case 954:
				ScriptingInterfaceOfIScene.call_GetRootEntitiesDelegate = (ScriptingInterfaceOfIScene.GetRootEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetRootEntitiesDelegate));
				return;
			case 955:
				ScriptingInterfaceOfIScene.call_GetRootEntityCountDelegate = (ScriptingInterfaceOfIScene.GetRootEntityCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetRootEntityCountDelegate));
				return;
			case 956:
				ScriptingInterfaceOfIScene.call_GetSceneColorGradeIndexDelegate = (ScriptingInterfaceOfIScene.GetSceneColorGradeIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSceneColorGradeIndexDelegate));
				return;
			case 957:
				ScriptingInterfaceOfIScene.call_GetSceneFilterIndexDelegate = (ScriptingInterfaceOfIScene.GetSceneFilterIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSceneFilterIndexDelegate));
				return;
			case 958:
				ScriptingInterfaceOfIScene.call_GetSceneLimitsDelegate = (ScriptingInterfaceOfIScene.GetSceneLimitsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSceneLimitsDelegate));
				return;
			case 959:
				ScriptingInterfaceOfIScene.call_GetSceneXMLCRCDelegate = (ScriptingInterfaceOfIScene.GetSceneXMLCRCDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSceneXMLCRCDelegate));
				return;
			case 960:
				ScriptingInterfaceOfIScene.call_GetScriptedEntityDelegate = (ScriptingInterfaceOfIScene.GetScriptedEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetScriptedEntityDelegate));
				return;
			case 961:
				ScriptingInterfaceOfIScene.call_GetScriptedEntityCountDelegate = (ScriptingInterfaceOfIScene.GetScriptedEntityCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetScriptedEntityCountDelegate));
				return;
			case 962:
				ScriptingInterfaceOfIScene.call_GetSkyboxMeshDelegate = (ScriptingInterfaceOfIScene.GetSkyboxMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSkyboxMeshDelegate));
				return;
			case 963:
				ScriptingInterfaceOfIScene.call_GetSnowDensityDelegate = (ScriptingInterfaceOfIScene.GetSnowDensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSnowDensityDelegate));
				return;
			case 964:
				ScriptingInterfaceOfIScene.call_GetSoftBoundaryVertexDelegate = (ScriptingInterfaceOfIScene.GetSoftBoundaryVertexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSoftBoundaryVertexDelegate));
				return;
			case 965:
				ScriptingInterfaceOfIScene.call_GetSoftBoundaryVertexCountDelegate = (ScriptingInterfaceOfIScene.GetSoftBoundaryVertexCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSoftBoundaryVertexCountDelegate));
				return;
			case 966:
				ScriptingInterfaceOfIScene.call_GetSunDirectionDelegate = (ScriptingInterfaceOfIScene.GetSunDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetSunDirectionDelegate));
				return;
			case 967:
				ScriptingInterfaceOfIScene.call_GetTerrainDataDelegate = (ScriptingInterfaceOfIScene.GetTerrainDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainDataDelegate));
				return;
			case 968:
				ScriptingInterfaceOfIScene.call_GetTerrainHeightDelegate = (ScriptingInterfaceOfIScene.GetTerrainHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainHeightDelegate));
				return;
			case 969:
				ScriptingInterfaceOfIScene.call_GetTerrainHeightAndNormalDelegate = (ScriptingInterfaceOfIScene.GetTerrainHeightAndNormalDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainHeightAndNormalDelegate));
				return;
			case 970:
				ScriptingInterfaceOfIScene.call_GetTerrainMemoryUsageDelegate = (ScriptingInterfaceOfIScene.GetTerrainMemoryUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainMemoryUsageDelegate));
				return;
			case 971:
				ScriptingInterfaceOfIScene.call_GetTerrainMinMaxHeightDelegate = (ScriptingInterfaceOfIScene.GetTerrainMinMaxHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainMinMaxHeightDelegate));
				return;
			case 972:
				ScriptingInterfaceOfIScene.call_GetTerrainNodeDataDelegate = (ScriptingInterfaceOfIScene.GetTerrainNodeDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainNodeDataDelegate));
				return;
			case 973:
				ScriptingInterfaceOfIScene.call_GetTerrainPhysicsMaterialIndexAtLayerDelegate = (ScriptingInterfaceOfIScene.GetTerrainPhysicsMaterialIndexAtLayerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTerrainPhysicsMaterialIndexAtLayerDelegate));
				return;
			case 974:
				ScriptingInterfaceOfIScene.call_GetTimeOfDayDelegate = (ScriptingInterfaceOfIScene.GetTimeOfDayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTimeOfDayDelegate));
				return;
			case 975:
				ScriptingInterfaceOfIScene.call_GetTimeSpeedDelegate = (ScriptingInterfaceOfIScene.GetTimeSpeedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetTimeSpeedDelegate));
				return;
			case 976:
				ScriptingInterfaceOfIScene.call_GetUpgradeLevelCountDelegate = (ScriptingInterfaceOfIScene.GetUpgradeLevelCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetUpgradeLevelCountDelegate));
				return;
			case 977:
				ScriptingInterfaceOfIScene.call_GetUpgradeLevelMaskDelegate = (ScriptingInterfaceOfIScene.GetUpgradeLevelMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetUpgradeLevelMaskDelegate));
				return;
			case 978:
				ScriptingInterfaceOfIScene.call_GetUpgradeLevelMaskOfLevelNameDelegate = (ScriptingInterfaceOfIScene.GetUpgradeLevelMaskOfLevelNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetUpgradeLevelMaskOfLevelNameDelegate));
				return;
			case 979:
				ScriptingInterfaceOfIScene.call_GetUpgradeLevelNameOfIndexDelegate = (ScriptingInterfaceOfIScene.GetUpgradeLevelNameOfIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetUpgradeLevelNameOfIndexDelegate));
				return;
			case 980:
				ScriptingInterfaceOfIScene.call_GetWaterLevelDelegate = (ScriptingInterfaceOfIScene.GetWaterLevelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWaterLevelDelegate));
				return;
			case 981:
				ScriptingInterfaceOfIScene.call_GetWaterLevelAtPositionDelegate = (ScriptingInterfaceOfIScene.GetWaterLevelAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWaterLevelAtPositionDelegate));
				return;
			case 982:
				ScriptingInterfaceOfIScene.call_GetWaterSpeedAtPositionDelegate = (ScriptingInterfaceOfIScene.GetWaterSpeedAtPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWaterSpeedAtPositionDelegate));
				return;
			case 983:
				ScriptingInterfaceOfIScene.call_GetWaterStrengthDelegate = (ScriptingInterfaceOfIScene.GetWaterStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWaterStrengthDelegate));
				return;
			case 984:
				ScriptingInterfaceOfIScene.call_GetWindFlowMapDataDelegate = (ScriptingInterfaceOfIScene.GetWindFlowMapDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWindFlowMapDataDelegate));
				return;
			case 985:
				ScriptingInterfaceOfIScene.call_GetWinterTimeFactorDelegate = (ScriptingInterfaceOfIScene.GetWinterTimeFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.GetWinterTimeFactorDelegate));
				return;
			case 986:
				ScriptingInterfaceOfIScene.call_HasDecalRendererDelegate = (ScriptingInterfaceOfIScene.HasDecalRendererDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.HasDecalRendererDelegate));
				return;
			case 987:
				ScriptingInterfaceOfIScene.call_HasNavmeshFaceUnsharedEdgesDelegate = (ScriptingInterfaceOfIScene.HasNavmeshFaceUnsharedEdgesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.HasNavmeshFaceUnsharedEdgesDelegate));
				return;
			case 988:
				ScriptingInterfaceOfIScene.call_HasTerrainHeightmapDelegate = (ScriptingInterfaceOfIScene.HasTerrainHeightmapDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.HasTerrainHeightmapDelegate));
				return;
			case 989:
				ScriptingInterfaceOfIScene.call_InvalidateTerrainPhysicsMaterialsDelegate = (ScriptingInterfaceOfIScene.InvalidateTerrainPhysicsMaterialsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.InvalidateTerrainPhysicsMaterialsDelegate));
				return;
			case 990:
				ScriptingInterfaceOfIScene.call_IsAnyFaceWithIdDelegate = (ScriptingInterfaceOfIScene.IsAnyFaceWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsAnyFaceWithIdDelegate));
				return;
			case 991:
				ScriptingInterfaceOfIScene.call_IsAtmosphereIndoorDelegate = (ScriptingInterfaceOfIScene.IsAtmosphereIndoorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsAtmosphereIndoorDelegate));
				return;
			case 992:
				ScriptingInterfaceOfIScene.call_IsDefaultEditorSceneDelegate = (ScriptingInterfaceOfIScene.IsDefaultEditorSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsDefaultEditorSceneDelegate));
				return;
			case 993:
				ScriptingInterfaceOfIScene.call_IsEditorSceneDelegate = (ScriptingInterfaceOfIScene.IsEditorSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsEditorSceneDelegate));
				return;
			case 994:
				ScriptingInterfaceOfIScene.call_IsLineToPointClearDelegate = (ScriptingInterfaceOfIScene.IsLineToPointClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsLineToPointClearDelegate));
				return;
			case 995:
				ScriptingInterfaceOfIScene.call_IsLineToPointClear2Delegate = (ScriptingInterfaceOfIScene.IsLineToPointClear2Delegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsLineToPointClear2Delegate));
				return;
			case 996:
				ScriptingInterfaceOfIScene.call_IsLoadingFinishedDelegate = (ScriptingInterfaceOfIScene.IsLoadingFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsLoadingFinishedDelegate));
				return;
			case 997:
				ScriptingInterfaceOfIScene.call_IsMultiplayerSceneDelegate = (ScriptingInterfaceOfIScene.IsMultiplayerSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsMultiplayerSceneDelegate));
				return;
			case 998:
				ScriptingInterfaceOfIScene.call_IsPositionOnADynamicNavMeshDelegate = (ScriptingInterfaceOfIScene.IsPositionOnADynamicNavMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.IsPositionOnADynamicNavMeshDelegate));
				return;
			case 999:
				ScriptingInterfaceOfIScene.call_LoadNavMeshPrefabDelegate = (ScriptingInterfaceOfIScene.LoadNavMeshPrefabDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.LoadNavMeshPrefabDelegate));
				return;
			case 1000:
				ScriptingInterfaceOfIScene.call_LoadNavMeshPrefabWithFrameDelegate = (ScriptingInterfaceOfIScene.LoadNavMeshPrefabWithFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.LoadNavMeshPrefabWithFrameDelegate));
				return;
			case 1001:
				ScriptingInterfaceOfIScene.call_MarkFacesWithIdAsLadderDelegate = (ScriptingInterfaceOfIScene.MarkFacesWithIdAsLadderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.MarkFacesWithIdAsLadderDelegate));
				return;
			case 1002:
				ScriptingInterfaceOfIScene.call_MergeFacesWithIdDelegate = (ScriptingInterfaceOfIScene.MergeFacesWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.MergeFacesWithIdDelegate));
				return;
			case 1003:
				ScriptingInterfaceOfIScene.call_OptimizeSceneDelegate = (ScriptingInterfaceOfIScene.OptimizeSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.OptimizeSceneDelegate));
				return;
			case 1004:
				ScriptingInterfaceOfIScene.call_PauseSceneSoundsDelegate = (ScriptingInterfaceOfIScene.PauseSceneSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.PauseSceneSoundsDelegate));
				return;
			case 1005:
				ScriptingInterfaceOfIScene.call_PreloadForRenderingDelegate = (ScriptingInterfaceOfIScene.PreloadForRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.PreloadForRenderingDelegate));
				return;
			case 1006:
				ScriptingInterfaceOfIScene.call_RayCastExcludingTwoEntitiesDelegate = (ScriptingInterfaceOfIScene.RayCastExcludingTwoEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RayCastExcludingTwoEntitiesDelegate));
				return;
			case 1007:
				ScriptingInterfaceOfIScene.call_RayCastForClosestEntityOrTerrainDelegate = (ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainDelegate));
				return;
			case 1008:
				ScriptingInterfaceOfIScene.call_RayCastForClosestEntityOrTerrainIgnoreEntityDelegate = (ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainIgnoreEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainIgnoreEntityDelegate));
				return;
			case 1009:
				ScriptingInterfaceOfIScene.call_RayCastForRammingDelegate = (ScriptingInterfaceOfIScene.RayCastForRammingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RayCastForRammingDelegate));
				return;
			case 1010:
				ScriptingInterfaceOfIScene.call_ReadDelegate = (ScriptingInterfaceOfIScene.ReadDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ReadDelegate));
				return;
			case 1011:
				ScriptingInterfaceOfIScene.call_ReadAndCalculateInitialCameraDelegate = (ScriptingInterfaceOfIScene.ReadAndCalculateInitialCameraDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ReadAndCalculateInitialCameraDelegate));
				return;
			case 1012:
				ScriptingInterfaceOfIScene.call_ReadInModuleDelegate = (ScriptingInterfaceOfIScene.ReadInModuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ReadInModuleDelegate));
				return;
			case 1013:
				ScriptingInterfaceOfIScene.call_RegisterShipVisualToWaterRendererDelegate = (ScriptingInterfaceOfIScene.RegisterShipVisualToWaterRendererDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RegisterShipVisualToWaterRendererDelegate));
				return;
			case 1014:
				ScriptingInterfaceOfIScene.call_RemoveAlwaysRenderedSkeletonDelegate = (ScriptingInterfaceOfIScene.RemoveAlwaysRenderedSkeletonDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RemoveAlwaysRenderedSkeletonDelegate));
				return;
			case 1015:
				ScriptingInterfaceOfIScene.call_RemoveDecalInstanceDelegate = (ScriptingInterfaceOfIScene.RemoveDecalInstanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RemoveDecalInstanceDelegate));
				return;
			case 1016:
				ScriptingInterfaceOfIScene.call_RemoveEntityDelegate = (ScriptingInterfaceOfIScene.RemoveEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.RemoveEntityDelegate));
				return;
			case 1017:
				ScriptingInterfaceOfIScene.call_ResumeLoadingRenderingsDelegate = (ScriptingInterfaceOfIScene.ResumeLoadingRenderingsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ResumeLoadingRenderingsDelegate));
				return;
			case 1018:
				ScriptingInterfaceOfIScene.call_ResumeSceneSoundsDelegate = (ScriptingInterfaceOfIScene.ResumeSceneSoundsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.ResumeSceneSoundsDelegate));
				return;
			case 1019:
				ScriptingInterfaceOfIScene.call_SaveNavMeshPrefabWithFrameDelegate = (ScriptingInterfaceOfIScene.SaveNavMeshPrefabWithFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SaveNavMeshPrefabWithFrameDelegate));
				return;
			case 1020:
				ScriptingInterfaceOfIScene.call_SceneHadWaterWakeRendererDelegate = (ScriptingInterfaceOfIScene.SceneHadWaterWakeRendererDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SceneHadWaterWakeRendererDelegate));
				return;
			case 1021:
				ScriptingInterfaceOfIScene.call_SelectEntitiesCollidedWithDelegate = (ScriptingInterfaceOfIScene.SelectEntitiesCollidedWithDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SelectEntitiesCollidedWithDelegate));
				return;
			case 1022:
				ScriptingInterfaceOfIScene.call_SelectEntitiesInBoxWithScriptComponentDelegate = (ScriptingInterfaceOfIScene.SelectEntitiesInBoxWithScriptComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SelectEntitiesInBoxWithScriptComponentDelegate));
				return;
			case 1023:
				ScriptingInterfaceOfIScene.call_SeparateFacesWithIdDelegate = (ScriptingInterfaceOfIScene.SeparateFacesWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SeparateFacesWithIdDelegate));
				return;
			case 1024:
				ScriptingInterfaceOfIScene.call_SetAberrationOffsetDelegate = (ScriptingInterfaceOfIScene.SetAberrationOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAberrationOffsetDelegate));
				return;
			case 1025:
				ScriptingInterfaceOfIScene.call_SetAberrationSizeDelegate = (ScriptingInterfaceOfIScene.SetAberrationSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAberrationSizeDelegate));
				return;
			case 1026:
				ScriptingInterfaceOfIScene.call_SetAberrationSmoothDelegate = (ScriptingInterfaceOfIScene.SetAberrationSmoothDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAberrationSmoothDelegate));
				return;
			case 1027:
				ScriptingInterfaceOfIScene.call_SetAbilityOfFacesWithIdDelegate = (ScriptingInterfaceOfIScene.SetAbilityOfFacesWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAbilityOfFacesWithIdDelegate));
				return;
			case 1028:
				ScriptingInterfaceOfIScene.call_SetActiveVisibilityLevelsDelegate = (ScriptingInterfaceOfIScene.SetActiveVisibilityLevelsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetActiveVisibilityLevelsDelegate));
				return;
			case 1029:
				ScriptingInterfaceOfIScene.call_SetAntialiasingModeDelegate = (ScriptingInterfaceOfIScene.SetAntialiasingModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAntialiasingModeDelegate));
				return;
			case 1030:
				ScriptingInterfaceOfIScene.call_SetAtmosphereWithNameDelegate = (ScriptingInterfaceOfIScene.SetAtmosphereWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetAtmosphereWithNameDelegate));
				return;
			case 1031:
				ScriptingInterfaceOfIScene.call_SetBloomDelegate = (ScriptingInterfaceOfIScene.SetBloomDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetBloomDelegate));
				return;
			case 1032:
				ScriptingInterfaceOfIScene.call_SetBloomAmountDelegate = (ScriptingInterfaceOfIScene.SetBloomAmountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetBloomAmountDelegate));
				return;
			case 1033:
				ScriptingInterfaceOfIScene.call_SetBloomStrengthDelegate = (ScriptingInterfaceOfIScene.SetBloomStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetBloomStrengthDelegate));
				return;
			case 1034:
				ScriptingInterfaceOfIScene.call_SetBrightpassTresholdDelegate = (ScriptingInterfaceOfIScene.SetBrightpassTresholdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetBrightpassTresholdDelegate));
				return;
			case 1035:
				ScriptingInterfaceOfIScene.call_SetClothSimulationStateDelegate = (ScriptingInterfaceOfIScene.SetClothSimulationStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetClothSimulationStateDelegate));
				return;
			case 1036:
				ScriptingInterfaceOfIScene.call_SetColorGradeBlendDelegate = (ScriptingInterfaceOfIScene.SetColorGradeBlendDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetColorGradeBlendDelegate));
				return;
			case 1037:
				ScriptingInterfaceOfIScene.call_SetDLSSModeDelegate = (ScriptingInterfaceOfIScene.SetDLSSModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDLSSModeDelegate));
				return;
			case 1038:
				ScriptingInterfaceOfIScene.call_SetDofFocusDelegate = (ScriptingInterfaceOfIScene.SetDofFocusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDofFocusDelegate));
				return;
			case 1039:
				ScriptingInterfaceOfIScene.call_SetDofModeDelegate = (ScriptingInterfaceOfIScene.SetDofModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDofModeDelegate));
				return;
			case 1040:
				ScriptingInterfaceOfIScene.call_SetDofParamsDelegate = (ScriptingInterfaceOfIScene.SetDofParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDofParamsDelegate));
				return;
			case 1041:
				ScriptingInterfaceOfIScene.call_SetDoNotAddEntitiesToTickListDelegate = (ScriptingInterfaceOfIScene.SetDoNotAddEntitiesToTickListDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDoNotAddEntitiesToTickListDelegate));
				return;
			case 1042:
				ScriptingInterfaceOfIScene.call_SetDoNotWaitForLoadingStatesToRenderDelegate = (ScriptingInterfaceOfIScene.SetDoNotWaitForLoadingStatesToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDoNotWaitForLoadingStatesToRenderDelegate));
				return;
			case 1043:
				ScriptingInterfaceOfIScene.call_SetDontLoadInvisibleEntitiesDelegate = (ScriptingInterfaceOfIScene.SetDontLoadInvisibleEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDontLoadInvisibleEntitiesDelegate));
				return;
			case 1044:
				ScriptingInterfaceOfIScene.call_SetDrynessFactorDelegate = (ScriptingInterfaceOfIScene.SetDrynessFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDrynessFactorDelegate));
				return;
			case 1045:
				ScriptingInterfaceOfIScene.call_SetDynamicShadowmapCascadesRadiusMultiplierDelegate = (ScriptingInterfaceOfIScene.SetDynamicShadowmapCascadesRadiusMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDynamicShadowmapCascadesRadiusMultiplierDelegate));
				return;
			case 1046:
				ScriptingInterfaceOfIScene.call_SetDynamicSnowTextureDelegate = (ScriptingInterfaceOfIScene.SetDynamicSnowTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetDynamicSnowTextureDelegate));
				return;
			case 1047:
				ScriptingInterfaceOfIScene.call_SetEnvironmentMultiplierDelegate = (ScriptingInterfaceOfIScene.SetEnvironmentMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetEnvironmentMultiplierDelegate));
				return;
			case 1048:
				ScriptingInterfaceOfIScene.call_SetExternalInjectionTextureDelegate = (ScriptingInterfaceOfIScene.SetExternalInjectionTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetExternalInjectionTextureDelegate));
				return;
			case 1049:
				ScriptingInterfaceOfIScene.call_SetFetchCrcInfoOfSceneDelegate = (ScriptingInterfaceOfIScene.SetFetchCrcInfoOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetFetchCrcInfoOfSceneDelegate));
				return;
			case 1050:
				ScriptingInterfaceOfIScene.call_SetFixedTickCallbackActiveDelegate = (ScriptingInterfaceOfIScene.SetFixedTickCallbackActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetFixedTickCallbackActiveDelegate));
				return;
			case 1051:
				ScriptingInterfaceOfIScene.call_SetFogDelegate = (ScriptingInterfaceOfIScene.SetFogDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetFogDelegate));
				return;
			case 1052:
				ScriptingInterfaceOfIScene.call_SetFogAdvancedDelegate = (ScriptingInterfaceOfIScene.SetFogAdvancedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetFogAdvancedDelegate));
				return;
			case 1053:
				ScriptingInterfaceOfIScene.call_SetFogAmbientColorDelegate = (ScriptingInterfaceOfIScene.SetFogAmbientColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetFogAmbientColorDelegate));
				return;
			case 1054:
				ScriptingInterfaceOfIScene.call_SetForcedSnowDelegate = (ScriptingInterfaceOfIScene.SetForcedSnowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetForcedSnowDelegate));
				return;
			case 1055:
				ScriptingInterfaceOfIScene.call_SetGlobalWindStrengthVectorDelegate = (ScriptingInterfaceOfIScene.SetGlobalWindStrengthVectorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetGlobalWindStrengthVectorDelegate));
				return;
			case 1056:
				ScriptingInterfaceOfIScene.call_SetGlobalWindVelocityDelegate = (ScriptingInterfaceOfIScene.SetGlobalWindVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetGlobalWindVelocityDelegate));
				return;
			case 1057:
				ScriptingInterfaceOfIScene.call_SetGrainAmountDelegate = (ScriptingInterfaceOfIScene.SetGrainAmountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetGrainAmountDelegate));
				return;
			case 1058:
				ScriptingInterfaceOfIScene.call_SetHexagonVignetteAlphaDelegate = (ScriptingInterfaceOfIScene.SetHexagonVignetteAlphaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetHexagonVignetteAlphaDelegate));
				return;
			case 1059:
				ScriptingInterfaceOfIScene.call_SetHexagonVignetteColorDelegate = (ScriptingInterfaceOfIScene.SetHexagonVignetteColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetHexagonVignetteColorDelegate));
				return;
			case 1060:
				ScriptingInterfaceOfIScene.call_SetHumidityDelegate = (ScriptingInterfaceOfIScene.SetHumidityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetHumidityDelegate));
				return;
			case 1061:
				ScriptingInterfaceOfIScene.call_SetLandscapeRainMaskDataDelegate = (ScriptingInterfaceOfIScene.SetLandscapeRainMaskDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLandscapeRainMaskDataDelegate));
				return;
			case 1062:
				ScriptingInterfaceOfIScene.call_SetLensDistortionDelegate = (ScriptingInterfaceOfIScene.SetLensDistortionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensDistortionDelegate));
				return;
			case 1063:
				ScriptingInterfaceOfIScene.call_SetLensFlareAberrationOffsetDelegate = (ScriptingInterfaceOfIScene.SetLensFlareAberrationOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareAberrationOffsetDelegate));
				return;
			case 1064:
				ScriptingInterfaceOfIScene.call_SetLensFlareAmountDelegate = (ScriptingInterfaceOfIScene.SetLensFlareAmountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareAmountDelegate));
				return;
			case 1065:
				ScriptingInterfaceOfIScene.call_SetLensFlareBlurSigmaDelegate = (ScriptingInterfaceOfIScene.SetLensFlareBlurSigmaDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareBlurSigmaDelegate));
				return;
			case 1066:
				ScriptingInterfaceOfIScene.call_SetLensFlareBlurSizeDelegate = (ScriptingInterfaceOfIScene.SetLensFlareBlurSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareBlurSizeDelegate));
				return;
			case 1067:
				ScriptingInterfaceOfIScene.call_SetLensFlareDiffractionWeightDelegate = (ScriptingInterfaceOfIScene.SetLensFlareDiffractionWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareDiffractionWeightDelegate));
				return;
			case 1068:
				ScriptingInterfaceOfIScene.call_SetLensFlareDirtWeightDelegate = (ScriptingInterfaceOfIScene.SetLensFlareDirtWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareDirtWeightDelegate));
				return;
			case 1069:
				ScriptingInterfaceOfIScene.call_SetLensFlareGhostSamplesDelegate = (ScriptingInterfaceOfIScene.SetLensFlareGhostSamplesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareGhostSamplesDelegate));
				return;
			case 1070:
				ScriptingInterfaceOfIScene.call_SetLensFlareGhostWeightDelegate = (ScriptingInterfaceOfIScene.SetLensFlareGhostWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareGhostWeightDelegate));
				return;
			case 1071:
				ScriptingInterfaceOfIScene.call_SetLensFlareHaloWeightDelegate = (ScriptingInterfaceOfIScene.SetLensFlareHaloWeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareHaloWeightDelegate));
				return;
			case 1072:
				ScriptingInterfaceOfIScene.call_SetLensFlareHaloWidthDelegate = (ScriptingInterfaceOfIScene.SetLensFlareHaloWidthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareHaloWidthDelegate));
				return;
			case 1073:
				ScriptingInterfaceOfIScene.call_SetLensFlareStrengthDelegate = (ScriptingInterfaceOfIScene.SetLensFlareStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareStrengthDelegate));
				return;
			case 1074:
				ScriptingInterfaceOfIScene.call_SetLensFlareThresholdDelegate = (ScriptingInterfaceOfIScene.SetLensFlareThresholdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLensFlareThresholdDelegate));
				return;
			case 1075:
				ScriptingInterfaceOfIScene.call_SetLightDiffuseColorDelegate = (ScriptingInterfaceOfIScene.SetLightDiffuseColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLightDiffuseColorDelegate));
				return;
			case 1076:
				ScriptingInterfaceOfIScene.call_SetLightDirectionDelegate = (ScriptingInterfaceOfIScene.SetLightDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLightDirectionDelegate));
				return;
			case 1077:
				ScriptingInterfaceOfIScene.call_SetLightPositionDelegate = (ScriptingInterfaceOfIScene.SetLightPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetLightPositionDelegate));
				return;
			case 1078:
				ScriptingInterfaceOfIScene.call_SetMaxExposureDelegate = (ScriptingInterfaceOfIScene.SetMaxExposureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMaxExposureDelegate));
				return;
			case 1079:
				ScriptingInterfaceOfIScene.call_SetMiddleGrayDelegate = (ScriptingInterfaceOfIScene.SetMiddleGrayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMiddleGrayDelegate));
				return;
			case 1080:
				ScriptingInterfaceOfIScene.call_SetMieScatterFocusDelegate = (ScriptingInterfaceOfIScene.SetMieScatterFocusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMieScatterFocusDelegate));
				return;
			case 1081:
				ScriptingInterfaceOfIScene.call_SetMieScatterStrengthDelegate = (ScriptingInterfaceOfIScene.SetMieScatterStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMieScatterStrengthDelegate));
				return;
			case 1082:
				ScriptingInterfaceOfIScene.call_SetMinExposureDelegate = (ScriptingInterfaceOfIScene.SetMinExposureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMinExposureDelegate));
				return;
			case 1083:
				ScriptingInterfaceOfIScene.call_SetMotionBlurModeDelegate = (ScriptingInterfaceOfIScene.SetMotionBlurModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetMotionBlurModeDelegate));
				return;
			case 1084:
				ScriptingInterfaceOfIScene.call_SetNameDelegate = (ScriptingInterfaceOfIScene.SetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetNameDelegate));
				return;
			case 1085:
				ScriptingInterfaceOfIScene.call_SetNavMeshRegionMapDelegate = (ScriptingInterfaceOfIScene.SetNavMeshRegionMapDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetNavMeshRegionMapDelegate));
				return;
			case 1086:
				ScriptingInterfaceOfIScene.call_SetOcclusionModeDelegate = (ScriptingInterfaceOfIScene.SetOcclusionModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetOcclusionModeDelegate));
				return;
			case 1087:
				ScriptingInterfaceOfIScene.call_SetOnCollisionFilterCallbackActiveDelegate = (ScriptingInterfaceOfIScene.SetOnCollisionFilterCallbackActiveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetOnCollisionFilterCallbackActiveDelegate));
				return;
			case 1088:
				ScriptingInterfaceOfIScene.call_SetOwnerThreadDelegate = (ScriptingInterfaceOfIScene.SetOwnerThreadDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetOwnerThreadDelegate));
				return;
			case 1089:
				ScriptingInterfaceOfIScene.call_SetPhotoAtmosphereViaTodDelegate = (ScriptingInterfaceOfIScene.SetPhotoAtmosphereViaTodDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoAtmosphereViaTodDelegate));
				return;
			case 1090:
				ScriptingInterfaceOfIScene.call_SetPhotoModeFocusDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeFocusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeFocusDelegate));
				return;
			case 1091:
				ScriptingInterfaceOfIScene.call_SetPhotoModeFovDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeFovDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeFovDelegate));
				return;
			case 1092:
				ScriptingInterfaceOfIScene.call_SetPhotoModeOnDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeOnDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeOnDelegate));
				return;
			case 1093:
				ScriptingInterfaceOfIScene.call_SetPhotoModeOrbitDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeOrbitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeOrbitDelegate));
				return;
			case 1094:
				ScriptingInterfaceOfIScene.call_SetPhotoModeRollDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeRollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeRollDelegate));
				return;
			case 1095:
				ScriptingInterfaceOfIScene.call_SetPhotoModeVignetteDelegate = (ScriptingInterfaceOfIScene.SetPhotoModeVignetteDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPhotoModeVignetteDelegate));
				return;
			case 1096:
				ScriptingInterfaceOfIScene.call_SetPlaySoundEventsAfterReadyToRenderDelegate = (ScriptingInterfaceOfIScene.SetPlaySoundEventsAfterReadyToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetPlaySoundEventsAfterReadyToRenderDelegate));
				return;
			case 1097:
				ScriptingInterfaceOfIScene.call_SetRainDensityDelegate = (ScriptingInterfaceOfIScene.SetRainDensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetRainDensityDelegate));
				return;
			case 1098:
				ScriptingInterfaceOfIScene.call_SetSceneColorGradeDelegate = (ScriptingInterfaceOfIScene.SetSceneColorGradeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSceneColorGradeDelegate));
				return;
			case 1099:
				ScriptingInterfaceOfIScene.call_SetSceneColorGradeIndexDelegate = (ScriptingInterfaceOfIScene.SetSceneColorGradeIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSceneColorGradeIndexDelegate));
				return;
			case 1100:
				ScriptingInterfaceOfIScene.call_SetSceneFilterIndexDelegate = (ScriptingInterfaceOfIScene.SetSceneFilterIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSceneFilterIndexDelegate));
				return;
			case 1101:
				ScriptingInterfaceOfIScene.call_SetShadowDelegate = (ScriptingInterfaceOfIScene.SetShadowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetShadowDelegate));
				return;
			case 1102:
				ScriptingInterfaceOfIScene.call_SetSkyBrightnessDelegate = (ScriptingInterfaceOfIScene.SetSkyBrightnessDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSkyBrightnessDelegate));
				return;
			case 1103:
				ScriptingInterfaceOfIScene.call_SetSkyRotationDelegate = (ScriptingInterfaceOfIScene.SetSkyRotationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSkyRotationDelegate));
				return;
			case 1104:
				ScriptingInterfaceOfIScene.call_SetSnowDensityDelegate = (ScriptingInterfaceOfIScene.SetSnowDensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSnowDensityDelegate));
				return;
			case 1105:
				ScriptingInterfaceOfIScene.call_SetStreakAmountDelegate = (ScriptingInterfaceOfIScene.SetStreakAmountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakAmountDelegate));
				return;
			case 1106:
				ScriptingInterfaceOfIScene.call_SetStreakIntensityDelegate = (ScriptingInterfaceOfIScene.SetStreakIntensityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakIntensityDelegate));
				return;
			case 1107:
				ScriptingInterfaceOfIScene.call_SetStreakStrengthDelegate = (ScriptingInterfaceOfIScene.SetStreakStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakStrengthDelegate));
				return;
			case 1108:
				ScriptingInterfaceOfIScene.call_SetStreakStretchDelegate = (ScriptingInterfaceOfIScene.SetStreakStretchDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakStretchDelegate));
				return;
			case 1109:
				ScriptingInterfaceOfIScene.call_SetStreakThresholdDelegate = (ScriptingInterfaceOfIScene.SetStreakThresholdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakThresholdDelegate));
				return;
			case 1110:
				ScriptingInterfaceOfIScene.call_SetStreakTintDelegate = (ScriptingInterfaceOfIScene.SetStreakTintDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetStreakTintDelegate));
				return;
			case 1111:
				ScriptingInterfaceOfIScene.call_SetSunDelegate = (ScriptingInterfaceOfIScene.SetSunDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunDelegate));
				return;
			case 1112:
				ScriptingInterfaceOfIScene.call_SetSunAngleAltitudeDelegate = (ScriptingInterfaceOfIScene.SetSunAngleAltitudeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunAngleAltitudeDelegate));
				return;
			case 1113:
				ScriptingInterfaceOfIScene.call_SetSunDirectionDelegate = (ScriptingInterfaceOfIScene.SetSunDirectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunDirectionDelegate));
				return;
			case 1114:
				ScriptingInterfaceOfIScene.call_SetSunLightDelegate = (ScriptingInterfaceOfIScene.SetSunLightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunLightDelegate));
				return;
			case 1115:
				ScriptingInterfaceOfIScene.call_SetSunshaftModeDelegate = (ScriptingInterfaceOfIScene.SetSunshaftModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunshaftModeDelegate));
				return;
			case 1116:
				ScriptingInterfaceOfIScene.call_SetSunShaftStrengthDelegate = (ScriptingInterfaceOfIScene.SetSunShaftStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunShaftStrengthDelegate));
				return;
			case 1117:
				ScriptingInterfaceOfIScene.call_SetSunSizeDelegate = (ScriptingInterfaceOfIScene.SetSunSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetSunSizeDelegate));
				return;
			case 1118:
				ScriptingInterfaceOfIScene.call_SetTargetExposureDelegate = (ScriptingInterfaceOfIScene.SetTargetExposureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetTargetExposureDelegate));
				return;
			case 1119:
				ScriptingInterfaceOfIScene.call_SetTemperatureDelegate = (ScriptingInterfaceOfIScene.SetTemperatureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetTemperatureDelegate));
				return;
			case 1120:
				ScriptingInterfaceOfIScene.call_SetTerrainDynamicParamsDelegate = (ScriptingInterfaceOfIScene.SetTerrainDynamicParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetTerrainDynamicParamsDelegate));
				return;
			case 1121:
				ScriptingInterfaceOfIScene.call_SetTimeOfDayDelegate = (ScriptingInterfaceOfIScene.SetTimeOfDayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetTimeOfDayDelegate));
				return;
			case 1122:
				ScriptingInterfaceOfIScene.call_SetTimeSpeedDelegate = (ScriptingInterfaceOfIScene.SetTimeSpeedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetTimeSpeedDelegate));
				return;
			case 1123:
				ScriptingInterfaceOfIScene.call_SetUpgradeLevelDelegate = (ScriptingInterfaceOfIScene.SetUpgradeLevelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetUpgradeLevelDelegate));
				return;
			case 1124:
				ScriptingInterfaceOfIScene.call_SetUpgradeLevelVisibilityDelegate = (ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityDelegate));
				return;
			case 1125:
				ScriptingInterfaceOfIScene.call_SetUpgradeLevelVisibilityWithMaskDelegate = (ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityWithMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityWithMaskDelegate));
				return;
			case 1126:
				ScriptingInterfaceOfIScene.call_SetUseConstantTimeDelegate = (ScriptingInterfaceOfIScene.SetUseConstantTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetUseConstantTimeDelegate));
				return;
			case 1127:
				ScriptingInterfaceOfIScene.call_SetUsesDeleteLaterSystemDelegate = (ScriptingInterfaceOfIScene.SetUsesDeleteLaterSystemDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetUsesDeleteLaterSystemDelegate));
				return;
			case 1128:
				ScriptingInterfaceOfIScene.call_SetVignetteInnerRadiusDelegate = (ScriptingInterfaceOfIScene.SetVignetteInnerRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetVignetteInnerRadiusDelegate));
				return;
			case 1129:
				ScriptingInterfaceOfIScene.call_SetVignetteOpacityDelegate = (ScriptingInterfaceOfIScene.SetVignetteOpacityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetVignetteOpacityDelegate));
				return;
			case 1130:
				ScriptingInterfaceOfIScene.call_SetVignetteOuterRadiusDelegate = (ScriptingInterfaceOfIScene.SetVignetteOuterRadiusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetVignetteOuterRadiusDelegate));
				return;
			case 1131:
				ScriptingInterfaceOfIScene.call_SetWaterStrengthDelegate = (ScriptingInterfaceOfIScene.SetWaterStrengthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetWaterStrengthDelegate));
				return;
			case 1132:
				ScriptingInterfaceOfIScene.call_SetWaterWakeCameraOffsetDelegate = (ScriptingInterfaceOfIScene.SetWaterWakeCameraOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetWaterWakeCameraOffsetDelegate));
				return;
			case 1133:
				ScriptingInterfaceOfIScene.call_SetWaterWakeWorldSizeDelegate = (ScriptingInterfaceOfIScene.SetWaterWakeWorldSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetWaterWakeWorldSizeDelegate));
				return;
			case 1134:
				ScriptingInterfaceOfIScene.call_SetWinterTimeFactorDelegate = (ScriptingInterfaceOfIScene.SetWinterTimeFactorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SetWinterTimeFactorDelegate));
				return;
			case 1135:
				ScriptingInterfaceOfIScene.call_StallLoadingRenderingsUntilFurtherNoticeDelegate = (ScriptingInterfaceOfIScene.StallLoadingRenderingsUntilFurtherNoticeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.StallLoadingRenderingsUntilFurtherNoticeDelegate));
				return;
			case 1136:
				ScriptingInterfaceOfIScene.call_SwapFaceConnectionsWithIdDelegate = (ScriptingInterfaceOfIScene.SwapFaceConnectionsWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.SwapFaceConnectionsWithIdDelegate));
				return;
			case 1137:
				ScriptingInterfaceOfIScene.call_TakePhotoModePictureDelegate = (ScriptingInterfaceOfIScene.TakePhotoModePictureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.TakePhotoModePictureDelegate));
				return;
			case 1138:
				ScriptingInterfaceOfIScene.call_TickDelegate = (ScriptingInterfaceOfIScene.TickDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.TickDelegate));
				return;
			case 1139:
				ScriptingInterfaceOfIScene.call_TickWakeDelegate = (ScriptingInterfaceOfIScene.TickWakeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.TickWakeDelegate));
				return;
			case 1140:
				ScriptingInterfaceOfIScene.call_WaitWaterRendererCPUSimulationDelegate = (ScriptingInterfaceOfIScene.WaitWaterRendererCPUSimulationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.WaitWaterRendererCPUSimulationDelegate));
				return;
			case 1141:
				ScriptingInterfaceOfIScene.call_WorldPositionComputeNearestNavMeshDelegate = (ScriptingInterfaceOfIScene.WorldPositionComputeNearestNavMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.WorldPositionComputeNearestNavMeshDelegate));
				return;
			case 1142:
				ScriptingInterfaceOfIScene.call_WorldPositionValidateZDelegate = (ScriptingInterfaceOfIScene.WorldPositionValidateZDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScene.WorldPositionValidateZDelegate));
				return;
			case 1143:
				ScriptingInterfaceOfISceneView.call_AddClearTaskDelegate = (ScriptingInterfaceOfISceneView.AddClearTaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.AddClearTaskDelegate));
				return;
			case 1144:
				ScriptingInterfaceOfISceneView.call_CheckSceneReadyToRenderDelegate = (ScriptingInterfaceOfISceneView.CheckSceneReadyToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.CheckSceneReadyToRenderDelegate));
				return;
			case 1145:
				ScriptingInterfaceOfISceneView.call_ClearAllDelegate = (ScriptingInterfaceOfISceneView.ClearAllDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.ClearAllDelegate));
				return;
			case 1146:
				ScriptingInterfaceOfISceneView.call_CreateSceneViewDelegate = (ScriptingInterfaceOfISceneView.CreateSceneViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.CreateSceneViewDelegate));
				return;
			case 1147:
				ScriptingInterfaceOfISceneView.call_DoNotClearDelegate = (ScriptingInterfaceOfISceneView.DoNotClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.DoNotClearDelegate));
				return;
			case 1148:
				ScriptingInterfaceOfISceneView.call_GetSceneDelegate = (ScriptingInterfaceOfISceneView.GetSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.GetSceneDelegate));
				return;
			case 1149:
				ScriptingInterfaceOfISceneView.call_ProjectedMousePositionOnGroundDelegate = (ScriptingInterfaceOfISceneView.ProjectedMousePositionOnGroundDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.ProjectedMousePositionOnGroundDelegate));
				return;
			case 1150:
				ScriptingInterfaceOfISceneView.call_ProjectedMousePositionOnWaterDelegate = (ScriptingInterfaceOfISceneView.ProjectedMousePositionOnWaterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.ProjectedMousePositionOnWaterDelegate));
				return;
			case 1151:
				ScriptingInterfaceOfISceneView.call_RayCastForClosestEntityOrTerrainDelegate = (ScriptingInterfaceOfISceneView.RayCastForClosestEntityOrTerrainDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.RayCastForClosestEntityOrTerrainDelegate));
				return;
			case 1152:
				ScriptingInterfaceOfISceneView.call_ReadyToRenderDelegate = (ScriptingInterfaceOfISceneView.ReadyToRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.ReadyToRenderDelegate));
				return;
			case 1153:
				ScriptingInterfaceOfISceneView.call_ScreenPointToViewportPointDelegate = (ScriptingInterfaceOfISceneView.ScreenPointToViewportPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.ScreenPointToViewportPointDelegate));
				return;
			case 1154:
				ScriptingInterfaceOfISceneView.call_SetAcceptGlobalDebugRenderObjectsDelegate = (ScriptingInterfaceOfISceneView.SetAcceptGlobalDebugRenderObjectsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetAcceptGlobalDebugRenderObjectsDelegate));
				return;
			case 1155:
				ScriptingInterfaceOfISceneView.call_SetCameraDelegate = (ScriptingInterfaceOfISceneView.SetCameraDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetCameraDelegate));
				return;
			case 1156:
				ScriptingInterfaceOfISceneView.call_SetCleanScreenUntilLoadingDoneDelegate = (ScriptingInterfaceOfISceneView.SetCleanScreenUntilLoadingDoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetCleanScreenUntilLoadingDoneDelegate));
				return;
			case 1157:
				ScriptingInterfaceOfISceneView.call_SetClearAndDisableAfterSucessfullRenderDelegate = (ScriptingInterfaceOfISceneView.SetClearAndDisableAfterSucessfullRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetClearAndDisableAfterSucessfullRenderDelegate));
				return;
			case 1158:
				ScriptingInterfaceOfISceneView.call_SetClearGbufferDelegate = (ScriptingInterfaceOfISceneView.SetClearGbufferDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetClearGbufferDelegate));
				return;
			case 1159:
				ScriptingInterfaceOfISceneView.call_SetDoQuickExposureDelegate = (ScriptingInterfaceOfISceneView.SetDoQuickExposureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetDoQuickExposureDelegate));
				return;
			case 1160:
				ScriptingInterfaceOfISceneView.call_SetFocusedShadowmapDelegate = (ScriptingInterfaceOfISceneView.SetFocusedShadowmapDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetFocusedShadowmapDelegate));
				return;
			case 1161:
				ScriptingInterfaceOfISceneView.call_SetForceShaderCompilationDelegate = (ScriptingInterfaceOfISceneView.SetForceShaderCompilationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetForceShaderCompilationDelegate));
				return;
			case 1162:
				ScriptingInterfaceOfISceneView.call_SetPointlightResolutionMultiplierDelegate = (ScriptingInterfaceOfISceneView.SetPointlightResolutionMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetPointlightResolutionMultiplierDelegate));
				return;
			case 1163:
				ScriptingInterfaceOfISceneView.call_SetPostfxConfigParamsDelegate = (ScriptingInterfaceOfISceneView.SetPostfxConfigParamsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetPostfxConfigParamsDelegate));
				return;
			case 1164:
				ScriptingInterfaceOfISceneView.call_SetPostfxFromConfigDelegate = (ScriptingInterfaceOfISceneView.SetPostfxFromConfigDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetPostfxFromConfigDelegate));
				return;
			case 1165:
				ScriptingInterfaceOfISceneView.call_SetRenderWithPostfxDelegate = (ScriptingInterfaceOfISceneView.SetRenderWithPostfxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetRenderWithPostfxDelegate));
				return;
			case 1166:
				ScriptingInterfaceOfISceneView.call_SetResolutionScalingDelegate = (ScriptingInterfaceOfISceneView.SetResolutionScalingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetResolutionScalingDelegate));
				return;
			case 1167:
				ScriptingInterfaceOfISceneView.call_SetSceneDelegate = (ScriptingInterfaceOfISceneView.SetSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetSceneDelegate));
				return;
			case 1168:
				ScriptingInterfaceOfISceneView.call_SetSceneUsesContourDelegate = (ScriptingInterfaceOfISceneView.SetSceneUsesContourDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetSceneUsesContourDelegate));
				return;
			case 1169:
				ScriptingInterfaceOfISceneView.call_SetSceneUsesShadowsDelegate = (ScriptingInterfaceOfISceneView.SetSceneUsesShadowsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetSceneUsesShadowsDelegate));
				return;
			case 1170:
				ScriptingInterfaceOfISceneView.call_SetSceneUsesSkyboxDelegate = (ScriptingInterfaceOfISceneView.SetSceneUsesSkyboxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetSceneUsesSkyboxDelegate));
				return;
			case 1171:
				ScriptingInterfaceOfISceneView.call_SetShadowmapResolutionMultiplierDelegate = (ScriptingInterfaceOfISceneView.SetShadowmapResolutionMultiplierDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.SetShadowmapResolutionMultiplierDelegate));
				return;
			case 1172:
				ScriptingInterfaceOfISceneView.call_TranslateMouseDelegate = (ScriptingInterfaceOfISceneView.TranslateMouseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.TranslateMouseDelegate));
				return;
			case 1173:
				ScriptingInterfaceOfISceneView.call_WorldPointToScreenPointDelegate = (ScriptingInterfaceOfISceneView.WorldPointToScreenPointDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISceneView.WorldPointToScreenPointDelegate));
				return;
			case 1174:
				ScriptingInterfaceOfIScreen.call_GetAspectRatioDelegate = (ScriptingInterfaceOfIScreen.GetAspectRatioDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetAspectRatioDelegate));
				return;
			case 1175:
				ScriptingInterfaceOfIScreen.call_GetDesktopHeightDelegate = (ScriptingInterfaceOfIScreen.GetDesktopHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetDesktopHeightDelegate));
				return;
			case 1176:
				ScriptingInterfaceOfIScreen.call_GetDesktopWidthDelegate = (ScriptingInterfaceOfIScreen.GetDesktopWidthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetDesktopWidthDelegate));
				return;
			case 1177:
				ScriptingInterfaceOfIScreen.call_GetMouseVisibleDelegate = (ScriptingInterfaceOfIScreen.GetMouseVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetMouseVisibleDelegate));
				return;
			case 1178:
				ScriptingInterfaceOfIScreen.call_GetRealScreenResolutionHeightDelegate = (ScriptingInterfaceOfIScreen.GetRealScreenResolutionHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetRealScreenResolutionHeightDelegate));
				return;
			case 1179:
				ScriptingInterfaceOfIScreen.call_GetRealScreenResolutionWidthDelegate = (ScriptingInterfaceOfIScreen.GetRealScreenResolutionWidthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetRealScreenResolutionWidthDelegate));
				return;
			case 1180:
				ScriptingInterfaceOfIScreen.call_GetUsableAreaPercentagesDelegate = (ScriptingInterfaceOfIScreen.GetUsableAreaPercentagesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.GetUsableAreaPercentagesDelegate));
				return;
			case 1181:
				ScriptingInterfaceOfIScreen.call_IsEnterButtonCrossDelegate = (ScriptingInterfaceOfIScreen.IsEnterButtonCrossDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.IsEnterButtonCrossDelegate));
				return;
			case 1182:
				ScriptingInterfaceOfIScreen.call_SetMouseVisibleDelegate = (ScriptingInterfaceOfIScreen.SetMouseVisibleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScreen.SetMouseVisibleDelegate));
				return;
			case 1183:
				ScriptingInterfaceOfIScriptComponent.call_GetNameDelegate = (ScriptingInterfaceOfIScriptComponent.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScriptComponent.GetNameDelegate));
				return;
			case 1184:
				ScriptingInterfaceOfIScriptComponent.call_GetScriptComponentBehaviorDelegate = (ScriptingInterfaceOfIScriptComponent.GetScriptComponentBehaviorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScriptComponent.GetScriptComponentBehaviorDelegate));
				return;
			case 1185:
				ScriptingInterfaceOfIScriptComponent.call_SetVariableEditorWidgetStatusDelegate = (ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetStatusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetStatusDelegate));
				return;
			case 1186:
				ScriptingInterfaceOfIScriptComponent.call_SetVariableEditorWidgetValueDelegate = (ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIScriptComponent.SetVariableEditorWidgetValueDelegate));
				return;
			case 1187:
				ScriptingInterfaceOfIShader.call_GetFromResourceDelegate = (ScriptingInterfaceOfIShader.GetFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIShader.GetFromResourceDelegate));
				return;
			case 1188:
				ScriptingInterfaceOfIShader.call_GetMaterialShaderFlagMaskDelegate = (ScriptingInterfaceOfIShader.GetMaterialShaderFlagMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIShader.GetMaterialShaderFlagMaskDelegate));
				return;
			case 1189:
				ScriptingInterfaceOfIShader.call_GetNameDelegate = (ScriptingInterfaceOfIShader.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIShader.GetNameDelegate));
				return;
			case 1190:
				ScriptingInterfaceOfIShader.call_ReleaseDelegate = (ScriptingInterfaceOfIShader.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIShader.ReleaseDelegate));
				return;
			case 1191:
				ScriptingInterfaceOfISkeleton.call_ActivateRagdollDelegate = (ScriptingInterfaceOfISkeleton.ActivateRagdollDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ActivateRagdollDelegate));
				return;
			case 1192:
				ScriptingInterfaceOfISkeleton.call_AddComponentDelegate = (ScriptingInterfaceOfISkeleton.AddComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.AddComponentDelegate));
				return;
			case 1193:
				ScriptingInterfaceOfISkeleton.call_AddComponentToBoneDelegate = (ScriptingInterfaceOfISkeleton.AddComponentToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.AddComponentToBoneDelegate));
				return;
			case 1194:
				ScriptingInterfaceOfISkeleton.call_AddMeshDelegate = (ScriptingInterfaceOfISkeleton.AddMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.AddMeshDelegate));
				return;
			case 1195:
				ScriptingInterfaceOfISkeleton.call_AddMeshToBoneDelegate = (ScriptingInterfaceOfISkeleton.AddMeshToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.AddMeshToBoneDelegate));
				return;
			case 1196:
				ScriptingInterfaceOfISkeleton.call_AddPrefabEntityToBoneDelegate = (ScriptingInterfaceOfISkeleton.AddPrefabEntityToBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.AddPrefabEntityToBoneDelegate));
				return;
			case 1197:
				ScriptingInterfaceOfISkeleton.call_ClearComponentsDelegate = (ScriptingInterfaceOfISkeleton.ClearComponentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ClearComponentsDelegate));
				return;
			case 1198:
				ScriptingInterfaceOfISkeleton.call_ClearMeshesDelegate = (ScriptingInterfaceOfISkeleton.ClearMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ClearMeshesDelegate));
				return;
			case 1199:
				ScriptingInterfaceOfISkeleton.call_ClearMeshesAtBoneDelegate = (ScriptingInterfaceOfISkeleton.ClearMeshesAtBoneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ClearMeshesAtBoneDelegate));
				return;
			case 1200:
				ScriptingInterfaceOfISkeleton.call_CreateFromModelDelegate = (ScriptingInterfaceOfISkeleton.CreateFromModelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.CreateFromModelDelegate));
				return;
			case 1201:
				ScriptingInterfaceOfISkeleton.call_CreateFromModelWithNullAnimTreeDelegate = (ScriptingInterfaceOfISkeleton.CreateFromModelWithNullAnimTreeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.CreateFromModelWithNullAnimTreeDelegate));
				return;
			case 1202:
				ScriptingInterfaceOfISkeleton.call_EnableScriptDrivenPostIntegrateCallbackDelegate = (ScriptingInterfaceOfISkeleton.EnableScriptDrivenPostIntegrateCallbackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.EnableScriptDrivenPostIntegrateCallbackDelegate));
				return;
			case 1203:
				ScriptingInterfaceOfISkeleton.call_ForceUpdateBoneFramesDelegate = (ScriptingInterfaceOfISkeleton.ForceUpdateBoneFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ForceUpdateBoneFramesDelegate));
				return;
			case 1204:
				ScriptingInterfaceOfISkeleton.call_FreezeDelegate = (ScriptingInterfaceOfISkeleton.FreezeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.FreezeDelegate));
				return;
			case 1205:
				ScriptingInterfaceOfISkeleton.call_GetAllMeshesDelegate = (ScriptingInterfaceOfISkeleton.GetAllMeshesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetAllMeshesDelegate));
				return;
			case 1206:
				ScriptingInterfaceOfISkeleton.call_GetAnimationAtChannelDelegate = (ScriptingInterfaceOfISkeleton.GetAnimationAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetAnimationAtChannelDelegate));
				return;
			case 1207:
				ScriptingInterfaceOfISkeleton.call_GetAnimationIndexAtChannelDelegate = (ScriptingInterfaceOfISkeleton.GetAnimationIndexAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetAnimationIndexAtChannelDelegate));
				return;
			case 1208:
				ScriptingInterfaceOfISkeleton.call_GetBoneBodyDelegate = (ScriptingInterfaceOfISkeleton.GetBoneBodyDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneBodyDelegate));
				return;
			case 1209:
				ScriptingInterfaceOfISkeleton.call_GetBoneChildAtIndexDelegate = (ScriptingInterfaceOfISkeleton.GetBoneChildAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneChildAtIndexDelegate));
				return;
			case 1210:
				ScriptingInterfaceOfISkeleton.call_GetBoneChildCountDelegate = (ScriptingInterfaceOfISkeleton.GetBoneChildCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneChildCountDelegate));
				return;
			case 1211:
				ScriptingInterfaceOfISkeleton.call_GetBoneComponentAtIndexDelegate = (ScriptingInterfaceOfISkeleton.GetBoneComponentAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneComponentAtIndexDelegate));
				return;
			case 1212:
				ScriptingInterfaceOfISkeleton.call_GetBoneComponentCountDelegate = (ScriptingInterfaceOfISkeleton.GetBoneComponentCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneComponentCountDelegate));
				return;
			case 1213:
				ScriptingInterfaceOfISkeleton.call_GetBoneCountDelegate = (ScriptingInterfaceOfISkeleton.GetBoneCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneCountDelegate));
				return;
			case 1214:
				ScriptingInterfaceOfISkeleton.call_GetBoneEntitialFrameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameDelegate));
				return;
			case 1215:
				ScriptingInterfaceOfISkeleton.call_GetBoneEntitialFrameAtChannelDelegate = (ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameAtChannelDelegate));
				return;
			case 1216:
				ScriptingInterfaceOfISkeleton.call_GetBoneEntitialFrameWithIndexDelegate = (ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameWithIndexDelegate));
				return;
			case 1217:
				ScriptingInterfaceOfISkeleton.call_GetBoneEntitialFrameWithNameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameWithNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneEntitialFrameWithNameDelegate));
				return;
			case 1218:
				ScriptingInterfaceOfISkeleton.call_GetBoneEntitialRestFrameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneEntitialRestFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneEntitialRestFrameDelegate));
				return;
			case 1219:
				ScriptingInterfaceOfISkeleton.call_GetBoneIndexFromNameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneIndexFromNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneIndexFromNameDelegate));
				return;
			case 1220:
				ScriptingInterfaceOfISkeleton.call_GetBoneLocalRestFrameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneLocalRestFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneLocalRestFrameDelegate));
				return;
			case 1221:
				ScriptingInterfaceOfISkeleton.call_GetBoneNameDelegate = (ScriptingInterfaceOfISkeleton.GetBoneNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetBoneNameDelegate));
				return;
			case 1222:
				ScriptingInterfaceOfISkeleton.call_GetComponentAtIndexDelegate = (ScriptingInterfaceOfISkeleton.GetComponentAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetComponentAtIndexDelegate));
				return;
			case 1223:
				ScriptingInterfaceOfISkeleton.call_GetComponentCountDelegate = (ScriptingInterfaceOfISkeleton.GetComponentCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetComponentCountDelegate));
				return;
			case 1224:
				ScriptingInterfaceOfISkeleton.call_GetCurrentRagdollStateDelegate = (ScriptingInterfaceOfISkeleton.GetCurrentRagdollStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetCurrentRagdollStateDelegate));
				return;
			case 1225:
				ScriptingInterfaceOfISkeleton.call_GetEntitialOutTransformDelegate = (ScriptingInterfaceOfISkeleton.GetEntitialOutTransformDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetEntitialOutTransformDelegate));
				return;
			case 1226:
				ScriptingInterfaceOfISkeleton.call_GetNameDelegate = (ScriptingInterfaceOfISkeleton.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetNameDelegate));
				return;
			case 1227:
				ScriptingInterfaceOfISkeleton.call_GetParentBoneIndexDelegate = (ScriptingInterfaceOfISkeleton.GetParentBoneIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetParentBoneIndexDelegate));
				return;
			case 1228:
				ScriptingInterfaceOfISkeleton.call_GetSkeletonAnimationParameterAtChannelDelegate = (ScriptingInterfaceOfISkeleton.GetSkeletonAnimationParameterAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetSkeletonAnimationParameterAtChannelDelegate));
				return;
			case 1229:
				ScriptingInterfaceOfISkeleton.call_GetSkeletonAnimationSpeedAtChannelDelegate = (ScriptingInterfaceOfISkeleton.GetSkeletonAnimationSpeedAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetSkeletonAnimationSpeedAtChannelDelegate));
				return;
			case 1230:
				ScriptingInterfaceOfISkeleton.call_GetSkeletonBoneMappingDelegate = (ScriptingInterfaceOfISkeleton.GetSkeletonBoneMappingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.GetSkeletonBoneMappingDelegate));
				return;
			case 1231:
				ScriptingInterfaceOfISkeleton.call_HasBoneComponentDelegate = (ScriptingInterfaceOfISkeleton.HasBoneComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.HasBoneComponentDelegate));
				return;
			case 1232:
				ScriptingInterfaceOfISkeleton.call_HasComponentDelegate = (ScriptingInterfaceOfISkeleton.HasComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.HasComponentDelegate));
				return;
			case 1233:
				ScriptingInterfaceOfISkeleton.call_IsFrozenDelegate = (ScriptingInterfaceOfISkeleton.IsFrozenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.IsFrozenDelegate));
				return;
			case 1234:
				ScriptingInterfaceOfISkeleton.call_RemoveBoneComponentDelegate = (ScriptingInterfaceOfISkeleton.RemoveBoneComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.RemoveBoneComponentDelegate));
				return;
			case 1235:
				ScriptingInterfaceOfISkeleton.call_RemoveComponentDelegate = (ScriptingInterfaceOfISkeleton.RemoveComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.RemoveComponentDelegate));
				return;
			case 1236:
				ScriptingInterfaceOfISkeleton.call_ResetClothsDelegate = (ScriptingInterfaceOfISkeleton.ResetClothsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ResetClothsDelegate));
				return;
			case 1237:
				ScriptingInterfaceOfISkeleton.call_ResetFramesDelegate = (ScriptingInterfaceOfISkeleton.ResetFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.ResetFramesDelegate));
				return;
			case 1238:
				ScriptingInterfaceOfISkeleton.call_SetBoneLocalFrameDelegate = (ScriptingInterfaceOfISkeleton.SetBoneLocalFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetBoneLocalFrameDelegate));
				return;
			case 1239:
				ScriptingInterfaceOfISkeleton.call_SetOutBoneDisplacementDelegate = (ScriptingInterfaceOfISkeleton.SetOutBoneDisplacementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetOutBoneDisplacementDelegate));
				return;
			case 1240:
				ScriptingInterfaceOfISkeleton.call_SetOutQuatDelegate = (ScriptingInterfaceOfISkeleton.SetOutQuatDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetOutQuatDelegate));
				return;
			case 1241:
				ScriptingInterfaceOfISkeleton.call_SetSkeletonAnimationParameterAtChannelDelegate = (ScriptingInterfaceOfISkeleton.SetSkeletonAnimationParameterAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetSkeletonAnimationParameterAtChannelDelegate));
				return;
			case 1242:
				ScriptingInterfaceOfISkeleton.call_SetSkeletonAnimationSpeedAtChannelDelegate = (ScriptingInterfaceOfISkeleton.SetSkeletonAnimationSpeedAtChannelDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetSkeletonAnimationSpeedAtChannelDelegate));
				return;
			case 1243:
				ScriptingInterfaceOfISkeleton.call_SetSkeletonUptoDateDelegate = (ScriptingInterfaceOfISkeleton.SetSkeletonUptoDateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetSkeletonUptoDateDelegate));
				return;
			case 1244:
				ScriptingInterfaceOfISkeleton.call_SetUsePreciseBoundingVolumeDelegate = (ScriptingInterfaceOfISkeleton.SetUsePreciseBoundingVolumeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SetUsePreciseBoundingVolumeDelegate));
				return;
			case 1245:
				ScriptingInterfaceOfISkeleton.call_SkeletonModelExistDelegate = (ScriptingInterfaceOfISkeleton.SkeletonModelExistDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.SkeletonModelExistDelegate));
				return;
			case 1246:
				ScriptingInterfaceOfISkeleton.call_TickAnimationsDelegate = (ScriptingInterfaceOfISkeleton.TickAnimationsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.TickAnimationsDelegate));
				return;
			case 1247:
				ScriptingInterfaceOfISkeleton.call_TickAnimationsAndForceUpdateDelegate = (ScriptingInterfaceOfISkeleton.TickAnimationsAndForceUpdateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.TickAnimationsAndForceUpdateDelegate));
				return;
			case 1248:
				ScriptingInterfaceOfISkeleton.call_UpdateEntitialFramesFromLocalFramesDelegate = (ScriptingInterfaceOfISkeleton.UpdateEntitialFramesFromLocalFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISkeleton.UpdateEntitialFramesFromLocalFramesDelegate));
				return;
			case 1249:
				ScriptingInterfaceOfISoundEvent.call_CreateEventDelegate = (ScriptingInterfaceOfISoundEvent.CreateEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.CreateEventDelegate));
				return;
			case 1250:
				ScriptingInterfaceOfISoundEvent.call_CreateEventFromExternalFileDelegate = (ScriptingInterfaceOfISoundEvent.CreateEventFromExternalFileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.CreateEventFromExternalFileDelegate));
				return;
			case 1251:
				ScriptingInterfaceOfISoundEvent.call_CreateEventFromSoundBufferDelegate = (ScriptingInterfaceOfISoundEvent.CreateEventFromSoundBufferDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.CreateEventFromSoundBufferDelegate));
				return;
			case 1252:
				ScriptingInterfaceOfISoundEvent.call_CreateEventFromStringDelegate = (ScriptingInterfaceOfISoundEvent.CreateEventFromStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.CreateEventFromStringDelegate));
				return;
			case 1253:
				ScriptingInterfaceOfISoundEvent.call_GetEventIdFromStringDelegate = (ScriptingInterfaceOfISoundEvent.GetEventIdFromStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.GetEventIdFromStringDelegate));
				return;
			case 1254:
				ScriptingInterfaceOfISoundEvent.call_GetEventMinMaxDistanceDelegate = (ScriptingInterfaceOfISoundEvent.GetEventMinMaxDistanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.GetEventMinMaxDistanceDelegate));
				return;
			case 1255:
				ScriptingInterfaceOfISoundEvent.call_GetTotalEventCountDelegate = (ScriptingInterfaceOfISoundEvent.GetTotalEventCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.GetTotalEventCountDelegate));
				return;
			case 1256:
				ScriptingInterfaceOfISoundEvent.call_IsPausedDelegate = (ScriptingInterfaceOfISoundEvent.IsPausedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.IsPausedDelegate));
				return;
			case 1257:
				ScriptingInterfaceOfISoundEvent.call_IsPlayingDelegate = (ScriptingInterfaceOfISoundEvent.IsPlayingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.IsPlayingDelegate));
				return;
			case 1258:
				ScriptingInterfaceOfISoundEvent.call_IsValidDelegate = (ScriptingInterfaceOfISoundEvent.IsValidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.IsValidDelegate));
				return;
			case 1259:
				ScriptingInterfaceOfISoundEvent.call_PauseEventDelegate = (ScriptingInterfaceOfISoundEvent.PauseEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.PauseEventDelegate));
				return;
			case 1260:
				ScriptingInterfaceOfISoundEvent.call_PlayExtraEventDelegate = (ScriptingInterfaceOfISoundEvent.PlayExtraEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.PlayExtraEventDelegate));
				return;
			case 1261:
				ScriptingInterfaceOfISoundEvent.call_PlaySound2DDelegate = (ScriptingInterfaceOfISoundEvent.PlaySound2DDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.PlaySound2DDelegate));
				return;
			case 1262:
				ScriptingInterfaceOfISoundEvent.call_ReleaseEventDelegate = (ScriptingInterfaceOfISoundEvent.ReleaseEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.ReleaseEventDelegate));
				return;
			case 1263:
				ScriptingInterfaceOfISoundEvent.call_ResumeEventDelegate = (ScriptingInterfaceOfISoundEvent.ResumeEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.ResumeEventDelegate));
				return;
			case 1264:
				ScriptingInterfaceOfISoundEvent.call_SetEventMinMaxDistanceDelegate = (ScriptingInterfaceOfISoundEvent.SetEventMinMaxDistanceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetEventMinMaxDistanceDelegate));
				return;
			case 1265:
				ScriptingInterfaceOfISoundEvent.call_SetEventParameterAtIndexDelegate = (ScriptingInterfaceOfISoundEvent.SetEventParameterAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetEventParameterAtIndexDelegate));
				return;
			case 1266:
				ScriptingInterfaceOfISoundEvent.call_SetEventParameterFromStringDelegate = (ScriptingInterfaceOfISoundEvent.SetEventParameterFromStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetEventParameterFromStringDelegate));
				return;
			case 1267:
				ScriptingInterfaceOfISoundEvent.call_SetEventPositionDelegate = (ScriptingInterfaceOfISoundEvent.SetEventPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetEventPositionDelegate));
				return;
			case 1268:
				ScriptingInterfaceOfISoundEvent.call_SetEventVelocityDelegate = (ScriptingInterfaceOfISoundEvent.SetEventVelocityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetEventVelocityDelegate));
				return;
			case 1269:
				ScriptingInterfaceOfISoundEvent.call_SetSwitchDelegate = (ScriptingInterfaceOfISoundEvent.SetSwitchDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.SetSwitchDelegate));
				return;
			case 1270:
				ScriptingInterfaceOfISoundEvent.call_StartEventDelegate = (ScriptingInterfaceOfISoundEvent.StartEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.StartEventDelegate));
				return;
			case 1271:
				ScriptingInterfaceOfISoundEvent.call_StartEventInPositionDelegate = (ScriptingInterfaceOfISoundEvent.StartEventInPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.StartEventInPositionDelegate));
				return;
			case 1272:
				ScriptingInterfaceOfISoundEvent.call_StopEventDelegate = (ScriptingInterfaceOfISoundEvent.StopEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.StopEventDelegate));
				return;
			case 1273:
				ScriptingInterfaceOfISoundEvent.call_TriggerCueDelegate = (ScriptingInterfaceOfISoundEvent.TriggerCueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundEvent.TriggerCueDelegate));
				return;
			case 1274:
				ScriptingInterfaceOfISoundManager.call_AddSoundClientWithIdDelegate = (ScriptingInterfaceOfISoundManager.AddSoundClientWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.AddSoundClientWithIdDelegate));
				return;
			case 1275:
				ScriptingInterfaceOfISoundManager.call_AddXBOXRemoteUserDelegate = (ScriptingInterfaceOfISoundManager.AddXBOXRemoteUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.AddXBOXRemoteUserDelegate));
				return;
			case 1276:
				ScriptingInterfaceOfISoundManager.call_ApplyPushToTalkDelegate = (ScriptingInterfaceOfISoundManager.ApplyPushToTalkDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ApplyPushToTalkDelegate));
				return;
			case 1277:
				ScriptingInterfaceOfISoundManager.call_ClearDataToBeSentDelegate = (ScriptingInterfaceOfISoundManager.ClearDataToBeSentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ClearDataToBeSentDelegate));
				return;
			case 1278:
				ScriptingInterfaceOfISoundManager.call_ClearXBOXSoundManagerDelegate = (ScriptingInterfaceOfISoundManager.ClearXBOXSoundManagerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ClearXBOXSoundManagerDelegate));
				return;
			case 1279:
				ScriptingInterfaceOfISoundManager.call_CompressDataDelegate = (ScriptingInterfaceOfISoundManager.CompressDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.CompressDataDelegate));
				return;
			case 1280:
				ScriptingInterfaceOfISoundManager.call_CreateVoiceEventDelegate = (ScriptingInterfaceOfISoundManager.CreateVoiceEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.CreateVoiceEventDelegate));
				return;
			case 1281:
				ScriptingInterfaceOfISoundManager.call_DecompressDataDelegate = (ScriptingInterfaceOfISoundManager.DecompressDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.DecompressDataDelegate));
				return;
			case 1282:
				ScriptingInterfaceOfISoundManager.call_DeleteSoundClientWithIdDelegate = (ScriptingInterfaceOfISoundManager.DeleteSoundClientWithIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.DeleteSoundClientWithIdDelegate));
				return;
			case 1283:
				ScriptingInterfaceOfISoundManager.call_DestroyVoiceEventDelegate = (ScriptingInterfaceOfISoundManager.DestroyVoiceEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.DestroyVoiceEventDelegate));
				return;
			case 1284:
				ScriptingInterfaceOfISoundManager.call_FinalizeVoicePlayEventDelegate = (ScriptingInterfaceOfISoundManager.FinalizeVoicePlayEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.FinalizeVoicePlayEventDelegate));
				return;
			case 1285:
				ScriptingInterfaceOfISoundManager.call_GetAttenuationPositionDelegate = (ScriptingInterfaceOfISoundManager.GetAttenuationPositionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetAttenuationPositionDelegate));
				return;
			case 1286:
				ScriptingInterfaceOfISoundManager.call_GetDataToBeSentAtDelegate = (ScriptingInterfaceOfISoundManager.GetDataToBeSentAtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetDataToBeSentAtDelegate));
				return;
			case 1287:
				ScriptingInterfaceOfISoundManager.call_GetGlobalIndexOfEventDelegate = (ScriptingInterfaceOfISoundManager.GetGlobalIndexOfEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetGlobalIndexOfEventDelegate));
				return;
			case 1288:
				ScriptingInterfaceOfISoundManager.call_GetListenerFrameDelegate = (ScriptingInterfaceOfISoundManager.GetListenerFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetListenerFrameDelegate));
				return;
			case 1289:
				ScriptingInterfaceOfISoundManager.call_GetSizeOfDataToBeSentAtDelegate = (ScriptingInterfaceOfISoundManager.GetSizeOfDataToBeSentAtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetSizeOfDataToBeSentAtDelegate));
				return;
			case 1290:
				ScriptingInterfaceOfISoundManager.call_GetVoiceDataDelegate = (ScriptingInterfaceOfISoundManager.GetVoiceDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.GetVoiceDataDelegate));
				return;
			case 1291:
				ScriptingInterfaceOfISoundManager.call_HandleStateChangesDelegate = (ScriptingInterfaceOfISoundManager.HandleStateChangesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.HandleStateChangesDelegate));
				return;
			case 1292:
				ScriptingInterfaceOfISoundManager.call_InitializeVoicePlayEventDelegate = (ScriptingInterfaceOfISoundManager.InitializeVoicePlayEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.InitializeVoicePlayEventDelegate));
				return;
			case 1293:
				ScriptingInterfaceOfISoundManager.call_InitializeXBOXSoundManagerDelegate = (ScriptingInterfaceOfISoundManager.InitializeXBOXSoundManagerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.InitializeXBOXSoundManagerDelegate));
				return;
			case 1294:
				ScriptingInterfaceOfISoundManager.call_LoadEventFileAuxDelegate = (ScriptingInterfaceOfISoundManager.LoadEventFileAuxDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.LoadEventFileAuxDelegate));
				return;
			case 1295:
				ScriptingInterfaceOfISoundManager.call_PauseBusDelegate = (ScriptingInterfaceOfISoundManager.PauseBusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.PauseBusDelegate));
				return;
			case 1296:
				ScriptingInterfaceOfISoundManager.call_ProcessDataToBeReceivedDelegate = (ScriptingInterfaceOfISoundManager.ProcessDataToBeReceivedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ProcessDataToBeReceivedDelegate));
				return;
			case 1297:
				ScriptingInterfaceOfISoundManager.call_ProcessDataToBeSentDelegate = (ScriptingInterfaceOfISoundManager.ProcessDataToBeSentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ProcessDataToBeSentDelegate));
				return;
			case 1298:
				ScriptingInterfaceOfISoundManager.call_RemoveXBOXRemoteUserDelegate = (ScriptingInterfaceOfISoundManager.RemoveXBOXRemoteUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.RemoveXBOXRemoteUserDelegate));
				return;
			case 1299:
				ScriptingInterfaceOfISoundManager.call_ResetDelegate = (ScriptingInterfaceOfISoundManager.ResetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.ResetDelegate));
				return;
			case 1300:
				ScriptingInterfaceOfISoundManager.call_SetGlobalParameterDelegate = (ScriptingInterfaceOfISoundManager.SetGlobalParameterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.SetGlobalParameterDelegate));
				return;
			case 1301:
				ScriptingInterfaceOfISoundManager.call_SetListenerFrameDelegate = (ScriptingInterfaceOfISoundManager.SetListenerFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.SetListenerFrameDelegate));
				return;
			case 1302:
				ScriptingInterfaceOfISoundManager.call_SetStateDelegate = (ScriptingInterfaceOfISoundManager.SetStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.SetStateDelegate));
				return;
			case 1303:
				ScriptingInterfaceOfISoundManager.call_StartOneShotEventDelegate = (ScriptingInterfaceOfISoundManager.StartOneShotEventDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.StartOneShotEventDelegate));
				return;
			case 1304:
				ScriptingInterfaceOfISoundManager.call_StartOneShotEventWithIndexDelegate = (ScriptingInterfaceOfISoundManager.StartOneShotEventWithIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.StartOneShotEventWithIndexDelegate));
				return;
			case 1305:
				ScriptingInterfaceOfISoundManager.call_StartOneShotEventWithParamDelegate = (ScriptingInterfaceOfISoundManager.StartOneShotEventWithParamDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.StartOneShotEventWithParamDelegate));
				return;
			case 1306:
				ScriptingInterfaceOfISoundManager.call_StartVoiceRecordDelegate = (ScriptingInterfaceOfISoundManager.StartVoiceRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.StartVoiceRecordDelegate));
				return;
			case 1307:
				ScriptingInterfaceOfISoundManager.call_StopVoiceRecordDelegate = (ScriptingInterfaceOfISoundManager.StopVoiceRecordDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.StopVoiceRecordDelegate));
				return;
			case 1308:
				ScriptingInterfaceOfISoundManager.call_UnpauseBusDelegate = (ScriptingInterfaceOfISoundManager.UnpauseBusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.UnpauseBusDelegate));
				return;
			case 1309:
				ScriptingInterfaceOfISoundManager.call_UpdateVoiceToPlayDelegate = (ScriptingInterfaceOfISoundManager.UpdateVoiceToPlayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.UpdateVoiceToPlayDelegate));
				return;
			case 1310:
				ScriptingInterfaceOfISoundManager.call_UpdateXBOXChatCommunicationFlagsDelegate = (ScriptingInterfaceOfISoundManager.UpdateXBOXChatCommunicationFlagsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.UpdateXBOXChatCommunicationFlagsDelegate));
				return;
			case 1311:
				ScriptingInterfaceOfISoundManager.call_UpdateXBOXLocalUserDelegate = (ScriptingInterfaceOfISoundManager.UpdateXBOXLocalUserDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfISoundManager.UpdateXBOXLocalUserDelegate));
				return;
			case 1312:
				ScriptingInterfaceOfITableauView.call_CreateTableauViewDelegate = (ScriptingInterfaceOfITableauView.CreateTableauViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITableauView.CreateTableauViewDelegate));
				return;
			case 1313:
				ScriptingInterfaceOfITableauView.call_SetContinousRenderingDelegate = (ScriptingInterfaceOfITableauView.SetContinousRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITableauView.SetContinousRenderingDelegate));
				return;
			case 1314:
				ScriptingInterfaceOfITableauView.call_SetDeleteAfterRenderingDelegate = (ScriptingInterfaceOfITableauView.SetDeleteAfterRenderingDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITableauView.SetDeleteAfterRenderingDelegate));
				return;
			case 1315:
				ScriptingInterfaceOfITableauView.call_SetDoNotRenderThisFrameDelegate = (ScriptingInterfaceOfITableauView.SetDoNotRenderThisFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITableauView.SetDoNotRenderThisFrameDelegate));
				return;
			case 1316:
				ScriptingInterfaceOfITableauView.call_SetSortingEnabledDelegate = (ScriptingInterfaceOfITableauView.SetSortingEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITableauView.SetSortingEnabledDelegate));
				return;
			case 1317:
				ScriptingInterfaceOfITexture.call_CheckAndGetFromResourceDelegate = (ScriptingInterfaceOfITexture.CheckAndGetFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CheckAndGetFromResourceDelegate));
				return;
			case 1318:
				ScriptingInterfaceOfITexture.call_CreateDepthTargetDelegate = (ScriptingInterfaceOfITexture.CreateDepthTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CreateDepthTargetDelegate));
				return;
			case 1319:
				ScriptingInterfaceOfITexture.call_CreateFromByteArrayDelegate = (ScriptingInterfaceOfITexture.CreateFromByteArrayDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CreateFromByteArrayDelegate));
				return;
			case 1320:
				ScriptingInterfaceOfITexture.call_CreateFromMemoryDelegate = (ScriptingInterfaceOfITexture.CreateFromMemoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CreateFromMemoryDelegate));
				return;
			case 1321:
				ScriptingInterfaceOfITexture.call_CreateRenderTargetDelegate = (ScriptingInterfaceOfITexture.CreateRenderTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CreateRenderTargetDelegate));
				return;
			case 1322:
				ScriptingInterfaceOfITexture.call_CreateTextureFromPathDelegate = (ScriptingInterfaceOfITexture.CreateTextureFromPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.CreateTextureFromPathDelegate));
				return;
			case 1323:
				ScriptingInterfaceOfITexture.call_GetCurObjectDelegate = (ScriptingInterfaceOfITexture.GetCurObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetCurObjectDelegate));
				return;
			case 1324:
				ScriptingInterfaceOfITexture.call_GetFromResourceDelegate = (ScriptingInterfaceOfITexture.GetFromResourceDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetFromResourceDelegate));
				return;
			case 1325:
				ScriptingInterfaceOfITexture.call_GetHeightDelegate = (ScriptingInterfaceOfITexture.GetHeightDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetHeightDelegate));
				return;
			case 1326:
				ScriptingInterfaceOfITexture.call_GetMemorySizeDelegate = (ScriptingInterfaceOfITexture.GetMemorySizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetMemorySizeDelegate));
				return;
			case 1327:
				ScriptingInterfaceOfITexture.call_GetNameDelegate = (ScriptingInterfaceOfITexture.GetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetNameDelegate));
				return;
			case 1328:
				ScriptingInterfaceOfITexture.call_GetPixelDataDelegate = (ScriptingInterfaceOfITexture.GetPixelDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetPixelDataDelegate));
				return;
			case 1329:
				ScriptingInterfaceOfITexture.call_GetRenderTargetComponentDelegate = (ScriptingInterfaceOfITexture.GetRenderTargetComponentDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetRenderTargetComponentDelegate));
				return;
			case 1330:
				ScriptingInterfaceOfITexture.call_GetSDFBoundingBoxDataDelegate = (ScriptingInterfaceOfITexture.GetSDFBoundingBoxDataDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetSDFBoundingBoxDataDelegate));
				return;
			case 1331:
				ScriptingInterfaceOfITexture.call_GetTableauViewDelegate = (ScriptingInterfaceOfITexture.GetTableauViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetTableauViewDelegate));
				return;
			case 1332:
				ScriptingInterfaceOfITexture.call_GetWidthDelegate = (ScriptingInterfaceOfITexture.GetWidthDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.GetWidthDelegate));
				return;
			case 1333:
				ScriptingInterfaceOfITexture.call_IsLoadedDelegate = (ScriptingInterfaceOfITexture.IsLoadedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.IsLoadedDelegate));
				return;
			case 1334:
				ScriptingInterfaceOfITexture.call_IsRenderTargetDelegate = (ScriptingInterfaceOfITexture.IsRenderTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.IsRenderTargetDelegate));
				return;
			case 1335:
				ScriptingInterfaceOfITexture.call_LoadTextureFromPathDelegate = (ScriptingInterfaceOfITexture.LoadTextureFromPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.LoadTextureFromPathDelegate));
				return;
			case 1336:
				ScriptingInterfaceOfITexture.call_ReleaseDelegate = (ScriptingInterfaceOfITexture.ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.ReleaseDelegate));
				return;
			case 1337:
				ScriptingInterfaceOfITexture.call_ReleaseAfterNumberOfFramesDelegate = (ScriptingInterfaceOfITexture.ReleaseAfterNumberOfFramesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.ReleaseAfterNumberOfFramesDelegate));
				return;
			case 1338:
				ScriptingInterfaceOfITexture.call_ReleaseGpuMemoriesDelegate = (ScriptingInterfaceOfITexture.ReleaseGpuMemoriesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.ReleaseGpuMemoriesDelegate));
				return;
			case 1339:
				ScriptingInterfaceOfITexture.call_ReleaseNextFrameDelegate = (ScriptingInterfaceOfITexture.ReleaseNextFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.ReleaseNextFrameDelegate));
				return;
			case 1340:
				ScriptingInterfaceOfITexture.call_RemoveContinousTableauTextureDelegate = (ScriptingInterfaceOfITexture.RemoveContinousTableauTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.RemoveContinousTableauTextureDelegate));
				return;
			case 1341:
				ScriptingInterfaceOfITexture.call_SaveTextureAsAlwaysValidDelegate = (ScriptingInterfaceOfITexture.SaveTextureAsAlwaysValidDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.SaveTextureAsAlwaysValidDelegate));
				return;
			case 1342:
				ScriptingInterfaceOfITexture.call_SaveToFileDelegate = (ScriptingInterfaceOfITexture.SaveToFileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.SaveToFileDelegate));
				return;
			case 1343:
				ScriptingInterfaceOfITexture.call_SetNameDelegate = (ScriptingInterfaceOfITexture.SetNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.SetNameDelegate));
				return;
			case 1344:
				ScriptingInterfaceOfITexture.call_SetTableauViewDelegate = (ScriptingInterfaceOfITexture.SetTableauViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.SetTableauViewDelegate));
				return;
			case 1345:
				ScriptingInterfaceOfITexture.call_TransformRenderTargetToResourceTextureDelegate = (ScriptingInterfaceOfITexture.TransformRenderTargetToResourceTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITexture.TransformRenderTargetToResourceTextureDelegate));
				return;
			case 1346:
				ScriptingInterfaceOfITextureView.call_CreateTextureViewDelegate = (ScriptingInterfaceOfITextureView.CreateTextureViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITextureView.CreateTextureViewDelegate));
				return;
			case 1347:
				ScriptingInterfaceOfITextureView.call_SetTextureDelegate = (ScriptingInterfaceOfITextureView.SetTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITextureView.SetTextureDelegate));
				return;
			case 1348:
				ScriptingInterfaceOfIThumbnailCreatorView.call_CancelRequestDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.CancelRequestDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.CancelRequestDelegate));
				return;
			case 1349:
				ScriptingInterfaceOfIThumbnailCreatorView.call_ClearRequestsDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.ClearRequestsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.ClearRequestsDelegate));
				return;
			case 1350:
				ScriptingInterfaceOfIThumbnailCreatorView.call_CreateThumbnailCreatorViewDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.CreateThumbnailCreatorViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.CreateThumbnailCreatorViewDelegate));
				return;
			case 1351:
				ScriptingInterfaceOfIThumbnailCreatorView.call_GetNumberOfPendingRequestsDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.GetNumberOfPendingRequestsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.GetNumberOfPendingRequestsDelegate));
				return;
			case 1352:
				ScriptingInterfaceOfIThumbnailCreatorView.call_IsMemoryClearedDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.IsMemoryClearedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.IsMemoryClearedDelegate));
				return;
			case 1353:
				ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterCachedEntityDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.RegisterCachedEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.RegisterCachedEntityDelegate));
				return;
			case 1354:
				ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterRenderRequestDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.RegisterRenderRequestDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.RegisterRenderRequestDelegate));
				return;
			case 1355:
				ScriptingInterfaceOfIThumbnailCreatorView.call_RegisterSceneDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.RegisterSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.RegisterSceneDelegate));
				return;
			case 1356:
				ScriptingInterfaceOfIThumbnailCreatorView.call_UnregisterCachedEntityDelegate = (ScriptingInterfaceOfIThumbnailCreatorView.UnregisterCachedEntityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIThumbnailCreatorView.UnregisterCachedEntityDelegate));
				return;
			case 1357:
				ScriptingInterfaceOfITime.call_GetApplicationTimeDelegate = (ScriptingInterfaceOfITime.GetApplicationTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITime.GetApplicationTimeDelegate));
				return;
			case 1358:
				ScriptingInterfaceOfITwoDimensionView.call_AddCachedTextMeshDelegate = (ScriptingInterfaceOfITwoDimensionView.AddCachedTextMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.AddCachedTextMeshDelegate));
				return;
			case 1359:
				ScriptingInterfaceOfITwoDimensionView.call_AddNewMeshDelegate = (ScriptingInterfaceOfITwoDimensionView.AddNewMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.AddNewMeshDelegate));
				return;
			case 1360:
				ScriptingInterfaceOfITwoDimensionView.call_AddNewQuadMeshDelegate = (ScriptingInterfaceOfITwoDimensionView.AddNewQuadMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.AddNewQuadMeshDelegate));
				return;
			case 1361:
				ScriptingInterfaceOfITwoDimensionView.call_AddNewTextMeshDelegate = (ScriptingInterfaceOfITwoDimensionView.AddNewTextMeshDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.AddNewTextMeshDelegate));
				return;
			case 1362:
				ScriptingInterfaceOfITwoDimensionView.call_BeginFrameDelegate = (ScriptingInterfaceOfITwoDimensionView.BeginFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.BeginFrameDelegate));
				return;
			case 1363:
				ScriptingInterfaceOfITwoDimensionView.call_ClearDelegate = (ScriptingInterfaceOfITwoDimensionView.ClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.ClearDelegate));
				return;
			case 1364:
				ScriptingInterfaceOfITwoDimensionView.call_CreateTwoDimensionViewDelegate = (ScriptingInterfaceOfITwoDimensionView.CreateTwoDimensionViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.CreateTwoDimensionViewDelegate));
				return;
			case 1365:
				ScriptingInterfaceOfITwoDimensionView.call_EndFrameDelegate = (ScriptingInterfaceOfITwoDimensionView.EndFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.EndFrameDelegate));
				return;
			case 1366:
				ScriptingInterfaceOfITwoDimensionView.call_GetOrCreateMaterialDelegate = (ScriptingInterfaceOfITwoDimensionView.GetOrCreateMaterialDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITwoDimensionView.GetOrCreateMaterialDelegate));
				return;
			case 1367:
				ScriptingInterfaceOfIUtil.call_AddCommandLineFunctionDelegate = (ScriptingInterfaceOfIUtil.AddCommandLineFunctionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.AddCommandLineFunctionDelegate));
				return;
			case 1368:
				ScriptingInterfaceOfIUtil.call_AddMainThreadPerformanceQueryDelegate = (ScriptingInterfaceOfIUtil.AddMainThreadPerformanceQueryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.AddMainThreadPerformanceQueryDelegate));
				return;
			case 1369:
				ScriptingInterfaceOfIUtil.call_AddPerformanceReportTokenDelegate = (ScriptingInterfaceOfIUtil.AddPerformanceReportTokenDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.AddPerformanceReportTokenDelegate));
				return;
			case 1370:
				ScriptingInterfaceOfIUtil.call_AddSceneObjectReportDelegate = (ScriptingInterfaceOfIUtil.AddSceneObjectReportDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.AddSceneObjectReportDelegate));
				return;
			case 1371:
				ScriptingInterfaceOfIUtil.call_CheckIfAssetsAndSourcesAreSameDelegate = (ScriptingInterfaceOfIUtil.CheckIfAssetsAndSourcesAreSameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CheckIfAssetsAndSourcesAreSameDelegate));
				return;
			case 1372:
				ScriptingInterfaceOfIUtil.call_CheckIfTerrainShaderHeaderGenerationFinishedDelegate = (ScriptingInterfaceOfIUtil.CheckIfTerrainShaderHeaderGenerationFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CheckIfTerrainShaderHeaderGenerationFinishedDelegate));
				return;
			case 1373:
				ScriptingInterfaceOfIUtil.call_CheckResourceModificationsDelegate = (ScriptingInterfaceOfIUtil.CheckResourceModificationsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CheckResourceModificationsDelegate));
				return;
			case 1374:
				ScriptingInterfaceOfIUtil.call_CheckSceneForProblemsDelegate = (ScriptingInterfaceOfIUtil.CheckSceneForProblemsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CheckSceneForProblemsDelegate));
				return;
			case 1375:
				ScriptingInterfaceOfIUtil.call_CheckShaderCompilationDelegate = (ScriptingInterfaceOfIUtil.CheckShaderCompilationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CheckShaderCompilationDelegate));
				return;
			case 1376:
				ScriptingInterfaceOfIUtil.call_clear_decal_atlasDelegate = (ScriptingInterfaceOfIUtil.clear_decal_atlasDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.clear_decal_atlasDelegate));
				return;
			case 1377:
				ScriptingInterfaceOfIUtil.call_ClearOldResourcesAndObjectsDelegate = (ScriptingInterfaceOfIUtil.ClearOldResourcesAndObjectsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ClearOldResourcesAndObjectsDelegate));
				return;
			case 1378:
				ScriptingInterfaceOfIUtil.call_ClearShaderMemoryDelegate = (ScriptingInterfaceOfIUtil.ClearShaderMemoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ClearShaderMemoryDelegate));
				return;
			case 1379:
				ScriptingInterfaceOfIUtil.call_CommandLineArgumentExistsDelegate = (ScriptingInterfaceOfIUtil.CommandLineArgumentExistsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CommandLineArgumentExistsDelegate));
				return;
			case 1380:
				ScriptingInterfaceOfIUtil.call_CompileAllShadersDelegate = (ScriptingInterfaceOfIUtil.CompileAllShadersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CompileAllShadersDelegate));
				return;
			case 1381:
				ScriptingInterfaceOfIUtil.call_CompileTerrainShadersDistDelegate = (ScriptingInterfaceOfIUtil.CompileTerrainShadersDistDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CompileTerrainShadersDistDelegate));
				return;
			case 1382:
				ScriptingInterfaceOfIUtil.call_CreateSelectionInEditorDelegate = (ScriptingInterfaceOfIUtil.CreateSelectionInEditorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.CreateSelectionInEditorDelegate));
				return;
			case 1383:
				ScriptingInterfaceOfIUtil.call_DebugSetGlobalLoadingWindowStateDelegate = (ScriptingInterfaceOfIUtil.DebugSetGlobalLoadingWindowStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DebugSetGlobalLoadingWindowStateDelegate));
				return;
			case 1384:
				ScriptingInterfaceOfIUtil.call_DeleteEntitiesInEditorSceneDelegate = (ScriptingInterfaceOfIUtil.DeleteEntitiesInEditorSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DeleteEntitiesInEditorSceneDelegate));
				return;
			case 1385:
				ScriptingInterfaceOfIUtil.call_DetachWatchdogDelegate = (ScriptingInterfaceOfIUtil.DetachWatchdogDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DetachWatchdogDelegate));
				return;
			case 1386:
				ScriptingInterfaceOfIUtil.call_DidAutomatedGIBakeFinishedDelegate = (ScriptingInterfaceOfIUtil.DidAutomatedGIBakeFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DidAutomatedGIBakeFinishedDelegate));
				return;
			case 1387:
				ScriptingInterfaceOfIUtil.call_DisableCoreGameDelegate = (ScriptingInterfaceOfIUtil.DisableCoreGameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DisableCoreGameDelegate));
				return;
			case 1388:
				ScriptingInterfaceOfIUtil.call_DisableGlobalEditDataCacherDelegate = (ScriptingInterfaceOfIUtil.DisableGlobalEditDataCacherDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DisableGlobalEditDataCacherDelegate));
				return;
			case 1389:
				ScriptingInterfaceOfIUtil.call_DisableGlobalLoadingWindowDelegate = (ScriptingInterfaceOfIUtil.DisableGlobalLoadingWindowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DisableGlobalLoadingWindowDelegate));
				return;
			case 1390:
				ScriptingInterfaceOfIUtil.call_DoDelayedexitDelegate = (ScriptingInterfaceOfIUtil.DoDelayedexitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DoDelayedexitDelegate));
				return;
			case 1391:
				ScriptingInterfaceOfIUtil.call_DoFullBakeAllLevelsAutomatedDelegate = (ScriptingInterfaceOfIUtil.DoFullBakeAllLevelsAutomatedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DoFullBakeAllLevelsAutomatedDelegate));
				return;
			case 1392:
				ScriptingInterfaceOfIUtil.call_DoFullBakeSingleLevelAutomatedDelegate = (ScriptingInterfaceOfIUtil.DoFullBakeSingleLevelAutomatedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DoFullBakeSingleLevelAutomatedDelegate));
				return;
			case 1393:
				ScriptingInterfaceOfIUtil.call_DoLightOnlyBakeAllLevelsAutomatedDelegate = (ScriptingInterfaceOfIUtil.DoLightOnlyBakeAllLevelsAutomatedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DoLightOnlyBakeAllLevelsAutomatedDelegate));
				return;
			case 1394:
				ScriptingInterfaceOfIUtil.call_DoLightOnlyBakeSingleLevelAutomatedDelegate = (ScriptingInterfaceOfIUtil.DoLightOnlyBakeSingleLevelAutomatedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DoLightOnlyBakeSingleLevelAutomatedDelegate));
				return;
			case 1395:
				ScriptingInterfaceOfIUtil.call_DumpGPUMemoryStatisticsDelegate = (ScriptingInterfaceOfIUtil.DumpGPUMemoryStatisticsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.DumpGPUMemoryStatisticsDelegate));
				return;
			case 1396:
				ScriptingInterfaceOfIUtil.call_EnableGlobalEditDataCacherDelegate = (ScriptingInterfaceOfIUtil.EnableGlobalEditDataCacherDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.EnableGlobalEditDataCacherDelegate));
				return;
			case 1397:
				ScriptingInterfaceOfIUtil.call_EnableGlobalLoadingWindowDelegate = (ScriptingInterfaceOfIUtil.EnableGlobalLoadingWindowDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.EnableGlobalLoadingWindowDelegate));
				return;
			case 1398:
				ScriptingInterfaceOfIUtil.call_EnableSingleGPUQueryPerFrameDelegate = (ScriptingInterfaceOfIUtil.EnableSingleGPUQueryPerFrameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.EnableSingleGPUQueryPerFrameDelegate));
				return;
			case 1399:
				ScriptingInterfaceOfIUtil.call_EndLoadingStuckCheckStateDelegate = (ScriptingInterfaceOfIUtil.EndLoadingStuckCheckStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.EndLoadingStuckCheckStateDelegate));
				return;
			case 1400:
				ScriptingInterfaceOfIUtil.call_ExecuteCommandLineCommandDelegate = (ScriptingInterfaceOfIUtil.ExecuteCommandLineCommandDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ExecuteCommandLineCommandDelegate));
				return;
			case 1401:
				ScriptingInterfaceOfIUtil.call_ExitProcessDelegate = (ScriptingInterfaceOfIUtil.ExitProcessDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ExitProcessDelegate));
				return;
			case 1402:
				ScriptingInterfaceOfIUtil.call_ExportNavMeshFaceMarksDelegate = (ScriptingInterfaceOfIUtil.ExportNavMeshFaceMarksDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ExportNavMeshFaceMarksDelegate));
				return;
			case 1403:
				ScriptingInterfaceOfIUtil.call_FindMeshesWithoutLodsDelegate = (ScriptingInterfaceOfIUtil.FindMeshesWithoutLodsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.FindMeshesWithoutLodsDelegate));
				return;
			case 1404:
				ScriptingInterfaceOfIUtil.call_FlushManagedObjectsMemoryDelegate = (ScriptingInterfaceOfIUtil.FlushManagedObjectsMemoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.FlushManagedObjectsMemoryDelegate));
				return;
			case 1405:
				ScriptingInterfaceOfIUtil.call_GatherCoreGameReferencesDelegate = (ScriptingInterfaceOfIUtil.GatherCoreGameReferencesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GatherCoreGameReferencesDelegate));
				return;
			case 1406:
				ScriptingInterfaceOfIUtil.call_GenerateTerrainShaderHeadersDelegate = (ScriptingInterfaceOfIUtil.GenerateTerrainShaderHeadersDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GenerateTerrainShaderHeadersDelegate));
				return;
			case 1407:
				ScriptingInterfaceOfIUtil.call_GetApplicationMemoryDelegate = (ScriptingInterfaceOfIUtil.GetApplicationMemoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetApplicationMemoryDelegate));
				return;
			case 1408:
				ScriptingInterfaceOfIUtil.call_GetApplicationMemoryStatisticsDelegate = (ScriptingInterfaceOfIUtil.GetApplicationMemoryStatisticsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetApplicationMemoryStatisticsDelegate));
				return;
			case 1409:
				ScriptingInterfaceOfIUtil.call_GetApplicationNameDelegate = (ScriptingInterfaceOfIUtil.GetApplicationNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetApplicationNameDelegate));
				return;
			case 1410:
				ScriptingInterfaceOfIUtil.call_GetAttachmentsPathDelegate = (ScriptingInterfaceOfIUtil.GetAttachmentsPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetAttachmentsPathDelegate));
				return;
			case 1411:
				ScriptingInterfaceOfIUtil.call_GetBaseDirectoryDelegate = (ScriptingInterfaceOfIUtil.GetBaseDirectoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetBaseDirectoryDelegate));
				return;
			case 1412:
				ScriptingInterfaceOfIUtil.call_GetBenchmarkStatusDelegate = (ScriptingInterfaceOfIUtil.GetBenchmarkStatusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetBenchmarkStatusDelegate));
				return;
			case 1413:
				ScriptingInterfaceOfIUtil.call_GetBuildNumberDelegate = (ScriptingInterfaceOfIUtil.GetBuildNumberDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetBuildNumberDelegate));
				return;
			case 1414:
				ScriptingInterfaceOfIUtil.call_GetConsoleHostMachineDelegate = (ScriptingInterfaceOfIUtil.GetConsoleHostMachineDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetConsoleHostMachineDelegate));
				return;
			case 1415:
				ScriptingInterfaceOfIUtil.call_GetCoreGameStateDelegate = (ScriptingInterfaceOfIUtil.GetCoreGameStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetCoreGameStateDelegate));
				return;
			case 1416:
				ScriptingInterfaceOfIUtil.call_GetCurrentCpuMemoryUsageDelegate = (ScriptingInterfaceOfIUtil.GetCurrentCpuMemoryUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetCurrentCpuMemoryUsageDelegate));
				return;
			case 1417:
				ScriptingInterfaceOfIUtil.call_GetCurrentEstimatedGPUMemoryCostMBDelegate = (ScriptingInterfaceOfIUtil.GetCurrentEstimatedGPUMemoryCostMBDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetCurrentEstimatedGPUMemoryCostMBDelegate));
				return;
			case 1418:
				ScriptingInterfaceOfIUtil.call_GetCurrentProcessIDDelegate = (ScriptingInterfaceOfIUtil.GetCurrentProcessIDDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetCurrentProcessIDDelegate));
				return;
			case 1419:
				ScriptingInterfaceOfIUtil.call_GetCurrentThreadIdDelegate = (ScriptingInterfaceOfIUtil.GetCurrentThreadIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetCurrentThreadIdDelegate));
				return;
			case 1420:
				ScriptingInterfaceOfIUtil.call_GetDeltaTimeDelegate = (ScriptingInterfaceOfIUtil.GetDeltaTimeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetDeltaTimeDelegate));
				return;
			case 1421:
				ScriptingInterfaceOfIUtil.call_GetDetailedGPUBufferMemoryStatsDelegate = (ScriptingInterfaceOfIUtil.GetDetailedGPUBufferMemoryStatsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetDetailedGPUBufferMemoryStatsDelegate));
				return;
			case 1422:
				ScriptingInterfaceOfIUtil.call_GetDetailedXBOXMemoryInfoDelegate = (ScriptingInterfaceOfIUtil.GetDetailedXBOXMemoryInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetDetailedXBOXMemoryInfoDelegate));
				return;
			case 1423:
				ScriptingInterfaceOfIUtil.call_GetEditorSelectedEntitiesDelegate = (ScriptingInterfaceOfIUtil.GetEditorSelectedEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetEditorSelectedEntitiesDelegate));
				return;
			case 1424:
				ScriptingInterfaceOfIUtil.call_GetEditorSelectedEntityCountDelegate = (ScriptingInterfaceOfIUtil.GetEditorSelectedEntityCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetEditorSelectedEntityCountDelegate));
				return;
			case 1425:
				ScriptingInterfaceOfIUtil.call_GetEngineFrameNoDelegate = (ScriptingInterfaceOfIUtil.GetEngineFrameNoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetEngineFrameNoDelegate));
				return;
			case 1426:
				ScriptingInterfaceOfIUtil.call_GetEntitiesOfSelectionSetDelegate = (ScriptingInterfaceOfIUtil.GetEntitiesOfSelectionSetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetEntitiesOfSelectionSetDelegate));
				return;
			case 1427:
				ScriptingInterfaceOfIUtil.call_GetEntityCountOfSelectionSetDelegate = (ScriptingInterfaceOfIUtil.GetEntityCountOfSelectionSetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetEntityCountOfSelectionSetDelegate));
				return;
			case 1428:
				ScriptingInterfaceOfIUtil.call_GetExecutableWorkingDirectoryDelegate = (ScriptingInterfaceOfIUtil.GetExecutableWorkingDirectoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetExecutableWorkingDirectoryDelegate));
				return;
			case 1429:
				ScriptingInterfaceOfIUtil.call_GetFpsDelegate = (ScriptingInterfaceOfIUtil.GetFpsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFpsDelegate));
				return;
			case 1430:
				ScriptingInterfaceOfIUtil.call_GetFrameLimiterWithSleepDelegate = (ScriptingInterfaceOfIUtil.GetFrameLimiterWithSleepDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFrameLimiterWithSleepDelegate));
				return;
			case 1431:
				ScriptingInterfaceOfIUtil.call_GetFullCommandLineStringDelegate = (ScriptingInterfaceOfIUtil.GetFullCommandLineStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFullCommandLineStringDelegate));
				return;
			case 1432:
				ScriptingInterfaceOfIUtil.call_GetFullFilePathOfSceneDelegate = (ScriptingInterfaceOfIUtil.GetFullFilePathOfSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFullFilePathOfSceneDelegate));
				return;
			case 1433:
				ScriptingInterfaceOfIUtil.call_GetFullModulePathDelegate = (ScriptingInterfaceOfIUtil.GetFullModulePathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFullModulePathDelegate));
				return;
			case 1434:
				ScriptingInterfaceOfIUtil.call_GetFullModulePathsDelegate = (ScriptingInterfaceOfIUtil.GetFullModulePathsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetFullModulePathsDelegate));
				return;
			case 1435:
				ScriptingInterfaceOfIUtil.call_GetGPUMemoryMBDelegate = (ScriptingInterfaceOfIUtil.GetGPUMemoryMBDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetGPUMemoryMBDelegate));
				return;
			case 1436:
				ScriptingInterfaceOfIUtil.call_GetGpuMemoryOfAllocationGroupDelegate = (ScriptingInterfaceOfIUtil.GetGpuMemoryOfAllocationGroupDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetGpuMemoryOfAllocationGroupDelegate));
				return;
			case 1437:
				ScriptingInterfaceOfIUtil.call_GetGPUMemoryStatsDelegate = (ScriptingInterfaceOfIUtil.GetGPUMemoryStatsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetGPUMemoryStatsDelegate));
				return;
			case 1438:
				ScriptingInterfaceOfIUtil.call_GetLocalOutputPathDelegate = (ScriptingInterfaceOfIUtil.GetLocalOutputPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetLocalOutputPathDelegate));
				return;
			case 1439:
				ScriptingInterfaceOfIUtil.call_GetMainFpsDelegate = (ScriptingInterfaceOfIUtil.GetMainFpsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetMainFpsDelegate));
				return;
			case 1440:
				ScriptingInterfaceOfIUtil.call_GetMainThreadIdDelegate = (ScriptingInterfaceOfIUtil.GetMainThreadIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetMainThreadIdDelegate));
				return;
			case 1441:
				ScriptingInterfaceOfIUtil.call_GetMemoryUsageOfCategoryDelegate = (ScriptingInterfaceOfIUtil.GetMemoryUsageOfCategoryDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetMemoryUsageOfCategoryDelegate));
				return;
			case 1442:
				ScriptingInterfaceOfIUtil.call_GetModulesCodeDelegate = (ScriptingInterfaceOfIUtil.GetModulesCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetModulesCodeDelegate));
				return;
			case 1443:
				ScriptingInterfaceOfIUtil.call_GetNativeMemoryStatisticsDelegate = (ScriptingInterfaceOfIUtil.GetNativeMemoryStatisticsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetNativeMemoryStatisticsDelegate));
				return;
			case 1444:
				ScriptingInterfaceOfIUtil.call_GetNumberOfShaderCompilationsInProgressDelegate = (ScriptingInterfaceOfIUtil.GetNumberOfShaderCompilationsInProgressDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetNumberOfShaderCompilationsInProgressDelegate));
				return;
			case 1445:
				ScriptingInterfaceOfIUtil.call_GetPCInfoDelegate = (ScriptingInterfaceOfIUtil.GetPCInfoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetPCInfoDelegate));
				return;
			case 1446:
				ScriptingInterfaceOfIUtil.call_GetPlatformModulePathsDelegate = (ScriptingInterfaceOfIUtil.GetPlatformModulePathsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetPlatformModulePathsDelegate));
				return;
			case 1447:
				ScriptingInterfaceOfIUtil.call_GetPossibleCommandLineStartingWithDelegate = (ScriptingInterfaceOfIUtil.GetPossibleCommandLineStartingWithDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetPossibleCommandLineStartingWithDelegate));
				return;
			case 1448:
				ScriptingInterfaceOfIUtil.call_GetRendererFpsDelegate = (ScriptingInterfaceOfIUtil.GetRendererFpsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetRendererFpsDelegate));
				return;
			case 1449:
				ScriptingInterfaceOfIUtil.call_GetReturnCodeDelegate = (ScriptingInterfaceOfIUtil.GetReturnCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetReturnCodeDelegate));
				return;
			case 1450:
				ScriptingInterfaceOfIUtil.call_GetSingleModuleScenesOfModuleDelegate = (ScriptingInterfaceOfIUtil.GetSingleModuleScenesOfModuleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetSingleModuleScenesOfModuleDelegate));
				return;
			case 1451:
				ScriptingInterfaceOfIUtil.call_GetSteamAppIdDelegate = (ScriptingInterfaceOfIUtil.GetSteamAppIdDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetSteamAppIdDelegate));
				return;
			case 1452:
				ScriptingInterfaceOfIUtil.call_GetSystemLanguageDelegate = (ScriptingInterfaceOfIUtil.GetSystemLanguageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetSystemLanguageDelegate));
				return;
			case 1453:
				ScriptingInterfaceOfIUtil.call_GetVertexBufferChunkSystemMemoryUsageDelegate = (ScriptingInterfaceOfIUtil.GetVertexBufferChunkSystemMemoryUsageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetVertexBufferChunkSystemMemoryUsageDelegate));
				return;
			case 1454:
				ScriptingInterfaceOfIUtil.call_GetVisualTestsTestFilesPathDelegate = (ScriptingInterfaceOfIUtil.GetVisualTestsTestFilesPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetVisualTestsTestFilesPathDelegate));
				return;
			case 1455:
				ScriptingInterfaceOfIUtil.call_GetVisualTestsValidatePathDelegate = (ScriptingInterfaceOfIUtil.GetVisualTestsValidatePathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.GetVisualTestsValidatePathDelegate));
				return;
			case 1456:
				ScriptingInterfaceOfIUtil.call_IsAsyncPhysicsThreadDelegate = (ScriptingInterfaceOfIUtil.IsAsyncPhysicsThreadDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsAsyncPhysicsThreadDelegate));
				return;
			case 1457:
				ScriptingInterfaceOfIUtil.call_IsBenchmarkQuitedDelegate = (ScriptingInterfaceOfIUtil.IsBenchmarkQuitedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsBenchmarkQuitedDelegate));
				return;
			case 1458:
				ScriptingInterfaceOfIUtil.call_IsDetailedSoundLogOnDelegate = (ScriptingInterfaceOfIUtil.IsDetailedSoundLogOnDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsDetailedSoundLogOnDelegate));
				return;
			case 1459:
				ScriptingInterfaceOfIUtil.call_IsDevkitDelegate = (ScriptingInterfaceOfIUtil.IsDevkitDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsDevkitDelegate));
				return;
			case 1460:
				ScriptingInterfaceOfIUtil.call_IsEditModeEnabledDelegate = (ScriptingInterfaceOfIUtil.IsEditModeEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsEditModeEnabledDelegate));
				return;
			case 1461:
				ScriptingInterfaceOfIUtil.call_IsLockhartPlatformDelegate = (ScriptingInterfaceOfIUtil.IsLockhartPlatformDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsLockhartPlatformDelegate));
				return;
			case 1462:
				ScriptingInterfaceOfIUtil.call_IsSceneReportFinishedDelegate = (ScriptingInterfaceOfIUtil.IsSceneReportFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.IsSceneReportFinishedDelegate));
				return;
			case 1463:
				ScriptingInterfaceOfIUtil.call_LoadSkyBoxesDelegate = (ScriptingInterfaceOfIUtil.LoadSkyBoxesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.LoadSkyBoxesDelegate));
				return;
			case 1464:
				ScriptingInterfaceOfIUtil.call_LoadVirtualTextureTilesetDelegate = (ScriptingInterfaceOfIUtil.LoadVirtualTextureTilesetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.LoadVirtualTextureTilesetDelegate));
				return;
			case 1465:
				ScriptingInterfaceOfIUtil.call_ManagedParallelForDelegate = (ScriptingInterfaceOfIUtil.ManagedParallelForDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ManagedParallelForDelegate));
				return;
			case 1466:
				ScriptingInterfaceOfIUtil.call_ManagedParallelForWithDtDelegate = (ScriptingInterfaceOfIUtil.ManagedParallelForWithDtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ManagedParallelForWithDtDelegate));
				return;
			case 1467:
				ScriptingInterfaceOfIUtil.call_ManagedParallelForWithoutRenderThreadDelegate = (ScriptingInterfaceOfIUtil.ManagedParallelForWithoutRenderThreadDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ManagedParallelForWithoutRenderThreadDelegate));
				return;
			case 1468:
				ScriptingInterfaceOfIUtil.call_OnLoadingWindowDisabledDelegate = (ScriptingInterfaceOfIUtil.OnLoadingWindowDisabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OnLoadingWindowDisabledDelegate));
				return;
			case 1469:
				ScriptingInterfaceOfIUtil.call_OnLoadingWindowEnabledDelegate = (ScriptingInterfaceOfIUtil.OnLoadingWindowEnabledDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OnLoadingWindowEnabledDelegate));
				return;
			case 1470:
				ScriptingInterfaceOfIUtil.call_OpenNavalDlcPurchasePageDelegate = (ScriptingInterfaceOfIUtil.OpenNavalDlcPurchasePageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OpenNavalDlcPurchasePageDelegate));
				return;
			case 1471:
				ScriptingInterfaceOfIUtil.call_OpenOnscreenKeyboardDelegate = (ScriptingInterfaceOfIUtil.OpenOnscreenKeyboardDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OpenOnscreenKeyboardDelegate));
				return;
			case 1472:
				ScriptingInterfaceOfIUtil.call_OutputBenchmarkValuesToPerformanceReporterDelegate = (ScriptingInterfaceOfIUtil.OutputBenchmarkValuesToPerformanceReporterDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OutputBenchmarkValuesToPerformanceReporterDelegate));
				return;
			case 1473:
				ScriptingInterfaceOfIUtil.call_OutputPerformanceReportsDelegate = (ScriptingInterfaceOfIUtil.OutputPerformanceReportsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.OutputPerformanceReportsDelegate));
				return;
			case 1474:
				ScriptingInterfaceOfIUtil.call_PairSceneNameToModuleNameDelegate = (ScriptingInterfaceOfIUtil.PairSceneNameToModuleNameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.PairSceneNameToModuleNameDelegate));
				return;
			case 1475:
				ScriptingInterfaceOfIUtil.call_ProcessWindowTitleDelegate = (ScriptingInterfaceOfIUtil.ProcessWindowTitleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ProcessWindowTitleDelegate));
				return;
			case 1476:
				ScriptingInterfaceOfIUtil.call_QuitGameDelegate = (ScriptingInterfaceOfIUtil.QuitGameDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.QuitGameDelegate));
				return;
			case 1477:
				ScriptingInterfaceOfIUtil.call_RegisterGPUAllocationGroupDelegate = (ScriptingInterfaceOfIUtil.RegisterGPUAllocationGroupDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.RegisterGPUAllocationGroupDelegate));
				return;
			case 1478:
				ScriptingInterfaceOfIUtil.call_RegisterMeshForGPUMorphDelegate = (ScriptingInterfaceOfIUtil.RegisterMeshForGPUMorphDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.RegisterMeshForGPUMorphDelegate));
				return;
			case 1479:
				ScriptingInterfaceOfIUtil.call_SaveDataAsTextureDelegate = (ScriptingInterfaceOfIUtil.SaveDataAsTextureDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SaveDataAsTextureDelegate));
				return;
			case 1480:
				ScriptingInterfaceOfIUtil.call_SelectEntitiesDelegate = (ScriptingInterfaceOfIUtil.SelectEntitiesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SelectEntitiesDelegate));
				return;
			case 1481:
				ScriptingInterfaceOfIUtil.call_SetAllocationAlwaysValidSceneDelegate = (ScriptingInterfaceOfIUtil.SetAllocationAlwaysValidSceneDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetAllocationAlwaysValidSceneDelegate));
				return;
			case 1482:
				ScriptingInterfaceOfIUtil.call_SetAssertionAtShaderCompileDelegate = (ScriptingInterfaceOfIUtil.SetAssertionAtShaderCompileDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetAssertionAtShaderCompileDelegate));
				return;
			case 1483:
				ScriptingInterfaceOfIUtil.call_SetAssertionsAndWarningsSetExitCodeDelegate = (ScriptingInterfaceOfIUtil.SetAssertionsAndWarningsSetExitCodeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetAssertionsAndWarningsSetExitCodeDelegate));
				return;
			case 1484:
				ScriptingInterfaceOfIUtil.call_SetBenchmarkStatusDelegate = (ScriptingInterfaceOfIUtil.SetBenchmarkStatusDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetBenchmarkStatusDelegate));
				return;
			case 1485:
				ScriptingInterfaceOfIUtil.call_SetCanLoadModulesDelegate = (ScriptingInterfaceOfIUtil.SetCanLoadModulesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCanLoadModulesDelegate));
				return;
			case 1486:
				ScriptingInterfaceOfIUtil.call_SetCoreGameStateDelegate = (ScriptingInterfaceOfIUtil.SetCoreGameStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCoreGameStateDelegate));
				return;
			case 1487:
				ScriptingInterfaceOfIUtil.call_SetCrashOnAssertsDelegate = (ScriptingInterfaceOfIUtil.SetCrashOnAssertsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCrashOnAssertsDelegate));
				return;
			case 1488:
				ScriptingInterfaceOfIUtil.call_SetCrashOnWarningsDelegate = (ScriptingInterfaceOfIUtil.SetCrashOnWarningsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCrashOnWarningsDelegate));
				return;
			case 1489:
				ScriptingInterfaceOfIUtil.call_SetCrashReportCustomStackDelegate = (ScriptingInterfaceOfIUtil.SetCrashReportCustomStackDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCrashReportCustomStackDelegate));
				return;
			case 1490:
				ScriptingInterfaceOfIUtil.call_SetCrashReportCustomStringDelegate = (ScriptingInterfaceOfIUtil.SetCrashReportCustomStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCrashReportCustomStringDelegate));
				return;
			case 1491:
				ScriptingInterfaceOfIUtil.call_SetCreateDumpOnWarningsDelegate = (ScriptingInterfaceOfIUtil.SetCreateDumpOnWarningsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetCreateDumpOnWarningsDelegate));
				return;
			case 1492:
				ScriptingInterfaceOfIUtil.call_SetDisableDumpGenerationDelegate = (ScriptingInterfaceOfIUtil.SetDisableDumpGenerationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetDisableDumpGenerationDelegate));
				return;
			case 1493:
				ScriptingInterfaceOfIUtil.call_SetDumpFolderPathDelegate = (ScriptingInterfaceOfIUtil.SetDumpFolderPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetDumpFolderPathDelegate));
				return;
			case 1494:
				ScriptingInterfaceOfIUtil.call_SetFixedDtDelegate = (ScriptingInterfaceOfIUtil.SetFixedDtDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetFixedDtDelegate));
				return;
			case 1495:
				ScriptingInterfaceOfIUtil.call_SetForceDrawEntityIDDelegate = (ScriptingInterfaceOfIUtil.SetForceDrawEntityIDDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetForceDrawEntityIDDelegate));
				return;
			case 1496:
				ScriptingInterfaceOfIUtil.call_SetForceVsyncDelegate = (ScriptingInterfaceOfIUtil.SetForceVsyncDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetForceVsyncDelegate));
				return;
			case 1497:
				ScriptingInterfaceOfIUtil.call_SetFrameLimiterWithSleepDelegate = (ScriptingInterfaceOfIUtil.SetFrameLimiterWithSleepDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetFrameLimiterWithSleepDelegate));
				return;
			case 1498:
				ScriptingInterfaceOfIUtil.call_SetGraphicsPresetDelegate = (ScriptingInterfaceOfIUtil.SetGraphicsPresetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetGraphicsPresetDelegate));
				return;
			case 1499:
				ScriptingInterfaceOfIUtil.call_SetLoadingScreenPercentageDelegate = (ScriptingInterfaceOfIUtil.SetLoadingScreenPercentageDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetLoadingScreenPercentageDelegate));
				return;
			case 1500:
				ScriptingInterfaceOfIUtil.call_SetMessageLineRenderingStateDelegate = (ScriptingInterfaceOfIUtil.SetMessageLineRenderingStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetMessageLineRenderingStateDelegate));
				return;
			case 1501:
				ScriptingInterfaceOfIUtil.call_SetPrintCallstackAtCrahsesDelegate = (ScriptingInterfaceOfIUtil.SetPrintCallstackAtCrahsesDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetPrintCallstackAtCrahsesDelegate));
				return;
			case 1502:
				ScriptingInterfaceOfIUtil.call_SetRenderAgentsDelegate = (ScriptingInterfaceOfIUtil.SetRenderAgentsDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetRenderAgentsDelegate));
				return;
			case 1503:
				ScriptingInterfaceOfIUtil.call_SetRenderModeDelegate = (ScriptingInterfaceOfIUtil.SetRenderModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetRenderModeDelegate));
				return;
			case 1504:
				ScriptingInterfaceOfIUtil.call_SetReportModeDelegate = (ScriptingInterfaceOfIUtil.SetReportModeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetReportModeDelegate));
				return;
			case 1505:
				ScriptingInterfaceOfIUtil.call_SetScreenTextRenderingStateDelegate = (ScriptingInterfaceOfIUtil.SetScreenTextRenderingStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetScreenTextRenderingStateDelegate));
				return;
			case 1506:
				ScriptingInterfaceOfIUtil.call_SetWatchdogAutoreportDelegate = (ScriptingInterfaceOfIUtil.SetWatchdogAutoreportDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetWatchdogAutoreportDelegate));
				return;
			case 1507:
				ScriptingInterfaceOfIUtil.call_SetWatchdogValueDelegate = (ScriptingInterfaceOfIUtil.SetWatchdogValueDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetWatchdogValueDelegate));
				return;
			case 1508:
				ScriptingInterfaceOfIUtil.call_SetWindowTitleDelegate = (ScriptingInterfaceOfIUtil.SetWindowTitleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.SetWindowTitleDelegate));
				return;
			case 1509:
				ScriptingInterfaceOfIUtil.call_StartLoadingStuckCheckStateDelegate = (ScriptingInterfaceOfIUtil.StartLoadingStuckCheckStateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.StartLoadingStuckCheckStateDelegate));
				return;
			case 1510:
				ScriptingInterfaceOfIUtil.call_StartScenePerformanceReportDelegate = (ScriptingInterfaceOfIUtil.StartScenePerformanceReportDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.StartScenePerformanceReportDelegate));
				return;
			case 1511:
				ScriptingInterfaceOfIUtil.call_TakeScreenshotFromPlatformPathDelegate = (ScriptingInterfaceOfIUtil.TakeScreenshotFromPlatformPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.TakeScreenshotFromPlatformPathDelegate));
				return;
			case 1512:
				ScriptingInterfaceOfIUtil.call_TakeScreenshotFromStringPathDelegate = (ScriptingInterfaceOfIUtil.TakeScreenshotFromStringPathDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.TakeScreenshotFromStringPathDelegate));
				return;
			case 1513:
				ScriptingInterfaceOfIUtil.call_TakeSSFromTopDelegate = (ScriptingInterfaceOfIUtil.TakeSSFromTopDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.TakeSSFromTopDelegate));
				return;
			case 1514:
				ScriptingInterfaceOfIUtil.call_ToggleRenderDelegate = (ScriptingInterfaceOfIUtil.ToggleRenderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIUtil.ToggleRenderDelegate));
				return;
			case 1515:
				ScriptingInterfaceOfIVideoPlayerView.call_CreateVideoPlayerViewDelegate = (ScriptingInterfaceOfIVideoPlayerView.CreateVideoPlayerViewDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIVideoPlayerView.CreateVideoPlayerViewDelegate));
				return;
			case 1516:
				ScriptingInterfaceOfIVideoPlayerView.call_FinalizeDelegate = (ScriptingInterfaceOfIVideoPlayerView.FinalizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIVideoPlayerView.FinalizeDelegate));
				return;
			case 1517:
				ScriptingInterfaceOfIVideoPlayerView.call_IsVideoFinishedDelegate = (ScriptingInterfaceOfIVideoPlayerView.IsVideoFinishedDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIVideoPlayerView.IsVideoFinishedDelegate));
				return;
			case 1518:
				ScriptingInterfaceOfIVideoPlayerView.call_PlayVideoDelegate = (ScriptingInterfaceOfIVideoPlayerView.PlayVideoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIVideoPlayerView.PlayVideoDelegate));
				return;
			case 1519:
				ScriptingInterfaceOfIVideoPlayerView.call_StopVideoDelegate = (ScriptingInterfaceOfIVideoPlayerView.StopVideoDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIVideoPlayerView.StopVideoDelegate));
				return;
			case 1520:
				ScriptingInterfaceOfIView.call_SetAutoDepthTargetCreationDelegate = (ScriptingInterfaceOfIView.SetAutoDepthTargetCreationDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetAutoDepthTargetCreationDelegate));
				return;
			case 1521:
				ScriptingInterfaceOfIView.call_SetClearColorDelegate = (ScriptingInterfaceOfIView.SetClearColorDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetClearColorDelegate));
				return;
			case 1522:
				ScriptingInterfaceOfIView.call_SetDebugRenderFunctionalityDelegate = (ScriptingInterfaceOfIView.SetDebugRenderFunctionalityDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetDebugRenderFunctionalityDelegate));
				return;
			case 1523:
				ScriptingInterfaceOfIView.call_SetDepthTargetDelegate = (ScriptingInterfaceOfIView.SetDepthTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetDepthTargetDelegate));
				return;
			case 1524:
				ScriptingInterfaceOfIView.call_SetEnableDelegate = (ScriptingInterfaceOfIView.SetEnableDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetEnableDelegate));
				return;
			case 1525:
				ScriptingInterfaceOfIView.call_SetFileNameToSaveResultDelegate = (ScriptingInterfaceOfIView.SetFileNameToSaveResultDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetFileNameToSaveResultDelegate));
				return;
			case 1526:
				ScriptingInterfaceOfIView.call_SetFilePathToSaveResultDelegate = (ScriptingInterfaceOfIView.SetFilePathToSaveResultDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetFilePathToSaveResultDelegate));
				return;
			case 1527:
				ScriptingInterfaceOfIView.call_SetFileTypeToSaveDelegate = (ScriptingInterfaceOfIView.SetFileTypeToSaveDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetFileTypeToSaveDelegate));
				return;
			case 1528:
				ScriptingInterfaceOfIView.call_SetOffsetDelegate = (ScriptingInterfaceOfIView.SetOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetOffsetDelegate));
				return;
			case 1529:
				ScriptingInterfaceOfIView.call_SetRenderOnDemandDelegate = (ScriptingInterfaceOfIView.SetRenderOnDemandDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetRenderOnDemandDelegate));
				return;
			case 1530:
				ScriptingInterfaceOfIView.call_SetRenderOptionDelegate = (ScriptingInterfaceOfIView.SetRenderOptionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetRenderOptionDelegate));
				return;
			case 1531:
				ScriptingInterfaceOfIView.call_SetRenderOrderDelegate = (ScriptingInterfaceOfIView.SetRenderOrderDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetRenderOrderDelegate));
				return;
			case 1532:
				ScriptingInterfaceOfIView.call_SetRenderTargetDelegate = (ScriptingInterfaceOfIView.SetRenderTargetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetRenderTargetDelegate));
				return;
			case 1533:
				ScriptingInterfaceOfIView.call_SetSaveFinalResultToDiskDelegate = (ScriptingInterfaceOfIView.SetSaveFinalResultToDiskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetSaveFinalResultToDiskDelegate));
				return;
			case 1534:
				ScriptingInterfaceOfIView.call_SetScaleDelegate = (ScriptingInterfaceOfIView.SetScaleDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIView.SetScaleDelegate));
				return;
			default:
				return;
			}
		}

		// Token: 0x02000088 RID: 136
		private enum EngineInterfaceGeneratedEnum
		{
			// Token: 0x0400062D RID: 1581
			enm_IMono_AsyncTask_create_with_function,
			// Token: 0x0400062E RID: 1582
			enm_IMono_AsyncTask_invoke,
			// Token: 0x0400062F RID: 1583
			enm_IMono_AsyncTask_wait,
			// Token: 0x04000630 RID: 1584
			enm_IMono_BodyPart_do_segments_intersect,
			// Token: 0x04000631 RID: 1585
			enm_IMono_Camera_check_entity_visibility,
			// Token: 0x04000632 RID: 1586
			enm_IMono_Camera_construct_camera_from_position_elevation_bearing,
			// Token: 0x04000633 RID: 1587
			enm_IMono_Camera_create_camera,
			// Token: 0x04000634 RID: 1588
			enm_IMono_Camera_encloses_point,
			// Token: 0x04000635 RID: 1589
			enm_IMono_Camera_fill_parameters_from,
			// Token: 0x04000636 RID: 1590
			enm_IMono_Camera_get_aspect_ratio,
			// Token: 0x04000637 RID: 1591
			enm_IMono_Camera_get_entity,
			// Token: 0x04000638 RID: 1592
			enm_IMono_Camera_get_far,
			// Token: 0x04000639 RID: 1593
			enm_IMono_Camera_get_fov_horizontal,
			// Token: 0x0400063A RID: 1594
			enm_IMono_Camera_get_fov_vertical,
			// Token: 0x0400063B RID: 1595
			enm_IMono_Camera_get_frame,
			// Token: 0x0400063C RID: 1596
			enm_IMono_Camera_get_horizontal_fov,
			// Token: 0x0400063D RID: 1597
			enm_IMono_Camera_get_near,
			// Token: 0x0400063E RID: 1598
			enm_IMono_Camera_get_near_plane_points,
			// Token: 0x0400063F RID: 1599
			enm_IMono_Camera_get_near_plane_points_static,
			// Token: 0x04000640 RID: 1600
			enm_IMono_Camera_get_view_proj_matrix,
			// Token: 0x04000641 RID: 1601
			enm_IMono_Camera_look_at,
			// Token: 0x04000642 RID: 1602
			enm_IMono_Camera_release,
			// Token: 0x04000643 RID: 1603
			enm_IMono_Camera_release_camera_entity,
			// Token: 0x04000644 RID: 1604
			enm_IMono_Camera_render_frustrum,
			// Token: 0x04000645 RID: 1605
			enm_IMono_Camera_screen_space_ray_projection,
			// Token: 0x04000646 RID: 1606
			enm_IMono_Camera_set_entity,
			// Token: 0x04000647 RID: 1607
			enm_IMono_Camera_set_fov_horizontal,
			// Token: 0x04000648 RID: 1608
			enm_IMono_Camera_set_fov_vertical,
			// Token: 0x04000649 RID: 1609
			enm_IMono_Camera_set_frame,
			// Token: 0x0400064A RID: 1610
			enm_IMono_Camera_set_position,
			// Token: 0x0400064B RID: 1611
			enm_IMono_Camera_set_view_volume,
			// Token: 0x0400064C RID: 1612
			enm_IMono_Camera_viewport_point_to_world_ray,
			// Token: 0x0400064D RID: 1613
			enm_IMono_Camera_world_point_to_viewport_point,
			// Token: 0x0400064E RID: 1614
			enm_IMono_ClothSimulatorComponent_disable_forced_wind,
			// Token: 0x0400064F RID: 1615
			enm_IMono_ClothSimulatorComponent_disable_morph_animation,
			// Token: 0x04000650 RID: 1616
			enm_IMono_ClothSimulatorComponent_get_morph_anim_center_points,
			// Token: 0x04000651 RID: 1617
			enm_IMono_ClothSimulatorComponent_get_morph_anim_left_points,
			// Token: 0x04000652 RID: 1618
			enm_IMono_ClothSimulatorComponent_get_morph_anim_right_points,
			// Token: 0x04000653 RID: 1619
			enm_IMono_ClothSimulatorComponent_get_number_of_morph_keys,
			// Token: 0x04000654 RID: 1620
			enm_IMono_ClothSimulatorComponent_set_forced_gust_strength,
			// Token: 0x04000655 RID: 1621
			enm_IMono_ClothSimulatorComponent_set_forced_velocity,
			// Token: 0x04000656 RID: 1622
			enm_IMono_ClothSimulatorComponent_set_forced_wind,
			// Token: 0x04000657 RID: 1623
			enm_IMono_ClothSimulatorComponent_set_maxdistance_multiplier,
			// Token: 0x04000658 RID: 1624
			enm_IMono_ClothSimulatorComponent_set_morph_animation,
			// Token: 0x04000659 RID: 1625
			enm_IMono_ClothSimulatorComponent_set_reset_required,
			// Token: 0x0400065A RID: 1626
			enm_IMono_ClothSimulatorComponent_set_vector_argument,
			// Token: 0x0400065B RID: 1627
			enm_IMono_CompositeComponent_add_component,
			// Token: 0x0400065C RID: 1628
			enm_IMono_CompositeComponent_add_multi_mesh,
			// Token: 0x0400065D RID: 1629
			enm_IMono_CompositeComponent_add_prefab_entity,
			// Token: 0x0400065E RID: 1630
			enm_IMono_CompositeComponent_create_composite_component,
			// Token: 0x0400065F RID: 1631
			enm_IMono_CompositeComponent_create_copy,
			// Token: 0x04000660 RID: 1632
			enm_IMono_CompositeComponent_get_bounding_box,
			// Token: 0x04000661 RID: 1633
			enm_IMono_CompositeComponent_get_factor_1,
			// Token: 0x04000662 RID: 1634
			enm_IMono_CompositeComponent_get_factor_2,
			// Token: 0x04000663 RID: 1635
			enm_IMono_CompositeComponent_get_first_meta_mesh,
			// Token: 0x04000664 RID: 1636
			enm_IMono_CompositeComponent_get_frame,
			// Token: 0x04000665 RID: 1637
			enm_IMono_CompositeComponent_get_vector_user_data,
			// Token: 0x04000666 RID: 1638
			enm_IMono_CompositeComponent_is_visible,
			// Token: 0x04000667 RID: 1639
			enm_IMono_CompositeComponent_release,
			// Token: 0x04000668 RID: 1640
			enm_IMono_CompositeComponent_set_factor_1,
			// Token: 0x04000669 RID: 1641
			enm_IMono_CompositeComponent_set_factor_2,
			// Token: 0x0400066A RID: 1642
			enm_IMono_CompositeComponent_set_frame,
			// Token: 0x0400066B RID: 1643
			enm_IMono_CompositeComponent_set_material,
			// Token: 0x0400066C RID: 1644
			enm_IMono_CompositeComponent_set_vector_argument,
			// Token: 0x0400066D RID: 1645
			enm_IMono_CompositeComponent_set_vector_user_data,
			// Token: 0x0400066E RID: 1646
			enm_IMono_CompositeComponent_set_visibility_mask,
			// Token: 0x0400066F RID: 1647
			enm_IMono_CompositeComponent_set_visible,
			// Token: 0x04000670 RID: 1648
			enm_IMono_Config_apply,
			// Token: 0x04000671 RID: 1649
			enm_IMono_Config_apply_config_changes,
			// Token: 0x04000672 RID: 1650
			enm_IMono_Config_auto_save_in_minutes,
			// Token: 0x04000673 RID: 1651
			enm_IMono_Config_check_gfx_support_status,
			// Token: 0x04000674 RID: 1652
			enm_IMono_Config_get_auto_gfx_quality,
			// Token: 0x04000675 RID: 1653
			enm_IMono_Config_get_character_detail,
			// Token: 0x04000676 RID: 1654
			enm_IMono_Config_get_cheat_mode,
			// Token: 0x04000677 RID: 1655
			enm_IMono_Config_get_current_sound_device_index,
			// Token: 0x04000678 RID: 1656
			enm_IMono_Config_get_debug_login_password,
			// Token: 0x04000679 RID: 1657
			enm_IMono_Config_get_debug_login_username,
			// Token: 0x0400067A RID: 1658
			enm_IMono_Config_get_default_rgl_config,
			// Token: 0x0400067B RID: 1659
			enm_IMono_Config_get_desktop_resolution,
			// Token: 0x0400067C RID: 1660
			enm_IMono_Config_get_development_mode,
			// Token: 0x0400067D RID: 1661
			enm_IMono_Config_get_disable_gui_messages,
			// Token: 0x0400067E RID: 1662
			enm_IMono_Config_get_disable_sound,
			// Token: 0x0400067F RID: 1663
			enm_IMono_Config_get_dlss_option_count,
			// Token: 0x04000680 RID: 1664
			enm_IMono_Config_get_dlss_technique,
			// Token: 0x04000681 RID: 1665
			enm_IMono_Config_get_do_localization_check_at_startup,
			// Token: 0x04000682 RID: 1666
			enm_IMono_Config_get_enable_cloth_simulation,
			// Token: 0x04000683 RID: 1667
			enm_IMono_Config_get_enable_edit_mode,
			// Token: 0x04000684 RID: 1668
			enm_IMono_Config_get_invert_mouse,
			// Token: 0x04000685 RID: 1669
			enm_IMono_Config_get_last_opened_scene,
			// Token: 0x04000686 RID: 1670
			enm_IMono_Config_get_localization_debug_mode,
			// Token: 0x04000687 RID: 1671
			enm_IMono_Config_get_monitor_device_count,
			// Token: 0x04000688 RID: 1672
			enm_IMono_Config_get_monitor_device_name,
			// Token: 0x04000689 RID: 1673
			enm_IMono_Config_get_refresh_rate_at_index,
			// Token: 0x0400068A RID: 1674
			enm_IMono_Config_get_refresh_rate_count,
			// Token: 0x0400068B RID: 1675
			enm_IMono_Config_get_resolution,
			// Token: 0x0400068C RID: 1676
			enm_IMono_Config_get_resolution_at_index,
			// Token: 0x0400068D RID: 1677
			enm_IMono_Config_get_resolution_count,
			// Token: 0x0400068E RID: 1678
			enm_IMono_Config_get_rgl_config,
			// Token: 0x0400068F RID: 1679
			enm_IMono_Config_get_rgl_config_for_default_settings,
			// Token: 0x04000690 RID: 1680
			enm_IMono_Config_get_sound_device_count,
			// Token: 0x04000691 RID: 1681
			enm_IMono_Config_get_sound_device_name,
			// Token: 0x04000692 RID: 1682
			enm_IMono_Config_get_tableau_cache_mode,
			// Token: 0x04000693 RID: 1683
			enm_IMono_Config_get_ui_debug_mode,
			// Token: 0x04000694 RID: 1684
			enm_IMono_Config_get_ui_do_not_use_generated_prefabs,
			// Token: 0x04000695 RID: 1685
			enm_IMono_Config_get_video_device_count,
			// Token: 0x04000696 RID: 1686
			enm_IMono_Config_get_video_device_name,
			// Token: 0x04000697 RID: 1687
			enm_IMono_Config_is_120hz_available,
			// Token: 0x04000698 RID: 1688
			enm_IMono_Config_is_dlss_available,
			// Token: 0x04000699 RID: 1689
			enm_IMono_Config_read_rgl_config_files,
			// Token: 0x0400069A RID: 1690
			enm_IMono_Config_refresh_options_data,
			// Token: 0x0400069B RID: 1691
			enm_IMono_Config_save_rgl_config,
			// Token: 0x0400069C RID: 1692
			enm_IMono_Config_set_auto_config_wrt_hardware,
			// Token: 0x0400069D RID: 1693
			enm_IMono_Config_set_brightness,
			// Token: 0x0400069E RID: 1694
			enm_IMono_Config_set_custom_resolution,
			// Token: 0x0400069F RID: 1695
			enm_IMono_Config_set_default_game_config,
			// Token: 0x040006A0 RID: 1696
			enm_IMono_Config_set_rgl_config,
			// Token: 0x040006A1 RID: 1697
			enm_IMono_Config_set_sharpen_amount,
			// Token: 0x040006A2 RID: 1698
			enm_IMono_Config_set_sound_device,
			// Token: 0x040006A3 RID: 1699
			enm_IMono_Config_set_sound_preset,
			// Token: 0x040006A4 RID: 1700
			enm_IMono_Debug_abort_game,
			// Token: 0x040006A5 RID: 1701
			enm_IMono_Debug_assert_memory_usage,
			// Token: 0x040006A6 RID: 1702
			enm_IMono_Debug_clear_all_debug_render_objects,
			// Token: 0x040006A7 RID: 1703
			enm_IMono_Debug_content_warning,
			// Token: 0x040006A8 RID: 1704
			enm_IMono_Debug_echo_command_window,
			// Token: 0x040006A9 RID: 1705
			enm_IMono_Debug_error,
			// Token: 0x040006AA RID: 1706
			enm_IMono_Debug_failed_assert,
			// Token: 0x040006AB RID: 1707
			enm_IMono_Debug_get_debug_vector,
			// Token: 0x040006AC RID: 1708
			enm_IMono_Debug_get_show_debug_info,
			// Token: 0x040006AD RID: 1709
			enm_IMono_Debug_is_error_report_mode_active,
			// Token: 0x040006AE RID: 1710
			enm_IMono_Debug_is_error_report_mode_pause_mission,
			// Token: 0x040006AF RID: 1711
			enm_IMono_Debug_is_test_mode,
			// Token: 0x040006B0 RID: 1712
			enm_IMono_Debug_message_box,
			// Token: 0x040006B1 RID: 1713
			enm_IMono_Debug_post_warning_line,
			// Token: 0x040006B2 RID: 1714
			enm_IMono_Debug_render_debug_box_object,
			// Token: 0x040006B3 RID: 1715
			enm_IMono_Debug_render_debug_box_object_with_frame,
			// Token: 0x040006B4 RID: 1716
			enm_IMono_Debug_render_debug_capsule,
			// Token: 0x040006B5 RID: 1717
			enm_IMono_Debug_render_debug_direction_arrow,
			// Token: 0x040006B6 RID: 1718
			enm_IMono_Debug_render_debug_frame,
			// Token: 0x040006B7 RID: 1719
			enm_IMono_Debug_render_debug_line,
			// Token: 0x040006B8 RID: 1720
			enm_IMono_Debug_render_debug_rect,
			// Token: 0x040006B9 RID: 1721
			enm_IMono_Debug_render_debug_rect_with_color,
			// Token: 0x040006BA RID: 1722
			enm_IMono_Debug_render_debug_sphere,
			// Token: 0x040006BB RID: 1723
			enm_IMono_Debug_render_debug_text,
			// Token: 0x040006BC RID: 1724
			enm_IMono_Debug_render_debug_text3d,
			// Token: 0x040006BD RID: 1725
			enm_IMono_Debug_set_debug_vector,
			// Token: 0x040006BE RID: 1726
			enm_IMono_Debug_set_dump_generation_disabled,
			// Token: 0x040006BF RID: 1727
			enm_IMono_Debug_set_error_report_scene,
			// Token: 0x040006C0 RID: 1728
			enm_IMono_Debug_set_show_debug_info,
			// Token: 0x040006C1 RID: 1729
			enm_IMono_Debug_silent_assert,
			// Token: 0x040006C2 RID: 1730
			enm_IMono_Debug_warning,
			// Token: 0x040006C3 RID: 1731
			enm_IMono_Debug_write_debug_line_on_screen,
			// Token: 0x040006C4 RID: 1732
			enm_IMono_Debug_write_line,
			// Token: 0x040006C5 RID: 1733
			enm_IMono_Decal_check_and_register_to_decal_set,
			// Token: 0x040006C6 RID: 1734
			enm_IMono_Decal_create_copy,
			// Token: 0x040006C7 RID: 1735
			enm_IMono_Decal_create_decal,
			// Token: 0x040006C8 RID: 1736
			enm_IMono_Decal_get_factor_1,
			// Token: 0x040006C9 RID: 1737
			enm_IMono_Decal_get_global_frame,
			// Token: 0x040006CA RID: 1738
			enm_IMono_Decal_get_material,
			// Token: 0x040006CB RID: 1739
			enm_IMono_Decal_override_road_boundary_p0,
			// Token: 0x040006CC RID: 1740
			enm_IMono_Decal_override_road_boundary_p1,
			// Token: 0x040006CD RID: 1741
			enm_IMono_Decal_set_alpha,
			// Token: 0x040006CE RID: 1742
			enm_IMono_Decal_set_factor_1,
			// Token: 0x040006CF RID: 1743
			enm_IMono_Decal_set_factor_1_linear,
			// Token: 0x040006D0 RID: 1744
			enm_IMono_Decal_set_global_frame,
			// Token: 0x040006D1 RID: 1745
			enm_IMono_Decal_set_is_visible,
			// Token: 0x040006D2 RID: 1746
			enm_IMono_Decal_set_material,
			// Token: 0x040006D3 RID: 1747
			enm_IMono_Decal_set_vector_argument,
			// Token: 0x040006D4 RID: 1748
			enm_IMono_Decal_set_vector_argument_2,
			// Token: 0x040006D5 RID: 1749
			enm_IMono_EngineSizeChecker_get_engine_struct_member_offset,
			// Token: 0x040006D6 RID: 1750
			enm_IMono_EngineSizeChecker_get_engine_struct_size,
			// Token: 0x040006D7 RID: 1751
			enm_IMono_GameEntity_activate_ragdoll,
			// Token: 0x040006D8 RID: 1752
			enm_IMono_GameEntity_add_all_meshes_of_game_entity,
			// Token: 0x040006D9 RID: 1753
			enm_IMono_GameEntity_add_capsule_as_body,
			// Token: 0x040006DA RID: 1754
			enm_IMono_GameEntity_add_child,
			// Token: 0x040006DB RID: 1755
			enm_IMono_GameEntity_add_component,
			// Token: 0x040006DC RID: 1756
			enm_IMono_GameEntity_add_distance_joint,
			// Token: 0x040006DD RID: 1757
			enm_IMono_GameEntity_add_distance_joint_with_frames,
			// Token: 0x040006DE RID: 1758
			enm_IMono_GameEntity_add_edit_data_user_to_all_meshes,
			// Token: 0x040006DF RID: 1759
			enm_IMono_GameEntity_add_light,
			// Token: 0x040006E0 RID: 1760
			enm_IMono_GameEntity_add_mesh,
			// Token: 0x040006E1 RID: 1761
			enm_IMono_GameEntity_add_mesh_to_bone,
			// Token: 0x040006E2 RID: 1762
			enm_IMono_GameEntity_add_multi_mesh,
			// Token: 0x040006E3 RID: 1763
			enm_IMono_GameEntity_add_multi_mesh_to_skeleton,
			// Token: 0x040006E4 RID: 1764
			enm_IMono_GameEntity_add_multi_mesh_to_skeleton_bone,
			// Token: 0x040006E5 RID: 1765
			enm_IMono_GameEntity_add_particle_system_component,
			// Token: 0x040006E6 RID: 1766
			enm_IMono_GameEntity_add_physics,
			// Token: 0x040006E7 RID: 1767
			enm_IMono_GameEntity_add_sphere_as_body,
			// Token: 0x040006E8 RID: 1768
			enm_IMono_GameEntity_add_splash_position_to_water_visual_record,
			// Token: 0x040006E9 RID: 1769
			enm_IMono_GameEntity_add_tag,
			// Token: 0x040006EA RID: 1770
			enm_IMono_GameEntity_apply_acceleration_to_dynamic_body,
			// Token: 0x040006EB RID: 1771
			enm_IMono_GameEntity_apply_force_to_dynamic_body,
			// Token: 0x040006EC RID: 1772
			enm_IMono_GameEntity_apply_global_force_at_local_pos_to_dynamic_body,
			// Token: 0x040006ED RID: 1773
			enm_IMono_GameEntity_apply_local_force_at_local_pos_to_dynamic_body,
			// Token: 0x040006EE RID: 1774
			enm_IMono_GameEntity_apply_local_impulse_to_dynamic_body,
			// Token: 0x040006EF RID: 1775
			enm_IMono_GameEntity_apply_torque_to_dynamic_body,
			// Token: 0x040006F0 RID: 1776
			enm_IMono_GameEntity_attach_nav_mesh_faces_to_entity,
			// Token: 0x040006F1 RID: 1777
			enm_IMono_GameEntity_break_prefab,
			// Token: 0x040006F2 RID: 1778
			enm_IMono_GameEntity_burst_entity_particle,
			// Token: 0x040006F3 RID: 1779
			enm_IMono_GameEntity_call_script_callbacks,
			// Token: 0x040006F4 RID: 1780
			enm_IMono_GameEntity_change_meta_mesh_or_remove_it_if_not_exists,
			// Token: 0x040006F5 RID: 1781
			enm_IMono_GameEntity_change_resolution_multiplier_of_ship_visual,
			// Token: 0x040006F6 RID: 1782
			enm_IMono_GameEntity_check_is_prefab_link_root_prefab,
			// Token: 0x040006F7 RID: 1783
			enm_IMono_GameEntity_check_point_with_oriented_bounding_box,
			// Token: 0x040006F8 RID: 1784
			enm_IMono_GameEntity_check_resources,
			// Token: 0x040006F9 RID: 1785
			enm_IMono_GameEntity_clear_components,
			// Token: 0x040006FA RID: 1786
			enm_IMono_GameEntity_clear_entity_components,
			// Token: 0x040006FB RID: 1787
			enm_IMono_GameEntity_clear_only_own_components,
			// Token: 0x040006FC RID: 1788
			enm_IMono_GameEntity_compute_trajectory_volume,
			// Token: 0x040006FD RID: 1789
			enm_IMono_GameEntity_compute_velocity_delta_from_impulse,
			// Token: 0x040006FE RID: 1790
			enm_IMono_GameEntity_convert_dynamic_body_to_raycast,
			// Token: 0x040006FF RID: 1791
			enm_IMono_GameEntity_cook_triangle_physx_mesh,
			// Token: 0x04000700 RID: 1792
			enm_IMono_GameEntity_copy_components_to_skeleton,
			// Token: 0x04000701 RID: 1793
			enm_IMono_GameEntity_copy_from_prefab,
			// Token: 0x04000702 RID: 1794
			enm_IMono_GameEntity_copy_script_component_from_another_entity,
			// Token: 0x04000703 RID: 1795
			enm_IMono_GameEntity_create_and_add_script_component,
			// Token: 0x04000704 RID: 1796
			enm_IMono_GameEntity_create_empty,
			// Token: 0x04000705 RID: 1797
			enm_IMono_GameEntity_create_empty_physx_shape,
			// Token: 0x04000706 RID: 1798
			enm_IMono_GameEntity_create_empty_without_scene,
			// Token: 0x04000707 RID: 1799
			enm_IMono_GameEntity_create_from_prefab,
			// Token: 0x04000708 RID: 1800
			enm_IMono_GameEntity_create_from_prefab_with_initial_frame,
			// Token: 0x04000709 RID: 1801
			enm_IMono_GameEntity_create_physx_cooking_instance,
			// Token: 0x0400070A RID: 1802
			enm_IMono_GameEntity_create_variable_rate_physics,
			// Token: 0x0400070B RID: 1803
			enm_IMono_GameEntity_delete_empty_shape,
			// Token: 0x0400070C RID: 1804
			enm_IMono_GameEntity_delete_physx_cooking_instance,
			// Token: 0x0400070D RID: 1805
			enm_IMono_GameEntity_deregister_water_mesh_materials,
			// Token: 0x0400070E RID: 1806
			enm_IMono_GameEntity_deregister_water_sdf_clip,
			// Token: 0x0400070F RID: 1807
			enm_IMono_GameEntity_deselect_entity_on_editor,
			// Token: 0x04000710 RID: 1808
			enm_IMono_GameEntity_detach_all_attached_navigation_mesh_faces,
			// Token: 0x04000711 RID: 1809
			enm_IMono_GameEntity_disable_contour,
			// Token: 0x04000712 RID: 1810
			enm_IMono_GameEntity_disable_dynamic_body_simulation,
			// Token: 0x04000713 RID: 1811
			enm_IMono_GameEntity_disable_gravity,
			// Token: 0x04000714 RID: 1812
			enm_IMono_GameEntity_enable_dynamic_body,
			// Token: 0x04000715 RID: 1813
			enm_IMono_GameEntity_find_with_name,
			// Token: 0x04000716 RID: 1814
			enm_IMono_GameEntity_freeze,
			// Token: 0x04000717 RID: 1815
			enm_IMono_GameEntity_get_angular_velocity,
			// Token: 0x04000718 RID: 1816
			enm_IMono_GameEntity_get_attached_navmesh_face_count,
			// Token: 0x04000719 RID: 1817
			enm_IMono_GameEntity_get_attached_navmesh_face_records,
			// Token: 0x0400071A RID: 1818
			enm_IMono_GameEntity_get_attached_navmesh_face_vertex_indices,
			// Token: 0x0400071B RID: 1819
			enm_IMono_GameEntity_get_body_flags,
			// Token: 0x0400071C RID: 1820
			enm_IMono_GameEntity_get_body_shape,
			// Token: 0x0400071D RID: 1821
			enm_IMono_GameEntity_get_visual_body_world_transform,
			// Token: 0x0400071E RID: 1822
			enm_IMono_GameEntity_get_body_world_transform,
			// Token: 0x0400071F RID: 1823
			enm_IMono_GameEntity_get_bone_count,
			// Token: 0x04000720 RID: 1824
			enm_IMono_GameEntity_get_bone_entitial_frame_with_index,
			// Token: 0x04000721 RID: 1825
			enm_IMono_GameEntity_get_bone_entitial_frame_with_name,
			// Token: 0x04000722 RID: 1826
			enm_IMono_GameEntity_get_bounding_box_max,
			// Token: 0x04000723 RID: 1827
			enm_IMono_GameEntity_get_bounding_box_min,
			// Token: 0x04000724 RID: 1828
			enm_IMono_GameEntity_get_camera_params_from_camera_script,
			// Token: 0x04000725 RID: 1829
			enm_IMono_GameEntity_get_center_of_mass,
			// Token: 0x04000726 RID: 1830
			enm_IMono_GameEntity_get_child,
			// Token: 0x04000727 RID: 1831
			enm_IMono_GameEntity_get_child_count,
			// Token: 0x04000728 RID: 1832
			enm_IMono_GameEntity_get_child_pointer,
			// Token: 0x04000729 RID: 1833
			enm_IMono_GameEntity_get_component_at_index,
			// Token: 0x0400072A RID: 1834
			enm_IMono_GameEntity_get_component_count,
			// Token: 0x0400072B RID: 1835
			enm_IMono_GameEntity_get_edit_mode_level_visibility,
			// Token: 0x0400072C RID: 1836
			enm_IMono_GameEntity_get_entity_flags,
			// Token: 0x0400072D RID: 1837
			enm_IMono_GameEntity_get_entity_visibility_flags,
			// Token: 0x0400072E RID: 1838
			enm_IMono_GameEntity_get_factor_color,
			// Token: 0x0400072F RID: 1839
			enm_IMono_GameEntity_get_first_child_with_tag_recursive,
			// Token: 0x04000730 RID: 1840
			enm_IMono_GameEntity_get_first_entity_with_tag,
			// Token: 0x04000731 RID: 1841
			enm_IMono_GameEntity_get_first_entity_with_tag_expression,
			// Token: 0x04000732 RID: 1842
			enm_IMono_GameEntity_get_first_mesh,
			// Token: 0x04000733 RID: 1843
			enm_IMono_GameEntity_get_global_bounding_box,
			// Token: 0x04000734 RID: 1844
			enm_IMono_GameEntity_get_global_box_max,
			// Token: 0x04000735 RID: 1845
			enm_IMono_GameEntity_get_global_box_min,
			// Token: 0x04000736 RID: 1846
			enm_IMono_GameEntity_get_global_frame,
			// Token: 0x04000737 RID: 1847
			enm_IMono_GameEntity_get_global_frame_imprecise_for_fixed_tick,
			// Token: 0x04000738 RID: 1848
			enm_IMono_GameEntity_get_global_scale,
			// Token: 0x04000739 RID: 1849
			enm_IMono_GameEntity_get_global_wind_strength_vector_of_scene,
			// Token: 0x0400073A RID: 1850
			enm_IMono_GameEntity_get_global_wind_velocity_of_scene,
			// Token: 0x0400073B RID: 1851
			enm_IMono_GameEntity_get_global_wind_velocity_with_gust_noise_of_scene,
			// Token: 0x0400073C RID: 1852
			enm_IMono_GameEntity_get_guid,
			// Token: 0x0400073D RID: 1853
			enm_IMono_GameEntity_get_last_final_render_camera_position_of_scene,
			// Token: 0x0400073E RID: 1854
			enm_IMono_GameEntity_get_light,
			// Token: 0x0400073F RID: 1855
			enm_IMono_GameEntity_get_linear_velocity,
			// Token: 0x04000740 RID: 1856
			enm_IMono_GameEntity_get_local_bounding_box,
			// Token: 0x04000741 RID: 1857
			enm_IMono_GameEntity_get_local_frame,
			// Token: 0x04000742 RID: 1858
			enm_IMono_GameEntity_get_local_physics_bounding_box,
			// Token: 0x04000743 RID: 1859
			enm_IMono_GameEntity_get_lod_level_for_distance_sq,
			// Token: 0x04000744 RID: 1860
			enm_IMono_GameEntity_get_mass,
			// Token: 0x04000745 RID: 1861
			enm_IMono_GameEntity_get_mass_space_inertia,
			// Token: 0x04000746 RID: 1862
			enm_IMono_GameEntity_get_mass_space_inv_inertia,
			// Token: 0x04000747 RID: 1863
			enm_IMono_GameEntity_get_mesh_bended_position,
			// Token: 0x04000748 RID: 1864
			enm_IMono_GameEntity_get_mobility,
			// Token: 0x04000749 RID: 1865
			enm_IMono_GameEntity_get_name,
			// Token: 0x0400074A RID: 1866
			enm_IMono_GameEntity_get_native_script_component_variable,
			// Token: 0x0400074B RID: 1867
			enm_IMono_GameEntity_get_next_entity_with_tag,
			// Token: 0x0400074C RID: 1868
			enm_IMono_GameEntity_get_next_entity_with_tag_expression,
			// Token: 0x0400074D RID: 1869
			enm_IMono_GameEntity_get_next_prefab,
			// Token: 0x0400074E RID: 1870
			enm_IMono_GameEntity_get_old_prefab_name,
			// Token: 0x0400074F RID: 1871
			enm_IMono_GameEntity_get_parent,
			// Token: 0x04000750 RID: 1872
			enm_IMono_GameEntity_get_parent_pointer,
			// Token: 0x04000751 RID: 1873
			enm_IMono_GameEntity_get_physics_desc_body_flags,
			// Token: 0x04000752 RID: 1874
			enm_IMono_GameEntity_get_physics_material_index,
			// Token: 0x04000753 RID: 1875
			enm_IMono_GameEntity_get_physics_min_max,
			// Token: 0x04000754 RID: 1876
			enm_IMono_GameEntity_get_physics_state,
			// Token: 0x04000755 RID: 1877
			enm_IMono_GameEntity_get_physics_triangle_count,
			// Token: 0x04000756 RID: 1878
			enm_IMono_GameEntity_get_prefab_name,
			// Token: 0x04000757 RID: 1879
			enm_IMono_GameEntity_get_previous_global_frame,
			// Token: 0x04000758 RID: 1880
			enm_IMono_GameEntity_get_quick_bone_entitial_frame,
			// Token: 0x04000759 RID: 1881
			enm_IMono_GameEntity_get_radius,
			// Token: 0x0400075A RID: 1882
			enm_IMono_GameEntity_get_root_parent_pointer,
			// Token: 0x0400075B RID: 1883
			enm_IMono_GameEntity_get_scene,
			// Token: 0x0400075C RID: 1884
			enm_IMono_GameEntity_get_scene_pointer,
			// Token: 0x0400075D RID: 1885
			enm_IMono_GameEntity_get_script_component,
			// Token: 0x0400075E RID: 1886
			enm_IMono_GameEntity_get_script_component_at_index,
			// Token: 0x0400075F RID: 1887
			enm_IMono_GameEntity_get_script_component_count,
			// Token: 0x04000760 RID: 1888
			enm_IMono_GameEntity_get_script_component_index,
			// Token: 0x04000761 RID: 1889
			enm_IMono_GameEntity_get_skeleton,
			// Token: 0x04000762 RID: 1890
			enm_IMono_GameEntity_get_tags,
			// Token: 0x04000763 RID: 1891
			enm_IMono_GameEntity_get_upgrade_level_mask,
			// Token: 0x04000764 RID: 1892
			enm_IMono_GameEntity_get_upgrade_level_mask_cumulative,
			// Token: 0x04000765 RID: 1893
			enm_IMono_GameEntity_get_visibility_exclude_parents,
			// Token: 0x04000766 RID: 1894
			enm_IMono_GameEntity_get_visibility_level_mask_including_parents,
			// Token: 0x04000767 RID: 1895
			enm_IMono_GameEntity_get_water_level_at_position,
			// Token: 0x04000768 RID: 1896
			enm_IMono_GameEntity_has_batched_kinematic_physics_flag,
			// Token: 0x04000769 RID: 1897
			enm_IMono_GameEntity_has_batched_raycast_physics_flag,
			// Token: 0x0400076A RID: 1898
			enm_IMono_GameEntity_has_body,
			// Token: 0x0400076B RID: 1899
			enm_IMono_GameEntity_has_complex_anim_tree,
			// Token: 0x0400076C RID: 1900
			enm_IMono_GameEntity_has_component,
			// Token: 0x0400076D RID: 1901
			enm_IMono_GameEntity_has_dynamic_rigid_body,
			// Token: 0x0400076E RID: 1902
			enm_IMono_GameEntity_has_dynamic_rigid_body_and_active_simulation,
			// Token: 0x0400076F RID: 1903
			enm_IMono_GameEntity_has_frame_changed,
			// Token: 0x04000770 RID: 1904
			enm_IMono_GameEntity_has_kinematic_rigid_body,
			// Token: 0x04000771 RID: 1905
			enm_IMono_GameEntity_has_physics_body,
			// Token: 0x04000772 RID: 1906
			enm_IMono_GameEntity_has_physics_definition,
			// Token: 0x04000773 RID: 1907
			enm_IMono_GameEntity_has_scene,
			// Token: 0x04000774 RID: 1908
			enm_IMono_GameEntity_has_script_component,
			// Token: 0x04000775 RID: 1909
			enm_IMono_GameEntity_has_script_component_hash,
			// Token: 0x04000776 RID: 1910
			enm_IMono_GameEntity_has_static_physics_body,
			// Token: 0x04000777 RID: 1911
			enm_IMono_GameEntity_has_tag,
			// Token: 0x04000778 RID: 1912
			enm_IMono_GameEntity_is_dynamic_body_stationary,
			// Token: 0x04000779 RID: 1913
			enm_IMono_GameEntity_is_engine_body_sleeping,
			// Token: 0x0400077A RID: 1914
			enm_IMono_GameEntity_is_entity_selected_on_editor,
			// Token: 0x0400077B RID: 1915
			enm_IMono_GameEntity_is_frozen,
			// Token: 0x0400077C RID: 1916
			enm_IMono_GameEntity_is_ghost_object,
			// Token: 0x0400077D RID: 1917
			enm_IMono_GameEntity_is_gravity_disabled,
			// Token: 0x0400077E RID: 1918
			enm_IMono_GameEntity_is_guid_valid,
			// Token: 0x0400077F RID: 1919
			enm_IMono_GameEntity_is_in_editor_scene,
			// Token: 0x04000780 RID: 1920
			enm_IMono_GameEntity_is_visible_include_parents,
			// Token: 0x04000781 RID: 1921
			enm_IMono_GameEntity_pause_particle_system,
			// Token: 0x04000782 RID: 1922
			enm_IMono_GameEntity_pop_capsule_shape_from_entity_body,
			// Token: 0x04000783 RID: 1923
			enm_IMono_GameEntity_prefab_exists,
			// Token: 0x04000784 RID: 1924
			enm_IMono_GameEntity_push_capsule_shape_to_entity_body,
			// Token: 0x04000785 RID: 1925
			enm_IMono_GameEntity_ray_hit_entity,
			// Token: 0x04000786 RID: 1926
			enm_IMono_GameEntity_ray_hit_entity_with_normal,
			// Token: 0x04000787 RID: 1927
			enm_IMono_GameEntity_recompute_bounding_box,
			// Token: 0x04000788 RID: 1928
			enm_IMono_GameEntity_refresh_meshes_to_render_to_hull_water,
			// Token: 0x04000789 RID: 1929
			enm_IMono_GameEntity_register_water_sdf_clip,
			// Token: 0x0400078A RID: 1930
			enm_IMono_GameEntity_relax_local_bounding_box,
			// Token: 0x0400078B RID: 1931
			enm_IMono_GameEntity_release_edit_data_user_to_all_meshes,
			// Token: 0x0400078C RID: 1932
			enm_IMono_GameEntity_remove,
			// Token: 0x0400078D RID: 1933
			enm_IMono_GameEntity_delete_all_children,
			// Token: 0x0400078E RID: 1934
			enm_IMono_GameEntity_remove_all_particle_systems,
			// Token: 0x0400078F RID: 1935
			enm_IMono_GameEntity_remove_child,
			// Token: 0x04000790 RID: 1936
			enm_IMono_GameEntity_remove_component,
			// Token: 0x04000791 RID: 1937
			enm_IMono_GameEntity_remove_component_with_mesh,
			// Token: 0x04000792 RID: 1938
			enm_IMono_GameEntity_remove_engine_physics,
			// Token: 0x04000793 RID: 1939
			enm_IMono_GameEntity_remove_from_predisplay_entity,
			// Token: 0x04000794 RID: 1940
			enm_IMono_GameEntity_remove_joint,
			// Token: 0x04000795 RID: 1941
			enm_IMono_GameEntity_remove_multi_mesh,
			// Token: 0x04000796 RID: 1942
			enm_IMono_GameEntity_remove_multi_mesh_from_skeleton,
			// Token: 0x04000797 RID: 1943
			enm_IMono_GameEntity_remove_multi_mesh_from_skeleton_bone,
			// Token: 0x04000798 RID: 1944
			enm_IMono_GameEntity_remove_physics,
			// Token: 0x04000799 RID: 1945
			enm_IMono_GameEntity_remove_script_component,
			// Token: 0x0400079A RID: 1946
			enm_IMono_GameEntity_remove_tag,
			// Token: 0x0400079B RID: 1947
			enm_IMono_GameEntity_replace_physics_body_with_quad_physics_body,
			// Token: 0x0400079C RID: 1948
			enm_IMono_GameEntity_reset_hull_water,
			// Token: 0x0400079D RID: 1949
			enm_IMono_GameEntity_resume_particle_system,
			// Token: 0x0400079E RID: 1950
			enm_IMono_GameEntity_select_entity_on_editor,
			// Token: 0x0400079F RID: 1951
			enm_IMono_GameEntity_set_alpha,
			// Token: 0x040007A0 RID: 1952
			enm_IMono_GameEntity_set_angular_velocity,
			// Token: 0x040007A1 RID: 1953
			enm_IMono_GameEntity_set_animation_sound_activation,
			// Token: 0x040007A2 RID: 1954
			enm_IMono_GameEntity_set_anim_tree_channel_parameter,
			// Token: 0x040007A3 RID: 1955
			enm_IMono_GameEntity_set_as_contour_entity,
			// Token: 0x040007A4 RID: 1956
			enm_IMono_GameEntity_set_as_predisplay_entity,
			// Token: 0x040007A5 RID: 1957
			enm_IMono_GameEntity_set_as_replay_entity,
			// Token: 0x040007A6 RID: 1958
			enm_IMono_GameEntity_set_body_flags,
			// Token: 0x040007A7 RID: 1959
			enm_IMono_GameEntity_set_body_flags_recursive,
			// Token: 0x040007A8 RID: 1960
			enm_IMono_GameEntity_set_body_shape,
			// Token: 0x040007A9 RID: 1961
			enm_IMono_GameEntity_set_bone_frame_to_all_meshes,
			// Token: 0x040007AA RID: 1962
			enm_IMono_GameEntity_set_boundingbox_dirty,
			// Token: 0x040007AB RID: 1963
			enm_IMono_GameEntity_set_center_of_mass,
			// Token: 0x040007AC RID: 1964
			enm_IMono_GameEntity_set_cloth_component_keep_state,
			// Token: 0x040007AD RID: 1965
			enm_IMono_GameEntity_set_cloth_component_keep_state_of_all_meshes,
			// Token: 0x040007AE RID: 1966
			enm_IMono_GameEntity_set_cloth_max_distance_multiplier,
			// Token: 0x040007AF RID: 1967
			enm_IMono_GameEntity_set_color_to_all_meshes_with_tag_recursive,
			// Token: 0x040007B0 RID: 1968
			enm_IMono_GameEntity_set_contour_state,
			// Token: 0x040007B1 RID: 1969
			enm_IMono_GameEntity_set_cost_adder_for_attached_faces,
			// Token: 0x040007B2 RID: 1970
			enm_IMono_GameEntity_set_cull_mode,
			// Token: 0x040007B3 RID: 1971
			enm_IMono_GameEntity_set_custom_clip_plane,
			// Token: 0x040007B4 RID: 1972
			enm_IMono_GameEntity_set_custom_vertex_position_enabled,
			// Token: 0x040007B5 RID: 1973
			enm_IMono_GameEntity_set_damping,
			// Token: 0x040007B6 RID: 1974
			enm_IMono_GameEntity_set_do_not_check_visibility,
			// Token: 0x040007B7 RID: 1975
			enm_IMono_GameEntity_set_enforced_maximum_lod_level,
			// Token: 0x040007B8 RID: 1976
			enm_IMono_GameEntity_set_entity_env_map_visibility,
			// Token: 0x040007B9 RID: 1977
			enm_IMono_GameEntity_set_entity_flags,
			// Token: 0x040007BA RID: 1978
			enm_IMono_GameEntity_set_entity_visibility_flags,
			// Token: 0x040007BB RID: 1979
			enm_IMono_GameEntity_set_external_references_usage,
			// Token: 0x040007BC RID: 1980
			enm_IMono_GameEntity_set_factor2_color,
			// Token: 0x040007BD RID: 1981
			enm_IMono_GameEntity_set_factor_color,
			// Token: 0x040007BE RID: 1982
			enm_IMono_GameEntity_set_force_decals_to_render,
			// Token: 0x040007BF RID: 1983
			enm_IMono_GameEntity_set_force_not_affected_by_season,
			// Token: 0x040007C0 RID: 1984
			enm_IMono_GameEntity_set_frame_changed,
			// Token: 0x040007C1 RID: 1985
			enm_IMono_GameEntity_set_global_frame,
			// Token: 0x040007C2 RID: 1986
			enm_IMono_GameEntity_set_global_position,
			// Token: 0x040007C3 RID: 1987
			enm_IMono_GameEntity_set_has_custom_bounding_box_validation_system,
			// Token: 0x040007C4 RID: 1988
			enm_IMono_GameEntity_set_linear_velocity,
			// Token: 0x040007C5 RID: 1989
			enm_IMono_GameEntity_set_local_frame,
			// Token: 0x040007C6 RID: 1990
			enm_IMono_GameEntity_set_local_position,
			// Token: 0x040007C7 RID: 1991
			enm_IMono_GameEntity_set_manual_global_bounding_box,
			// Token: 0x040007C8 RID: 1992
			enm_IMono_GameEntity_set_manual_local_bounding_box,
			// Token: 0x040007C9 RID: 1993
			enm_IMono_GameEntity_set_mass_and_update_inertia_and_center_of_mass,
			// Token: 0x040007CA RID: 1994
			enm_IMono_GameEntity_set_mass_space_inertia,
			// Token: 0x040007CB RID: 1995
			enm_IMono_GameEntity_set_material_for_all_meshes,
			// Token: 0x040007CC RID: 1996
			enm_IMono_GameEntity_set_max_depenetration_velocity,
			// Token: 0x040007CD RID: 1997
			enm_IMono_GameEntity_set_mobility,
			// Token: 0x040007CE RID: 1998
			enm_IMono_GameEntity_set_morph_frame_of_components,
			// Token: 0x040007CF RID: 1999
			enm_IMono_GameEntity_set_name,
			// Token: 0x040007D0 RID: 2000
			enm_IMono_GameEntity_set_native_script_component_variable,
			// Token: 0x040007D1 RID: 2001
			enm_IMono_GameEntity_set_physics_move_to_batched,
			// Token: 0x040007D2 RID: 2002
			enm_IMono_GameEntity_set_physics_state,
			// Token: 0x040007D3 RID: 2003
			enm_IMono_GameEntity_set_physics_state_only_variable,
			// Token: 0x040007D4 RID: 2004
			enm_IMono_GameEntity_set_positions_for_attached_navmesh_vertices,
			// Token: 0x040007D5 RID: 2005
			enm_IMono_GameEntity_set_previous_frame_invalid,
			// Token: 0x040007D6 RID: 2006
			enm_IMono_GameEntity_set_ready_to_render,
			// Token: 0x040007D7 RID: 2007
			enm_IMono_GameEntity_set_runtime_emission_rate_multiplier,
			// Token: 0x040007D8 RID: 2008
			enm_IMono_GameEntity_set_skeleton,
			// Token: 0x040007D9 RID: 2009
			enm_IMono_GameEntity_set_solver_iteration_counts,
			// Token: 0x040007DA RID: 2010
			enm_IMono_GameEntity_setup_additional_bone_buffer_for_meshes,
			// Token: 0x040007DB RID: 2011
			enm_IMono_GameEntity_set_update_validity_on_frame_changed_of_faces_with_id,
			// Token: 0x040007DC RID: 2012
			enm_IMono_GameEntity_set_upgrade_level_mask,
			// Token: 0x040007DD RID: 2013
			enm_IMono_GameEntity_set_vector_argument,
			// Token: 0x040007DE RID: 2014
			enm_IMono_GameEntity_set_velocity_limits,
			// Token: 0x040007DF RID: 2015
			enm_IMono_GameEntity_set_visibility_exclude_parents,
			// Token: 0x040007E0 RID: 2016
			enm_IMono_GameEntity_set_visual_record_wake_params,
			// Token: 0x040007E1 RID: 2017
			enm_IMono_GameEntity_set_water_sdf_clip_data,
			// Token: 0x040007E2 RID: 2018
			enm_IMono_GameEntity_set_water_visual_record_frame_and_dt,
			// Token: 0x040007E3 RID: 2019
			enm_IMono_GameEntity_swap_physx_shape_in_entity,
			// Token: 0x040007E4 RID: 2020
			enm_IMono_GameEntity_update_attached_navigation_mesh_faces,
			// Token: 0x040007E5 RID: 2021
			enm_IMono_GameEntity_update_global_bounds,
			// Token: 0x040007E6 RID: 2022
			enm_IMono_GameEntity_update_hull_water_effect_frames,
			// Token: 0x040007E7 RID: 2023
			enm_IMono_GameEntity_update_triad_frame_for_editor,
			// Token: 0x040007E8 RID: 2024
			enm_IMono_GameEntity_update_visibility_mask,
			// Token: 0x040007E9 RID: 2025
			enm_IMono_GameEntity_validate_bounding_box,
			// Token: 0x040007EA RID: 2026
			enm_IMono_GameEntityComponent_get_entity,
			// Token: 0x040007EB RID: 2027
			enm_IMono_GameEntityComponent_get_entity_pointer,
			// Token: 0x040007EC RID: 2028
			enm_IMono_GameEntityComponent_get_first_meta_mesh,
			// Token: 0x040007ED RID: 2029
			enm_IMono_Highlights_add_highlight,
			// Token: 0x040007EE RID: 2030
			enm_IMono_Highlights_close_group,
			// Token: 0x040007EF RID: 2031
			enm_IMono_Highlights_initialize,
			// Token: 0x040007F0 RID: 2032
			enm_IMono_Highlights_open_group,
			// Token: 0x040007F1 RID: 2033
			enm_IMono_Highlights_open_summary,
			// Token: 0x040007F2 RID: 2034
			enm_IMono_Highlights_remove_highlight,
			// Token: 0x040007F3 RID: 2035
			enm_IMono_Highlights_save_screenshot,
			// Token: 0x040007F4 RID: 2036
			enm_IMono_Highlights_save_video,
			// Token: 0x040007F5 RID: 2037
			enm_IMono_Imgui_begin,
			// Token: 0x040007F6 RID: 2038
			enm_IMono_Imgui_begin_main_thread_scope,
			// Token: 0x040007F7 RID: 2039
			enm_IMono_Imgui_begin_with_close_button,
			// Token: 0x040007F8 RID: 2040
			enm_IMono_Imgui_button,
			// Token: 0x040007F9 RID: 2041
			enm_IMono_Imgui_checkbox,
			// Token: 0x040007FA RID: 2042
			enm_IMono_Imgui_collapsing_header,
			// Token: 0x040007FB RID: 2043
			enm_IMono_Imgui_columns,
			// Token: 0x040007FC RID: 2044
			enm_IMono_Imgui_combo,
			// Token: 0x040007FD RID: 2045
			enm_IMono_Imgui_combo_custom_seperator,
			// Token: 0x040007FE RID: 2046
			enm_IMono_Imgui_end,
			// Token: 0x040007FF RID: 2047
			enm_IMono_Imgui_end_main_thread_scope,
			// Token: 0x04000800 RID: 2048
			enm_IMono_Imgui_input_float,
			// Token: 0x04000801 RID: 2049
			enm_IMono_Imgui_input_float2,
			// Token: 0x04000802 RID: 2050
			enm_IMono_Imgui_input_float3,
			// Token: 0x04000803 RID: 2051
			enm_IMono_Imgui_input_float4,
			// Token: 0x04000804 RID: 2052
			enm_IMono_Imgui_input_int,
			// Token: 0x04000805 RID: 2053
			enm_IMono_Imgui_input_text,
			// Token: 0x04000806 RID: 2054
			enm_IMono_Imgui_input_text_multiline_copy_paste,
			// Token: 0x04000807 RID: 2055
			enm_IMono_Imgui_is_item_hovered,
			// Token: 0x04000808 RID: 2056
			enm_IMono_Imgui_new_frame,
			// Token: 0x04000809 RID: 2057
			enm_IMono_Imgui_new_line,
			// Token: 0x0400080A RID: 2058
			enm_IMono_Imgui_next_column,
			// Token: 0x0400080B RID: 2059
			enm_IMono_Imgui_plot_lines,
			// Token: 0x0400080C RID: 2060
			enm_IMono_Imgui_pop_style_color,
			// Token: 0x0400080D RID: 2061
			enm_IMono_Imgui_progress_bar,
			// Token: 0x0400080E RID: 2062
			enm_IMono_Imgui_push_style_color,
			// Token: 0x0400080F RID: 2063
			enm_IMono_Imgui_radio_button,
			// Token: 0x04000810 RID: 2064
			enm_IMono_Imgui_render,
			// Token: 0x04000811 RID: 2065
			enm_IMono_Imgui_same_line,
			// Token: 0x04000812 RID: 2066
			enm_IMono_Imgui_separator,
			// Token: 0x04000813 RID: 2067
			enm_IMono_Imgui_set_tool_tip,
			// Token: 0x04000814 RID: 2068
			enm_IMono_Imgui_slider_float,
			// Token: 0x04000815 RID: 2069
			enm_IMono_Imgui_small_button,
			// Token: 0x04000816 RID: 2070
			enm_IMono_Imgui_text,
			// Token: 0x04000817 RID: 2071
			enm_IMono_Imgui_tree_node,
			// Token: 0x04000818 RID: 2072
			enm_IMono_Imgui_tree_pop,
			// Token: 0x04000819 RID: 2073
			enm_IMono_Input_clear_keys,
			// Token: 0x0400081A RID: 2074
			enm_IMono_Input_get_clipboard_text,
			// Token: 0x0400081B RID: 2075
			enm_IMono_Input_get_controller_type,
			// Token: 0x0400081C RID: 2076
			enm_IMono_Input_get_gyro_x,
			// Token: 0x0400081D RID: 2077
			enm_IMono_Input_get_gyro_y,
			// Token: 0x0400081E RID: 2078
			enm_IMono_Input_get_gyro_z,
			// Token: 0x0400081F RID: 2079
			enm_IMono_Input_get_key_state,
			// Token: 0x04000820 RID: 2080
			enm_IMono_Input_get_mouse_delta_z,
			// Token: 0x04000821 RID: 2081
			enm_IMono_Input_get_mouse_move_x,
			// Token: 0x04000822 RID: 2082
			enm_IMono_Input_get_mouse_move_y,
			// Token: 0x04000823 RID: 2083
			enm_IMono_Input_get_mouse_position_x,
			// Token: 0x04000824 RID: 2084
			enm_IMono_Input_get_mouse_position_y,
			// Token: 0x04000825 RID: 2085
			enm_IMono_Input_get_mouse_scroll_value,
			// Token: 0x04000826 RID: 2086
			enm_IMono_Input_get_mouse_sensitivity,
			// Token: 0x04000827 RID: 2087
			enm_IMono_Input_get_virtual_key_code,
			// Token: 0x04000828 RID: 2088
			enm_IMono_Input_is_any_touch_active,
			// Token: 0x04000829 RID: 2089
			enm_IMono_Input_is_controller_connected,
			// Token: 0x0400082A RID: 2090
			enm_IMono_Input_is_key_down,
			// Token: 0x0400082B RID: 2091
			enm_IMono_Input_is_key_down_immediate,
			// Token: 0x0400082C RID: 2092
			enm_IMono_Input_is_key_pressed,
			// Token: 0x0400082D RID: 2093
			enm_IMono_Input_is_key_released,
			// Token: 0x0400082E RID: 2094
			enm_IMono_Input_is_mouse_active,
			// Token: 0x0400082F RID: 2095
			enm_IMono_Input_press_key,
			// Token: 0x04000830 RID: 2096
			enm_IMono_Input_set_clipboard_text,
			// Token: 0x04000831 RID: 2097
			enm_IMono_Input_set_cursor_friction_value,
			// Token: 0x04000832 RID: 2098
			enm_IMono_Input_set_cursor_position,
			// Token: 0x04000833 RID: 2099
			enm_IMono_Input_set_lightbar_color,
			// Token: 0x04000834 RID: 2100
			enm_IMono_Input_set_rumble_effect,
			// Token: 0x04000835 RID: 2101
			enm_IMono_Input_set_trigger_feedback,
			// Token: 0x04000836 RID: 2102
			enm_IMono_Input_set_trigger_vibration,
			// Token: 0x04000837 RID: 2103
			enm_IMono_Input_set_trigger_weapon_effect,
			// Token: 0x04000838 RID: 2104
			enm_IMono_Input_update_key_data,
			// Token: 0x04000839 RID: 2105
			enm_IMono_Light_create_point_light,
			// Token: 0x0400083A RID: 2106
			enm_IMono_Light_enable_shadow,
			// Token: 0x0400083B RID: 2107
			enm_IMono_Light_get_frame,
			// Token: 0x0400083C RID: 2108
			enm_IMono_Light_get_intensity,
			// Token: 0x0400083D RID: 2109
			enm_IMono_Light_get_light_color,
			// Token: 0x0400083E RID: 2110
			enm_IMono_Light_get_radius,
			// Token: 0x0400083F RID: 2111
			enm_IMono_Light_is_shadow_enabled,
			// Token: 0x04000840 RID: 2112
			enm_IMono_Light_release,
			// Token: 0x04000841 RID: 2113
			enm_IMono_Light_set_frame,
			// Token: 0x04000842 RID: 2114
			enm_IMono_Light_set_intensity,
			// Token: 0x04000843 RID: 2115
			enm_IMono_Light_set_light_color,
			// Token: 0x04000844 RID: 2116
			enm_IMono_Light_set_light_flicker,
			// Token: 0x04000845 RID: 2117
			enm_IMono_Light_set_radius,
			// Token: 0x04000846 RID: 2118
			enm_IMono_Light_set_shadows,
			// Token: 0x04000847 RID: 2119
			enm_IMono_Light_set_visibility,
			// Token: 0x04000848 RID: 2120
			enm_IMono_Light_set_volumetric_properties,
			// Token: 0x04000849 RID: 2121
			enm_IMono_ManagedMeshEditOperations_add_face,
			// Token: 0x0400084A RID: 2122
			enm_IMono_ManagedMeshEditOperations_add_face_corner1,
			// Token: 0x0400084B RID: 2123
			enm_IMono_ManagedMeshEditOperations_add_face_corner2,
			// Token: 0x0400084C RID: 2124
			enm_IMono_ManagedMeshEditOperations_add_line,
			// Token: 0x0400084D RID: 2125
			enm_IMono_ManagedMeshEditOperations_add_mesh,
			// Token: 0x0400084E RID: 2126
			enm_IMono_ManagedMeshEditOperations_add_mesh_aux,
			// Token: 0x0400084F RID: 2127
			enm_IMono_ManagedMeshEditOperations_add_mesh_to_bone,
			// Token: 0x04000850 RID: 2128
			enm_IMono_ManagedMeshEditOperations_add_mesh_with_color,
			// Token: 0x04000851 RID: 2129
			enm_IMono_ManagedMeshEditOperations_add_mesh_with_fixed_normals,
			// Token: 0x04000852 RID: 2130
			enm_IMono_ManagedMeshEditOperations_add_mesh_with_fixed_normals_with_height_gradient_color,
			// Token: 0x04000853 RID: 2131
			enm_IMono_ManagedMeshEditOperations_add_mesh_with_skin_data,
			// Token: 0x04000854 RID: 2132
			enm_IMono_ManagedMeshEditOperations_add_rect,
			// Token: 0x04000855 RID: 2133
			enm_IMono_ManagedMeshEditOperations_add_rectangle,
			// Token: 0x04000856 RID: 2134
			enm_IMono_ManagedMeshEditOperations_add_rectangle_with_inverse_uv,
			// Token: 0x04000857 RID: 2135
			enm_IMono_ManagedMeshEditOperations_add_rect_z_up,
			// Token: 0x04000858 RID: 2136
			enm_IMono_ManagedMeshEditOperations_add_skinned_mesh_with_color,
			// Token: 0x04000859 RID: 2137
			enm_IMono_ManagedMeshEditOperations_add_triangle1,
			// Token: 0x0400085A RID: 2138
			enm_IMono_ManagedMeshEditOperations_add_triangle2,
			// Token: 0x0400085B RID: 2139
			enm_IMono_ManagedMeshEditOperations_add_vertex,
			// Token: 0x0400085C RID: 2140
			enm_IMono_ManagedMeshEditOperations_apply_cpu_skinning,
			// Token: 0x0400085D RID: 2141
			enm_IMono_ManagedMeshEditOperations_clear_all,
			// Token: 0x0400085E RID: 2142
			enm_IMono_ManagedMeshEditOperations_compute_corner_normals,
			// Token: 0x0400085F RID: 2143
			enm_IMono_ManagedMeshEditOperations_compute_corner_normals_with_smoothing_data,
			// Token: 0x04000860 RID: 2144
			enm_IMono_ManagedMeshEditOperations_compute_tangents,
			// Token: 0x04000861 RID: 2145
			enm_IMono_ManagedMeshEditOperations_create,
			// Token: 0x04000862 RID: 2146
			enm_IMono_ManagedMeshEditOperations_ensure_transformed_vertices,
			// Token: 0x04000863 RID: 2147
			enm_IMono_ManagedMeshEditOperations_finalize_editing,
			// Token: 0x04000864 RID: 2148
			enm_IMono_ManagedMeshEditOperations_generate_grid,
			// Token: 0x04000865 RID: 2149
			enm_IMono_ManagedMeshEditOperations_get_position_of_vertex,
			// Token: 0x04000866 RID: 2150
			enm_IMono_ManagedMeshEditOperations_get_vertex_color,
			// Token: 0x04000867 RID: 2151
			enm_IMono_ManagedMeshEditOperations_get_vertex_color_alpha,
			// Token: 0x04000868 RID: 2152
			enm_IMono_ManagedMeshEditOperations_invert_faces_winding_order,
			// Token: 0x04000869 RID: 2153
			enm_IMono_ManagedMeshEditOperations_move_vertices_along_normal,
			// Token: 0x0400086A RID: 2154
			enm_IMono_ManagedMeshEditOperations_remove_duplicated_corners,
			// Token: 0x0400086B RID: 2155
			enm_IMono_ManagedMeshEditOperations_remove_face,
			// Token: 0x0400086C RID: 2156
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d,
			// Token: 0x0400086D RID: 2157
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d_repeat_x,
			// Token: 0x0400086E RID: 2158
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d_repeat_x_with_tiling,
			// Token: 0x0400086F RID: 2159
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d_repeat_y,
			// Token: 0x04000870 RID: 2160
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d_repeat_y_with_tiling,
			// Token: 0x04000871 RID: 2161
			enm_IMono_ManagedMeshEditOperations_rescale_mesh_2d_without_changing_uv,
			// Token: 0x04000872 RID: 2162
			enm_IMono_ManagedMeshEditOperations_reserve_face_corners,
			// Token: 0x04000873 RID: 2163
			enm_IMono_ManagedMeshEditOperations_reserve_faces,
			// Token: 0x04000874 RID: 2164
			enm_IMono_ManagedMeshEditOperations_reserve_vertices,
			// Token: 0x04000875 RID: 2165
			enm_IMono_ManagedMeshEditOperations_scale_vertices1,
			// Token: 0x04000876 RID: 2166
			enm_IMono_ManagedMeshEditOperations_scale_vertices2,
			// Token: 0x04000877 RID: 2167
			enm_IMono_ManagedMeshEditOperations_set_corner_vertex_uv,
			// Token: 0x04000878 RID: 2168
			enm_IMono_ManagedMeshEditOperations_set_corner_vertex_color,
			// Token: 0x04000879 RID: 2169
			enm_IMono_ManagedMeshEditOperations_set_position_of_vertex,
			// Token: 0x0400087A RID: 2170
			enm_IMono_ManagedMeshEditOperations_set_tangents_of_face_corner,
			// Token: 0x0400087B RID: 2171
			enm_IMono_ManagedMeshEditOperations_set_vertex_color,
			// Token: 0x0400087C RID: 2172
			enm_IMono_ManagedMeshEditOperations_set_vertex_color_alpha,
			// Token: 0x0400087D RID: 2173
			enm_IMono_ManagedMeshEditOperations_transform_vertices_to_local,
			// Token: 0x0400087E RID: 2174
			enm_IMono_ManagedMeshEditOperations_transform_vertices_to_parent,
			// Token: 0x0400087F RID: 2175
			enm_IMono_ManagedMeshEditOperations_translate_vertices,
			// Token: 0x04000880 RID: 2176
			enm_IMono_ManagedMeshEditOperations_update_overlapped_vertex_normals,
			// Token: 0x04000881 RID: 2177
			enm_IMono_ManagedMeshEditOperations_weld,
			// Token: 0x04000882 RID: 2178
			enm_IMono_Material_add_material_shader_flag,
			// Token: 0x04000883 RID: 2179
			enm_IMono_Material_create_copy,
			// Token: 0x04000884 RID: 2180
			enm_IMono_Material_get_alpha_blend_mode,
			// Token: 0x04000885 RID: 2181
			enm_IMono_Material_get_alpha_test_value,
			// Token: 0x04000886 RID: 2182
			enm_IMono_Material_get_default_material,
			// Token: 0x04000887 RID: 2183
			enm_IMono_Material_get_flags,
			// Token: 0x04000888 RID: 2184
			enm_IMono_Material_get_from_resource,
			// Token: 0x04000889 RID: 2185
			enm_IMono_Material_get_name,
			// Token: 0x0400088A RID: 2186
			enm_IMono_Material_get_outline_material,
			// Token: 0x0400088B RID: 2187
			enm_IMono_Material_get_shader,
			// Token: 0x0400088C RID: 2188
			enm_IMono_Material_get_shader_flags,
			// Token: 0x0400088D RID: 2189
			enm_IMono_Material_get_texture,
			// Token: 0x0400088E RID: 2190
			enm_IMono_Material_release,
			// Token: 0x0400088F RID: 2191
			enm_IMono_Material_remove_material_shader_flag,
			// Token: 0x04000890 RID: 2192
			enm_IMono_Material_set_alpha_blend_mode,
			// Token: 0x04000891 RID: 2193
			enm_IMono_Material_set_alpha_test_value,
			// Token: 0x04000892 RID: 2194
			enm_IMono_Material_set_area_map_scale,
			// Token: 0x04000893 RID: 2195
			enm_IMono_Material_set_enable_skinning,
			// Token: 0x04000894 RID: 2196
			enm_IMono_Material_set_flags,
			// Token: 0x04000895 RID: 2197
			enm_IMono_Material_set_mesh_vector_argument,
			// Token: 0x04000896 RID: 2198
			enm_IMono_Material_set_name,
			// Token: 0x04000897 RID: 2199
			enm_IMono_Material_set_shader,
			// Token: 0x04000898 RID: 2200
			enm_IMono_Material_set_shader_flags,
			// Token: 0x04000899 RID: 2201
			enm_IMono_Material_set_texture,
			// Token: 0x0400089A RID: 2202
			enm_IMono_Material_set_texture_at_slot,
			// Token: 0x0400089B RID: 2203
			enm_IMono_Material_using_skinning,
			// Token: 0x0400089C RID: 2204
			enm_IMono_Mesh_add_edit_data_user,
			// Token: 0x0400089D RID: 2205
			enm_IMono_Mesh_add_face,
			// Token: 0x0400089E RID: 2206
			enm_IMono_Mesh_add_face_corner,
			// Token: 0x0400089F RID: 2207
			enm_IMono_Mesh_add_mesh_to_mesh,
			// Token: 0x040008A0 RID: 2208
			enm_IMono_Mesh_add_triangle,
			// Token: 0x040008A1 RID: 2209
			enm_IMono_Mesh_add_triangle_with_vertex_colors,
			// Token: 0x040008A2 RID: 2210
			enm_IMono_Mesh_clear_mesh,
			// Token: 0x040008A3 RID: 2211
			enm_IMono_Mesh_compute_normals,
			// Token: 0x040008A4 RID: 2212
			enm_IMono_Mesh_compute_tangents,
			// Token: 0x040008A5 RID: 2213
			enm_IMono_Mesh_create_mesh,
			// Token: 0x040008A6 RID: 2214
			enm_IMono_Mesh_create_mesh_copy,
			// Token: 0x040008A7 RID: 2215
			enm_IMono_Mesh_create_mesh_with_material,
			// Token: 0x040008A8 RID: 2216
			enm_IMono_Mesh_disable_contour,
			// Token: 0x040008A9 RID: 2217
			enm_IMono_Mesh_get_base_mesh,
			// Token: 0x040008AA RID: 2218
			enm_IMono_Mesh_get_billboard,
			// Token: 0x040008AB RID: 2219
			enm_IMono_Mesh_get_bounding_box_height,
			// Token: 0x040008AC RID: 2220
			enm_IMono_Mesh_get_bounding_box_max,
			// Token: 0x040008AD RID: 2221
			enm_IMono_Mesh_get_bounding_box_min,
			// Token: 0x040008AE RID: 2222
			enm_IMono_Mesh_get_bounding_box_width,
			// Token: 0x040008AF RID: 2223
			enm_IMono_Mesh_get_cloth_linear_velocity_multiplier,
			// Token: 0x040008B0 RID: 2224
			enm_IMono_Mesh_get_color,
			// Token: 0x040008B1 RID: 2225
			enm_IMono_Mesh_get_color_2,
			// Token: 0x040008B2 RID: 2226
			enm_IMono_Mesh_get_edit_data_face_corner_count,
			// Token: 0x040008B3 RID: 2227
			enm_IMono_Mesh_get_edit_data_face_corner_vertex_color,
			// Token: 0x040008B4 RID: 2228
			enm_IMono_Mesh_get_face_corner_count,
			// Token: 0x040008B5 RID: 2229
			enm_IMono_Mesh_get_face_count,
			// Token: 0x040008B6 RID: 2230
			enm_IMono_Mesh_get_local_frame,
			// Token: 0x040008B7 RID: 2231
			enm_IMono_Mesh_get_material,
			// Token: 0x040008B8 RID: 2232
			enm_IMono_Mesh_get_mesh_from_resource,
			// Token: 0x040008B9 RID: 2233
			enm_IMono_Mesh_get_name,
			// Token: 0x040008BA RID: 2234
			enm_IMono_Mesh_get_random_mesh_with_vdecl,
			// Token: 0x040008BB RID: 2235
			enm_IMono_Mesh_get_second_material,
			// Token: 0x040008BC RID: 2236
			enm_IMono_Mesh_get_vector_argument,
			// Token: 0x040008BD RID: 2237
			enm_IMono_Mesh_get_vector_argument_2,
			// Token: 0x040008BE RID: 2238
			enm_IMono_Mesh_get_visibility_mask,
			// Token: 0x040008BF RID: 2239
			enm_IMono_Mesh_has_cloth,
			// Token: 0x040008C0 RID: 2240
			enm_IMono_Mesh_has_tag,
			// Token: 0x040008C1 RID: 2241
			enm_IMono_Mesh_hint_indices_dynamic,
			// Token: 0x040008C2 RID: 2242
			enm_IMono_Mesh_hint_vertices_dynamic,
			// Token: 0x040008C3 RID: 2243
			enm_IMono_Mesh_lock_edit_data_write,
			// Token: 0x040008C4 RID: 2244
			enm_IMono_Mesh_preload_for_rendering,
			// Token: 0x040008C5 RID: 2245
			enm_IMono_Mesh_recompute_bounding_box,
			// Token: 0x040008C6 RID: 2246
			enm_IMono_Mesh_release_edit_data_user,
			// Token: 0x040008C7 RID: 2247
			enm_IMono_Mesh_release_resources,
			// Token: 0x040008C8 RID: 2248
			enm_IMono_Mesh_set_additional_bone_frame,
			// Token: 0x040008C9 RID: 2249
			enm_IMono_Mesh_set_as_not_effected_by_season,
			// Token: 0x040008CA RID: 2250
			enm_IMono_Mesh_set_billboard,
			// Token: 0x040008CB RID: 2251
			enm_IMono_Mesh_set_color,
			// Token: 0x040008CC RID: 2252
			enm_IMono_Mesh_set_color_2,
			// Token: 0x040008CD RID: 2253
			enm_IMono_Mesh_set_color_alpha,
			// Token: 0x040008CE RID: 2254
			enm_IMono_Mesh_set_color_and_stroke,
			// Token: 0x040008CF RID: 2255
			enm_IMono_Mesh_set_contour_color,
			// Token: 0x040008D0 RID: 2256
			enm_IMono_Mesh_set_culling_mode,
			// Token: 0x040008D1 RID: 2257
			enm_IMono_Mesh_set_custom_clip_plane,
			// Token: 0x040008D2 RID: 2258
			enm_IMono_Mesh_set_edit_data_face_corner_vertex_color,
			// Token: 0x040008D3 RID: 2259
			enm_IMono_Mesh_set_edit_data_policy,
			// Token: 0x040008D4 RID: 2260
			enm_IMono_Mesh_set_external_bounding_box,
			// Token: 0x040008D5 RID: 2261
			enm_IMono_Mesh_set_local_frame,
			// Token: 0x040008D6 RID: 2262
			enm_IMono_Mesh_set_material,
			// Token: 0x040008D7 RID: 2263
			enm_IMono_Mesh_set_material_by_name,
			// Token: 0x040008D8 RID: 2264
			enm_IMono_Mesh_set_mesh_render_order,
			// Token: 0x040008D9 RID: 2265
			enm_IMono_Mesh_set_morph_time,
			// Token: 0x040008DA RID: 2266
			enm_IMono_Mesh_set_name,
			// Token: 0x040008DB RID: 2267
			enm_IMono_Mesh_setup_additional_bone_buffer,
			// Token: 0x040008DC RID: 2268
			enm_IMono_Mesh_set_vector_argument,
			// Token: 0x040008DD RID: 2269
			enm_IMono_Mesh_set_vector_argument_2,
			// Token: 0x040008DE RID: 2270
			enm_IMono_Mesh_set_visibility_mask,
			// Token: 0x040008DF RID: 2271
			enm_IMono_Mesh_unlock_edit_data_write,
			// Token: 0x040008E0 RID: 2272
			enm_IMono_Mesh_update_bounding_box,
			// Token: 0x040008E1 RID: 2273
			enm_IMono_MeshBuilder_create_tiling_button_mesh,
			// Token: 0x040008E2 RID: 2274
			enm_IMono_MeshBuilder_create_tiling_window_mesh,
			// Token: 0x040008E3 RID: 2275
			enm_IMono_MeshBuilder_finalize_mesh_builder,
			// Token: 0x040008E4 RID: 2276
			enm_IMono_MetaMesh_add_edit_data_user,
			// Token: 0x040008E5 RID: 2277
			enm_IMono_MetaMesh_add_mesh,
			// Token: 0x040008E6 RID: 2278
			enm_IMono_MetaMesh_add_meta_mesh,
			// Token: 0x040008E7 RID: 2279
			enm_IMono_MetaMesh_assign_cloth_body_from,
			// Token: 0x040008E8 RID: 2280
			enm_IMono_MetaMesh_batch_with_meta_mesh,
			// Token: 0x040008E9 RID: 2281
			enm_IMono_MetaMesh_batch_with_meta_mesh_multiple,
			// Token: 0x040008EA RID: 2282
			enm_IMono_MetaMesh_check_meta_mesh_existence,
			// Token: 0x040008EB RID: 2283
			enm_IMono_MetaMesh_check_resources,
			// Token: 0x040008EC RID: 2284
			enm_IMono_MetaMesh_clear_edit_data,
			// Token: 0x040008ED RID: 2285
			enm_IMono_MetaMesh_clear_meshes,
			// Token: 0x040008EE RID: 2286
			enm_IMono_MetaMesh_clear_meshes_for_lod,
			// Token: 0x040008EF RID: 2287
			enm_IMono_MetaMesh_clear_meshes_for_lower_lods,
			// Token: 0x040008F0 RID: 2288
			enm_IMono_MetaMesh_clear_meshes_for_other_lods,
			// Token: 0x040008F1 RID: 2289
			enm_IMono_MetaMesh_copy_to,
			// Token: 0x040008F2 RID: 2290
			enm_IMono_MetaMesh_create_copy,
			// Token: 0x040008F3 RID: 2291
			enm_IMono_MetaMesh_create_copy_from_name,
			// Token: 0x040008F4 RID: 2292
			enm_IMono_MetaMesh_create_meta_mesh,
			// Token: 0x040008F5 RID: 2293
			enm_IMono_MetaMesh_draw_text_with_default_font,
			// Token: 0x040008F6 RID: 2294
			enm_IMono_MetaMesh_get_all_multi_meshes,
			// Token: 0x040008F7 RID: 2295
			enm_IMono_MetaMesh_get_bounding_box,
			// Token: 0x040008F8 RID: 2296
			enm_IMono_MetaMesh_get_factor_1,
			// Token: 0x040008F9 RID: 2297
			enm_IMono_MetaMesh_get_factor_2,
			// Token: 0x040008FA RID: 2298
			enm_IMono_MetaMesh_get_frame,
			// Token: 0x040008FB RID: 2299
			enm_IMono_MetaMesh_get_lod_mask_for_mesh_at_index,
			// Token: 0x040008FC RID: 2300
			enm_IMono_MetaMesh_get_mesh_at_index,
			// Token: 0x040008FD RID: 2301
			enm_IMono_MetaMesh_get_mesh_count,
			// Token: 0x040008FE RID: 2302
			enm_IMono_MetaMesh_get_mesh_count_with_tag,
			// Token: 0x040008FF RID: 2303
			enm_IMono_MetaMesh_get_morphed_copy,
			// Token: 0x04000900 RID: 2304
			enm_IMono_MetaMesh_get_multi_mesh,
			// Token: 0x04000901 RID: 2305
			enm_IMono_MetaMesh_get_multi_mesh_count,
			// Token: 0x04000902 RID: 2306
			enm_IMono_MetaMesh_get_name,
			// Token: 0x04000903 RID: 2307
			enm_IMono_MetaMesh_get_total_gpu_size,
			// Token: 0x04000904 RID: 2308
			enm_IMono_MetaMesh_get_vector_argument_2,
			// Token: 0x04000905 RID: 2309
			enm_IMono_MetaMesh_get_vector_user_data,
			// Token: 0x04000906 RID: 2310
			enm_IMono_MetaMesh_get_visibility_mask,
			// Token: 0x04000907 RID: 2311
			enm_IMono_MetaMesh_has_any_generated_lods,
			// Token: 0x04000908 RID: 2312
			enm_IMono_MetaMesh_has_any_lods,
			// Token: 0x04000909 RID: 2313
			enm_IMono_MetaMesh_has_cloth_simulation_data,
			// Token: 0x0400090A RID: 2314
			enm_IMono_MetaMesh_has_vertex_buffer_or_edit_data_or_package_item,
			// Token: 0x0400090B RID: 2315
			enm_IMono_MetaMesh_merge_with_meta_mesh,
			// Token: 0x0400090C RID: 2316
			enm_IMono_MetaMesh_preload_for_rendering,
			// Token: 0x0400090D RID: 2317
			enm_IMono_MetaMesh_preload_shaders,
			// Token: 0x0400090E RID: 2318
			enm_IMono_MetaMesh_recompute_bounding_box,
			// Token: 0x0400090F RID: 2319
			enm_IMono_MetaMesh_release,
			// Token: 0x04000910 RID: 2320
			enm_IMono_MetaMesh_release_edit_data_user,
			// Token: 0x04000911 RID: 2321
			enm_IMono_MetaMesh_remove_meshes_without_tag,
			// Token: 0x04000912 RID: 2322
			enm_IMono_MetaMesh_remove_meshes_with_tag,
			// Token: 0x04000913 RID: 2323
			enm_IMono_MetaMesh_set_billboarding,
			// Token: 0x04000914 RID: 2324
			enm_IMono_MetaMesh_set_contour_color,
			// Token: 0x04000915 RID: 2325
			enm_IMono_MetaMesh_set_contour_state,
			// Token: 0x04000916 RID: 2326
			enm_IMono_MetaMesh_set_cull_mode,
			// Token: 0x04000917 RID: 2327
			enm_IMono_MetaMesh_set_edit_data_policy,
			// Token: 0x04000918 RID: 2328
			enm_IMono_MetaMesh_set_factor_1,
			// Token: 0x04000919 RID: 2329
			enm_IMono_MetaMesh_set_factor_1_linear,
			// Token: 0x0400091A RID: 2330
			enm_IMono_MetaMesh_set_factor_2,
			// Token: 0x0400091B RID: 2331
			enm_IMono_MetaMesh_set_factor_2_linear,
			// Token: 0x0400091C RID: 2332
			enm_IMono_MetaMesh_set_factor_color_to_sub_meshes_with_tag,
			// Token: 0x0400091D RID: 2333
			enm_IMono_MetaMesh_set_frame,
			// Token: 0x0400091E RID: 2334
			enm_IMono_MetaMesh_set_gloss_multiplier,
			// Token: 0x0400091F RID: 2335
			enm_IMono_MetaMesh_set_lod_bias,
			// Token: 0x04000920 RID: 2336
			enm_IMono_MetaMesh_set_material,
			// Token: 0x04000921 RID: 2337
			enm_IMono_MetaMesh_set_material_to_sub_meshes_with_tag,
			// Token: 0x04000922 RID: 2338
			enm_IMono_MetaMesh_set_num_lods,
			// Token: 0x04000923 RID: 2339
			enm_IMono_MetaMesh_set_shader_to_material,
			// Token: 0x04000924 RID: 2340
			enm_IMono_MetaMesh_set_vector_argument,
			// Token: 0x04000925 RID: 2341
			enm_IMono_MetaMesh_set_vector_argument_2,
			// Token: 0x04000926 RID: 2342
			enm_IMono_MetaMesh_set_vector_user_data,
			// Token: 0x04000927 RID: 2343
			enm_IMono_MetaMesh_set_visibility_mask,
			// Token: 0x04000928 RID: 2344
			enm_IMono_MetaMesh_use_head_bone_facegen_scaling,
			// Token: 0x04000929 RID: 2345
			enm_IMono_MouseManager_activate_mouse_cursor,
			// Token: 0x0400092A RID: 2346
			enm_IMono_MouseManager_lock_cursor_at_current_pos,
			// Token: 0x0400092B RID: 2347
			enm_IMono_MouseManager_lock_cursor_at_position,
			// Token: 0x0400092C RID: 2348
			enm_IMono_MouseManager_set_mouse_cursor,
			// Token: 0x0400092D RID: 2349
			enm_IMono_MouseManager_show_cursor,
			// Token: 0x0400092E RID: 2350
			enm_IMono_MouseManager_unlock_cursor,
			// Token: 0x0400092F RID: 2351
			enm_IMono_Music_get_free_music_channel_index,
			// Token: 0x04000930 RID: 2352
			enm_IMono_Music_is_clip_loaded,
			// Token: 0x04000931 RID: 2353
			enm_IMono_Music_is_music_playing,
			// Token: 0x04000932 RID: 2354
			enm_IMono_Music_load_clip,
			// Token: 0x04000933 RID: 2355
			enm_IMono_Music_pause_music,
			// Token: 0x04000934 RID: 2356
			enm_IMono_Music_play_delayed,
			// Token: 0x04000935 RID: 2357
			enm_IMono_Music_play_music,
			// Token: 0x04000936 RID: 2358
			enm_IMono_Music_set_volume,
			// Token: 0x04000937 RID: 2359
			enm_IMono_Music_stop_music,
			// Token: 0x04000938 RID: 2360
			enm_IMono_Music_unload_clip,
			// Token: 0x04000939 RID: 2361
			enm_IMono_ParticleSystem_create_particle_system_attached_to_bone,
			// Token: 0x0400093A RID: 2362
			enm_IMono_ParticleSystem_create_particle_system_attached_to_entity,
			// Token: 0x0400093B RID: 2363
			enm_IMono_ParticleSystem_get_local_frame,
			// Token: 0x0400093C RID: 2364
			enm_IMono_ParticleSystem_get_runtime_id_by_name,
			// Token: 0x0400093D RID: 2365
			enm_IMono_ParticleSystem_has_alive_particles,
			// Token: 0x0400093E RID: 2366
			enm_IMono_ParticleSystem_restart,
			// Token: 0x0400093F RID: 2367
			enm_IMono_ParticleSystem_set_dont_remove_from_entity,
			// Token: 0x04000940 RID: 2368
			enm_IMono_ParticleSystem_set_enable,
			// Token: 0x04000941 RID: 2369
			enm_IMono_ParticleSystem_set_local_frame,
			// Token: 0x04000942 RID: 2370
			enm_IMono_ParticleSystem_set_particle_effect_by_name,
			// Token: 0x04000943 RID: 2371
			enm_IMono_ParticleSystem_set_previous_global_frame,
			// Token: 0x04000944 RID: 2372
			enm_IMono_ParticleSystem_set_runtime_emission_rate_multiplier,
			// Token: 0x04000945 RID: 2373
			enm_IMono_Path_add_path_point,
			// Token: 0x04000946 RID: 2374
			enm_IMono_Path_delete_path_point,
			// Token: 0x04000947 RID: 2375
			enm_IMono_Path_get_arc_length,
			// Token: 0x04000948 RID: 2376
			enm_IMono_Path_get_hermite_frame_and_color_wrt_distance,
			// Token: 0x04000949 RID: 2377
			enm_IMono_Path_get_hermite_frame_wrt_distance,
			// Token: 0x0400094A RID: 2378
			enm_IMono_Path_get_hermite_frame_wrt_dt,
			// Token: 0x0400094B RID: 2379
			enm_IMono_Path_get_name,
			// Token: 0x0400094C RID: 2380
			enm_IMono_Path_get_nearest_hermite_frame_with_valid_alpha_wrt_distance,
			// Token: 0x0400094D RID: 2381
			enm_IMono_Path_get_number_of_points,
			// Token: 0x0400094E RID: 2382
			enm_IMono_Path_get_points,
			// Token: 0x0400094F RID: 2383
			enm_IMono_Path_get_path_length,
			// Token: 0x04000950 RID: 2384
			enm_IMono_Path_get_path_version,
			// Token: 0x04000951 RID: 2385
			enm_IMono_Path_has_valid_alpha_at_path_point,
			// Token: 0x04000952 RID: 2386
			enm_IMono_Path_set_frame_of_point,
			// Token: 0x04000953 RID: 2387
			enm_IMono_Path_set_tangent_position_of_point,
			// Token: 0x04000954 RID: 2388
			enm_IMono_PhysicsMaterial_get_angular_damping_at_index,
			// Token: 0x04000955 RID: 2389
			enm_IMono_PhysicsMaterial_get_dynamic_friction_at_index,
			// Token: 0x04000956 RID: 2390
			enm_IMono_PhysicsMaterial_get_material_flags_at_index,
			// Token: 0x04000957 RID: 2391
			enm_IMono_PhysicsMaterial_get_index_with_name,
			// Token: 0x04000958 RID: 2392
			enm_IMono_PhysicsMaterial_get_linear_damping_at_index,
			// Token: 0x04000959 RID: 2393
			enm_IMono_PhysicsMaterial_get_material_count,
			// Token: 0x0400095A RID: 2394
			enm_IMono_PhysicsMaterial_get_material_name_at_index,
			// Token: 0x0400095B RID: 2395
			enm_IMono_PhysicsMaterial_get_restitution_at_index,
			// Token: 0x0400095C RID: 2396
			enm_IMono_PhysicsMaterial_get_static_friction_at_index,
			// Token: 0x0400095D RID: 2397
			enm_IMono_PhysicsShape_add_capsule,
			// Token: 0x0400095E RID: 2398
			enm_IMono_PhysicsShape_add_preload_queue_with_name,
			// Token: 0x0400095F RID: 2399
			enm_IMono_PhysicsShape_add_sphere,
			// Token: 0x04000960 RID: 2400
			enm_IMono_PhysicsShape_capsule_count,
			// Token: 0x04000961 RID: 2401
			enm_IMono_PhysicsShape_clear,
			// Token: 0x04000962 RID: 2402
			enm_IMono_PhysicsShape_create_body_copy,
			// Token: 0x04000963 RID: 2403
			enm_IMono_PhysicsShape_get_bounding_box,
			// Token: 0x04000964 RID: 2404
			enm_IMono_PhysicsShape_get_bounding_box_center,
			// Token: 0x04000965 RID: 2405
			enm_IMono_PhysicsShape_get_capsule,
			// Token: 0x04000966 RID: 2406
			enm_IMono_PhysicsShape_get_capsule_with_material,
			// Token: 0x04000967 RID: 2407
			enm_IMono_PhysicsShape_get_dominant_material_index_for_mesh_at_index,
			// Token: 0x04000968 RID: 2408
			enm_IMono_PhysicsShape_get_from_resource,
			// Token: 0x04000969 RID: 2409
			enm_IMono_PhysicsShape_get_name,
			// Token: 0x0400096A RID: 2410
			enm_IMono_PhysicsShape_get_sphere,
			// Token: 0x0400096B RID: 2411
			enm_IMono_PhysicsShape_get_sphere_with_material,
			// Token: 0x0400096C RID: 2412
			enm_IMono_PhysicsShape_get_triangle,
			// Token: 0x0400096D RID: 2413
			enm_IMono_PhysicsShape_init_description,
			// Token: 0x0400096E RID: 2414
			enm_IMono_PhysicsShape_prepare,
			// Token: 0x0400096F RID: 2415
			enm_IMono_PhysicsShape_process_preload_queue,
			// Token: 0x04000970 RID: 2416
			enm_IMono_PhysicsShape_set_capsule,
			// Token: 0x04000971 RID: 2417
			enm_IMono_PhysicsShape_sphere_count,
			// Token: 0x04000972 RID: 2418
			enm_IMono_PhysicsShape_transform,
			// Token: 0x04000973 RID: 2419
			enm_IMono_PhysicsShape_triangle_count_in_triangle_mesh,
			// Token: 0x04000974 RID: 2420
			enm_IMono_PhysicsShape_triangle_mesh_count,
			// Token: 0x04000975 RID: 2421
			enm_IMono_PhysicsShape_unload_dynamic_bodies,
			// Token: 0x04000976 RID: 2422
			enm_IMono_Scene_add_always_rendered_skeleton,
			// Token: 0x04000977 RID: 2423
			enm_IMono_Scene_add_decal_instance,
			// Token: 0x04000978 RID: 2424
			enm_IMono_Scene_add_directional_light,
			// Token: 0x04000979 RID: 2425
			enm_IMono_Scene_add_entity_with_mesh,
			// Token: 0x0400097A RID: 2426
			enm_IMono_Scene_add_entity_with_multi_mesh,
			// Token: 0x0400097B RID: 2427
			enm_IMono_Scene_add_item_entity,
			// Token: 0x0400097C RID: 2428
			enm_IMono_Scene_add_path,
			// Token: 0x0400097D RID: 2429
			enm_IMono_Scene_add_path_point,
			// Token: 0x0400097E RID: 2430
			enm_IMono_Scene_add_point_light,
			// Token: 0x0400097F RID: 2431
			enm_IMono_Scene_add_water_wake_with_capsule,
			// Token: 0x04000980 RID: 2432
			enm_IMono_Scene_attach_entity,
			// Token: 0x04000981 RID: 2433
			enm_IMono_Scene_box_cast,
			// Token: 0x04000982 RID: 2434
			enm_IMono_Scene_box_cast_only_for_camera,
			// Token: 0x04000983 RID: 2435
			enm_IMono_Scene_calculate_effective_lighting,
			// Token: 0x04000984 RID: 2436
			enm_IMono_Scene_check_path_entities_frame_changed,
			// Token: 0x04000985 RID: 2437
			enm_IMono_Scene_check_point_can_see_point,
			// Token: 0x04000986 RID: 2438
			enm_IMono_Scene_check_resources,
			// Token: 0x04000987 RID: 2439
			enm_IMono_Scene_clear_all,
			// Token: 0x04000988 RID: 2440
			enm_IMono_Scene_clear_current_frame_tick_entities,
			// Token: 0x04000989 RID: 2441
			enm_IMono_Scene_clear_decals,
			// Token: 0x0400098A RID: 2442
			enm_IMono_Scene_clear_nav_mesh,
			// Token: 0x0400098B RID: 2443
			enm_IMono_Scene_contains_terrain,
			// Token: 0x0400098C RID: 2444
			enm_IMono_Scene_create_burst_particle,
			// Token: 0x0400098D RID: 2445
			enm_IMono_Scene_create_dynamic_rain_texture,
			// Token: 0x0400098E RID: 2446
			enm_IMono_Scene_create_new_scene,
			// Token: 0x0400098F RID: 2447
			enm_IMono_Scene_create_path_mesh,
			// Token: 0x04000990 RID: 2448
			enm_IMono_Scene_create_path_mesh2,
			// Token: 0x04000991 RID: 2449
			enm_IMono_Scene_delete_path_with_name,
			// Token: 0x04000992 RID: 2450
			enm_IMono_Scene_delete_water_wake_renderer,
			// Token: 0x04000993 RID: 2451
			enm_IMono_Scene_deregister_ship_visual,
			// Token: 0x04000994 RID: 2452
			enm_IMono_Scene_disable_static_shadows,
			// Token: 0x04000995 RID: 2453
			enm_IMono_Scene_does_path_exist_between_faces,
			// Token: 0x04000996 RID: 2454
			enm_IMono_Scene_does_path_exist_between_positions,
			// Token: 0x04000997 RID: 2455
			enm_IMono_Scene_enable_fixed_tick,
			// Token: 0x04000998 RID: 2456
			enm_IMono_Scene_enable_inclusive_async_physx,
			// Token: 0x04000999 RID: 2457
			enm_IMono_Scene_ensure_postfx_system,
			// Token: 0x0400099A RID: 2458
			enm_IMono_Scene_ensure_water_wake_renderer,
			// Token: 0x0400099B RID: 2459
			enm_IMono_Scene_fill_entity_with_hard_border_physics_barrier,
			// Token: 0x0400099C RID: 2460
			enm_IMono_Scene_fill_terrain_height_data,
			// Token: 0x0400099D RID: 2461
			enm_IMono_Scene_fill_terrain_physics_material_index_data,
			// Token: 0x0400099E RID: 2462
			enm_IMono_Scene_find_closest_exit_position_for_position_on_a_boundary_face,
			// Token: 0x0400099F RID: 2463
			enm_IMono_Scene_finish_scene_sounds,
			// Token: 0x040009A0 RID: 2464
			enm_IMono_Scene_focus_ray_cast_for_fixed_physics,
			// Token: 0x040009A1 RID: 2465
			enm_IMono_Scene_force_load_resources,
			// Token: 0x040009A2 RID: 2466
			enm_IMono_Scene_generate_contacts_with_capsule,
			// Token: 0x040009A3 RID: 2467
			enm_IMono_Scene_generate_contacts_with_capsule_against_entity,
			// Token: 0x040009A4 RID: 2468
			enm_IMono_Scene_get_all_color_grade_names,
			// Token: 0x040009A5 RID: 2469
			enm_IMono_Scene_get_all_entities_with_script_component,
			// Token: 0x040009A6 RID: 2470
			enm_IMono_Scene_get_all_filter_names,
			// Token: 0x040009A7 RID: 2471
			enm_IMono_Scene_get_all_nav_mesh_face_records,
			// Token: 0x040009A8 RID: 2472
			enm_IMono_Scene_get_bounding_box,
			// Token: 0x040009A9 RID: 2473
			enm_IMono_Scene_get_bulk_water_level_at_positions,
			// Token: 0x040009AA RID: 2474
			enm_IMono_Scene_get_bulk_water_level_at_volumes,
			// Token: 0x040009AB RID: 2475
			enm_IMono_Scene_get_campaign_entity_with_name,
			// Token: 0x040009AC RID: 2476
			enm_IMono_Scene_get_engine_physics_enabled,
			// Token: 0x040009AD RID: 2477
			enm_IMono_Scene_get_entities,
			// Token: 0x040009AE RID: 2478
			enm_IMono_Scene_get_entity_count,
			// Token: 0x040009AF RID: 2479
			enm_IMono_Scene_get_entity_with_guid,
			// Token: 0x040009B0 RID: 2480
			enm_IMono_Scene_get_fall_density,
			// Token: 0x040009B1 RID: 2481
			enm_IMono_Scene_get_first_entity_with_name,
			// Token: 0x040009B2 RID: 2482
			enm_IMono_Scene_get_first_entity_with_script_component,
			// Token: 0x040009B3 RID: 2483
			enm_IMono_Scene_get_flora_instance_count,
			// Token: 0x040009B4 RID: 2484
			enm_IMono_Scene_get_flora_renderer_texture_usage,
			// Token: 0x040009B5 RID: 2485
			enm_IMono_Scene_get_fog,
			// Token: 0x040009B6 RID: 2486
			enm_IMono_Scene_get_global_wind_strength_vector,
			// Token: 0x040009B7 RID: 2487
			enm_IMono_Scene_get_global_wind_velocity,
			// Token: 0x040009B8 RID: 2488
			enm_IMono_Scene_get_ground_height_and_body_flags_at_position,
			// Token: 0x040009B9 RID: 2489
			enm_IMono_Scene_get_ground_height_and_normal_at_position,
			// Token: 0x040009BA RID: 2490
			enm_IMono_Scene_get_ground_height_at_position,
			// Token: 0x040009BB RID: 2491
			enm_IMono_Scene_get_hard_boundary_vertex,
			// Token: 0x040009BC RID: 2492
			enm_IMono_Scene_get_hard_boundary_vertex_count,
			// Token: 0x040009BD RID: 2493
			enm_IMono_Scene_get_height_at_point,
			// Token: 0x040009BE RID: 2494
			enm_IMono_Scene_get_id_of_nav_mesh_face,
			// Token: 0x040009BF RID: 2495
			enm_IMono_Scene_get_interpolation_factor_for_body_world_transform_smoothing,
			// Token: 0x040009C0 RID: 2496
			enm_IMono_Scene_get_last_final_render_camera_frame,
			// Token: 0x040009C1 RID: 2497
			enm_IMono_Scene_get_last_final_render_camera_position,
			// Token: 0x040009C2 RID: 2498
			enm_IMono_Scene_get_last_point_on_navigation_mesh_from_position_to_destination,
			// Token: 0x040009C3 RID: 2499
			enm_IMono_Scene_get_last_point_on_navigation_mesh_from_world_position_to_destination,
			// Token: 0x040009C4 RID: 2500
			enm_IMono_Scene_get_last_position_on_nav_mesh_face_for_point_and_direction,
			// Token: 0x040009C5 RID: 2501
			enm_IMono_Scene_get_loading_state_name,
			// Token: 0x040009C6 RID: 2502
			enm_IMono_Scene_get_module_path,
			// Token: 0x040009C7 RID: 2503
			enm_IMono_Scene_get_name,
			// Token: 0x040009C8 RID: 2504
			enm_IMono_Scene_get_navigation_mesh_crc,
			// Token: 0x040009C9 RID: 2505
			enm_IMono_Scene_get_navigation_mesh_for_position,
			// Token: 0x040009CA RID: 2506
			enm_IMono_Scene_get_nav_mesh_face_center_position,
			// Token: 0x040009CB RID: 2507
			enm_IMono_Scene_get_nav_mesh_face_count,
			// Token: 0x040009CC RID: 2508
			enm_IMono_Scene_get_navmesh_face_count_between_two_ids,
			// Token: 0x040009CD RID: 2509
			enm_IMono_Scene_get_nav_mesh_face_first_vertex_z,
			// Token: 0x040009CE RID: 2510
			enm_IMono_Scene_get_nav_mesh_face_index_with_region,
			// Token: 0x040009CF RID: 2511
			enm_IMono_Scene_get_nav_mesh_face_index3,
			// Token: 0x040009D0 RID: 2512
			enm_IMono_Scene_get_navmesh_face_records_between_two_ids,
			// Token: 0x040009D1 RID: 2513
			enm_IMono_Scene_get_nav_mesh_path_face_record,
			// Token: 0x040009D2 RID: 2514
			enm_IMono_Scene_get_nearest_navigation_mesh_for_position,
			// Token: 0x040009D3 RID: 2515
			enm_IMono_Scene_get_node_data_count,
			// Token: 0x040009D4 RID: 2516
			enm_IMono_Scene_get_normal_at,
			// Token: 0x040009D5 RID: 2517
			enm_IMono_Scene_get_north_angle,
			// Token: 0x040009D6 RID: 2518
			enm_IMono_Scene_get_number_of_path_with_name_prefix,
			// Token: 0x040009D7 RID: 2519
			enm_IMono_Scene_get_path_between_ai_face_indices,
			// Token: 0x040009D8 RID: 2520
			enm_IMono_Scene_get_path_between_ai_face_indices_with_region_switch_cost,
			// Token: 0x040009D9 RID: 2521
			enm_IMono_Scene_get_path_between_ai_face_pointers,
			// Token: 0x040009DA RID: 2522
			enm_IMono_Scene_get_path_between_ai_face_pointers_with_region_switch_cost,
			// Token: 0x040009DB RID: 2523
			enm_IMono_Scene_get_path_distance_between_ai_faces,
			// Token: 0x040009DC RID: 2524
			enm_IMono_Scene_get_path_distance_between_positions,
			// Token: 0x040009DD RID: 2525
			enm_IMono_Scene_get_path_face_record_from_nav_mesh_face_pointer,
			// Token: 0x040009DE RID: 2526
			enm_IMono_Scene_get_paths_with_name_prefix,
			// Token: 0x040009DF RID: 2527
			enm_IMono_Scene_get_path_with_name,
			// Token: 0x040009E0 RID: 2528
			enm_IMono_Scene_get_photo_mode_focus,
			// Token: 0x040009E1 RID: 2529
			enm_IMono_Scene_get_photo_mode_fov,
			// Token: 0x040009E2 RID: 2530
			enm_IMono_Scene_get_photo_mode_on,
			// Token: 0x040009E3 RID: 2531
			enm_IMono_Scene_get_photo_mode_orbit,
			// Token: 0x040009E4 RID: 2532
			enm_IMono_Scene_get_photo_mode_roll,
			// Token: 0x040009E5 RID: 2533
			enm_IMono_Scene_get_physics_min_max,
			// Token: 0x040009E6 RID: 2534
			enm_IMono_Scene_get_rain_density,
			// Token: 0x040009E7 RID: 2535
			enm_IMono_Scene_get_root_entities,
			// Token: 0x040009E8 RID: 2536
			enm_IMono_Scene_get_root_entity_count,
			// Token: 0x040009E9 RID: 2537
			enm_IMono_Scene_get_scene_color_grade_index,
			// Token: 0x040009EA RID: 2538
			enm_IMono_Scene_get_scene_filter_index,
			// Token: 0x040009EB RID: 2539
			enm_IMono_Scene_get_scene_limits,
			// Token: 0x040009EC RID: 2540
			enm_IMono_Scene_get_scene_xml_crc,
			// Token: 0x040009ED RID: 2541
			enm_IMono_Scene_get_scripted_entity,
			// Token: 0x040009EE RID: 2542
			enm_IMono_Scene_get_scripted_entity_count,
			// Token: 0x040009EF RID: 2543
			enm_IMono_Scene_get_skybox_mesh,
			// Token: 0x040009F0 RID: 2544
			enm_IMono_Scene_get_snow_density,
			// Token: 0x040009F1 RID: 2545
			enm_IMono_Scene_get_soft_boundary_vertex,
			// Token: 0x040009F2 RID: 2546
			enm_IMono_Scene_get_soft_boundary_vertex_count,
			// Token: 0x040009F3 RID: 2547
			enm_IMono_Scene_get_sun_direction,
			// Token: 0x040009F4 RID: 2548
			enm_IMono_Scene_get_terrain_data,
			// Token: 0x040009F5 RID: 2549
			enm_IMono_Scene_get_terrain_height,
			// Token: 0x040009F6 RID: 2550
			enm_IMono_Scene_get_terrain_height_and_normal,
			// Token: 0x040009F7 RID: 2551
			enm_IMono_Scene_get_terrain_memory_usage,
			// Token: 0x040009F8 RID: 2552
			enm_IMono_Scene_get_terrain_min_max_height,
			// Token: 0x040009F9 RID: 2553
			enm_IMono_Scene_get_terrain_node_data,
			// Token: 0x040009FA RID: 2554
			enm_IMono_Scene_get_terrain_material_index_at_layer,
			// Token: 0x040009FB RID: 2555
			enm_IMono_Scene_get_time_of_day,
			// Token: 0x040009FC RID: 2556
			enm_IMono_Scene_get_time_speed,
			// Token: 0x040009FD RID: 2557
			enm_IMono_Scene_get_upgrade_level_count,
			// Token: 0x040009FE RID: 2558
			enm_IMono_Scene_get_upgrade_level_mask,
			// Token: 0x040009FF RID: 2559
			enm_IMono_Scene_get_upgrade_level_mask_of_level_name,
			// Token: 0x04000A00 RID: 2560
			enm_IMono_Scene_get_level_name_of_level_index,
			// Token: 0x04000A01 RID: 2561
			enm_IMono_Scene_get_water_level,
			// Token: 0x04000A02 RID: 2562
			enm_IMono_Scene_get_water_level_at_position,
			// Token: 0x04000A03 RID: 2563
			enm_IMono_Scene_get_water_speed_at_position,
			// Token: 0x04000A04 RID: 2564
			enm_IMono_Scene_get_water_strength,
			// Token: 0x04000A05 RID: 2565
			enm_IMono_Scene_get_flowmap_data,
			// Token: 0x04000A06 RID: 2566
			enm_IMono_Scene_get_winter_time_factor,
			// Token: 0x04000A07 RID: 2567
			enm_IMono_Scene_has_decal_renderer,
			// Token: 0x04000A08 RID: 2568
			enm_IMono_Scene_has_navmesh_face_unshared_edges,
			// Token: 0x04000A09 RID: 2569
			enm_IMono_Scene_has_terrain_heightmap,
			// Token: 0x04000A0A RID: 2570
			enm_IMono_Scene_invalidate_terrain_physics_materials,
			// Token: 0x04000A0B RID: 2571
			enm_IMono_Scene_is_any_face_with_id,
			// Token: 0x04000A0C RID: 2572
			enm_IMono_Scene_is_atmosphere_indoor,
			// Token: 0x04000A0D RID: 2573
			enm_IMono_Scene_is_default_editor_scene,
			// Token: 0x04000A0E RID: 2574
			enm_IMono_Scene_is_editor_scene,
			// Token: 0x04000A0F RID: 2575
			enm_IMono_Scene_is_line_to_point_clear,
			// Token: 0x04000A10 RID: 2576
			enm_IMono_Scene_is_line_to_point_clear2,
			// Token: 0x04000A11 RID: 2577
			enm_IMono_Scene_is_loading_finished,
			// Token: 0x04000A12 RID: 2578
			enm_IMono_Scene_is_multiplayer_scene,
			// Token: 0x04000A13 RID: 2579
			enm_IMono_Scene_is_position_on_a_dynamic_nav_mesh,
			// Token: 0x04000A14 RID: 2580
			enm_IMono_Scene_load_nav_mesh_prefab,
			// Token: 0x04000A15 RID: 2581
			enm_IMono_Scene_load_nav_mesh_prefab_with_frame,
			// Token: 0x04000A16 RID: 2582
			enm_IMono_Scene_mark_faces_with_id_as_ladder,
			// Token: 0x04000A17 RID: 2583
			enm_IMono_Scene_merge_faces_with_id,
			// Token: 0x04000A18 RID: 2584
			enm_IMono_Scene_optimize_scene,
			// Token: 0x04000A19 RID: 2585
			enm_IMono_Scene_pause_scene_sounds,
			// Token: 0x04000A1A RID: 2586
			enm_IMono_Scene_preload_for_rendering,
			// Token: 0x04000A1B RID: 2587
			enm_IMono_Scene_ray_cast_excluding_two_entities,
			// Token: 0x04000A1C RID: 2588
			enm_IMono_Scene_ray_cast_for_closest_entity_or_terrain,
			// Token: 0x04000A1D RID: 2589
			enm_IMono_Scene_ray_cast_for_closest_entity_or_terrain_ignore_entity,
			// Token: 0x04000A1E RID: 2590
			enm_IMono_Scene_ray_cast_for_ramming,
			// Token: 0x04000A1F RID: 2591
			enm_IMono_Scene_read,
			// Token: 0x04000A20 RID: 2592
			enm_IMono_Scene_read_and_calculate_initial_camera,
			// Token: 0x04000A21 RID: 2593
			enm_IMono_Scene_read_in_module,
			// Token: 0x04000A22 RID: 2594
			enm_IMono_Scene_register_ship_visual_to_water_renderer,
			// Token: 0x04000A23 RID: 2595
			enm_IMono_Scene_remove_always_rendered_skeleton,
			// Token: 0x04000A24 RID: 2596
			enm_IMono_Scene_remove_decal_instance,
			// Token: 0x04000A25 RID: 2597
			enm_IMono_Scene_remove_entity,
			// Token: 0x04000A26 RID: 2598
			enm_IMono_Scene_resume_loading_renderings,
			// Token: 0x04000A27 RID: 2599
			enm_IMono_Scene_resume_scene_sounds,
			// Token: 0x04000A28 RID: 2600
			enm_IMono_Scene_save_nav_mesh_prefab_with_frame,
			// Token: 0x04000A29 RID: 2601
			enm_IMono_Scene_scene_had_water_wake_renderer,
			// Token: 0x04000A2A RID: 2602
			enm_IMono_Scene_select_entities_collided_with,
			// Token: 0x04000A2B RID: 2603
			enm_IMono_Scene_select_entities_in_box_with_script_component,
			// Token: 0x04000A2C RID: 2604
			enm_IMono_Scene_separate_faces_with_id,
			// Token: 0x04000A2D RID: 2605
			enm_IMono_Scene_set_aberration_offset,
			// Token: 0x04000A2E RID: 2606
			enm_IMono_Scene_set_aberration_size,
			// Token: 0x04000A2F RID: 2607
			enm_IMono_Scene_set_aberration_smooth,
			// Token: 0x04000A30 RID: 2608
			enm_IMono_Scene_set_ability_of_faces_with_id,
			// Token: 0x04000A31 RID: 2609
			enm_IMono_Scene_set_active_visibility_levels,
			// Token: 0x04000A32 RID: 2610
			enm_IMono_Scene_set_antialiasing_mode,
			// Token: 0x04000A33 RID: 2611
			enm_IMono_Scene_set_atmosphere_with_name,
			// Token: 0x04000A34 RID: 2612
			enm_IMono_Scene_set_bloom,
			// Token: 0x04000A35 RID: 2613
			enm_IMono_Scene_set_bloom_amount,
			// Token: 0x04000A36 RID: 2614
			enm_IMono_Scene_set_bloom_strength,
			// Token: 0x04000A37 RID: 2615
			enm_IMono_Scene_set_brightpass_threshold,
			// Token: 0x04000A38 RID: 2616
			enm_IMono_Scene_set_cloth_simulation_state,
			// Token: 0x04000A39 RID: 2617
			enm_IMono_Scene_set_color_grade_blend,
			// Token: 0x04000A3A RID: 2618
			enm_IMono_Scene_set_dlss_mode,
			// Token: 0x04000A3B RID: 2619
			enm_IMono_Scene_set_dof_focus,
			// Token: 0x04000A3C RID: 2620
			enm_IMono_Scene_set_dof_mode,
			// Token: 0x04000A3D RID: 2621
			enm_IMono_Scene_set_dof_params,
			// Token: 0x04000A3E RID: 2622
			enm_IMono_Scene_set_do_not_add_entities_to_tick_list,
			// Token: 0x04000A3F RID: 2623
			enm_IMono_Scene_set_do_not_wait_for_loading_states_to_render,
			// Token: 0x04000A40 RID: 2624
			enm_IMono_Scene_set_dont_load_invisible_entities,
			// Token: 0x04000A41 RID: 2625
			enm_IMono_Scene_set_dryness_factor,
			// Token: 0x04000A42 RID: 2626
			enm_IMono_Scene_set_dynamic_shadowmap_cascades_radius_multiplier,
			// Token: 0x04000A43 RID: 2627
			enm_IMono_Scene_set_dynamic_snow_texture,
			// Token: 0x04000A44 RID: 2628
			enm_IMono_Scene_set_env_map_multiplier,
			// Token: 0x04000A45 RID: 2629
			enm_IMono_Scene_set_external_injection_texture,
			// Token: 0x04000A46 RID: 2630
			enm_IMono_Scene_set_fetch_crc_info_of_scene,
			// Token: 0x04000A47 RID: 2631
			enm_IMono_Scene_set_fixed_tick_callback_active,
			// Token: 0x04000A48 RID: 2632
			enm_IMono_Scene_set_fog,
			// Token: 0x04000A49 RID: 2633
			enm_IMono_Scene_set_fog_advanced,
			// Token: 0x04000A4A RID: 2634
			enm_IMono_Scene_set_fog_ambient_color,
			// Token: 0x04000A4B RID: 2635
			enm_IMono_Scene_set_forced_snow,
			// Token: 0x04000A4C RID: 2636
			enm_IMono_Scene_set_global_wind_strength_vector,
			// Token: 0x04000A4D RID: 2637
			enm_IMono_Scene_set_global_wind_velocity,
			// Token: 0x04000A4E RID: 2638
			enm_IMono_Scene_set_grain_amount,
			// Token: 0x04000A4F RID: 2639
			enm_IMono_Scene_set_hexagon_vignette_alpha,
			// Token: 0x04000A50 RID: 2640
			enm_IMono_Scene_set_hexagon_vignette_color,
			// Token: 0x04000A51 RID: 2641
			enm_IMono_Scene_set_humidity,
			// Token: 0x04000A52 RID: 2642
			enm_IMono_Scene_set_landscape_rain_mask_data,
			// Token: 0x04000A53 RID: 2643
			enm_IMono_Scene_set_lens_distortion,
			// Token: 0x04000A54 RID: 2644
			enm_IMono_Scene_set_lens_flare_aberration_offset,
			// Token: 0x04000A55 RID: 2645
			enm_IMono_Scene_set_lens_flare_amount,
			// Token: 0x04000A56 RID: 2646
			enm_IMono_Scene_set_lens_flare_blur_sigma,
			// Token: 0x04000A57 RID: 2647
			enm_IMono_Scene_set_lens_flare_blur_size,
			// Token: 0x04000A58 RID: 2648
			enm_IMono_Scene_set_lens_flare_diffraction_weight,
			// Token: 0x04000A59 RID: 2649
			enm_IMono_Scene_set_lens_flare_dirt_weight,
			// Token: 0x04000A5A RID: 2650
			enm_IMono_Scene_set_lens_flare_ghost_samples,
			// Token: 0x04000A5B RID: 2651
			enm_IMono_Scene_set_lens_flare_ghost_weight,
			// Token: 0x04000A5C RID: 2652
			enm_IMono_Scene_set_lens_flare_halo_weight,
			// Token: 0x04000A5D RID: 2653
			enm_IMono_Scene_set_lens_flare_halo_width,
			// Token: 0x04000A5E RID: 2654
			enm_IMono_Scene_set_lens_flare_strength,
			// Token: 0x04000A5F RID: 2655
			enm_IMono_Scene_set_lens_flare_threshold,
			// Token: 0x04000A60 RID: 2656
			enm_IMono_Scene_set_light_diffuse_color,
			// Token: 0x04000A61 RID: 2657
			enm_IMono_Scene_set_light_direction,
			// Token: 0x04000A62 RID: 2658
			enm_IMono_Scene_set_light_position,
			// Token: 0x04000A63 RID: 2659
			enm_IMono_Scene_set_max_exposure,
			// Token: 0x04000A64 RID: 2660
			enm_IMono_Scene_set_middle_gray,
			// Token: 0x04000A65 RID: 2661
			enm_IMono_Scene_set_mie_scatter_particle_size,
			// Token: 0x04000A66 RID: 2662
			enm_IMono_Scene_set_rayleigh_constant,
			// Token: 0x04000A67 RID: 2663
			enm_IMono_Scene_set_min_exposure,
			// Token: 0x04000A68 RID: 2664
			enm_IMono_Scene_set_motionblur_mode,
			// Token: 0x04000A69 RID: 2665
			enm_IMono_Scene_set_name,
			// Token: 0x04000A6A RID: 2666
			enm_IMono_Scene_set_nav_mesh_region_map,
			// Token: 0x04000A6B RID: 2667
			enm_IMono_Scene_set_occlusion_mode,
			// Token: 0x04000A6C RID: 2668
			enm_IMono_Scene_set_on_collision_filter_callback_active,
			// Token: 0x04000A6D RID: 2669
			enm_IMono_Scene_set_owner_thread,
			// Token: 0x04000A6E RID: 2670
			enm_IMono_Scene_set_photo_atmosphere_via_tod,
			// Token: 0x04000A6F RID: 2671
			enm_IMono_Scene_set_photo_mode_focus,
			// Token: 0x04000A70 RID: 2672
			enm_IMono_Scene_set_photo_mode_fov,
			// Token: 0x04000A71 RID: 2673
			enm_IMono_Scene_set_photo_mode_on,
			// Token: 0x04000A72 RID: 2674
			enm_IMono_Scene_set_photo_mode_orbit,
			// Token: 0x04000A73 RID: 2675
			enm_IMono_Scene_set_photo_mode_roll,
			// Token: 0x04000A74 RID: 2676
			enm_IMono_Scene_set_photo_mode_vignette,
			// Token: 0x04000A75 RID: 2677
			enm_IMono_Scene_set_play_sound_events_after_render_ready,
			// Token: 0x04000A76 RID: 2678
			enm_IMono_Scene_set_rain_density,
			// Token: 0x04000A77 RID: 2679
			enm_IMono_Scene_set_scene_color_grade,
			// Token: 0x04000A78 RID: 2680
			enm_IMono_Scene_set_scene_color_grade_index,
			// Token: 0x04000A79 RID: 2681
			enm_IMono_Scene_set_scene_filter_index,
			// Token: 0x04000A7A RID: 2682
			enm_IMono_Scene_set_shadow,
			// Token: 0x04000A7B RID: 2683
			enm_IMono_Scene_set_sky_brightness,
			// Token: 0x04000A7C RID: 2684
			enm_IMono_Scene_set_sky_rotation,
			// Token: 0x04000A7D RID: 2685
			enm_IMono_Scene_set_snow_density,
			// Token: 0x04000A7E RID: 2686
			enm_IMono_Scene_set_streak_amount,
			// Token: 0x04000A7F RID: 2687
			enm_IMono_Scene_set_streak_intensity,
			// Token: 0x04000A80 RID: 2688
			enm_IMono_Scene_set_streak_strength,
			// Token: 0x04000A81 RID: 2689
			enm_IMono_Scene_set_streak_stretch,
			// Token: 0x04000A82 RID: 2690
			enm_IMono_Scene_set_streak_threshold,
			// Token: 0x04000A83 RID: 2691
			enm_IMono_Scene_set_streak_tint,
			// Token: 0x04000A84 RID: 2692
			enm_IMono_Scene_set_sun,
			// Token: 0x04000A85 RID: 2693
			enm_IMono_Scene_set_sun_angle_altitude,
			// Token: 0x04000A86 RID: 2694
			enm_IMono_Scene_set_sun_direction,
			// Token: 0x04000A87 RID: 2695
			enm_IMono_Scene_set_sun_light,
			// Token: 0x04000A88 RID: 2696
			enm_IMono_Scene_set_sunshaft_mode,
			// Token: 0x04000A89 RID: 2697
			enm_IMono_Scene_set_sunshafts_strength,
			// Token: 0x04000A8A RID: 2698
			enm_IMono_Scene_set_sun_size,
			// Token: 0x04000A8B RID: 2699
			enm_IMono_Scene_set_target_exposure,
			// Token: 0x04000A8C RID: 2700
			enm_IMono_Scene_set_temperature,
			// Token: 0x04000A8D RID: 2701
			enm_IMono_Scene_set_terrain_dynamic_params,
			// Token: 0x04000A8E RID: 2702
			enm_IMono_Scene_set_time_of_day,
			// Token: 0x04000A8F RID: 2703
			enm_IMono_Scene_set_time_speed,
			// Token: 0x04000A90 RID: 2704
			enm_IMono_Scene_set_upgrade_level,
			// Token: 0x04000A91 RID: 2705
			enm_IMono_Scene_set_upgrade_level_visibility,
			// Token: 0x04000A92 RID: 2706
			enm_IMono_Scene_set_upgrade_level_visibility_with_mask,
			// Token: 0x04000A93 RID: 2707
			enm_IMono_Scene_set_use_constant_time,
			// Token: 0x04000A94 RID: 2708
			enm_IMono_Scene_set_uses_delete_later_system,
			// Token: 0x04000A95 RID: 2709
			enm_IMono_Scene_set_vignette_inner_radius,
			// Token: 0x04000A96 RID: 2710
			enm_IMono_Scene_set_vignette_opacity,
			// Token: 0x04000A97 RID: 2711
			enm_IMono_Scene_set_vignette_outer_radius,
			// Token: 0x04000A98 RID: 2712
			enm_IMono_Scene_set_water_strength,
			// Token: 0x04000A99 RID: 2713
			enm_IMono_Scene_set_water_wake_camera_offset,
			// Token: 0x04000A9A RID: 2714
			enm_IMono_Scene_set_water_wake_world_size,
			// Token: 0x04000A9B RID: 2715
			enm_IMono_Scene_set_winter_time_factor,
			// Token: 0x04000A9C RID: 2716
			enm_IMono_Scene_stall_loading_renderings,
			// Token: 0x04000A9D RID: 2717
			enm_IMono_Scene_swap_face_connections_with_id,
			// Token: 0x04000A9E RID: 2718
			enm_IMono_Scene_take_photo_mode_picture,
			// Token: 0x04000A9F RID: 2719
			enm_IMono_Scene_tick,
			// Token: 0x04000AA0 RID: 2720
			enm_IMono_Scene_tick_wake,
			// Token: 0x04000AA1 RID: 2721
			enm_IMono_Scene_wait_water_renderer_cpu_simulation,
			// Token: 0x04000AA2 RID: 2722
			enm_IMono_Scene_world_position_compute_nearest_nav_mesh,
			// Token: 0x04000AA3 RID: 2723
			enm_IMono_Scene_world_position_validate_z,
			// Token: 0x04000AA4 RID: 2724
			enm_IMono_SceneView_add_clear_task,
			// Token: 0x04000AA5 RID: 2725
			enm_IMono_SceneView_check_scene_ready_to_render,
			// Token: 0x04000AA6 RID: 2726
			enm_IMono_SceneView_clear_all,
			// Token: 0x04000AA7 RID: 2727
			enm_IMono_SceneView_create_scene_view,
			// Token: 0x04000AA8 RID: 2728
			enm_IMono_SceneView_do_not_clear,
			// Token: 0x04000AA9 RID: 2729
			enm_IMono_SceneView_get_scene,
			// Token: 0x04000AAA RID: 2730
			enm_IMono_SceneView_projected_mouse_position_on_ground,
			// Token: 0x04000AAB RID: 2731
			enm_IMono_SceneView_projected_mouse_position_on_water,
			// Token: 0x04000AAC RID: 2732
			enm_IMono_SceneView_ray_cast_for_closest_entity_or_terrain,
			// Token: 0x04000AAD RID: 2733
			enm_IMono_SceneView_ready_to_render,
			// Token: 0x04000AAE RID: 2734
			enm_IMono_SceneView_screen_point_to_viewport_point,
			// Token: 0x04000AAF RID: 2735
			enm_IMono_SceneView_set_accept_global_debug_render_objects,
			// Token: 0x04000AB0 RID: 2736
			enm_IMono_SceneView_set_camera,
			// Token: 0x04000AB1 RID: 2737
			enm_IMono_SceneView_set_clean_screen_until_loading_done,
			// Token: 0x04000AB2 RID: 2738
			enm_IMono_SceneView_set_clear_and_disable_after_succesfull_render,
			// Token: 0x04000AB3 RID: 2739
			enm_IMono_SceneView_set_clear_gbuffer,
			// Token: 0x04000AB4 RID: 2740
			enm_IMono_SceneView_set_do_quick_exposure,
			// Token: 0x04000AB5 RID: 2741
			enm_IMono_SceneView_set_focused_shadowmap,
			// Token: 0x04000AB6 RID: 2742
			enm_IMono_SceneView_set_force_shader_compilation,
			// Token: 0x04000AB7 RID: 2743
			enm_IMono_SceneView_set_pointlight_resolution_multiplier,
			// Token: 0x04000AB8 RID: 2744
			enm_IMono_SceneView_set_postfx_config_params,
			// Token: 0x04000AB9 RID: 2745
			enm_IMono_SceneView_set_postfx_from_config,
			// Token: 0x04000ABA RID: 2746
			enm_IMono_SceneView_set_render_with_postfx,
			// Token: 0x04000ABB RID: 2747
			enm_IMono_SceneView_set_resolution_scaling,
			// Token: 0x04000ABC RID: 2748
			enm_IMono_SceneView_set_scene,
			// Token: 0x04000ABD RID: 2749
			enm_IMono_SceneView_set_scene_uses_contour,
			// Token: 0x04000ABE RID: 2750
			enm_IMono_SceneView_set_scene_uses_shadows,
			// Token: 0x04000ABF RID: 2751
			enm_IMono_SceneView_set_scene_uses_skybox,
			// Token: 0x04000AC0 RID: 2752
			enm_IMono_SceneView_set_shadowmap_resolution_multiplier,
			// Token: 0x04000AC1 RID: 2753
			enm_IMono_SceneView_translate_mouse,
			// Token: 0x04000AC2 RID: 2754
			enm_IMono_SceneView_world_point_to_screen_point,
			// Token: 0x04000AC3 RID: 2755
			enm_IMono_Screen_get_aspect_ratio,
			// Token: 0x04000AC4 RID: 2756
			enm_IMono_Screen_get_desktop_height,
			// Token: 0x04000AC5 RID: 2757
			enm_IMono_Screen_get_desktop_width,
			// Token: 0x04000AC6 RID: 2758
			enm_IMono_Screen_get_mouse_visible,
			// Token: 0x04000AC7 RID: 2759
			enm_IMono_Screen_get_real_screen_resolution_height,
			// Token: 0x04000AC8 RID: 2760
			enm_IMono_Screen_get_real_screen_resolution_width,
			// Token: 0x04000AC9 RID: 2761
			enm_IMono_Screen_get_usable_area_percentages,
			// Token: 0x04000ACA RID: 2762
			enm_IMono_Screen_is_enter_button_cross,
			// Token: 0x04000ACB RID: 2763
			enm_IMono_Screen_set_mouse_visible,
			// Token: 0x04000ACC RID: 2764
			enm_IMono_ScriptComponent_get_name,
			// Token: 0x04000ACD RID: 2765
			enm_IMono_ScriptComponent_get_script_component_behavior,
			// Token: 0x04000ACE RID: 2766
			enm_IMono_ScriptComponent_set_variable_editor_widget_status,
			// Token: 0x04000ACF RID: 2767
			enm_IMono_ScriptComponent_set_variable_editor_widget_value,
			// Token: 0x04000AD0 RID: 2768
			enm_IMono_Shader_get_from_resource,
			// Token: 0x04000AD1 RID: 2769
			enm_IMono_Shader_get_material_shader_flag_mask,
			// Token: 0x04000AD2 RID: 2770
			enm_IMono_Shader_get_name,
			// Token: 0x04000AD3 RID: 2771
			enm_IMono_Shader_release,
			// Token: 0x04000AD4 RID: 2772
			enm_IMono_Skeleton_activate_ragdoll,
			// Token: 0x04000AD5 RID: 2773
			enm_IMono_Skeleton_add_component,
			// Token: 0x04000AD6 RID: 2774
			enm_IMono_Skeleton_add_component_to_bone,
			// Token: 0x04000AD7 RID: 2775
			enm_IMono_Skeleton_add_mesh,
			// Token: 0x04000AD8 RID: 2776
			enm_IMono_Skeleton_add_mesh_to_bone,
			// Token: 0x04000AD9 RID: 2777
			enm_IMono_Skeleton_add_prefab_entity_to_bone,
			// Token: 0x04000ADA RID: 2778
			enm_IMono_Skeleton_clear_components,
			// Token: 0x04000ADB RID: 2779
			enm_IMono_Skeleton_clear_meshes,
			// Token: 0x04000ADC RID: 2780
			enm_IMono_Skeleton_clear_meshes_at_bone,
			// Token: 0x04000ADD RID: 2781
			enm_IMono_Skeleton_create_from_model,
			// Token: 0x04000ADE RID: 2782
			enm_IMono_Skeleton_create_from_model_with_null_anim_tree,
			// Token: 0x04000ADF RID: 2783
			enm_IMono_Skeleton_enable_script_driven_post_integrate_callback,
			// Token: 0x04000AE0 RID: 2784
			enm_IMono_Skeleton_force_update_bone_frames,
			// Token: 0x04000AE1 RID: 2785
			enm_IMono_Skeleton_freeze,
			// Token: 0x04000AE2 RID: 2786
			enm_IMono_Skeleton_get_all_meshes,
			// Token: 0x04000AE3 RID: 2787
			enm_IMono_Skeleton_get_animation_at_channel,
			// Token: 0x04000AE4 RID: 2788
			enm_IMono_Skeleton_get_animation_index_at_channel,
			// Token: 0x04000AE5 RID: 2789
			enm_IMono_Skeleton_get_bone_body,
			// Token: 0x04000AE6 RID: 2790
			enm_IMono_Skeleton_get_bone_child_at_index,
			// Token: 0x04000AE7 RID: 2791
			enm_IMono_Skeleton_get_bone_child_count,
			// Token: 0x04000AE8 RID: 2792
			enm_IMono_Skeleton_get_bone_component_at_index,
			// Token: 0x04000AE9 RID: 2793
			enm_IMono_Skeleton_get_bone_component_count,
			// Token: 0x04000AEA RID: 2794
			enm_IMono_Skeleton_get_bone_count,
			// Token: 0x04000AEB RID: 2795
			enm_IMono_Skeleton_get_bone_entitial_frame,
			// Token: 0x04000AEC RID: 2796
			enm_IMono_Skeleton_get_bone_entitial_frame_at_channel,
			// Token: 0x04000AED RID: 2797
			enm_IMono_Skeleton_get_bone_entitial_frame_with_index,
			// Token: 0x04000AEE RID: 2798
			enm_IMono_Skeleton_get_bone_entitial_frame_with_name,
			// Token: 0x04000AEF RID: 2799
			enm_IMono_Skeleton_get_bone_entitial_rest_frame,
			// Token: 0x04000AF0 RID: 2800
			enm_IMono_Skeleton_get_bone_index_from_name,
			// Token: 0x04000AF1 RID: 2801
			enm_IMono_Skeleton_get_bone_local_rest_frame,
			// Token: 0x04000AF2 RID: 2802
			enm_IMono_Skeleton_get_bone_name,
			// Token: 0x04000AF3 RID: 2803
			enm_IMono_Skeleton_get_component_at_index,
			// Token: 0x04000AF4 RID: 2804
			enm_IMono_Skeleton_get_component_count,
			// Token: 0x04000AF5 RID: 2805
			enm_IMono_Skeleton_get_current_ragdoll_state,
			// Token: 0x04000AF6 RID: 2806
			enm_IMono_Skeleton_get_entitial_out_transform,
			// Token: 0x04000AF7 RID: 2807
			enm_IMono_Skeleton_get_name,
			// Token: 0x04000AF8 RID: 2808
			enm_IMono_Skeleton_get_parent_bone_index,
			// Token: 0x04000AF9 RID: 2809
			enm_IMono_Skeleton_get_skeleton_animation_parameter_at_channel,
			// Token: 0x04000AFA RID: 2810
			enm_IMono_Skeleton_get_skeleton_animation_speed_at_channel,
			// Token: 0x04000AFB RID: 2811
			enm_IMono_Skeleton_get_skeleton_bone_mapping,
			// Token: 0x04000AFC RID: 2812
			enm_IMono_Skeleton_has_bone_component,
			// Token: 0x04000AFD RID: 2813
			enm_IMono_Skeleton_has_component,
			// Token: 0x04000AFE RID: 2814
			enm_IMono_Skeleton_is_frozen,
			// Token: 0x04000AFF RID: 2815
			enm_IMono_Skeleton_remove_bone_component,
			// Token: 0x04000B00 RID: 2816
			enm_IMono_Skeleton_remove_component,
			// Token: 0x04000B01 RID: 2817
			enm_IMono_Skeleton_reset_cloths,
			// Token: 0x04000B02 RID: 2818
			enm_IMono_Skeleton_reset_frames,
			// Token: 0x04000B03 RID: 2819
			enm_IMono_Skeleton_set_bone_local_frame,
			// Token: 0x04000B04 RID: 2820
			enm_IMono_Skeleton_set_out_bone_displacement,
			// Token: 0x04000B05 RID: 2821
			enm_IMono_Skeleton_set_out_quat,
			// Token: 0x04000B06 RID: 2822
			enm_IMono_Skeleton_set_skeleton_animation_parameter_at_channel,
			// Token: 0x04000B07 RID: 2823
			enm_IMono_Skeleton_set_skeleton_animation_speed_at_channel,
			// Token: 0x04000B08 RID: 2824
			enm_IMono_Skeleton_set_up_to_date,
			// Token: 0x04000B09 RID: 2825
			enm_IMono_Skeleton_set_use_precise_bounding_volume,
			// Token: 0x04000B0A RID: 2826
			enm_IMono_Skeleton_skeleton_model_exist,
			// Token: 0x04000B0B RID: 2827
			enm_IMono_Skeleton_tick_animations,
			// Token: 0x04000B0C RID: 2828
			enm_IMono_Skeleton_tick_animations_and_force_update,
			// Token: 0x04000B0D RID: 2829
			enm_IMono_Skeleton_update_entitial_frames_from_local_frames,
			// Token: 0x04000B0E RID: 2830
			enm_IMono_SoundEvent_create_event,
			// Token: 0x04000B0F RID: 2831
			enm_IMono_SoundEvent_create_event_from_external_file,
			// Token: 0x04000B10 RID: 2832
			enm_IMono_SoundEvent_create_event_from_sound_buffer,
			// Token: 0x04000B11 RID: 2833
			enm_IMono_SoundEvent_create_event_from_string,
			// Token: 0x04000B12 RID: 2834
			enm_IMono_SoundEvent_get_event_id_from_string,
			// Token: 0x04000B13 RID: 2835
			enm_IMono_SoundEvent_get_event_min_max_distance,
			// Token: 0x04000B14 RID: 2836
			enm_IMono_SoundEvent_get_total_event_count,
			// Token: 0x04000B15 RID: 2837
			enm_IMono_SoundEvent_is_paused,
			// Token: 0x04000B16 RID: 2838
			enm_IMono_SoundEvent_is_playing,
			// Token: 0x04000B17 RID: 2839
			enm_IMono_SoundEvent_is_valid,
			// Token: 0x04000B18 RID: 2840
			enm_IMono_SoundEvent_pause_event,
			// Token: 0x04000B19 RID: 2841
			enm_IMono_SoundEvent_play_extra_event,
			// Token: 0x04000B1A RID: 2842
			enm_IMono_SoundEvent_play_sound_2d,
			// Token: 0x04000B1B RID: 2843
			enm_IMono_SoundEvent_release_event,
			// Token: 0x04000B1C RID: 2844
			enm_IMono_SoundEvent_resume_event,
			// Token: 0x04000B1D RID: 2845
			enm_IMono_SoundEvent_set_event_min_max_distance,
			// Token: 0x04000B1E RID: 2846
			enm_IMono_SoundEvent_set_event_parameter_at_index,
			// Token: 0x04000B1F RID: 2847
			enm_IMono_SoundEvent_set_event_parameter_from_string,
			// Token: 0x04000B20 RID: 2848
			enm_IMono_SoundEvent_set_event_position,
			// Token: 0x04000B21 RID: 2849
			enm_IMono_SoundEvent_set_event_velocity,
			// Token: 0x04000B22 RID: 2850
			enm_IMono_SoundEvent_set_switch,
			// Token: 0x04000B23 RID: 2851
			enm_IMono_SoundEvent_start_event,
			// Token: 0x04000B24 RID: 2852
			enm_IMono_SoundEvent_start_event_in_position,
			// Token: 0x04000B25 RID: 2853
			enm_IMono_SoundEvent_stop_event,
			// Token: 0x04000B26 RID: 2854
			enm_IMono_SoundEvent_trigger_cue,
			// Token: 0x04000B27 RID: 2855
			enm_IMono_SoundManager_add_sound_client_with_id,
			// Token: 0x04000B28 RID: 2856
			enm_IMono_SoundManager_add_xbox_remote_user,
			// Token: 0x04000B29 RID: 2857
			enm_IMono_SoundManager_apply_push_to_talk,
			// Token: 0x04000B2A RID: 2858
			enm_IMono_SoundManager_clear_data_to_be_sent,
			// Token: 0x04000B2B RID: 2859
			enm_IMono_SoundManager_clear_xbox_sound_manager,
			// Token: 0x04000B2C RID: 2860
			enm_IMono_SoundManager_compress_voice_data,
			// Token: 0x04000B2D RID: 2861
			enm_IMono_SoundManager_create_voice_event,
			// Token: 0x04000B2E RID: 2862
			enm_IMono_SoundManager_decompress_voice_data,
			// Token: 0x04000B2F RID: 2863
			enm_IMono_SoundManager_delete_sound_client_with_id,
			// Token: 0x04000B30 RID: 2864
			enm_IMono_SoundManager_destroy_voice_event,
			// Token: 0x04000B31 RID: 2865
			enm_IMono_SoundManager_finalize_voice_play_event,
			// Token: 0x04000B32 RID: 2866
			enm_IMono_SoundManager_get_attenuation_position,
			// Token: 0x04000B33 RID: 2867
			enm_IMono_SoundManager_get_data_to_be_sent_at,
			// Token: 0x04000B34 RID: 2868
			enm_IMono_SoundManager_get_global_index_of_event,
			// Token: 0x04000B35 RID: 2869
			enm_IMono_SoundManager_get_listener_frame,
			// Token: 0x04000B36 RID: 2870
			enm_IMono_SoundManager_get_size_of_data_to_be_sent_at,
			// Token: 0x04000B37 RID: 2871
			enm_IMono_SoundManager_get_voice_data,
			// Token: 0x04000B38 RID: 2872
			enm_IMono_SoundManager_handle_state_changes,
			// Token: 0x04000B39 RID: 2873
			enm_IMono_SoundManager_init_voice_play_event,
			// Token: 0x04000B3A RID: 2874
			enm_IMono_SoundManager_initialize_xbox_sound_manager,
			// Token: 0x04000B3B RID: 2875
			enm_IMono_SoundManager_load_event_file_aux,
			// Token: 0x04000B3C RID: 2876
			enm_IMono_SoundManager_pause_bus,
			// Token: 0x04000B3D RID: 2877
			enm_IMono_SoundManager_process_data_to_be_received,
			// Token: 0x04000B3E RID: 2878
			enm_IMono_SoundManager_process_data_to_be_sent,
			// Token: 0x04000B3F RID: 2879
			enm_IMono_SoundManager_remove_xbox_remote_user,
			// Token: 0x04000B40 RID: 2880
			enm_IMono_SoundManager_reset,
			// Token: 0x04000B41 RID: 2881
			enm_IMono_SoundManager_set_global_parameter,
			// Token: 0x04000B42 RID: 2882
			enm_IMono_SoundManager_set_listener_frame,
			// Token: 0x04000B43 RID: 2883
			enm_IMono_SoundManager_set_state,
			// Token: 0x04000B44 RID: 2884
			enm_IMono_SoundManager_start_one_shot_event,
			// Token: 0x04000B45 RID: 2885
			enm_IMono_SoundManager_start_one_shot_event_with_index,
			// Token: 0x04000B46 RID: 2886
			enm_IMono_SoundManager_start_one_shot_event_with_param,
			// Token: 0x04000B47 RID: 2887
			enm_IMono_SoundManager_start_voice_record,
			// Token: 0x04000B48 RID: 2888
			enm_IMono_SoundManager_stop_voice_record,
			// Token: 0x04000B49 RID: 2889
			enm_IMono_SoundManager_unpause_bus,
			// Token: 0x04000B4A RID: 2890
			enm_IMono_SoundManager_update_voice_to_play,
			// Token: 0x04000B4B RID: 2891
			enm_IMono_SoundManager_update_xbox_chat_communication_flags,
			// Token: 0x04000B4C RID: 2892
			enm_IMono_SoundManager_update_xbox_local_user,
			// Token: 0x04000B4D RID: 2893
			enm_IMono_TableauView_create_tableau_view,
			// Token: 0x04000B4E RID: 2894
			enm_IMono_TableauView_set_continous_rendering,
			// Token: 0x04000B4F RID: 2895
			enm_IMono_TableauView_set_delete_after_rendering,
			// Token: 0x04000B50 RID: 2896
			enm_IMono_TableauView_set_do_not_render_this_frame,
			// Token: 0x04000B51 RID: 2897
			enm_IMono_TableauView_set_sort_meshes,
			// Token: 0x04000B52 RID: 2898
			enm_IMono_Texture_check_and_get_from_resource,
			// Token: 0x04000B53 RID: 2899
			enm_IMono_Texture_create_depth_target,
			// Token: 0x04000B54 RID: 2900
			enm_IMono_Texture_create_from_byte_array,
			// Token: 0x04000B55 RID: 2901
			enm_IMono_Texture_create_from_memory,
			// Token: 0x04000B56 RID: 2902
			enm_IMono_Texture_create_render_target,
			// Token: 0x04000B57 RID: 2903
			enm_IMono_Texture_create_texture_from_path,
			// Token: 0x04000B58 RID: 2904
			enm_IMono_Texture_get_cur_object,
			// Token: 0x04000B59 RID: 2905
			enm_IMono_Texture_get_from_resource,
			// Token: 0x04000B5A RID: 2906
			enm_IMono_Texture_get_height,
			// Token: 0x04000B5B RID: 2907
			enm_IMono_Texture_get_memory_size,
			// Token: 0x04000B5C RID: 2908
			enm_IMono_Texture_get_name,
			// Token: 0x04000B5D RID: 2909
			enm_IMono_Texture_get_pixel_data,
			// Token: 0x04000B5E RID: 2910
			enm_IMono_Texture_get_render_target_component,
			// Token: 0x04000B5F RID: 2911
			enm_IMono_Texture_get_sdf_bounding_box_data,
			// Token: 0x04000B60 RID: 2912
			enm_IMono_Texture_get_tableau_view,
			// Token: 0x04000B61 RID: 2913
			enm_IMono_Texture_get_width,
			// Token: 0x04000B62 RID: 2914
			enm_IMono_Texture_is_loaded,
			// Token: 0x04000B63 RID: 2915
			enm_IMono_Texture_is_render_target,
			// Token: 0x04000B64 RID: 2916
			enm_IMono_Texture_load_texture_from_path,
			// Token: 0x04000B65 RID: 2917
			enm_IMono_Texture_release,
			// Token: 0x04000B66 RID: 2918
			enm_IMono_Texture_release_after_number_of_frames,
			// Token: 0x04000B67 RID: 2919
			enm_IMono_Texture_release_gpu_memories,
			// Token: 0x04000B68 RID: 2920
			enm_IMono_Texture_release_next_frame,
			// Token: 0x04000B69 RID: 2921
			enm_IMono_Texture_remove_continous_tableau_texture,
			// Token: 0x04000B6A RID: 2922
			enm_IMono_Texture_set_texture_as_always_valid,
			// Token: 0x04000B6B RID: 2923
			enm_IMono_Texture_save_to_file,
			// Token: 0x04000B6C RID: 2924
			enm_IMono_Texture_set_name,
			// Token: 0x04000B6D RID: 2925
			enm_IMono_Texture_set_tableau_view,
			// Token: 0x04000B6E RID: 2926
			enm_IMono_Texture_transform_render_target_to_resource_texture,
			// Token: 0x04000B6F RID: 2927
			enm_IMono_TextureView_create_texture_view,
			// Token: 0x04000B70 RID: 2928
			enm_IMono_TextureView_set_texture,
			// Token: 0x04000B71 RID: 2929
			enm_IMono_ThumbnailCreatorView_cancel_request,
			// Token: 0x04000B72 RID: 2930
			enm_IMono_ThumbnailCreatorView_clear_requests,
			// Token: 0x04000B73 RID: 2931
			enm_IMono_ThumbnailCreatorView_create_thumbnail_creator_view,
			// Token: 0x04000B74 RID: 2932
			enm_IMono_ThumbnailCreatorView_get_number_of_pending_requests,
			// Token: 0x04000B75 RID: 2933
			enm_IMono_ThumbnailCreatorView_is_memory_cleared,
			// Token: 0x04000B76 RID: 2934
			enm_IMono_ThumbnailCreatorView_register_cached_entity,
			// Token: 0x04000B77 RID: 2935
			enm_IMono_ThumbnailCreatorView_register_render_request,
			// Token: 0x04000B78 RID: 2936
			enm_IMono_ThumbnailCreatorView_register_scene,
			// Token: 0x04000B79 RID: 2937
			enm_IMono_ThumbnailCreatorView_unregister_cached_entity,
			// Token: 0x04000B7A RID: 2938
			enm_IMono_Time_get_application_time,
			// Token: 0x04000B7B RID: 2939
			enm_IMono_TwoDimensionView_add_cached_text_mesh,
			// Token: 0x04000B7C RID: 2940
			enm_IMono_TwoDimensionView_add_new_mesh,
			// Token: 0x04000B7D RID: 2941
			enm_IMono_TwoDimensionView_add_new_quad_mesh,
			// Token: 0x04000B7E RID: 2942
			enm_IMono_TwoDimensionView_add_new_text_mesh,
			// Token: 0x04000B7F RID: 2943
			enm_IMono_TwoDimensionView_begin_frame,
			// Token: 0x04000B80 RID: 2944
			enm_IMono_TwoDimensionView_clear,
			// Token: 0x04000B81 RID: 2945
			enm_IMono_TwoDimensionView_create_twodimension_view,
			// Token: 0x04000B82 RID: 2946
			enm_IMono_TwoDimensionView_end_frame,
			// Token: 0x04000B83 RID: 2947
			enm_IMono_TwoDimensionView_get_or_create_material,
			// Token: 0x04000B84 RID: 2948
			enm_IMono_Util_add_command_line_function,
			// Token: 0x04000B85 RID: 2949
			enm_IMono_Util_add_main_thread_performance_query,
			// Token: 0x04000B86 RID: 2950
			enm_IMono_Util_add_performance_report_token,
			// Token: 0x04000B87 RID: 2951
			enm_IMono_Util_add_scene_object_report,
			// Token: 0x04000B88 RID: 2952
			enm_IMono_Util_check_if_assets_and_sources_are_same,
			// Token: 0x04000B89 RID: 2953
			enm_IMono_Util_check_if_terrain_shader_header_generation_finished,
			// Token: 0x04000B8A RID: 2954
			enm_IMono_Util_check_resource_modifications,
			// Token: 0x04000B8B RID: 2955
			enm_IMono_Util_check_scene_for_problems,
			// Token: 0x04000B8C RID: 2956
			enm_IMono_Util_check_shader_compilation,
			// Token: 0x04000B8D RID: 2957
			enm_IMono_Util_clear_decal_atlas,
			// Token: 0x04000B8E RID: 2958
			enm_IMono_Util_clear_old_resources_and_objects,
			// Token: 0x04000B8F RID: 2959
			enm_IMono_Util_clear_shader_memory,
			// Token: 0x04000B90 RID: 2960
			enm_IMono_Util_command_line_argument_exits,
			// Token: 0x04000B91 RID: 2961
			enm_IMono_Util_compile_all_shaders,
			// Token: 0x04000B92 RID: 2962
			enm_IMono_Util_compile_terrain_shaders_dist,
			// Token: 0x04000B93 RID: 2963
			enm_IMono_Util_create_selection_set_in_editor,
			// Token: 0x04000B94 RID: 2964
			enm_IMono_Util_debug_set_global_loading_window_state,
			// Token: 0x04000B95 RID: 2965
			enm_IMono_Util_delete_entities_in_editor_scene,
			// Token: 0x04000B96 RID: 2966
			enm_IMono_Util_detach_watchdog,
			// Token: 0x04000B97 RID: 2967
			enm_IMono_Util_did_automated_gi_bake_finished,
			// Token: 0x04000B98 RID: 2968
			enm_IMono_Util_disable_core_game,
			// Token: 0x04000B99 RID: 2969
			enm_IMono_Util_disable_global_edit_data_cacher,
			// Token: 0x04000B9A RID: 2970
			enm_IMono_Util_disable_global_loading_window,
			// Token: 0x04000B9B RID: 2971
			enm_IMono_Util_do_delayed_exit,
			// Token: 0x04000B9C RID: 2972
			enm_IMono_Util_do_full_bake_all_levels_automated,
			// Token: 0x04000B9D RID: 2973
			enm_IMono_Util_do_full_bake_single_level_automated,
			// Token: 0x04000B9E RID: 2974
			enm_IMono_Util_do_light_only_bake_all_levels_automated,
			// Token: 0x04000B9F RID: 2975
			enm_IMono_Util_do_light_only_bake_single_level_automated,
			// Token: 0x04000BA0 RID: 2976
			enm_IMono_Util_dump_gpu_memory_statistics,
			// Token: 0x04000BA1 RID: 2977
			enm_IMono_Util_enable_global_edit_data_cacher,
			// Token: 0x04000BA2 RID: 2978
			enm_IMono_Util_enable_global_loading_window,
			// Token: 0x04000BA3 RID: 2979
			enm_IMono_Util_enable_single_gpu_query_per_frame,
			// Token: 0x04000BA4 RID: 2980
			enm_IMono_Util_end_loading_stuck_check_state,
			// Token: 0x04000BA5 RID: 2981
			enm_IMono_Util_execute_command_line_command,
			// Token: 0x04000BA6 RID: 2982
			enm_IMono_Util_exit_process,
			// Token: 0x04000BA7 RID: 2983
			enm_IMono_Util_export_nav_mesh_face_marks,
			// Token: 0x04000BA8 RID: 2984
			enm_IMono_Util_find_meshes_without_lods,
			// Token: 0x04000BA9 RID: 2985
			enm_IMono_Util_flush_managed_objects_memory,
			// Token: 0x04000BAA RID: 2986
			enm_IMono_Util_gather_core_game_references,
			// Token: 0x04000BAB RID: 2987
			enm_IMono_Util_generate_terrain_shader_headers,
			// Token: 0x04000BAC RID: 2988
			enm_IMono_Util_get_application_memory,
			// Token: 0x04000BAD RID: 2989
			enm_IMono_Util_get_application_memory_statistics,
			// Token: 0x04000BAE RID: 2990
			enm_IMono_Util_get_application_name,
			// Token: 0x04000BAF RID: 2991
			enm_IMono_Util_get_attachments_path,
			// Token: 0x04000BB0 RID: 2992
			enm_IMono_Util_get_base_directory,
			// Token: 0x04000BB1 RID: 2993
			enm_IMono_Util_get_benchmark_status,
			// Token: 0x04000BB2 RID: 2994
			enm_IMono_Util_get_build_number,
			// Token: 0x04000BB3 RID: 2995
			enm_IMono_Util_get_console_host_machine,
			// Token: 0x04000BB4 RID: 2996
			enm_IMono_Util_get_core_game_state,
			// Token: 0x04000BB5 RID: 2997
			enm_IMono_Util_get_current_cpu_memory_usage,
			// Token: 0x04000BB6 RID: 2998
			enm_IMono_Util_get_current_estimated_gpu_memory_cost_mb,
			// Token: 0x04000BB7 RID: 2999
			enm_IMono_Util_get_current_process_id,
			// Token: 0x04000BB8 RID: 3000
			enm_IMono_Util_get_current_thread_id,
			// Token: 0x04000BB9 RID: 3001
			enm_IMono_Util_get_delta_time,
			// Token: 0x04000BBA RID: 3002
			enm_IMono_Util_get_detailed_gpu_buffer_memory_stats,
			// Token: 0x04000BBB RID: 3003
			enm_IMono_Util_get_detailed_xbox_memory_info,
			// Token: 0x04000BBC RID: 3004
			enm_IMono_Util_get_editor_selected_entities,
			// Token: 0x04000BBD RID: 3005
			enm_IMono_Util_get_editor_selected_entity_count,
			// Token: 0x04000BBE RID: 3006
			enm_IMono_Util_get_engine_frame_no,
			// Token: 0x04000BBF RID: 3007
			enm_IMono_Util_get_entities_of_selection_set,
			// Token: 0x04000BC0 RID: 3008
			enm_IMono_Util_get_entity_count_of_selection_set,
			// Token: 0x04000BC1 RID: 3009
			enm_IMono_Util_get_executable_working_directory,
			// Token: 0x04000BC2 RID: 3010
			enm_IMono_Util_get_fps,
			// Token: 0x04000BC3 RID: 3011
			enm_IMono_Util_get_frame_limiter_with_sleep,
			// Token: 0x04000BC4 RID: 3012
			enm_IMono_Util_get_full_command_line_string,
			// Token: 0x04000BC5 RID: 3013
			enm_IMono_Util_get_full_file_path_of_scene,
			// Token: 0x04000BC6 RID: 3014
			enm_IMono_Util_get_full_module_path,
			// Token: 0x04000BC7 RID: 3015
			enm_IMono_Util_get_full_module_paths,
			// Token: 0x04000BC8 RID: 3016
			enm_IMono_Util_get_gpu_memory_mb,
			// Token: 0x04000BC9 RID: 3017
			enm_IMono_Util_get_gpu_memory_of_allocation_group,
			// Token: 0x04000BCA RID: 3018
			enm_IMono_Util_get_gpu_memory_stats,
			// Token: 0x04000BCB RID: 3019
			enm_IMono_Util_get_local_output_dir,
			// Token: 0x04000BCC RID: 3020
			enm_IMono_Util_get_main_fps,
			// Token: 0x04000BCD RID: 3021
			enm_IMono_Util_get_main_thread_id,
			// Token: 0x04000BCE RID: 3022
			enm_IMono_Util_get_memory_usage_of_category,
			// Token: 0x04000BCF RID: 3023
			enm_IMono_Util_get_modules_code,
			// Token: 0x04000BD0 RID: 3024
			enm_IMono_Util_get_native_memory_statistics,
			// Token: 0x04000BD1 RID: 3025
			enm_IMono_Util_get_number_of_shader_compilations_in_progress,
			// Token: 0x04000BD2 RID: 3026
			enm_IMono_Util_get_pc_info,
			// Token: 0x04000BD3 RID: 3027
			enm_IMono_Util_get_platform_module_paths,
			// Token: 0x04000BD4 RID: 3028
			enm_IMono_Util_get_possible_command_line_starting_with,
			// Token: 0x04000BD5 RID: 3029
			enm_IMono_Util_get_renderer_fps,
			// Token: 0x04000BD6 RID: 3030
			enm_IMono_Util_get_return_code,
			// Token: 0x04000BD7 RID: 3031
			enm_IMono_Util_get_single_module_scenes_of_module,
			// Token: 0x04000BD8 RID: 3032
			enm_IMono_Util_get_steam_appid,
			// Token: 0x04000BD9 RID: 3033
			enm_IMono_Util_get_system_language,
			// Token: 0x04000BDA RID: 3034
			enm_IMono_Util_get_vertex_buffer_chunk_system_memory_usage,
			// Token: 0x04000BDB RID: 3035
			enm_IMono_Util_get_visual_tests_test_files_path,
			// Token: 0x04000BDC RID: 3036
			enm_IMono_Util_get_visual_tests_validate_path,
			// Token: 0x04000BDD RID: 3037
			enm_IMono_Util_is_async_physics_thread,
			// Token: 0x04000BDE RID: 3038
			enm_IMono_Util_is_benchmark_quited,
			// Token: 0x04000BDF RID: 3039
			enm_IMono_Util_is_detailed_soung_log_on,
			// Token: 0x04000BE0 RID: 3040
			enm_IMono_Util_is_dev_kit,
			// Token: 0x04000BE1 RID: 3041
			enm_IMono_Util_is_edit_mode_enabled,
			// Token: 0x04000BE2 RID: 3042
			enm_IMono_Util_is_gen9_xbox_lockhart,
			// Token: 0x04000BE3 RID: 3043
			enm_IMono_Util_is_scene_performance_report_finished,
			// Token: 0x04000BE4 RID: 3044
			enm_IMono_Util_load_sky_boxes,
			// Token: 0x04000BE5 RID: 3045
			enm_IMono_Util_load_virtual_texture_tileset,
			// Token: 0x04000BE6 RID: 3046
			enm_IMono_Util_managed_parallel_for,
			// Token: 0x04000BE7 RID: 3047
			enm_IMono_Util_managed_parallel_for_with_dt,
			// Token: 0x04000BE8 RID: 3048
			enm_IMono_Util_managed_parallel_for_without_render_thread,
			// Token: 0x04000BE9 RID: 3049
			enm_IMono_Util_on_loading_window_disabled,
			// Token: 0x04000BEA RID: 3050
			enm_IMono_Util_on_loading_window_enabled,
			// Token: 0x04000BEB RID: 3051
			enm_IMono_Util_open_naval_dlc_purchase_page,
			// Token: 0x04000BEC RID: 3052
			enm_IMono_Util_open_onscreen_keyboard,
			// Token: 0x04000BED RID: 3053
			enm_IMono_Util_output_benchmark_values_to_performance_reporter,
			// Token: 0x04000BEE RID: 3054
			enm_IMono_Util_output_performance_reports,
			// Token: 0x04000BEF RID: 3055
			enm_IMono_Util_pair_scene_name_to_module_name,
			// Token: 0x04000BF0 RID: 3056
			enm_IMono_Util_process_window_title,
			// Token: 0x04000BF1 RID: 3057
			enm_IMono_Util_quit_game,
			// Token: 0x04000BF2 RID: 3058
			enm_IMono_Util_register_gpu_allocation_group,
			// Token: 0x04000BF3 RID: 3059
			enm_IMono_Util_register_mesh_for_gpu_morph,
			// Token: 0x04000BF4 RID: 3060
			enm_IMono_Util_save_data_as_texture,
			// Token: 0x04000BF5 RID: 3061
			enm_IMono_Util_select_entities_in_editor,
			// Token: 0x04000BF6 RID: 3062
			enm_IMono_Util_set_allocation_always_valid_scene,
			// Token: 0x04000BF7 RID: 3063
			enm_IMono_Util_set_assertion_at_shader_compile,
			// Token: 0x04000BF8 RID: 3064
			enm_IMono_Util_set_assertions_and_warnings_set_exit_code,
			// Token: 0x04000BF9 RID: 3065
			enm_IMono_Util_set_benchmark_status,
			// Token: 0x04000BFA RID: 3066
			enm_IMono_Util_set_can_load_modules,
			// Token: 0x04000BFB RID: 3067
			enm_IMono_Util_set_core_game_state,
			// Token: 0x04000BFC RID: 3068
			enm_IMono_Util_set_crash_on_asserts,
			// Token: 0x04000BFD RID: 3069
			enm_IMono_Util_set_crash_on_warnings,
			// Token: 0x04000BFE RID: 3070
			enm_IMono_Util_set_crash_report_custom_managed_stack,
			// Token: 0x04000BFF RID: 3071
			enm_IMono_Util_set_crash_report_custom_string,
			// Token: 0x04000C00 RID: 3072
			enm_IMono_Util_set_create_dump_on_warnings,
			// Token: 0x04000C01 RID: 3073
			enm_IMono_Util_set_disable_dump_generation,
			// Token: 0x04000C02 RID: 3074
			enm_IMono_Util_set_dump_folder_path,
			// Token: 0x04000C03 RID: 3075
			enm_IMono_Util_set_fixed_dt,
			// Token: 0x04000C04 RID: 3076
			enm_IMono_Util_set_force_draw_entity_id,
			// Token: 0x04000C05 RID: 3077
			enm_IMono_Util_set_force_vsync,
			// Token: 0x04000C06 RID: 3078
			enm_IMono_Util_set_frame_limiter_with_sleep,
			// Token: 0x04000C07 RID: 3079
			enm_IMono_Util_set_graphics_preset,
			// Token: 0x04000C08 RID: 3080
			enm_IMono_Util_set_loading_screen_percentage,
			// Token: 0x04000C09 RID: 3081
			enm_IMono_Util_set_message_line_rendering_state,
			// Token: 0x04000C0A RID: 3082
			enm_IMono_Util_set_print_callstack_at_crashes,
			// Token: 0x04000C0B RID: 3083
			enm_IMono_Util_set_render_agents,
			// Token: 0x04000C0C RID: 3084
			enm_IMono_Util_set_render_mode,
			// Token: 0x04000C0D RID: 3085
			enm_IMono_Util_set_report_mode,
			// Token: 0x04000C0E RID: 3086
			enm_IMono_Util_set_screen_text_rendering_state,
			// Token: 0x04000C0F RID: 3087
			enm_IMono_Util_set_watchdog_autoreport,
			// Token: 0x04000C10 RID: 3088
			enm_IMono_Util_set_watchdog_value,
			// Token: 0x04000C11 RID: 3089
			enm_IMono_Util_set_window_title,
			// Token: 0x04000C12 RID: 3090
			enm_IMono_Util_start_loading_stuck_check_state,
			// Token: 0x04000C13 RID: 3091
			enm_IMono_Util_start_scene_performance_report,
			// Token: 0x04000C14 RID: 3092
			enm_IMono_Util_take_screenshot_from_platform_path,
			// Token: 0x04000C15 RID: 3093
			enm_IMono_Util_take_screenshot_from_string_path,
			// Token: 0x04000C16 RID: 3094
			enm_IMono_Util_take_ss_from_top,
			// Token: 0x04000C17 RID: 3095
			enm_IMono_Util_toggle_render,
			// Token: 0x04000C18 RID: 3096
			enm_IMono_VideoPlayerView_create_video_player_view,
			// Token: 0x04000C19 RID: 3097
			enm_IMono_VideoPlayerView_finalize,
			// Token: 0x04000C1A RID: 3098
			enm_IMono_VideoPlayerView_is_video_finished,
			// Token: 0x04000C1B RID: 3099
			enm_IMono_VideoPlayerView_play_video,
			// Token: 0x04000C1C RID: 3100
			enm_IMono_VideoPlayerView_stop_video,
			// Token: 0x04000C1D RID: 3101
			enm_IMono_View_set_auto_depth_creation,
			// Token: 0x04000C1E RID: 3102
			enm_IMono_View_set_clear_color,
			// Token: 0x04000C1F RID: 3103
			enm_IMono_View_set_debug_render_functionality,
			// Token: 0x04000C20 RID: 3104
			enm_IMono_View_set_depth_target,
			// Token: 0x04000C21 RID: 3105
			enm_IMono_View_set_enable,
			// Token: 0x04000C22 RID: 3106
			enm_IMono_View_set_file_name_to_save_result,
			// Token: 0x04000C23 RID: 3107
			enm_IMono_View_set_file_path_to_save_result,
			// Token: 0x04000C24 RID: 3108
			enm_IMono_View_set_file_type_to_save,
			// Token: 0x04000C25 RID: 3109
			enm_IMono_View_set_offset,
			// Token: 0x04000C26 RID: 3110
			enm_IMono_View_set_render_on_demand,
			// Token: 0x04000C27 RID: 3111
			enm_IMono_View_set_render_option,
			// Token: 0x04000C28 RID: 3112
			enm_IMono_View_set_render_order,
			// Token: 0x04000C29 RID: 3113
			enm_IMono_View_set_render_target,
			// Token: 0x04000C2A RID: 3114
			enm_IMono_View_set_save_final_result_to_disk,
			// Token: 0x04000C2B RID: 3115
			enm_IMono_View_set_scale
		}
	}
}
