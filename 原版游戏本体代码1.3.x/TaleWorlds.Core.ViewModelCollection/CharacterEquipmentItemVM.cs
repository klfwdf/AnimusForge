using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection
{
	// Token: 0x02000007 RID: 7
	public class CharacterEquipmentItemVM : ViewModel
	{
		// Token: 0x0600001A RID: 26 RVA: 0x0000213C File Offset: 0x0000033C
		public CharacterEquipmentItemVM(ItemObject item)
		{
			this._item = item;
			if (this._item == null)
			{
				this.HasItem = false;
				this.Type = ItemObject.ItemTypeEnum.Invalid.ToString();
				return;
			}
			this.HasItem = true;
			this.Type = this._item.Type.ToString();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000219E File Offset: 0x0000039E
		public virtual void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(typeof(ItemObject), new object[]
			{
				new EquipmentElement(this._item, null, null, false)
			});
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000021CB File Offset: 0x000003CB
		public virtual void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600001D RID: 29 RVA: 0x000021D2 File Offset: 0x000003D2
		// (set) Token: 0x0600001E RID: 30 RVA: 0x000021DA File Offset: 0x000003DA
		[DataSourceProperty]
		public string Type
		{
			get
			{
				return this._type;
			}
			set
			{
				if (value != this._type)
				{
					this._type = value;
					base.OnPropertyChangedWithValue<string>(value, "Type");
				}
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600001F RID: 31 RVA: 0x000021FD File Offset: 0x000003FD
		// (set) Token: 0x06000020 RID: 32 RVA: 0x00002205 File Offset: 0x00000405
		[DataSourceProperty]
		public bool HasItem
		{
			get
			{
				return this._hasItem;
			}
			set
			{
				if (value != this._hasItem)
				{
					this._hasItem = value;
					base.OnPropertyChangedWithValue(value, "HasItem");
				}
			}
		}

		// Token: 0x04000005 RID: 5
		private readonly ItemObject _item;

		// Token: 0x04000006 RID: 6
		private string _type;

		// Token: 0x04000007 RID: 7
		private bool _hasItem;
	}
}
