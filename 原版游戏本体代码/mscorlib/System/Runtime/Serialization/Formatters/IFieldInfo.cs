using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000761 RID: 1889
	[ComVisible(true)]
	public interface IFieldInfo
	{
		// Token: 0x17000DBE RID: 3518
		// (get) Token: 0x06005305 RID: 21253
		// (set) Token: 0x06005306 RID: 21254
		string[] FieldNames
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}

		// Token: 0x17000DBF RID: 3519
		// (get) Token: 0x06005307 RID: 21255
		// (set) Token: 0x06005308 RID: 21256
		Type[] FieldTypes
		{
			[SecurityCritical]
			get;
			[SecurityCritical]
			set;
		}
	}
}
