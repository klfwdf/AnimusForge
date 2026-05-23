using System;
using System.Runtime.InteropServices;

namespace System.Resources
{
	// Token: 0x0200038F RID: 911
	[ComVisible(true)]
	public interface IResourceWriter : IDisposable
	{
		// Token: 0x06002CF1 RID: 11505
		void AddResource(string name, string value);

		// Token: 0x06002CF2 RID: 11506
		void AddResource(string name, object value);

		// Token: 0x06002CF3 RID: 11507
		void AddResource(string name, byte[] value);

		// Token: 0x06002CF4 RID: 11508
		void Close();

		// Token: 0x06002CF5 RID: 11509
		void Generate();
	}
}
