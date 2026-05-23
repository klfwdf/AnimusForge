using System;
using System.Collections;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x02000866 RID: 2150
	internal class MessageDictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		// Token: 0x06005B26 RID: 23334 RVA: 0x0013FC6F File Offset: 0x0013DE6F
		public MessageDictionaryEnumerator(MessageDictionary md, IDictionary hashtable)
		{
			this._md = md;
			if (hashtable != null)
			{
				this._enumHash = hashtable.GetEnumerator();
				return;
			}
			this._enumHash = null;
		}

		// Token: 0x17000F60 RID: 3936
		// (get) Token: 0x06005B27 RID: 23335 RVA: 0x0013FC9C File Offset: 0x0013DE9C
		public object Key
		{
			get
			{
				if (this.i < 0)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
				}
				if (this.i < this._md._keys.Length)
				{
					return this._md._keys[this.i];
				}
				return this._enumHash.Key;
			}
		}

		// Token: 0x17000F61 RID: 3937
		// (get) Token: 0x06005B28 RID: 23336 RVA: 0x0013FCF8 File Offset: 0x0013DEF8
		public object Value
		{
			get
			{
				if (this.i < 0)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
				}
				if (this.i < this._md._keys.Length)
				{
					return this._md.GetMessageValue(this.i);
				}
				return this._enumHash.Value;
			}
		}

		// Token: 0x06005B29 RID: 23337 RVA: 0x0013FD50 File Offset: 0x0013DF50
		public bool MoveNext()
		{
			if (this.i == -2)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
			}
			this.i++;
			if (this.i < this._md._keys.Length)
			{
				return true;
			}
			if (this._enumHash != null && this._enumHash.MoveNext())
			{
				return true;
			}
			this.i = -2;
			return false;
		}

		// Token: 0x17000F62 RID: 3938
		// (get) Token: 0x06005B2A RID: 23338 RVA: 0x0013FDBC File Offset: 0x0013DFBC
		public object Current
		{
			get
			{
				return this.Entry;
			}
		}

		// Token: 0x17000F63 RID: 3939
		// (get) Token: 0x06005B2B RID: 23339 RVA: 0x0013FDC9 File Offset: 0x0013DFC9
		public DictionaryEntry Entry
		{
			get
			{
				return new DictionaryEntry(this.Key, this.Value);
			}
		}

		// Token: 0x06005B2C RID: 23340 RVA: 0x0013FDDC File Offset: 0x0013DFDC
		public void Reset()
		{
			this.i = -1;
			if (this._enumHash != null)
			{
				this._enumHash.Reset();
			}
		}

		// Token: 0x04002947 RID: 10567
		private int i = -1;

		// Token: 0x04002948 RID: 10568
		private IDictionaryEnumerator _enumHash;

		// Token: 0x04002949 RID: 10569
		private MessageDictionary _md;
	}
}
