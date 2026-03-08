using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A0F RID: 2575
	internal class ICustomPropertyProviderProxy<T1, T2> : IGetProxyTarget, ICustomPropertyProvider, ICustomQueryInterface, IEnumerable, IBindableVector, IBindableIterable, IBindableVectorView
	{
		// Token: 0x06006589 RID: 25993 RVA: 0x00159628 File Offset: 0x00157828
		internal ICustomPropertyProviderProxy(object target, InterfaceForwardingSupport flags)
		{
			this._target = target;
			this._flags = flags;
		}

		// Token: 0x0600658A RID: 25994 RVA: 0x00159640 File Offset: 0x00157840
		internal static object CreateInstance(object target)
		{
			InterfaceForwardingSupport interfaceForwardingSupport = InterfaceForwardingSupport.None;
			if (target is IList)
			{
				interfaceForwardingSupport |= InterfaceForwardingSupport.IBindableVector;
			}
			if (target is IList<!0>)
			{
				interfaceForwardingSupport |= InterfaceForwardingSupport.IVector;
			}
			if (target is IBindableVectorView)
			{
				interfaceForwardingSupport |= InterfaceForwardingSupport.IBindableVectorView;
			}
			if (target is IReadOnlyList<T2>)
			{
				interfaceForwardingSupport |= InterfaceForwardingSupport.IVectorView;
			}
			if (target is IEnumerable)
			{
				interfaceForwardingSupport |= InterfaceForwardingSupport.IBindableIterableOrIIterable;
			}
			return new ICustomPropertyProviderProxy<T1, T2>(target, interfaceForwardingSupport);
		}

		// Token: 0x0600658B RID: 25995 RVA: 0x00159693 File Offset: 0x00157893
		ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
		{
			return ICustomPropertyProviderImpl.CreateProperty(this._target, name);
		}

		// Token: 0x0600658C RID: 25996 RVA: 0x001596A1 File Offset: 0x001578A1
		ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type indexParameterType)
		{
			return ICustomPropertyProviderImpl.CreateIndexedProperty(this._target, name, indexParameterType);
		}

		// Token: 0x0600658D RID: 25997 RVA: 0x001596B0 File Offset: 0x001578B0
		string ICustomPropertyProvider.GetStringRepresentation()
		{
			return IStringableHelper.ToString(this._target);
		}

		// Token: 0x17001170 RID: 4464
		// (get) Token: 0x0600658E RID: 25998 RVA: 0x001596BD File Offset: 0x001578BD
		Type ICustomPropertyProvider.Type
		{
			get
			{
				return this._target.GetType();
			}
		}

		// Token: 0x0600658F RID: 25999 RVA: 0x001596CA File Offset: 0x001578CA
		public override string ToString()
		{
			return IStringableHelper.ToString(this._target);
		}

		// Token: 0x06006590 RID: 26000 RVA: 0x001596D7 File Offset: 0x001578D7
		object IGetProxyTarget.GetTarget()
		{
			return this._target;
		}

		// Token: 0x06006591 RID: 26001 RVA: 0x001596E0 File Offset: 0x001578E0
		[SecurityCritical]
		public CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv)
		{
			ppv = IntPtr.Zero;
			if (iid == typeof(IBindableIterable).GUID && (this._flags & InterfaceForwardingSupport.IBindableIterableOrIIterable) == InterfaceForwardingSupport.None)
			{
				return CustomQueryInterfaceResult.Failed;
			}
			if (iid == typeof(IBindableVector).GUID && (this._flags & (InterfaceForwardingSupport.IBindableVector | InterfaceForwardingSupport.IVector)) == InterfaceForwardingSupport.None)
			{
				return CustomQueryInterfaceResult.Failed;
			}
			if (iid == typeof(IBindableVectorView).GUID && (this._flags & (InterfaceForwardingSupport.IBindableVectorView | InterfaceForwardingSupport.IVectorView)) == InterfaceForwardingSupport.None)
			{
				return CustomQueryInterfaceResult.Failed;
			}
			return CustomQueryInterfaceResult.NotHandled;
		}

		// Token: 0x06006592 RID: 26002 RVA: 0x0015976F File Offset: 0x0015796F
		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable)this._target).GetEnumerator();
		}

		// Token: 0x06006593 RID: 26003 RVA: 0x00159784 File Offset: 0x00157984
		object IBindableVector.GetAt(uint index)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				return ibindableVectorNoThrow.GetAt(index);
			}
			return this.GetVectorOfT().GetAt(index);
		}

		// Token: 0x17001171 RID: 4465
		// (get) Token: 0x06006594 RID: 26004 RVA: 0x001597B4 File Offset: 0x001579B4
		uint IBindableVector.Size
		{
			get
			{
				IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
				if (ibindableVectorNoThrow != null)
				{
					return ibindableVectorNoThrow.Size;
				}
				return this.GetVectorOfT().Size;
			}
		}

		// Token: 0x06006595 RID: 26005 RVA: 0x001597E0 File Offset: 0x001579E0
		IBindableVectorView IBindableVector.GetView()
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				return ibindableVectorNoThrow.GetView();
			}
			return new ICustomPropertyProviderProxy<T1, T2>.IVectorViewToIBindableVectorViewAdapter<T1>(this.GetVectorOfT().GetView());
		}

		// Token: 0x06006596 RID: 26006 RVA: 0x00159810 File Offset: 0x00157A10
		bool IBindableVector.IndexOf(object value, out uint index)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				return ibindableVectorNoThrow.IndexOf(value, out index);
			}
			return this.GetVectorOfT().IndexOf(ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T1>(value), out index);
		}

		// Token: 0x06006597 RID: 26007 RVA: 0x00159844 File Offset: 0x00157A44
		void IBindableVector.SetAt(uint index, object value)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.SetAt(index, value);
				return;
			}
			this.GetVectorOfT().SetAt(index, ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T1>(value));
		}

		// Token: 0x06006598 RID: 26008 RVA: 0x00159878 File Offset: 0x00157A78
		void IBindableVector.InsertAt(uint index, object value)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.InsertAt(index, value);
				return;
			}
			this.GetVectorOfT().InsertAt(index, ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T1>(value));
		}

		// Token: 0x06006599 RID: 26009 RVA: 0x001598AC File Offset: 0x00157AAC
		void IBindableVector.RemoveAt(uint index)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.RemoveAt(index);
				return;
			}
			this.GetVectorOfT().RemoveAt(index);
		}

		// Token: 0x0600659A RID: 26010 RVA: 0x001598D8 File Offset: 0x00157AD8
		void IBindableVector.Append(object value)
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.Append(value);
				return;
			}
			this.GetVectorOfT().Append(ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T1>(value));
		}

		// Token: 0x0600659B RID: 26011 RVA: 0x00159908 File Offset: 0x00157B08
		void IBindableVector.RemoveAtEnd()
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.RemoveAtEnd();
				return;
			}
			this.GetVectorOfT().RemoveAtEnd();
		}

		// Token: 0x0600659C RID: 26012 RVA: 0x00159934 File Offset: 0x00157B34
		void IBindableVector.Clear()
		{
			IBindableVector ibindableVectorNoThrow = this.GetIBindableVectorNoThrow();
			if (ibindableVectorNoThrow != null)
			{
				ibindableVectorNoThrow.Clear();
				return;
			}
			this.GetVectorOfT().Clear();
		}

		// Token: 0x0600659D RID: 26013 RVA: 0x0015995D File Offset: 0x00157B5D
		[SecuritySafeCritical]
		private IBindableVector GetIBindableVectorNoThrow()
		{
			if ((this._flags & InterfaceForwardingSupport.IBindableVector) != InterfaceForwardingSupport.None)
			{
				return JitHelpers.UnsafeCast<IBindableVector>(this._target);
			}
			return null;
		}

		// Token: 0x0600659E RID: 26014 RVA: 0x00159976 File Offset: 0x00157B76
		[SecuritySafeCritical]
		private IVector_Raw<T1> GetVectorOfT()
		{
			if ((this._flags & InterfaceForwardingSupport.IVector) != InterfaceForwardingSupport.None)
			{
				return JitHelpers.UnsafeCast<IVector_Raw<T1>>(this._target);
			}
			throw new InvalidOperationException();
		}

		// Token: 0x0600659F RID: 26015 RVA: 0x00159994 File Offset: 0x00157B94
		object IBindableVectorView.GetAt(uint index)
		{
			IBindableVectorView ibindableVectorViewNoThrow = this.GetIBindableVectorViewNoThrow();
			if (ibindableVectorViewNoThrow != null)
			{
				return ibindableVectorViewNoThrow.GetAt(index);
			}
			return this.GetVectorViewOfT().GetAt(index);
		}

		// Token: 0x17001172 RID: 4466
		// (get) Token: 0x060065A0 RID: 26016 RVA: 0x001599C4 File Offset: 0x00157BC4
		uint IBindableVectorView.Size
		{
			get
			{
				IBindableVectorView ibindableVectorViewNoThrow = this.GetIBindableVectorViewNoThrow();
				if (ibindableVectorViewNoThrow != null)
				{
					return ibindableVectorViewNoThrow.Size;
				}
				return this.GetVectorViewOfT().Size;
			}
		}

		// Token: 0x060065A1 RID: 26017 RVA: 0x001599F0 File Offset: 0x00157BF0
		bool IBindableVectorView.IndexOf(object value, out uint index)
		{
			IBindableVectorView ibindableVectorViewNoThrow = this.GetIBindableVectorViewNoThrow();
			if (ibindableVectorViewNoThrow != null)
			{
				return ibindableVectorViewNoThrow.IndexOf(value, out index);
			}
			return this.GetVectorViewOfT().IndexOf(ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T2>(value), out index);
		}

		// Token: 0x060065A2 RID: 26018 RVA: 0x00159A24 File Offset: 0x00157C24
		IBindableIterator IBindableIterable.First()
		{
			IBindableVectorView ibindableVectorViewNoThrow = this.GetIBindableVectorViewNoThrow();
			if (ibindableVectorViewNoThrow != null)
			{
				return ibindableVectorViewNoThrow.First();
			}
			return new ICustomPropertyProviderProxy<T1, T2>.IteratorOfTToIteratorAdapter<T2>(this.GetVectorViewOfT().First());
		}

		// Token: 0x060065A3 RID: 26019 RVA: 0x00159A52 File Offset: 0x00157C52
		[SecuritySafeCritical]
		private IBindableVectorView GetIBindableVectorViewNoThrow()
		{
			if ((this._flags & InterfaceForwardingSupport.IBindableVectorView) != InterfaceForwardingSupport.None)
			{
				return JitHelpers.UnsafeCast<IBindableVectorView>(this._target);
			}
			return null;
		}

		// Token: 0x060065A4 RID: 26020 RVA: 0x00159A6B File Offset: 0x00157C6B
		[SecuritySafeCritical]
		private IVectorView<T2> GetVectorViewOfT()
		{
			if ((this._flags & InterfaceForwardingSupport.IVectorView) != InterfaceForwardingSupport.None)
			{
				return JitHelpers.UnsafeCast<IVectorView<T2>>(this._target);
			}
			throw new InvalidOperationException();
		}

		// Token: 0x060065A5 RID: 26021 RVA: 0x00159A88 File Offset: 0x00157C88
		private static T ConvertTo<T>(object value)
		{
			ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);
			return (T)((object)value);
		}

		// Token: 0x04002D42 RID: 11586
		private object _target;

		// Token: 0x04002D43 RID: 11587
		private InterfaceForwardingSupport _flags;

		// Token: 0x02000CA8 RID: 3240
		private sealed class IVectorViewToIBindableVectorViewAdapter<T> : IBindableVectorView, IBindableIterable
		{
			// Token: 0x0600714E RID: 29006 RVA: 0x00185FDF File Offset: 0x001841DF
			public IVectorViewToIBindableVectorViewAdapter(IVectorView<T> vectorView)
			{
				this._vectorView = vectorView;
			}

			// Token: 0x0600714F RID: 29007 RVA: 0x00185FEE File Offset: 0x001841EE
			object IBindableVectorView.GetAt(uint index)
			{
				return this._vectorView.GetAt(index);
			}

			// Token: 0x1700136C RID: 4972
			// (get) Token: 0x06007150 RID: 29008 RVA: 0x00186001 File Offset: 0x00184201
			uint IBindableVectorView.Size
			{
				get
				{
					return this._vectorView.Size;
				}
			}

			// Token: 0x06007151 RID: 29009 RVA: 0x0018600E File Offset: 0x0018420E
			bool IBindableVectorView.IndexOf(object value, out uint index)
			{
				return this._vectorView.IndexOf(ICustomPropertyProviderProxy<T1, T2>.ConvertTo<T>(value), out index);
			}

			// Token: 0x06007152 RID: 29010 RVA: 0x00186022 File Offset: 0x00184222
			IBindableIterator IBindableIterable.First()
			{
				return new ICustomPropertyProviderProxy<T1, T2>.IteratorOfTToIteratorAdapter<T>(this._vectorView.First());
			}

			// Token: 0x0400388B RID: 14475
			private IVectorView<T> _vectorView;
		}

		// Token: 0x02000CA9 RID: 3241
		private sealed class IteratorOfTToIteratorAdapter<T> : IBindableIterator
		{
			// Token: 0x06007153 RID: 29011 RVA: 0x00186034 File Offset: 0x00184234
			public IteratorOfTToIteratorAdapter(IIterator<T> iterator)
			{
				this._iterator = iterator;
			}

			// Token: 0x1700136D RID: 4973
			// (get) Token: 0x06007154 RID: 29012 RVA: 0x00186043 File Offset: 0x00184243
			public bool HasCurrent
			{
				get
				{
					return this._iterator.HasCurrent;
				}
			}

			// Token: 0x1700136E RID: 4974
			// (get) Token: 0x06007155 RID: 29013 RVA: 0x00186050 File Offset: 0x00184250
			public object Current
			{
				get
				{
					return this._iterator.Current;
				}
			}

			// Token: 0x06007156 RID: 29014 RVA: 0x00186062 File Offset: 0x00184262
			public bool MoveNext()
			{
				return this._iterator.MoveNext();
			}

			// Token: 0x0400388C RID: 14476
			private IIterator<T> _iterator;
		}
	}
}
