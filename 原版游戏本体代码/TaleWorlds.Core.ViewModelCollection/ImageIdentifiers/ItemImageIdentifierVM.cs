using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x02000022 RID: 34
	public class ItemImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x060001A9 RID: 425 RVA: 0x00005B54 File Offset: 0x00003D54
		public ItemImageIdentifierVM(ItemObject itemObject, string bannerCode = "")
		{
			this._itemObject = itemObject;
			this._bannerCode = bannerCode;
			base.ImageIdentifier = new ItemImageIdentifier(this._itemObject, this._bannerCode);
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00005B81 File Offset: 0x00003D81
		public ItemImageIdentifierVM Clone()
		{
			return new ItemImageIdentifierVM(this._itemObject, this._bannerCode);
		}

		// Token: 0x040000A9 RID: 169
		private readonly ItemObject _itemObject;

		// Token: 0x040000AA RID: 170
		private readonly string _bannerCode;
	}
}
