using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x0200084E RID: 2126
	[ComVisible(true)]
	public class SinkProviderData
	{
		// Token: 0x06005A35 RID: 23093 RVA: 0x0013D83F File Offset: 0x0013BA3F
		public SinkProviderData(string name)
		{
			this._name = name;
		}

		// Token: 0x17000F01 RID: 3841
		// (get) Token: 0x06005A36 RID: 23094 RVA: 0x0013D869 File Offset: 0x0013BA69
		public string Name
		{
			get
			{
				return this._name;
			}
		}

		// Token: 0x17000F02 RID: 3842
		// (get) Token: 0x06005A37 RID: 23095 RVA: 0x0013D871 File Offset: 0x0013BA71
		public IDictionary Properties
		{
			get
			{
				return this._properties;
			}
		}

		// Token: 0x17000F03 RID: 3843
		// (get) Token: 0x06005A38 RID: 23096 RVA: 0x0013D879 File Offset: 0x0013BA79
		public IList Children
		{
			get
			{
				return this._children;
			}
		}

		// Token: 0x040028FF RID: 10495
		private string _name;

		// Token: 0x04002900 RID: 10496
		private Hashtable _properties = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

		// Token: 0x04002901 RID: 10497
		private ArrayList _children = new ArrayList();
	}
}
