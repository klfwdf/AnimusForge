using System;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Cheat
{
	// Token: 0x0200004F RID: 79
	public class CheatGroupItemVM : CheatItemBaseVM
	{
		// Token: 0x060004DC RID: 1244 RVA: 0x00012A0F File Offset: 0x00010C0F
		public CheatGroupItemVM(GameplayCheatGroup cheatGroup, Action<CheatGroupItemVM> onSelectCheatGroup)
		{
			this.CheatGroup = cheatGroup;
			this._onSelectCheatGroup = onSelectCheatGroup;
			this.RefreshValues();
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x00012A2B File Offset: 0x00010C2B
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject name = this.CheatGroup.GetName();
			base.Name = ((name != null) ? name.ToString() : null);
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x00012A50 File Offset: 0x00010C50
		public override void ExecuteAction()
		{
			Action<CheatGroupItemVM> onSelectCheatGroup = this._onSelectCheatGroup;
			if (onSelectCheatGroup == null)
			{
				return;
			}
			onSelectCheatGroup(this);
		}

		// Token: 0x04000266 RID: 614
		public readonly GameplayCheatGroup CheatGroup;

		// Token: 0x04000267 RID: 615
		private readonly Action<CheatGroupItemVM> _onSelectCheatGroup;
	}
}
