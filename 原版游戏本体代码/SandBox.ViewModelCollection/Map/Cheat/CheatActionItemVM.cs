using System;

namespace SandBox.ViewModelCollection.Map.Cheat
{
	// Token: 0x0200004E RID: 78
	public class CheatActionItemVM : CheatItemBaseVM
	{
		// Token: 0x060004D9 RID: 1241 RVA: 0x000129AA File Offset: 0x00010BAA
		public CheatActionItemVM(GameplayCheatItem cheat, Action<CheatActionItemVM> onCheatExecuted)
		{
			this._onCheatExecuted = onCheatExecuted;
			this.Cheat = cheat;
			this.RefreshValues();
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x000129C6 File Offset: 0x00010BC6
		public override void RefreshValues()
		{
			base.RefreshValues();
			GameplayCheatItem cheat = this.Cheat;
			base.Name = ((cheat != null) ? cheat.GetName().ToString() : null);
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x000129EB File Offset: 0x00010BEB
		public override void ExecuteAction()
		{
			GameplayCheatItem cheat = this.Cheat;
			if (cheat != null)
			{
				cheat.ExecuteCheat();
			}
			Action<CheatActionItemVM> onCheatExecuted = this._onCheatExecuted;
			if (onCheatExecuted == null)
			{
				return;
			}
			onCheatExecuted(this);
		}

		// Token: 0x04000264 RID: 612
		public readonly GameplayCheatItem Cheat;

		// Token: 0x04000265 RID: 613
		private readonly Action<CheatActionItemVM> _onCheatExecuted;
	}
}
