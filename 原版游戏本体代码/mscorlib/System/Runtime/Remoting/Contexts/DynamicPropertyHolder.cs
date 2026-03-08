using System;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using System.Security;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000810 RID: 2064
	internal class DynamicPropertyHolder
	{
		// Token: 0x060058D1 RID: 22737 RVA: 0x00138FD4 File Offset: 0x001371D4
		[SecurityCritical]
		internal virtual bool AddDynamicProperty(IDynamicProperty prop)
		{
			bool result;
			lock (this)
			{
				DynamicPropertyHolder.CheckPropertyNameClash(prop.Name, this._props, this._numProps);
				bool flag2 = false;
				if (this._props == null || this._numProps == this._props.Length)
				{
					this._props = DynamicPropertyHolder.GrowPropertiesArray(this._props);
					flag2 = true;
				}
				IDynamicProperty[] props = this._props;
				int numProps = this._numProps;
				this._numProps = numProps + 1;
				props[numProps] = prop;
				if (flag2)
				{
					this._sinks = DynamicPropertyHolder.GrowDynamicSinksArray(this._sinks);
				}
				if (this._sinks == null)
				{
					this._sinks = new IDynamicMessageSink[this._props.Length];
					for (int i = 0; i < this._numProps; i++)
					{
						this._sinks[i] = ((IContributeDynamicSink)this._props[i]).GetDynamicSink();
					}
				}
				else
				{
					this._sinks[this._numProps - 1] = ((IContributeDynamicSink)prop).GetDynamicSink();
				}
				result = true;
			}
			return result;
		}

		// Token: 0x060058D2 RID: 22738 RVA: 0x001390E8 File Offset: 0x001372E8
		[SecurityCritical]
		internal virtual bool RemoveDynamicProperty(string name)
		{
			lock (this)
			{
				for (int i = 0; i < this._numProps; i++)
				{
					if (this._props[i].Name.Equals(name))
					{
						this._props[i] = this._props[this._numProps - 1];
						this._numProps--;
						this._sinks = null;
						return true;
					}
				}
				throw new RemotingException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Contexts_NoProperty"), name));
			}
			bool result;
			return result;
		}

		// Token: 0x17000EBA RID: 3770
		// (get) Token: 0x060058D3 RID: 22739 RVA: 0x00139190 File Offset: 0x00137390
		internal virtual IDynamicProperty[] DynamicProperties
		{
			get
			{
				if (this._props == null)
				{
					return null;
				}
				IDynamicProperty[] result;
				lock (this)
				{
					IDynamicProperty[] array = new IDynamicProperty[this._numProps];
					Array.Copy(this._props, array, this._numProps);
					result = array;
				}
				return result;
			}
		}

		// Token: 0x17000EBB RID: 3771
		// (get) Token: 0x060058D4 RID: 22740 RVA: 0x001391F0 File Offset: 0x001373F0
		internal virtual ArrayWithSize DynamicSinks
		{
			[SecurityCritical]
			get
			{
				if (this._numProps == 0)
				{
					return null;
				}
				lock (this)
				{
					if (this._sinks == null)
					{
						this._sinks = new IDynamicMessageSink[this._numProps + 8];
						for (int i = 0; i < this._numProps; i++)
						{
							this._sinks[i] = ((IContributeDynamicSink)this._props[i]).GetDynamicSink();
						}
					}
				}
				return new ArrayWithSize(this._sinks, this._numProps);
			}
		}

		// Token: 0x060058D5 RID: 22741 RVA: 0x00139288 File Offset: 0x00137488
		private static IDynamicMessageSink[] GrowDynamicSinksArray(IDynamicMessageSink[] sinks)
		{
			int num = ((sinks != null) ? sinks.Length : 0) + 8;
			IDynamicMessageSink[] array = new IDynamicMessageSink[num];
			if (sinks != null)
			{
				Array.Copy(sinks, array, sinks.Length);
			}
			return array;
		}

		// Token: 0x060058D6 RID: 22742 RVA: 0x001392B8 File Offset: 0x001374B8
		[SecurityCritical]
		internal static void NotifyDynamicSinks(IMessage msg, ArrayWithSize dynSinks, bool bCliSide, bool bStart, bool bAsync)
		{
			for (int i = 0; i < dynSinks.Count; i++)
			{
				if (bStart)
				{
					dynSinks.Sinks[i].ProcessMessageStart(msg, bCliSide, bAsync);
				}
				else
				{
					dynSinks.Sinks[i].ProcessMessageFinish(msg, bCliSide, bAsync);
				}
			}
		}

		// Token: 0x060058D7 RID: 22743 RVA: 0x00139300 File Offset: 0x00137500
		[SecurityCritical]
		internal static void CheckPropertyNameClash(string name, IDynamicProperty[] props, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (props[i].Name.Equals(name))
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DuplicatePropertyName"));
				}
			}
		}

		// Token: 0x060058D8 RID: 22744 RVA: 0x0013933C File Offset: 0x0013753C
		internal static IDynamicProperty[] GrowPropertiesArray(IDynamicProperty[] props)
		{
			int num = ((props != null) ? props.Length : 0) + 8;
			IDynamicProperty[] array = new IDynamicProperty[num];
			if (props != null)
			{
				Array.Copy(props, array, props.Length);
			}
			return array;
		}

		// Token: 0x04002876 RID: 10358
		private const int GROW_BY = 8;

		// Token: 0x04002877 RID: 10359
		private IDynamicProperty[] _props;

		// Token: 0x04002878 RID: 10360
		private int _numProps;

		// Token: 0x04002879 RID: 10361
		private IDynamicMessageSink[] _sinks;
	}
}
