using System;
using SandBox.View.Map.Visuals;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.View.Map
{
	// Token: 0x0200003E RID: 62
	public class CampaignEntityVisualComponent : IEntityComponent
	{
		// Token: 0x060001FA RID: 506 RVA: 0x00013FAF File Offset: 0x000121AF
		public virtual void OnVisualTick(MapScreen screen, float realDt, float dt)
		{
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00013FB1 File Offset: 0x000121B1
		public virtual bool OnMouseClick(MapEntityVisual visualOfSelectedEntity, Vec3 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
		{
			return false;
		}

		// Token: 0x060001FC RID: 508 RVA: 0x00013FB4 File Offset: 0x000121B4
		public virtual bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
		{
			return false;
		}

		// Token: 0x060001FD RID: 509 RVA: 0x00013FB7 File Offset: 0x000121B7
		public virtual void OnFrameTick(float dt)
		{
		}

		// Token: 0x060001FE RID: 510 RVA: 0x00013FB9 File Offset: 0x000121B9
		public virtual void OnGameLoadFinished()
		{
		}

		// Token: 0x060001FF RID: 511 RVA: 0x00013FBB File Offset: 0x000121BB
		public virtual void OnTick(float realDt, float dt)
		{
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00013FBD File Offset: 0x000121BD
		public virtual void ClearVisualMemory()
		{
		}

		// Token: 0x06000201 RID: 513 RVA: 0x00013FBF File Offset: 0x000121BF
		void IEntityComponent.OnInitialize()
		{
			this.OnInitialize();
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00013FC7 File Offset: 0x000121C7
		void IEntityComponent.OnFinalize()
		{
			this.OnFinalize();
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00013FCF File Offset: 0x000121CF
		protected virtual void OnInitialize()
		{
		}

		// Token: 0x06000204 RID: 516 RVA: 0x00013FD1 File Offset: 0x000121D1
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000205 RID: 517 RVA: 0x00013FD3 File Offset: 0x000121D3
		public virtual int Priority
		{
			get
			{
				return 0;
			}
		}
	}
}
