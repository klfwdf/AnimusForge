using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace SandBox.View.Map.Visuals
{
	// Token: 0x02000060 RID: 96
	public abstract class MapEntityVisual
	{
		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060003C7 RID: 967 RVA: 0x0001E23C File Offset: 0x0001C43C
		public MapScreen MapScreen
		{
			get
			{
				return MapScreen.Instance;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060003C8 RID: 968
		public abstract CampaignVec2 InteractionPositionForPlayer { get; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060003C9 RID: 969
		public abstract MapEntityVisual AttachedTo { get; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060003CA RID: 970 RVA: 0x0001E243 File Offset: 0x0001C443
		public virtual bool IsMobileEntity
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060003CB RID: 971 RVA: 0x0001E246 File Offset: 0x0001C446
		// (set) Token: 0x060003CC RID: 972 RVA: 0x0001E24E File Offset: 0x0001C44E
		public virtual MatrixFrame CircleLocalFrame { get; protected set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060003CD RID: 973 RVA: 0x0001E257 File Offset: 0x0001C457
		public virtual bool IsMainEntity
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060003CE RID: 974 RVA: 0x0001E25A File Offset: 0x0001C45A
		public virtual float BearingRotation { get; }

		// Token: 0x060003CF RID: 975
		public abstract bool OnMapClick(bool followModifierUsed);

		// Token: 0x060003D0 RID: 976
		public abstract void OnHover();

		// Token: 0x060003D1 RID: 977
		public abstract void OnOpenEncyclopedia();

		// Token: 0x060003D2 RID: 978
		public abstract bool IsVisibleOrFadingOut();

		// Token: 0x060003D3 RID: 979
		public abstract Vec3 GetVisualPosition();

		// Token: 0x060003D4 RID: 980 RVA: 0x0001E262 File Offset: 0x0001C462
		public virtual void ReleaseResources()
		{
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x0001E264 File Offset: 0x0001C464
		public virtual void OnHoverEnd()
		{
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0001E266 File Offset: 0x0001C466
		public virtual void OnTrackAction()
		{
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0001E268 File Offset: 0x0001C468
		public virtual bool IsEnemyOf(IFaction faction)
		{
			return false;
		}

		// Token: 0x060003D8 RID: 984 RVA: 0x0001E26B File Offset: 0x0001C46B
		public virtual bool IsAllyOf(IFaction faction)
		{
			return false;
		}
	}
}
