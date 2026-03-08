using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.CharacterCreation
{
	// Token: 0x0200007D RID: 125
	public abstract class CharacterCreationStageViewBase : ICharacterCreationStageListener
	{
		// Token: 0x06000558 RID: 1368 RVA: 0x00028678 File Offset: 0x00026878
		protected CharacterCreationStageViewBase(ControlCharacterCreationStage affirmativeAction, ControlCharacterCreationStage negativeAction, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
		{
			this._affirmativeAction = affirmativeAction;
			this._negativeAction = negativeAction;
			this._refreshAction = refreshAction;
			this._getTotalStageCountAction = getTotalStageCountAction;
			this._getCurrentStageIndexAction = getCurrentStageIndexAction;
			this._getFurthestIndexAction = getFurthestIndexAction;
			this._goToIndexAction = goToIndexAction;
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x000286DF File Offset: 0x000268DF
		public virtual void SetGenericScene(Scene scene)
		{
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x000286E1 File Offset: 0x000268E1
		protected virtual void OnRefresh()
		{
			this._refreshAction();
		}

		// Token: 0x0600055B RID: 1371
		public abstract IEnumerable<ScreenLayer> GetLayers();

		// Token: 0x0600055C RID: 1372
		public abstract void NextStage();

		// Token: 0x0600055D RID: 1373
		public abstract void PreviousStage();

		// Token: 0x0600055E RID: 1374 RVA: 0x000286EE File Offset: 0x000268EE
		void ICharacterCreationStageListener.OnStageFinalize()
		{
			this.OnFinalize();
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x000286F6 File Offset: 0x000268F6
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x000286F8 File Offset: 0x000268F8
		public virtual void Tick(float dt)
		{
		}

		// Token: 0x06000561 RID: 1377
		public abstract int GetVirtualStageCount();

		// Token: 0x06000562 RID: 1378 RVA: 0x000286FA File Offset: 0x000268FA
		public virtual void GoToIndex(int index)
		{
			this._goToIndexAction(index);
		}

		// Token: 0x06000563 RID: 1379
		public abstract void LoadEscapeMenuMovie();

		// Token: 0x06000564 RID: 1380
		public abstract void ReleaseEscapeMenuMovie();

		// Token: 0x06000565 RID: 1381 RVA: 0x00028708 File Offset: 0x00026908
		public void HandleEscapeMenu(CharacterCreationStageViewBase view, ScreenLayer screenLayer)
		{
			if (screenLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
			{
				if (this._isEscapeOpen)
				{
					this.RemoveEscapeMenu(view);
					return;
				}
				this.OpenEscapeMenu(view);
			}
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x00028733 File Offset: 0x00026933
		private void OpenEscapeMenu(CharacterCreationStageViewBase view)
		{
			view.LoadEscapeMenuMovie();
			this._isEscapeOpen = true;
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x00028742 File Offset: 0x00026942
		private void RemoveEscapeMenu(CharacterCreationStageViewBase view)
		{
			view.ReleaseEscapeMenuMovie();
			this._isEscapeOpen = false;
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00028754 File Offset: 0x00026954
		public List<EscapeMenuItemVM> GetEscapeMenuItems(CharacterCreationStageViewBase view)
		{
			TextObject characterCreationDisabledReason = GameTexts.FindText("str_pause_menu_disabled_hint", "CharacterCreation");
			List<EscapeMenuItemVM> list = new List<EscapeMenuItemVM>();
			list.Add(new EscapeMenuItemVM(new TextObject("{=5Saniypu}Resume", null), delegate(object o)
			{
				this.RemoveEscapeMenu(view);
			}, null, () => new Tuple<bool, TextObject>(false, null), true));
			list.Add(new EscapeMenuItemVM(new TextObject("{=PXT6aA4J}Campaign Options", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=bV75iwKa}Save", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=e0KdfaNe}Save As", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=9NuttOBC}Load", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=AbEh2y8o}Save And Exit", null), delegate(object o)
			{
			}, null, () => new Tuple<bool, TextObject>(true, characterCreationDisabledReason), false));
			list.Add(new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu", null), delegate(object o)
			{
				this.RemoveEscapeMenu(view);
				view.OnFinalize();
				MBGameManager.EndGame();
			}, null, () => new Tuple<bool, TextObject>(false, null), false));
			return list;
		}

		// Token: 0x04000283 RID: 643
		protected readonly ControlCharacterCreationStage _affirmativeAction;

		// Token: 0x04000284 RID: 644
		protected readonly ControlCharacterCreationStage _negativeAction;

		// Token: 0x04000285 RID: 645
		protected readonly ControlCharacterCreationStage _refreshAction;

		// Token: 0x04000286 RID: 646
		protected readonly ControlCharacterCreationStageReturnInt _getTotalStageCountAction;

		// Token: 0x04000287 RID: 647
		protected readonly ControlCharacterCreationStageReturnInt _getCurrentStageIndexAction;

		// Token: 0x04000288 RID: 648
		protected readonly ControlCharacterCreationStageReturnInt _getFurthestIndexAction;

		// Token: 0x04000289 RID: 649
		protected readonly ControlCharacterCreationStageWithInt _goToIndexAction;

		// Token: 0x0400028A RID: 650
		protected readonly Vec3 _cameraPosition = new Vec3(6.45f, 4.35f, 1.6f, -1f);

		// Token: 0x0400028B RID: 651
		private bool _isEscapeOpen;
	}
}
