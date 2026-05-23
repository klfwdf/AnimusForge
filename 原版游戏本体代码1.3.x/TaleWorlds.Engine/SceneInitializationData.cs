using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200007F RID: 127
	[EngineStruct("rglScene_initialization_data", false, null)]
	public struct SceneInitializationData
	{
		// Token: 0x06000AB3 RID: 2739 RVA: 0x0000B078 File Offset: 0x00009278
		public SceneInitializationData(bool initializeWithDefaults)
		{
			if (initializeWithDefaults)
			{
				this.CamPosFromScene = MatrixFrame.Identity;
				this.InitPhysicsWorld = true;
				this.LoadNavMesh = true;
				this.InitFloraNodes = true;
				this.UsePhysicsMaterials = true;
				this.EnableFloraPhysics = true;
				this.UseTerrainMeshBlending = true;
				this.DoNotUseLoadingScreen = false;
				this.CreateOros = false;
				this.ForTerrainShaderCompile = false;
				this.InitSkyboxFromStart = true;
				return;
			}
			this.CamPosFromScene = MatrixFrame.Identity;
			this.InitPhysicsWorld = false;
			this.LoadNavMesh = false;
			this.InitFloraNodes = false;
			this.UsePhysicsMaterials = false;
			this.EnableFloraPhysics = false;
			this.UseTerrainMeshBlending = false;
			this.DoNotUseLoadingScreen = false;
			this.CreateOros = false;
			this.ForTerrainShaderCompile = false;
			this.InitSkyboxFromStart = true;
		}

		// Token: 0x04000196 RID: 406
		public MatrixFrame CamPosFromScene;

		// Token: 0x04000197 RID: 407
		[MarshalAs(UnmanagedType.U1)]
		public bool InitPhysicsWorld;

		// Token: 0x04000198 RID: 408
		[MarshalAs(UnmanagedType.U1)]
		public bool LoadNavMesh;

		// Token: 0x04000199 RID: 409
		[MarshalAs(UnmanagedType.U1)]
		public bool InitFloraNodes;

		// Token: 0x0400019A RID: 410
		[MarshalAs(UnmanagedType.U1)]
		public bool UsePhysicsMaterials;

		// Token: 0x0400019B RID: 411
		[MarshalAs(UnmanagedType.U1)]
		public bool EnableFloraPhysics;

		// Token: 0x0400019C RID: 412
		[MarshalAs(UnmanagedType.U1)]
		public bool UseTerrainMeshBlending;

		// Token: 0x0400019D RID: 413
		[MarshalAs(UnmanagedType.U1)]
		public bool DoNotUseLoadingScreen;

		// Token: 0x0400019E RID: 414
		[MarshalAs(UnmanagedType.U1)]
		public bool CreateOros;

		// Token: 0x0400019F RID: 415
		[MarshalAs(UnmanagedType.U1)]
		public bool ForTerrainShaderCompile;

		// Token: 0x040001A0 RID: 416
		[MarshalAs(UnmanagedType.U1)]
		public bool InitSkyboxFromStart;
	}
}
