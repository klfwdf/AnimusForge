using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000037 RID: 55
	public class CampaignEntityComponent : IEntityComponent
	{
		// Token: 0x060003CE RID: 974 RVA: 0x0001E16B File Offset: 0x0001C36B
		void IEntityComponent.OnInitialize()
		{
			this.OnInitialize();
		}

		// Token: 0x060003CF RID: 975 RVA: 0x0001E173 File Offset: 0x0001C373
		void IEntityComponent.OnFinalize()
		{
			this.OnFinalize();
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0001E17B File Offset: 0x0001C37B
		protected virtual void OnInitialize()
		{
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0001E17D File Offset: 0x0001C37D
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0001E17F File Offset: 0x0001C37F
		public virtual void OnTick(float realDt, float dt)
		{
		}
	}
}
