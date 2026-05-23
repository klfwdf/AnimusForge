using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A01 RID: 2561
	internal sealed class CLRIReferenceImpl<T> : CLRIPropertyValueImpl, IReference<T>, IPropertyValue, ICustomPropertyProvider
	{
		// Token: 0x06006530 RID: 25904 RVA: 0x00158CE0 File Offset: 0x00156EE0
		public CLRIReferenceImpl(PropertyType type, T obj)
			: base(type, obj)
		{
			this._value = obj;
		}

		// Token: 0x17001161 RID: 4449
		// (get) Token: 0x06006531 RID: 25905 RVA: 0x00158CF6 File Offset: 0x00156EF6
		public T Value
		{
			get
			{
				return this._value;
			}
		}

		// Token: 0x06006532 RID: 25906 RVA: 0x00158CFE File Offset: 0x00156EFE
		public override string ToString()
		{
			if (this._value != null)
			{
				return this._value.ToString();
			}
			return base.ToString();
		}

		// Token: 0x06006533 RID: 25907 RVA: 0x00158D25 File Offset: 0x00156F25
		ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
		{
			return ICustomPropertyProviderImpl.CreateProperty(this._value, name);
		}

		// Token: 0x06006534 RID: 25908 RVA: 0x00158D38 File Offset: 0x00156F38
		ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type indexParameterType)
		{
			return ICustomPropertyProviderImpl.CreateIndexedProperty(this._value, name, indexParameterType);
		}

		// Token: 0x06006535 RID: 25909 RVA: 0x00158D4C File Offset: 0x00156F4C
		string ICustomPropertyProvider.GetStringRepresentation()
		{
			return this._value.ToString();
		}

		// Token: 0x17001162 RID: 4450
		// (get) Token: 0x06006536 RID: 25910 RVA: 0x00158D5E File Offset: 0x00156F5E
		Type ICustomPropertyProvider.Type
		{
			get
			{
				return this._value.GetType();
			}
		}

		// Token: 0x06006537 RID: 25911 RVA: 0x00158D70 File Offset: 0x00156F70
		[FriendAccessAllowed]
		internal static object UnboxHelper(object wrapper)
		{
			IReference<T> reference = (IReference<T>)wrapper;
			return reference.Value;
		}

		// Token: 0x04002D35 RID: 11573
		private T _value;
	}
}
