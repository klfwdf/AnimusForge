using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000760 RID: 1888
	[ComVisible(true)]
	public interface ISoapMessage
	{
		// Token: 0x17000DB8 RID: 3512
		// (get) Token: 0x060052F9 RID: 21241
		// (set) Token: 0x060052FA RID: 21242
		string[] ParamNames { get; set; }

		// Token: 0x17000DB9 RID: 3513
		// (get) Token: 0x060052FB RID: 21243
		// (set) Token: 0x060052FC RID: 21244
		object[] ParamValues { get; set; }

		// Token: 0x17000DBA RID: 3514
		// (get) Token: 0x060052FD RID: 21245
		// (set) Token: 0x060052FE RID: 21246
		Type[] ParamTypes { get; set; }

		// Token: 0x17000DBB RID: 3515
		// (get) Token: 0x060052FF RID: 21247
		// (set) Token: 0x06005300 RID: 21248
		string MethodName { get; set; }

		// Token: 0x17000DBC RID: 3516
		// (get) Token: 0x06005301 RID: 21249
		// (set) Token: 0x06005302 RID: 21250
		string XmlNameSpace { get; set; }

		// Token: 0x17000DBD RID: 3517
		// (get) Token: 0x06005303 RID: 21251
		// (set) Token: 0x06005304 RID: 21252
		Header[] Headers { get; set; }
	}
}
