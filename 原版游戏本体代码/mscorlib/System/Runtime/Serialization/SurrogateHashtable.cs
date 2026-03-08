using System;
using System.Collections;

namespace System.Runtime.Serialization
{
	// Token: 0x0200075B RID: 1883
	internal class SurrogateHashtable : Hashtable
	{
		// Token: 0x060052F3 RID: 21235 RVA: 0x00123B20 File Offset: 0x00121D20
		internal SurrogateHashtable(int size)
			: base(size)
		{
		}

		// Token: 0x060052F4 RID: 21236 RVA: 0x00123B2C File Offset: 0x00121D2C
		protected override bool KeyEquals(object key, object item)
		{
			SurrogateKey surrogateKey = (SurrogateKey)item;
			SurrogateKey surrogateKey2 = (SurrogateKey)key;
			return surrogateKey2.m_type == surrogateKey.m_type && (surrogateKey2.m_context.m_state & surrogateKey.m_context.m_state) == surrogateKey.m_context.m_state && surrogateKey2.m_context.Context == surrogateKey.m_context.Context;
		}
	}
}
