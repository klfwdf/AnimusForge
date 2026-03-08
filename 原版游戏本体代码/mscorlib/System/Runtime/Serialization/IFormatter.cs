using System;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization
{
	// Token: 0x02000732 RID: 1842
	[ComVisible(true)]
	public interface IFormatter
	{
		// Token: 0x060051A7 RID: 20903
		object Deserialize(Stream serializationStream);

		// Token: 0x060051A8 RID: 20904
		void Serialize(Stream serializationStream, object graph);

		// Token: 0x17000D71 RID: 3441
		// (get) Token: 0x060051A9 RID: 20905
		// (set) Token: 0x060051AA RID: 20906
		ISurrogateSelector SurrogateSelector { get; set; }

		// Token: 0x17000D72 RID: 3442
		// (get) Token: 0x060051AB RID: 20907
		// (set) Token: 0x060051AC RID: 20908
		SerializationBinder Binder { get; set; }

		// Token: 0x17000D73 RID: 3443
		// (get) Token: 0x060051AD RID: 20909
		// (set) Token: 0x060051AE RID: 20910
		StreamingContext Context { get; set; }
	}
}
