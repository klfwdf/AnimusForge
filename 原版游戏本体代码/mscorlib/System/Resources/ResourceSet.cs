using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Resources
{
	// Token: 0x0200039A RID: 922
	[ComVisible(true)]
	[Serializable]
	public class ResourceSet : IDisposable, IEnumerable
	{
		// Token: 0x06002D6A RID: 11626 RVA: 0x000AD63D File Offset: 0x000AB83D
		protected ResourceSet()
		{
			this.CommonInit();
		}

		// Token: 0x06002D6B RID: 11627 RVA: 0x000AD64B File Offset: 0x000AB84B
		internal ResourceSet(bool junk)
		{
		}

		// Token: 0x06002D6C RID: 11628 RVA: 0x000AD653 File Offset: 0x000AB853
		public ResourceSet(string fileName)
		{
			this.Reader = new ResourceReader(fileName);
			this.CommonInit();
			this.ReadResources();
		}

		// Token: 0x06002D6D RID: 11629 RVA: 0x000AD673 File Offset: 0x000AB873
		[SecurityCritical]
		public ResourceSet(Stream stream)
		{
			this.Reader = new ResourceReader(stream);
			this.CommonInit();
			this.ReadResources();
		}

		// Token: 0x06002D6E RID: 11630 RVA: 0x000AD693 File Offset: 0x000AB893
		public ResourceSet(IResourceReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.Reader = reader;
			this.CommonInit();
			this.ReadResources();
		}

		// Token: 0x06002D6F RID: 11631 RVA: 0x000AD6BC File Offset: 0x000AB8BC
		private void CommonInit()
		{
			this.Table = new Hashtable();
		}

		// Token: 0x06002D70 RID: 11632 RVA: 0x000AD6C9 File Offset: 0x000AB8C9
		public virtual void Close()
		{
			this.Dispose(true);
		}

		// Token: 0x06002D71 RID: 11633 RVA: 0x000AD6D4 File Offset: 0x000AB8D4
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				IResourceReader reader = this.Reader;
				this.Reader = null;
				if (reader != null)
				{
					reader.Close();
				}
			}
			this.Reader = null;
			this._caseInsensitiveTable = null;
			this.Table = null;
		}

		// Token: 0x06002D72 RID: 11634 RVA: 0x000AD710 File Offset: 0x000AB910
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06002D73 RID: 11635 RVA: 0x000AD719 File Offset: 0x000AB919
		public virtual Type GetDefaultReader()
		{
			return typeof(ResourceReader);
		}

		// Token: 0x06002D74 RID: 11636 RVA: 0x000AD725 File Offset: 0x000AB925
		public virtual Type GetDefaultWriter()
		{
			return typeof(ResourceWriter);
		}

		// Token: 0x06002D75 RID: 11637 RVA: 0x000AD731 File Offset: 0x000AB931
		[ComVisible(false)]
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return this.GetEnumeratorHelper();
		}

		// Token: 0x06002D76 RID: 11638 RVA: 0x000AD739 File Offset: 0x000AB939
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumeratorHelper();
		}

		// Token: 0x06002D77 RID: 11639 RVA: 0x000AD744 File Offset: 0x000AB944
		private IDictionaryEnumerator GetEnumeratorHelper()
		{
			Hashtable table = this.Table;
			if (table == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
			}
			return table.GetEnumerator();
		}

		// Token: 0x06002D78 RID: 11640 RVA: 0x000AD774 File Offset: 0x000AB974
		public virtual string GetString(string name)
		{
			object objectInternal = this.GetObjectInternal(name);
			string result;
			try
			{
				result = (string)objectInternal;
			}
			catch (InvalidCastException)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", new object[] { name }));
			}
			return result;
		}

		// Token: 0x06002D79 RID: 11641 RVA: 0x000AD7C0 File Offset: 0x000AB9C0
		public virtual string GetString(string name, bool ignoreCase)
		{
			object obj = this.GetObjectInternal(name);
			string text;
			try
			{
				text = (string)obj;
			}
			catch (InvalidCastException)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", new object[] { name }));
			}
			if (text != null || !ignoreCase)
			{
				return text;
			}
			obj = this.GetCaseInsensitiveObjectInternal(name);
			string result;
			try
			{
				result = (string)obj;
			}
			catch (InvalidCastException)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", new object[] { name }));
			}
			return result;
		}

		// Token: 0x06002D7A RID: 11642 RVA: 0x000AD84C File Offset: 0x000ABA4C
		public virtual object GetObject(string name)
		{
			return this.GetObjectInternal(name);
		}

		// Token: 0x06002D7B RID: 11643 RVA: 0x000AD858 File Offset: 0x000ABA58
		public virtual object GetObject(string name, bool ignoreCase)
		{
			object objectInternal = this.GetObjectInternal(name);
			if (objectInternal != null || !ignoreCase)
			{
				return objectInternal;
			}
			return this.GetCaseInsensitiveObjectInternal(name);
		}

		// Token: 0x06002D7C RID: 11644 RVA: 0x000AD87C File Offset: 0x000ABA7C
		protected virtual void ReadResources()
		{
			IDictionaryEnumerator enumerator = this.Reader.GetEnumerator();
			while (enumerator.MoveNext())
			{
				object value = enumerator.Value;
				this.Table.Add(enumerator.Key, value);
			}
		}

		// Token: 0x06002D7D RID: 11645 RVA: 0x000AD8B8 File Offset: 0x000ABAB8
		private object GetObjectInternal(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			Hashtable table = this.Table;
			if (table == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
			}
			return table[name];
		}

		// Token: 0x06002D7E RID: 11646 RVA: 0x000AD8F8 File Offset: 0x000ABAF8
		private object GetCaseInsensitiveObjectInternal(string name)
		{
			Hashtable table = this.Table;
			if (table == null)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
			}
			Hashtable hashtable = this._caseInsensitiveTable;
			if (hashtable == null)
			{
				hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase);
				IDictionaryEnumerator enumerator = table.GetEnumerator();
				while (enumerator.MoveNext())
				{
					hashtable.Add(enumerator.Key, enumerator.Value);
				}
				this._caseInsensitiveTable = hashtable;
			}
			return hashtable[name];
		}

		// Token: 0x0400126C RID: 4716
		[NonSerialized]
		protected IResourceReader Reader;

		// Token: 0x0400126D RID: 4717
		protected Hashtable Table;

		// Token: 0x0400126E RID: 4718
		private Hashtable _caseInsensitiveTable;
	}
}
