using System;
using System.Collections.Generic;
using SandBox.View.Map;
using SandBox.View.Map.Visuals;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.View
{
	// Token: 0x0200000B RID: 11
	public class SandBoxViewVisualManager
	{
		// Token: 0x06000055 RID: 85 RVA: 0x00003E74 File Offset: 0x00002074
		public SandBoxViewVisualManager()
		{
			this._components = new EntitySystem<CampaignEntityVisualComponent>();
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003E88 File Offset: 0x00002088
		public static void VisualTick(MapScreen screen, float realDt, float dt)
		{
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				campaignEntityVisualComponent.OnVisualTick(screen, realDt, dt);
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003EE0 File Offset: 0x000020E0
		public static void OnTick(float realDt, float dt)
		{
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				campaignEntityVisualComponent.OnTick(realDt, dt);
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003F38 File Offset: 0x00002138
		public static void ClearVisualMemory()
		{
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				campaignEntityVisualComponent.ClearVisualMemory();
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003F8C File Offset: 0x0000218C
		public static void OnFrameTick(float dt)
		{
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				campaignEntityVisualComponent.OnFrameTick(dt);
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003FE4 File Offset: 0x000021E4
		public static bool OnMouseClick(MapEntityVisual visualOfSelectedEntity, Vec3 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
		{
			bool flag = false;
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				flag |= campaignEntityVisualComponent.OnMouseClick(visualOfSelectedEntity, intersectionPoint, mouseOverFaceIndex, isDoubleClick);
			}
			return flag;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00004044 File Offset: 0x00002244
		public static void OnGameLoadFinished()
		{
			foreach (CampaignEntityVisualComponent campaignEntityVisualComponent in SandBoxViewSubModule.SandBoxViewVisualManager.GetComponents())
			{
				campaignEntityVisualComponent.OnGameLoadFinished();
			}
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00004098 File Offset: 0x00002298
		public TComponent GetEntityComponent<TComponent>() where TComponent : CampaignEntityVisualComponent
		{
			EntitySystem<CampaignEntityVisualComponent> components = this._components;
			if (components == null)
			{
				return default(TComponent);
			}
			return components.GetComponent<TComponent>();
		}

		// Token: 0x0600005D RID: 93 RVA: 0x000040BE File Offset: 0x000022BE
		public TComponent AddEntityComponent<TComponent>() where TComponent : CampaignEntityVisualComponent, new()
		{
			TComponent result = this._components.AddComponent<TComponent>();
			this.SortComponents();
			return result;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x000040D1 File Offset: 0x000022D1
		public void RemoveEntityComponent<TComponent>() where TComponent : CampaignEntityVisualComponent
		{
			this._components.RemoveComponent<TComponent>();
		}

		// Token: 0x0600005F RID: 95 RVA: 0x000040DE File Offset: 0x000022DE
		public void Finalize<TComponent>(TComponent component) where TComponent : CampaignEntityVisualComponent
		{
			this._components.Finalize(component);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x000040F1 File Offset: 0x000022F1
		public void RemoveEntityComponent<TComponent>(TComponent component) where TComponent : CampaignEntityVisualComponent
		{
			this._components.RemoveComponent(component);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00004104 File Offset: 0x00002304
		public List<TComponent> GetComponents<TComponent>() where TComponent : CampaignEntityVisualComponent
		{
			return this._components.GetComponents<TComponent>();
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00004111 File Offset: 0x00002311
		public MBList<CampaignEntityVisualComponent> GetComponents()
		{
			return this._components.GetComponents();
		}

		// Token: 0x06000063 RID: 99 RVA: 0x0000411E File Offset: 0x0000231E
		private void SortComponents()
		{
			this._components.SortComponents<CampaignEntityVisualComponent>(SandBoxViewVisualManager._comparisonDelegate);
		}

		// Token: 0x04000012 RID: 18
		private EntitySystem<CampaignEntityVisualComponent> _components;

		// Token: 0x04000013 RID: 19
		private static readonly Comparison<CampaignEntityVisualComponent> _comparisonDelegate = (CampaignEntityVisualComponent x, CampaignEntityVisualComponent y) => x.Priority.CompareTo(y.Priority);
	}
}
