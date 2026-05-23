using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000851 RID: 2129
	[SecurityCritical]
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
	public abstract class BaseChannelObjectWithProperties : IDictionary, ICollection, IEnumerable
	{
		// Token: 0x17000F05 RID: 3845
		// (get) Token: 0x06005A3D RID: 23101 RVA: 0x0013D921 File Offset: 0x0013BB21
		public virtual IDictionary Properties
		{
			[SecurityCritical]
			get
			{
				return this;
			}
		}

		// Token: 0x17000F06 RID: 3846
		public virtual object this[object key]
		{
			[SecuritySafeCritical]
			get
			{
				return null;
			}
			[SecuritySafeCritical]
			set
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000F07 RID: 3847
		// (get) Token: 0x06005A40 RID: 23104 RVA: 0x0013D92E File Offset: 0x0013BB2E
		public virtual ICollection Keys
		{
			[SecuritySafeCritical]
			get
			{
				return null;
			}
		}

		// Token: 0x17000F08 RID: 3848
		// (get) Token: 0x06005A41 RID: 23105 RVA: 0x0013D934 File Offset: 0x0013BB34
		public virtual ICollection Values
		{
			[SecuritySafeCritical]
			get
			{
				ICollection keys = this.Keys;
				if (keys == null)
				{
					return null;
				}
				ArrayList arrayList = new ArrayList();
				foreach (object key in keys)
				{
					arrayList.Add(this[key]);
				}
				return arrayList;
			}
		}

		// Token: 0x06005A42 RID: 23106 RVA: 0x0013D9A0 File Offset: 0x0013BBA0
		[SecuritySafeCritical]
		public virtual bool Contains(object key)
		{
			if (key == null)
			{
				return false;
			}
			ICollection keys = this.Keys;
			if (keys == null)
			{
				return false;
			}
			string text = key as string;
			foreach (object obj in keys)
			{
				if (text != null)
				{
					string text2 = obj as string;
					if (text2 != null)
					{
						if (string.Compare(text, text2, StringComparison.OrdinalIgnoreCase) == 0)
						{
							return true;
						}
						continue;
					}
				}
				if (key.Equals(obj))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000F09 RID: 3849
		// (get) Token: 0x06005A43 RID: 23107 RVA: 0x0013DA34 File Offset: 0x0013BC34
		public virtual bool IsReadOnly
		{
			[SecuritySafeCritical]
			get
			{
				return false;
			}
		}

		// Token: 0x17000F0A RID: 3850
		// (get) Token: 0x06005A44 RID: 23108 RVA: 0x0013DA37 File Offset: 0x0013BC37
		public virtual bool IsFixedSize
		{
			[SecuritySafeCritical]
			get
			{
				return true;
			}
		}

		// Token: 0x06005A45 RID: 23109 RVA: 0x0013DA3A File Offset: 0x0013BC3A
		[SecuritySafeCritical]
		public virtual void Add(object key, object value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A46 RID: 23110 RVA: 0x0013DA41 File Offset: 0x0013BC41
		[SecuritySafeCritical]
		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A47 RID: 23111 RVA: 0x0013DA48 File Offset: 0x0013BC48
		[SecuritySafeCritical]
		public virtual void Remove(object key)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06005A48 RID: 23112 RVA: 0x0013DA4F File Offset: 0x0013BC4F
		[SecuritySafeCritical]
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return new DictionaryEnumeratorByKeys(this);
		}

		// Token: 0x06005A49 RID: 23113 RVA: 0x0013DA57 File Offset: 0x0013BC57
		[SecuritySafeCritical]
		public virtual void CopyTo(Array array, int index)
		{
			throw new NotSupportedException();
		}

		// Token: 0x17000F0B RID: 3851
		// (get) Token: 0x06005A4A RID: 23114 RVA: 0x0013DA60 File Offset: 0x0013BC60
		public virtual int Count
		{
			[SecuritySafeCritical]
			get
			{
				ICollection keys = this.Keys;
				if (keys == null)
				{
					return 0;
				}
				return keys.Count;
			}
		}

		// Token: 0x17000F0C RID: 3852
		// (get) Token: 0x06005A4B RID: 23115 RVA: 0x0013DA7F File Offset: 0x0013BC7F
		public virtual object SyncRoot
		{
			[SecuritySafeCritical]
			get
			{
				return this;
			}
		}

		// Token: 0x17000F0D RID: 3853
		// (get) Token: 0x06005A4C RID: 23116 RVA: 0x0013DA82 File Offset: 0x0013BC82
		public virtual bool IsSynchronized
		{
			[SecuritySafeCritical]
			get
			{
				return false;
			}
		}

		// Token: 0x06005A4D RID: 23117 RVA: 0x0013DA85 File Offset: 0x0013BC85
		[SecuritySafeCritical]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new DictionaryEnumeratorByKeys(this);
		}
	}
}
