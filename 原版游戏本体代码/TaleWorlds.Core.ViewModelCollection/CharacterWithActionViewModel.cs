using System;

namespace TaleWorlds.Core.ViewModelCollection
{
	// Token: 0x02000009 RID: 9
	public class CharacterWithActionViewModel : CharacterViewModel
	{
		// Token: 0x06000054 RID: 84 RVA: 0x00002990 File Offset: 0x00000B90
		public CharacterWithActionViewModel(Action onAction)
		{
			this._onAction = onAction;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x0000299F File Offset: 0x00000B9F
		private void ExecuteAction()
		{
			Action onAction = this._onAction;
			if (onAction == null)
			{
				return;
			}
			onAction();
		}

		// Token: 0x04000020 RID: 32
		private Action _onAction;
	}
}
