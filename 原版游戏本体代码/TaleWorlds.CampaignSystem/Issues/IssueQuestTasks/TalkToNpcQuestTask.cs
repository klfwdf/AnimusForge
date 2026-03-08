using System;

namespace TaleWorlds.CampaignSystem.Issues.IssueQuestTasks
{
	// Token: 0x02000382 RID: 898
	public class TalkToNpcQuestTask : QuestTaskBase
	{
		// Token: 0x0600340E RID: 13326 RVA: 0x000D5A83 File Offset: 0x000D3C83
		public TalkToNpcQuestTask(Hero hero, Action onSucceededAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, null, null)
		{
			this._character = hero.CharacterObject;
		}

		// Token: 0x0600340F RID: 13327 RVA: 0x000D5A9B File Offset: 0x000D3C9B
		public TalkToNpcQuestTask(CharacterObject character, Action onSucceededAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, null, null)
		{
			this._character = character;
		}

		// Token: 0x06003410 RID: 13328 RVA: 0x000D5AAE File Offset: 0x000D3CAE
		public bool IsTaskCharacter()
		{
			return this._character == CharacterObject.OneToOneConversationCharacter;
		}

		// Token: 0x06003411 RID: 13329 RVA: 0x000D5ABD File Offset: 0x000D3CBD
		protected override void OnFinished()
		{
			this._character = null;
		}

		// Token: 0x06003412 RID: 13330 RVA: 0x000D5AC6 File Offset: 0x000D3CC6
		public override void SetReferences()
		{
		}

		// Token: 0x04000EDA RID: 3802
		private CharacterObject _character;
	}
}
