using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections
{
	// Token: 0x02000492 RID: 1170
	[DebuggerTypeProxy(typeof(Stack.StackDebugView))]
	[DebuggerDisplay("Count = {Count}")]
	[ComVisible(true)]
	[Serializable]
	public class Stack : ICollection, IEnumerable, ICloneable
	{
		// Token: 0x06003822 RID: 14370 RVA: 0x000D7911 File Offset: 0x000D5B11
		public Stack()
		{
			this._array = new object[10];
			this._size = 0;
			this._version = 0;
		}

		// Token: 0x06003823 RID: 14371 RVA: 0x000D7934 File Offset: 0x000D5B34
		public Stack(int initialCapacity)
		{
			if (initialCapacity < 0)
			{
				throw new ArgumentOutOfRangeException("initialCapacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (initialCapacity < 10)
			{
				initialCapacity = 10;
			}
			this._array = new object[initialCapacity];
			this._size = 0;
			this._version = 0;
		}

		// Token: 0x06003824 RID: 14372 RVA: 0x000D7984 File Offset: 0x000D5B84
		public Stack(ICollection col)
			: this((col == null) ? 32 : col.Count)
		{
			if (col == null)
			{
				throw new ArgumentNullException("col");
			}
			foreach (object obj in col)
			{
				this.Push(obj);
			}
		}

		// Token: 0x1700084A RID: 2122
		// (get) Token: 0x06003825 RID: 14373 RVA: 0x000D79CF File Offset: 0x000D5BCF
		public virtual int Count
		{
			get
			{
				return this._size;
			}
		}

		// Token: 0x1700084B RID: 2123
		// (get) Token: 0x06003826 RID: 14374 RVA: 0x000D79D7 File Offset: 0x000D5BD7
		public virtual bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700084C RID: 2124
		// (get) Token: 0x06003827 RID: 14375 RVA: 0x000D79DA File Offset: 0x000D5BDA
		public virtual object SyncRoot
		{
			get
			{
				if (this._syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref this._syncRoot, new object(), null);
				}
				return this._syncRoot;
			}
		}

		// Token: 0x06003828 RID: 14376 RVA: 0x000D79FC File Offset: 0x000D5BFC
		public virtual void Clear()
		{
			Array.Clear(this._array, 0, this._size);
			this._size = 0;
			this._version++;
		}

		// Token: 0x06003829 RID: 14377 RVA: 0x000D7A28 File Offset: 0x000D5C28
		public virtual object Clone()
		{
			Stack stack = new Stack(this._size);
			stack._size = this._size;
			Array.Copy(this._array, 0, stack._array, 0, this._size);
			stack._version = this._version;
			return stack;
		}

		// Token: 0x0600382A RID: 14378 RVA: 0x000D7A74 File Offset: 0x000D5C74
		public virtual bool Contains(object obj)
		{
			int size = this._size;
			while (size-- > 0)
			{
				if (obj == null)
				{
					if (this._array[size] == null)
					{
						return true;
					}
				}
				else if (this._array[size] != null && this._array[size].Equals(obj))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600382B RID: 14379 RVA: 0x000D7AC0 File Offset: 0x000D5CC0
		public virtual void CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - index < this._size)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			int i = 0;
			if (array is object[])
			{
				object[] array2 = (object[])array;
				while (i < this._size)
				{
					array2[i + index] = this._array[this._size - i - 1];
					i++;
				}
				return;
			}
			while (i < this._size)
			{
				array.SetValue(this._array[this._size - i - 1], i + index);
				i++;
			}
		}

		// Token: 0x0600382C RID: 14380 RVA: 0x000D7B8B File Offset: 0x000D5D8B
		public virtual IEnumerator GetEnumerator()
		{
			return new Stack.StackEnumerator(this);
		}

		// Token: 0x0600382D RID: 14381 RVA: 0x000D7B93 File Offset: 0x000D5D93
		public virtual object Peek()
		{
			if (this._size == 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EmptyStack"));
			}
			return this._array[this._size - 1];
		}

		// Token: 0x0600382E RID: 14382 RVA: 0x000D7BBC File Offset: 0x000D5DBC
		public virtual object Pop()
		{
			if (this._size == 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EmptyStack"));
			}
			this._version++;
			object[] array = this._array;
			int num = this._size - 1;
			this._size = num;
			object result = array[num];
			this._array[this._size] = null;
			return result;
		}

		// Token: 0x0600382F RID: 14383 RVA: 0x000D7C18 File Offset: 0x000D5E18
		public virtual void Push(object obj)
		{
			if (this._size == this._array.Length)
			{
				object[] array = new object[2 * this._array.Length];
				Array.Copy(this._array, 0, array, 0, this._size);
				this._array = array;
			}
			object[] array2 = this._array;
			int size = this._size;
			this._size = size + 1;
			array2[size] = obj;
			this._version++;
		}

		// Token: 0x06003830 RID: 14384 RVA: 0x000D7C87 File Offset: 0x000D5E87
		[HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
		public static Stack Synchronized(Stack stack)
		{
			if (stack == null)
			{
				throw new ArgumentNullException("stack");
			}
			return new Stack.SyncStack(stack);
		}

		// Token: 0x06003831 RID: 14385 RVA: 0x000D7CA0 File Offset: 0x000D5EA0
		public virtual object[] ToArray()
		{
			object[] array = new object[this._size];
			for (int i = 0; i < this._size; i++)
			{
				array[i] = this._array[this._size - i - 1];
			}
			return array;
		}

		// Token: 0x040018D3 RID: 6355
		private object[] _array;

		// Token: 0x040018D4 RID: 6356
		private int _size;

		// Token: 0x040018D5 RID: 6357
		private int _version;

		// Token: 0x040018D6 RID: 6358
		[NonSerialized]
		private object _syncRoot;

		// Token: 0x040018D7 RID: 6359
		private const int _defaultCapacity = 10;

		// Token: 0x02000BB0 RID: 2992
		[Serializable]
		private class SyncStack : Stack
		{
			// Token: 0x06006DCC RID: 28108 RVA: 0x0017B5A8 File Offset: 0x001797A8
			internal SyncStack(Stack stack)
			{
				this._s = stack;
				this._root = stack.SyncRoot;
			}

			// Token: 0x170012A0 RID: 4768
			// (get) Token: 0x06006DCD RID: 28109 RVA: 0x0017B5C3 File Offset: 0x001797C3
			public override bool IsSynchronized
			{
				get
				{
					return true;
				}
			}

			// Token: 0x170012A1 RID: 4769
			// (get) Token: 0x06006DCE RID: 28110 RVA: 0x0017B5C6 File Offset: 0x001797C6
			public override object SyncRoot
			{
				get
				{
					return this._root;
				}
			}

			// Token: 0x170012A2 RID: 4770
			// (get) Token: 0x06006DCF RID: 28111 RVA: 0x0017B5D0 File Offset: 0x001797D0
			public override int Count
			{
				get
				{
					object root = this._root;
					int count;
					lock (root)
					{
						count = this._s.Count;
					}
					return count;
				}
			}

			// Token: 0x06006DD0 RID: 28112 RVA: 0x0017B618 File Offset: 0x00179818
			public override bool Contains(object obj)
			{
				object root = this._root;
				bool result;
				lock (root)
				{
					result = this._s.Contains(obj);
				}
				return result;
			}

			// Token: 0x06006DD1 RID: 28113 RVA: 0x0017B660 File Offset: 0x00179860
			public override object Clone()
			{
				object root = this._root;
				object result;
				lock (root)
				{
					result = new Stack.SyncStack((Stack)this._s.Clone());
				}
				return result;
			}

			// Token: 0x06006DD2 RID: 28114 RVA: 0x0017B6B4 File Offset: 0x001798B4
			public override void Clear()
			{
				object root = this._root;
				lock (root)
				{
					this._s.Clear();
				}
			}

			// Token: 0x06006DD3 RID: 28115 RVA: 0x0017B6FC File Offset: 0x001798FC
			public override void CopyTo(Array array, int arrayIndex)
			{
				object root = this._root;
				lock (root)
				{
					this._s.CopyTo(array, arrayIndex);
				}
			}

			// Token: 0x06006DD4 RID: 28116 RVA: 0x0017B744 File Offset: 0x00179944
			public override void Push(object value)
			{
				object root = this._root;
				lock (root)
				{
					this._s.Push(value);
				}
			}

			// Token: 0x06006DD5 RID: 28117 RVA: 0x0017B78C File Offset: 0x0017998C
			public override object Pop()
			{
				object root = this._root;
				object result;
				lock (root)
				{
					result = this._s.Pop();
				}
				return result;
			}

			// Token: 0x06006DD6 RID: 28118 RVA: 0x0017B7D4 File Offset: 0x001799D4
			public override IEnumerator GetEnumerator()
			{
				object root = this._root;
				IEnumerator enumerator;
				lock (root)
				{
					enumerator = this._s.GetEnumerator();
				}
				return enumerator;
			}

			// Token: 0x06006DD7 RID: 28119 RVA: 0x0017B81C File Offset: 0x00179A1C
			public override object Peek()
			{
				object root = this._root;
				object result;
				lock (root)
				{
					result = this._s.Peek();
				}
				return result;
			}

			// Token: 0x06006DD8 RID: 28120 RVA: 0x0017B864 File Offset: 0x00179A64
			public override object[] ToArray()
			{
				object root = this._root;
				object[] result;
				lock (root)
				{
					result = this._s.ToArray();
				}
				return result;
			}

			// Token: 0x04003561 RID: 13665
			private Stack _s;

			// Token: 0x04003562 RID: 13666
			private object _root;
		}

		// Token: 0x02000BB1 RID: 2993
		[Serializable]
		private class StackEnumerator : IEnumerator, ICloneable
		{
			// Token: 0x06006DD9 RID: 28121 RVA: 0x0017B8AC File Offset: 0x00179AAC
			internal StackEnumerator(Stack stack)
			{
				this._stack = stack;
				this._version = this._stack._version;
				this._index = -2;
				this.currentElement = null;
			}

			// Token: 0x06006DDA RID: 28122 RVA: 0x0017B8DB File Offset: 0x00179ADB
			public object Clone()
			{
				return base.MemberwiseClone();
			}

			// Token: 0x06006DDB RID: 28123 RVA: 0x0017B8E4 File Offset: 0x00179AE4
			public virtual bool MoveNext()
			{
				if (this._version != this._stack._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				bool flag;
				if (this._index == -2)
				{
					this._index = this._stack._size - 1;
					flag = this._index >= 0;
					if (flag)
					{
						this.currentElement = this._stack._array[this._index];
					}
					return flag;
				}
				if (this._index == -1)
				{
					return false;
				}
				int num = this._index - 1;
				this._index = num;
				flag = num >= 0;
				if (flag)
				{
					this.currentElement = this._stack._array[this._index];
				}
				else
				{
					this.currentElement = null;
				}
				return flag;
			}

			// Token: 0x170012A3 RID: 4771
			// (get) Token: 0x06006DDC RID: 28124 RVA: 0x0017B9A3 File Offset: 0x00179BA3
			public virtual object Current
			{
				get
				{
					if (this._index == -2)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					if (this._index == -1)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
					}
					return this.currentElement;
				}
			}

			// Token: 0x06006DDD RID: 28125 RVA: 0x0017B9DE File Offset: 0x00179BDE
			public virtual void Reset()
			{
				if (this._version != this._stack._version)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
				}
				this._index = -2;
				this.currentElement = null;
			}

			// Token: 0x04003563 RID: 13667
			private Stack _stack;

			// Token: 0x04003564 RID: 13668
			private int _index;

			// Token: 0x04003565 RID: 13669
			private int _version;

			// Token: 0x04003566 RID: 13670
			private object currentElement;
		}

		// Token: 0x02000BB2 RID: 2994
		internal class StackDebugView
		{
			// Token: 0x06006DDE RID: 28126 RVA: 0x0017BA12 File Offset: 0x00179C12
			public StackDebugView(Stack stack)
			{
				if (stack == null)
				{
					throw new ArgumentNullException("stack");
				}
				this.stack = stack;
			}

			// Token: 0x170012A4 RID: 4772
			// (get) Token: 0x06006DDF RID: 28127 RVA: 0x0017BA2F File Offset: 0x00179C2F
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public object[] Items
			{
				get
				{
					return this.stack.ToArray();
				}
			}

			// Token: 0x04003567 RID: 13671
			private Stack stack;
		}
	}
}
