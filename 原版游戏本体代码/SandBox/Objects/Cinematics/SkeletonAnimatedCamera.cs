using System;
using System.Collections.Generic;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Cinematics
{
	// Token: 0x02000042 RID: 66
	public class SkeletonAnimatedCamera : ScriptComponentBehavior
	{
		// Token: 0x06000282 RID: 642 RVA: 0x0000EFD8 File Offset: 0x0000D1D8
		private void CreateVisualizer()
		{
			if (this.SkeletonName != "" && this.AnimationName != "")
			{
				base.GameEntity.CreateSimpleSkeleton(this.SkeletonName);
				base.GameEntity.Skeleton.SetAnimationAtChannel(this.AnimationName, 0, 1f, -1f, 0f);
			}
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0000F043 File Offset: 0x0000D243
		protected override void OnInit()
		{
			base.OnInit();
			this.CreateVisualizer();
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0000F051 File Offset: 0x0000D251
		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			this.OnInit();
		}

		// Token: 0x06000285 RID: 645 RVA: 0x0000F060 File Offset: 0x0000D260
		protected override void OnTick(float dt)
		{
			GameEntity gameEntity = base.GameEntity.Scene.FindEntityWithTag("camera_instance");
			if (gameEntity != null && base.GameEntity.Skeleton != null)
			{
				MatrixFrame matrixFrame = base.GameEntity.Skeleton.GetBoneEntitialFrame((sbyte)this.BoneIndex);
				matrixFrame = base.GameEntity.GetGlobalFrame().TransformToParent(matrixFrame);
				MatrixFrame listenerFrame = default(MatrixFrame);
				listenerFrame.rotation = matrixFrame.rotation;
				listenerFrame.rotation.u = -matrixFrame.rotation.s;
				listenerFrame.rotation.f = -matrixFrame.rotation.u;
				listenerFrame.rotation.s = matrixFrame.rotation.f;
				listenerFrame.origin = matrixFrame.origin + this.AttachmentOffset;
				gameEntity.SetGlobalFrame(listenerFrame, true);
				SoundManager.SetListenerFrame(listenerFrame);
			}
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000F16B File Offset: 0x0000D36B
		protected override void OnEditorTick(float dt)
		{
			this.OnTick(dt);
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000F174 File Offset: 0x0000D374
		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "SkeletonName" || variableName == "AnimationName")
			{
				this.CreateVisualizer();
			}
			if (variableName == "Restart")
			{
				List<GameEntity> list = new List<GameEntity>();
				base.GameEntity.Scene.GetAllEntitiesWithScriptComponent<AnimationPoint>(ref list);
				foreach (GameEntity gameEntity in list)
				{
					gameEntity.GetFirstScriptOfType<AnimationPoint>().RequestResync();
				}
				this.CreateVisualizer();
			}
		}

		// Token: 0x04000117 RID: 279
		public string SkeletonName = "human_skeleton";

		// Token: 0x04000118 RID: 280
		public int BoneIndex;

		// Token: 0x04000119 RID: 281
		public Vec3 AttachmentOffset = new Vec3(0f, 0f, 0f, -1f);

		// Token: 0x0400011A RID: 282
		public string AnimationName = "";

		// Token: 0x0400011B RID: 283
		public SimpleButton Restart;
	}
}
