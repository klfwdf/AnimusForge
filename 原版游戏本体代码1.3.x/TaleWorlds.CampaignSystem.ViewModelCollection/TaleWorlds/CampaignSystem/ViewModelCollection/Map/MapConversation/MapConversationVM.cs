using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapConversation
{
	// Token: 0x02000058 RID: 88
	public class MapConversationVM : ViewModel
	{
		// Token: 0x0600065E RID: 1630 RVA: 0x00020804 File Offset: 0x0001EA04
		public MapConversationVM(Action onContinue, Func<string> getContinueInputText)
		{
			this._onContinue = onContinue;
			this.DialogController = new MissionConversationVM(getContinueInputText, false);
			this.TableauData = null;
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x00020827 File Offset: 0x0001EA27
		public void ExecuteContinue()
		{
			Action onContinue = this._onContinue;
			if (onContinue == null)
			{
				return;
			}
			onContinue();
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x00020839 File Offset: 0x0001EA39
		public override void OnFinalize()
		{
			base.OnFinalize();
			MissionConversationVM dialogController = this.DialogController;
			if (dialogController != null)
			{
				dialogController.OnFinalize();
			}
			this.DialogController = null;
			this.TableauData = null;
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x00020860 File Offset: 0x0001EA60
		public void Tick(float dt)
		{
			MissionConversationVM dialogController = this.DialogController;
			if (dialogController == null)
			{
				return;
			}
			dialogController.Tick(dt);
		}

		// Token: 0x170001B3 RID: 435
		// (get) Token: 0x06000662 RID: 1634 RVA: 0x00020873 File Offset: 0x0001EA73
		// (set) Token: 0x06000663 RID: 1635 RVA: 0x0002087B File Offset: 0x0001EA7B
		[DataSourceProperty]
		public MissionConversationVM DialogController
		{
			get
			{
				return this._dialogController;
			}
			set
			{
				if (value != this._dialogController)
				{
					this._dialogController = value;
					base.OnPropertyChangedWithValue<MissionConversationVM>(value, "DialogController");
				}
			}
		}

		// Token: 0x170001B4 RID: 436
		// (get) Token: 0x06000664 RID: 1636 RVA: 0x00020899 File Offset: 0x0001EA99
		// (set) Token: 0x06000665 RID: 1637 RVA: 0x000208A1 File Offset: 0x0001EAA1
		[DataSourceProperty]
		public object TableauData
		{
			get
			{
				return this._tableauData;
			}
			set
			{
				if (value != this._tableauData)
				{
					this._tableauData = value;
					base.OnPropertyChangedWithValue<object>(value, "TableauData");
				}
			}
		}

		// Token: 0x170001B5 RID: 437
		// (get) Token: 0x06000666 RID: 1638 RVA: 0x000208BF File Offset: 0x0001EABF
		// (set) Token: 0x06000667 RID: 1639 RVA: 0x000208C7 File Offset: 0x0001EAC7
		[DataSourceProperty]
		public bool IsBarterActive
		{
			get
			{
				return this._isBarterActive;
			}
			set
			{
				if (value != this._isBarterActive)
				{
					this._isBarterActive = value;
					base.OnPropertyChangedWithValue(value, "IsBarterActive");
				}
			}
		}

		// Token: 0x040002B4 RID: 692
		private readonly Action _onContinue;

		// Token: 0x040002B5 RID: 693
		private MissionConversationVM _dialogController;

		// Token: 0x040002B6 RID: 694
		private object _tableauData;

		// Token: 0x040002B7 RID: 695
		private bool _isBarterActive;
	}
}
