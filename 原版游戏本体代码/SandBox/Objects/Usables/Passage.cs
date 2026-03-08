using System;
using SandBox.AI;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables
{
	// Token: 0x0200004D RID: 77
	public class Passage : UsableMachine
	{
		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060002D5 RID: 725 RVA: 0x00010158 File Offset: 0x0000E358
		public Location ToLocation
		{
			get
			{
				PassageUsePoint passageUsePoint;
				if ((passageUsePoint = base.PilotStandingPoint as PassageUsePoint) == null)
				{
					return null;
				}
				return passageUsePoint.ToLocation;
			}
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x0001017C File Offset: 0x0000E37C
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			StandingPoint pilotStandingPoint = base.PilotStandingPoint;
			return ((pilotStandingPoint != null) ? pilotStandingPoint.DescriptionMessage : null) ?? TextObject.GetEmpty();
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x00010199 File Offset: 0x0000E399
		public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
		{
			StandingPoint pilotStandingPoint = base.PilotStandingPoint;
			if (pilotStandingPoint == null)
			{
				return null;
			}
			return pilotStandingPoint.ActionMessage;
		}

		// Token: 0x060002D8 RID: 728 RVA: 0x000101AC File Offset: 0x0000E3AC
		public override UsableMachineAIBase CreateAIBehaviorObject()
		{
			return new PassageAI(this);
		}
	}
}
