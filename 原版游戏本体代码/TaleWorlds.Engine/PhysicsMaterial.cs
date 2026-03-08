using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000078 RID: 120
	[EngineStruct("int", false, null)]
	public readonly struct PhysicsMaterial
	{
		// Token: 0x06000A8F RID: 2703 RVA: 0x0000ACFF File Offset: 0x00008EFF
		internal PhysicsMaterial(int index)
		{
			this = default(PhysicsMaterial);
			this.Index = index;
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000A90 RID: 2704 RVA: 0x0000AD0F File Offset: 0x00008F0F
		public bool IsValid
		{
			get
			{
				return this.Index >= 0;
			}
		}

		// Token: 0x06000A91 RID: 2705 RVA: 0x0000AD1D File Offset: 0x00008F1D
		public PhysicsMaterialFlags GetFlags()
		{
			return PhysicsMaterial.GetFlagsAtIndex(this.Index);
		}

		// Token: 0x06000A92 RID: 2706 RVA: 0x0000AD2A File Offset: 0x00008F2A
		public float GetDynamicFriction()
		{
			return PhysicsMaterial.GetDynamicFrictionAtIndex(this.Index);
		}

		// Token: 0x06000A93 RID: 2707 RVA: 0x0000AD37 File Offset: 0x00008F37
		public float GetStaticFriction()
		{
			return PhysicsMaterial.GetStaticFrictionAtIndex(this.Index);
		}

		// Token: 0x06000A94 RID: 2708 RVA: 0x0000AD44 File Offset: 0x00008F44
		public float GetRestitution()
		{
			return PhysicsMaterial.GetRestitutionAtIndex(this.Index);
		}

		// Token: 0x06000A95 RID: 2709 RVA: 0x0000AD51 File Offset: 0x00008F51
		public float GetLinearDamping()
		{
			return PhysicsMaterial.GetLinearDampingAtIndex(this.Index);
		}

		// Token: 0x06000A96 RID: 2710 RVA: 0x0000AD5E File Offset: 0x00008F5E
		public float GetAngularDamping()
		{
			return PhysicsMaterial.GetAngularDampingAtIndex(this.Index);
		}

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000A97 RID: 2711 RVA: 0x0000AD6B File Offset: 0x00008F6B
		public string Name
		{
			get
			{
				return PhysicsMaterial.GetNameAtIndex(this.Index);
			}
		}

		// Token: 0x06000A98 RID: 2712 RVA: 0x0000AD78 File Offset: 0x00008F78
		public bool Equals(PhysicsMaterial m)
		{
			return this.Index == m.Index;
		}

		// Token: 0x06000A99 RID: 2713 RVA: 0x0000AD88 File Offset: 0x00008F88
		public static int GetMaterialCount()
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetMaterialCount();
		}

		// Token: 0x06000A9A RID: 2714 RVA: 0x0000AD94 File Offset: 0x00008F94
		public static PhysicsMaterial GetFromName(string id)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetIndexWithName(id);
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x0000ADA1 File Offset: 0x00008FA1
		public static string GetNameAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetMaterialNameAtIndex(index);
		}

		// Token: 0x06000A9C RID: 2716 RVA: 0x0000ADAE File Offset: 0x00008FAE
		public static PhysicsMaterialFlags GetFlagsAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetFlagsAtIndex(index);
		}

		// Token: 0x06000A9D RID: 2717 RVA: 0x0000ADBB File Offset: 0x00008FBB
		public static float GetRestitutionAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetRestitutionAtIndex(index);
		}

		// Token: 0x06000A9E RID: 2718 RVA: 0x0000ADC8 File Offset: 0x00008FC8
		public static float GetDynamicFrictionAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetDynamicFrictionAtIndex(index);
		}

		// Token: 0x06000A9F RID: 2719 RVA: 0x0000ADD5 File Offset: 0x00008FD5
		public static float GetStaticFrictionAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetStaticFrictionAtIndex(index);
		}

		// Token: 0x06000AA0 RID: 2720 RVA: 0x0000ADE2 File Offset: 0x00008FE2
		public static float GetLinearDampingAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetLinearDampingAtIndex(index);
		}

		// Token: 0x06000AA1 RID: 2721 RVA: 0x0000ADEF File Offset: 0x00008FEF
		public static float GetAngularDampingAtIndex(int index)
		{
			return EngineApplicationInterface.IPhysicsMaterial.GetAngularDampingAtIndex(index);
		}

		// Token: 0x06000AA2 RID: 2722 RVA: 0x0000ADFC File Offset: 0x00008FFC
		public static PhysicsMaterial GetFromIndex(int index)
		{
			return new PhysicsMaterial(index);
		}

		// Token: 0x0400016A RID: 362
		[CustomEngineStructMemberData("ignoredMember", true)]
		public readonly int Index;

		// Token: 0x0400016B RID: 363
		public static readonly PhysicsMaterial InvalidPhysicsMaterial = new PhysicsMaterial(-1);
	}
}
