using System;
using System.ComponentModel;

namespace TaleWorlds.Library
{
	// Token: 0x02000048 RID: 72
	public interface IViewModel : INotifyPropertyChanged
	{
		// Token: 0x0600024B RID: 587
		object GetViewModelAtPath(BindingPath path);

		// Token: 0x0600024C RID: 588
		object GetViewModelAtPath(BindingPath path, bool isList);

		// Token: 0x0600024D RID: 589
		object GetPropertyValue(string name);

		// Token: 0x0600024E RID: 590
		object GetPropertyValue(string name, PropertyTypeFeeder propertyTypeFeeder);

		// Token: 0x0600024F RID: 591
		void SetPropertyValue(string name, object value);

		// Token: 0x06000250 RID: 592
		void ExecuteCommand(string commandName, object[] parameters);

		// Token: 0x1400000C RID: 12
		// (add) Token: 0x06000251 RID: 593
		// (remove) Token: 0x06000252 RID: 594
		event PropertyChangedWithValueEventHandler PropertyChangedWithValue;

		// Token: 0x1400000D RID: 13
		// (add) Token: 0x06000253 RID: 595
		// (remove) Token: 0x06000254 RID: 596
		event PropertyChangedWithBoolValueEventHandler PropertyChangedWithBoolValue;

		// Token: 0x1400000E RID: 14
		// (add) Token: 0x06000255 RID: 597
		// (remove) Token: 0x06000256 RID: 598
		event PropertyChangedWithIntValueEventHandler PropertyChangedWithIntValue;

		// Token: 0x1400000F RID: 15
		// (add) Token: 0x06000257 RID: 599
		// (remove) Token: 0x06000258 RID: 600
		event PropertyChangedWithFloatValueEventHandler PropertyChangedWithFloatValue;

		// Token: 0x14000010 RID: 16
		// (add) Token: 0x06000259 RID: 601
		// (remove) Token: 0x0600025A RID: 602
		event PropertyChangedWithUIntValueEventHandler PropertyChangedWithUIntValue;

		// Token: 0x14000011 RID: 17
		// (add) Token: 0x0600025B RID: 603
		// (remove) Token: 0x0600025C RID: 604
		event PropertyChangedWithColorValueEventHandler PropertyChangedWithColorValue;

		// Token: 0x14000012 RID: 18
		// (add) Token: 0x0600025D RID: 605
		// (remove) Token: 0x0600025E RID: 606
		event PropertyChangedWithDoubleValueEventHandler PropertyChangedWithDoubleValue;

		// Token: 0x14000013 RID: 19
		// (add) Token: 0x0600025F RID: 607
		// (remove) Token: 0x06000260 RID: 608
		event PropertyChangedWithVec2ValueEventHandler PropertyChangedWithVec2Value;
	}
}
