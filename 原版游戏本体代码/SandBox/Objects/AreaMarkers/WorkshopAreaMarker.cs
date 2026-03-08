using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace SandBox.Objects.AreaMarkers
{
	// Token: 0x02000047 RID: 71
	public class WorkshopAreaMarker : AreaMarker
	{
		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x0000F648 File Offset: 0x0000D848
		public override string Tag
		{
			get
			{
				Workshop workshop = this.GetWorkshop();
				if (workshop == null)
				{
					return null;
				}
				return workshop.Tag;
			}
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x0000F65C File Offset: 0x0000D85C
		public Workshop GetWorkshop()
		{
			Workshop result = null;
			Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
			if (settlement != null && settlement.IsTown && settlement.Town.Workshops.Length > this.AreaIndex && this.AreaIndex > 0)
			{
				result = settlement.Town.Workshops[this.AreaIndex];
			}
			return result;
		}

		// Token: 0x060002A7 RID: 679 RVA: 0x0000F6B4 File Offset: 0x0000D8B4
		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			if (MBEditor.HelpersEnabled() && this.CheckToggle)
			{
				float distanceSquared = this.AreaRadius * this.AreaRadius;
				List<GameEntity> list = new List<GameEntity>();
				base.Scene.GetEntities(ref list);
				foreach (GameEntity gameEntity in list)
				{
					gameEntity.HasTag("shop_prop");
				}
				foreach (UsableMachine usableMachine in (from x in list.FindAllWithType<UsableMachine>()
					where x.GameEntity.GlobalPosition.DistanceSquared(this.GameEntity.GlobalPosition) <= distanceSquared
					select x).ToList<UsableMachine>())
				{
				}
			}
		}

		// Token: 0x060002A8 RID: 680 RVA: 0x0000F7A8 File Offset: 0x0000D9A8
		public WorkshopType GetWorkshopType()
		{
			Workshop workshop = this.GetWorkshop();
			if (workshop == null)
			{
				return null;
			}
			return workshop.WorkshopType;
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x0000F7BB File Offset: 0x0000D9BB
		public override TextObject GetName()
		{
			Workshop workshop = this.GetWorkshop();
			if (workshop == null)
			{
				return null;
			}
			return workshop.Name;
		}
	}
}
