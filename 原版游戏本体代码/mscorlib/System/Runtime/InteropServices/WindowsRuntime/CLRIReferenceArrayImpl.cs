using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A02 RID: 2562
	internal sealed class CLRIReferenceArrayImpl<T> : CLRIPropertyValueImpl, IReferenceArray<T>, IPropertyValue, ICustomPropertyProvider, IList, ICollection, IEnumerable
	{
		// Token: 0x06006538 RID: 25912 RVA: 0x00158D8F File Offset: 0x00156F8F
		public CLRIReferenceArrayImpl(PropertyType type, T[] obj)
			: base(type, obj)
		{
			this._value = obj;
			this._list = this._value;
		}

		// Token: 0x17001163 RID: 4451
		// (get) Token: 0x06006539 RID: 25913 RVA: 0x00158DAC File Offset: 0x00156FAC
		public T[] Value
		{
			get
			{
				return this._value;
			}
		}

		// Token: 0x0600653A RID: 25914 RVA: 0x00158DB4 File Offset: 0x00156FB4
		public override string ToString()
		{
			if (this._value != null)
			{
				return this._value.ToString();
			}
			return base.ToString();
		}

		// Token: 0x0600653B RID: 25915 RVA: 0x00158DD0 File Offset: 0x00156FD0
		ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
		{
			return ICustomPropertyProviderImpl.CreateProperty(this._value, name);
		}

		// Token: 0x0600653C RID: 25916 RVA: 0x00158DDE File Offset: 0x00156FDE
		ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type indexParameterType)
		{
			return ICustomPropertyProviderImpl.CreateIndexedProperty(this._value, name, indexParameterType);
		}

		// Token: 0x0600653D RID: 25917 RVA: 0x00158DED File Offset: 0x00156FED
		string ICustomPropertyProvider.GetStringRepresentation()
		{
			return this._value.ToString();
		}

		// Token: 0x17001164 RID: 4452
		// (get) Token: 0x0600653E RID: 25918 RVA: 0x00158DFA File Offset: 0x00156FFA
		Type ICustomPropertyProvider.Type
		{
			get
			{
				return this._value.GetType();
			}
		}

		// Token: 0x0600653F RID: 25919 RVA: 0x00158E07 File Offset: 0x00157007
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._value.GetEnumerator();
		}

		// Token: 0x17001165 RID: 4453
		object IList.this[int index]
		{
			get
			{
				return this._list[index];
			}
			set
			{
				this._list[index] = value;
			}
		}

		// Token: 0x06006542 RID: 25922 RVA: 0x00158E31 File Offset: 0x00157031
		int IList.Add(object value)
		{
			return this._list.Add(value);
		}

		// Token: 0x06006543 RID: 25923 RVA: 0x00158E3F File Offset: 0x0015703F
		bool IList.Contains(object value)
		{
			return this._list.Contains(value);
		}

		// Token: 0x06006544 RID: 25924 RVA: 0x00158E4D File Offset: 0x0015704D
		void IList.Clear()
		{
			this._list.Clear();
		}

		// Token: 0x17001166 RID: 4454
		// (get) Token: 0x06006545 RID: 25925 RVA: 0x00158E5A File Offset: 0x0015705A
		bool IList.IsReadOnly
		{
			get
			{
				return this._list.IsReadOnly;
			}
		}

		// Token: 0x17001167 RID: 4455
		// (get) Token: 0x06006546 RID: 25926 RVA: 0x00158E67 File Offset: 0x00157067
		bool IList.IsFixedSize
		{
			get
			{
				return this._list.IsFixedSize;
			}
		}

		// Token: 0x06006547 RID: 25927 RVA: 0x00158E74 File Offset: 0x00157074
		int IList.IndexOf(object value)
		{
			return this._list.IndexOf(value);
		}

		// Token: 0x06006548 RID: 25928 RVA: 0x00158E82 File Offset: 0x00157082
		void IList.Insert(int index, object value)
		{
			this._list.Insert(index, value);
		}

		// Token: 0x06006549 RID: 25929 RVA: 0x00158E91 File Offset: 0x00157091
		void IList.Remove(object value)
		{
			this._list.Remove(value);
		}

		// Token: 0x0600654A RID: 25930 RVA: 0x00158E9F File Offset: 0x0015709F
		void IList.RemoveAt(int index)
		{
			this._list.RemoveAt(index);
		}

		// Token: 0x0600654B RID: 25931 RVA: 0x00158EAD File Offset: 0x001570AD
		void ICollection.CopyTo(Array array, int index)
		{
			this._list.CopyTo(array, index);
		}

		// Token: 0x17001168 RID: 4456
		// (get) Token: 0x0600654C RID: 25932 RVA: 0x00158EBC File Offset: 0x001570BC
		int ICollection.Count
		{
			get
			{
				return this._list.Count;
			}
		}

		// Token: 0x17001169 RID: 4457
		// (get) Token: 0x0600654D RID: 25933 RVA: 0x00158EC9 File Offset: 0x001570C9
		object ICollection.SyncRoot
		{
			get
			{
				return this._list.SyncRoot;
			}
		}

		// Token: 0x1700116A RID: 4458
		// (get) Token: 0x0600654E RID: 25934 RVA: 0x00158ED6 File Offset: 0x001570D6
		bool ICollection.IsSynchronized
		{
			get
			{
				return this._list.IsSynchronized;
			}
		}

		// Token: 0x0600654F RID: 25935 RVA: 0x00158EE4 File Offset: 0x001570E4
		[FriendAccessAllowed]
		internal static object UnboxHelper(object wrapper)
		{
			IReferenceArray<T> referenceArray = (IReferenceArray<T>)wrapper;
			return referenceArray.Value;
		}

		// Token: 0x04002D36 RID: 11574
		private T[] _value;

		// Token: 0x04002D37 RID: 11575
		private IList _list;
	}
}
