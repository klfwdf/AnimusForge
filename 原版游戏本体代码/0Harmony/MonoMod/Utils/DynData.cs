using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008A7 RID: 2215
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class DynData<TTarget> : IDisposable where TTarget : class
	{
		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06002D80 RID: 11648 RVA: 0x00099060 File Offset: 0x00097260
		// (remove) Token: 0x06002D81 RID: 11649 RVA: 0x00099094 File Offset: 0x00097294
		[Nullable(new byte[] { 2, 1, 1, 2 })]
		[Nullable(new byte[] { 2, 1, 1, 2 })]
		public static event Action<DynData<TTarget>, TTarget> OnInitialize;

		// Token: 0x17000842 RID: 2114
		// (get) Token: 0x06002D82 RID: 11650 RVA: 0x000990C7 File Offset: 0x000972C7
		[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
		public Dictionary<string, Func<TTarget, object>> Getters
		{
			[return: Nullable(new byte[] { 1, 1, 1, 1, 2 })]
			get
			{
				return this._Data.Getters;
			}
		}

		// Token: 0x17000843 RID: 2115
		// (get) Token: 0x06002D83 RID: 11651 RVA: 0x000990D4 File Offset: 0x000972D4
		[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
		public Dictionary<string, Action<TTarget, object>> Setters
		{
			[return: Nullable(new byte[] { 1, 1, 1, 1, 2 })]
			get
			{
				return this._Data.Setters;
			}
		}

		// Token: 0x17000844 RID: 2116
		// (get) Token: 0x06002D84 RID: 11652 RVA: 0x000990E1 File Offset: 0x000972E1
		[Nullable(new byte[] { 1, 1, 2 })]
		public Dictionary<string, object> Data
		{
			[return: Nullable(new byte[] { 1, 1, 2 })]
			get
			{
				return this._Data.Data;
			}
		}

		// Token: 0x06002D85 RID: 11653 RVA: 0x000990F0 File Offset: 0x000972F0
		static DynData()
		{
			FieldInfo[] fields = typeof(TTarget).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				FieldInfo field = fields[i];
				string name = field.Name;
				DynData<TTarget>._SpecialGetters[name] = (TTarget obj) => field.GetValue(obj);
				DynData<TTarget>._SpecialSetters[name] = delegate(TTarget obj, [Nullable(2)] object value)
				{
					field.SetValue(obj, value);
				};
			}
			PropertyInfo[] properties = typeof(TTarget).GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < properties.Length; i++)
			{
				PropertyInfo prop = properties[i];
				string name2 = prop.Name;
				MethodInfo get = prop.GetGetMethod(true);
				if (get != null)
				{
					DynData<TTarget>._SpecialGetters[name2] = (TTarget obj) => get.Invoke(obj, ArrayEx.Empty<object>());
				}
				MethodInfo set = prop.GetSetMethod(true);
				if (set != null)
				{
					DynData<TTarget>._SpecialSetters[name2] = delegate(TTarget obj, [Nullable(2)] object value)
					{
						set.Invoke(obj, new object[] { value });
					};
				}
			}
		}

		// Token: 0x17000845 RID: 2117
		// (get) Token: 0x06002D86 RID: 11654 RVA: 0x0009923A File Offset: 0x0009743A
		public bool IsAlive
		{
			get
			{
				return this.Weak == null || this.Weak.SafeGetIsAlive();
			}
		}

		// Token: 0x17000846 RID: 2118
		// (get) Token: 0x06002D87 RID: 11655 RVA: 0x00099251 File Offset: 0x00097451
		public TTarget Target
		{
			get
			{
				WeakReference weak = this.Weak;
				return (TTarget)((object)((weak != null) ? weak.SafeGetTarget() : null));
			}
		}

		// Token: 0x17000847 RID: 2119
		[Nullable(2)]
		public object this[string name]
		{
			[return: Nullable(2)]
			get
			{
				Func<TTarget, object> cb;
				if (DynData<TTarget>._SpecialGetters.TryGetValue(name, out cb) || this.Getters.TryGetValue(name, out cb))
				{
					return cb(this.Target);
				}
				object value;
				if (this.Data.TryGetValue(name, out value))
				{
					return value;
				}
				return null;
			}
			[param: Nullable(2)]
			set
			{
				Action<TTarget, object> cb;
				if (DynData<TTarget>._SpecialSetters.TryGetValue(name, out cb) || this.Setters.TryGetValue(name, out cb))
				{
					cb(this.Target, value);
					return;
				}
				object prev;
				if (this._Data.Disposable.Contains(name) && (prev = this[name]) != null)
				{
					IDisposable prevDisposable = prev as IDisposable;
					if (prevDisposable != null)
					{
						prevDisposable.Dispose();
					}
				}
				this.Data[name] = value;
			}
		}

		// Token: 0x06002D8A RID: 11658 RVA: 0x00099330 File Offset: 0x00097530
		public DynData()
			: this(default(TTarget), false)
		{
		}

		// Token: 0x06002D8B RID: 11659 RVA: 0x0009934D File Offset: 0x0009754D
		[NullableContext(2)]
		public DynData(TTarget obj)
			: this(obj, true)
		{
		}

		// Token: 0x06002D8C RID: 11660 RVA: 0x00099358 File Offset: 0x00097558
		[NullableContext(2)]
		public DynData(TTarget obj, bool keepAlive)
		{
			if (obj != null)
			{
				WeakReference weak = new WeakReference(obj);
				object key = obj;
				DynData<TTarget>._Data_ data;
				if (!DynData<TTarget>._DataMap.TryGetValue(key, out data))
				{
					data = new DynData<TTarget>._Data_();
					DynData<TTarget>._DataMap.Add(key, data);
				}
				this._Data = data;
				this.Weak = weak;
				if (keepAlive)
				{
					this.KeepAlive = obj;
				}
			}
			else
			{
				this._Data = DynData<TTarget>._DataStatic;
			}
			Action<DynData<TTarget>, TTarget> onInitialize = DynData<TTarget>.OnInitialize;
			if (onInitialize == null)
			{
				return;
			}
			onInitialize(this, obj);
		}

		// Token: 0x06002D8D RID: 11661 RVA: 0x000993DD File Offset: 0x000975DD
		[NullableContext(2)]
		public T Get<T>([Nullable(1)] string name)
		{
			return (T)((object)this[name]);
		}

		// Token: 0x06002D8E RID: 11662 RVA: 0x000993EB File Offset: 0x000975EB
		public void Set<[Nullable(2)] T>(string name, T value)
		{
			this[name] = value;
		}

		// Token: 0x06002D8F RID: 11663 RVA: 0x000993FA File Offset: 0x000975FA
		public void RegisterProperty(string name, [Nullable(new byte[] { 1, 1, 2 })] Func<TTarget, object> getter, [Nullable(new byte[] { 1, 1, 2 })] Action<TTarget, object> setter)
		{
			this.Getters[name] = getter;
			this.Setters[name] = setter;
		}

		// Token: 0x06002D90 RID: 11664 RVA: 0x00099416 File Offset: 0x00097616
		public void UnregisterProperty(string name)
		{
			this.Getters.Remove(name);
			this.Setters.Remove(name);
		}

		// Token: 0x06002D91 RID: 11665 RVA: 0x00099432 File Offset: 0x00097632
		private void Dispose(bool disposing)
		{
			this.KeepAlive = default(TTarget);
			if (disposing)
			{
				this._Data.Dispose();
			}
		}

		// Token: 0x06002D92 RID: 11666 RVA: 0x00099450 File Offset: 0x00097650
		~DynData()
		{
			this.Dispose(false);
		}

		// Token: 0x06002D93 RID: 11667 RVA: 0x00099480 File Offset: 0x00097680
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x04003ADA RID: 15066
		[Nullable(new byte[] { 1, 0 })]
		private static readonly DynData<TTarget>._Data_ _DataStatic = new DynData<TTarget>._Data_();

		// Token: 0x04003ADB RID: 15067
		[Nullable(new byte[] { 1, 1, 1, 0 })]
		private static readonly ConditionalWeakTable<object, DynData<TTarget>._Data_> _DataMap = new ConditionalWeakTable<object, DynData<TTarget>._Data_>();

		// Token: 0x04003ADC RID: 15068
		[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
		private static readonly Dictionary<string, Func<TTarget, object>> _SpecialGetters = new Dictionary<string, Func<TTarget, object>>();

		// Token: 0x04003ADD RID: 15069
		[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
		private static readonly Dictionary<string, Action<TTarget, object>> _SpecialSetters = new Dictionary<string, Action<TTarget, object>>();

		// Token: 0x04003ADE RID: 15070
		[Nullable(2)]
		private readonly WeakReference Weak;

		// Token: 0x04003ADF RID: 15071
		[Nullable(2)]
		private TTarget KeepAlive;

		// Token: 0x04003AE0 RID: 15072
		[Nullable(new byte[] { 1, 0 })]
		private readonly DynData<TTarget>._Data_ _Data;

		// Token: 0x020008A8 RID: 2216
		[NullableContext(0)]
		private class _Data_ : IDisposable
		{
			// Token: 0x06002D94 RID: 11668 RVA: 0x00099490 File Offset: 0x00097690
			~_Data_()
			{
				this.Dispose();
			}

			// Token: 0x06002D95 RID: 11669 RVA: 0x000994BC File Offset: 0x000976BC
			public void Dispose()
			{
				Dictionary<string, object> data = this.Data;
				lock (data)
				{
					if (this.Data.Count == 0)
					{
						return;
					}
					foreach (string name in this.Disposable)
					{
						object value;
						if (this.Data.TryGetValue(name, out value))
						{
							IDisposable valueDisposable = value as IDisposable;
							if (valueDisposable != null)
							{
								valueDisposable.Dispose();
							}
						}
					}
					this.Disposable.Clear();
					this.Data.Clear();
				}
				GC.SuppressFinalize(this);
			}

			// Token: 0x04003AE1 RID: 15073
			[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
			public readonly Dictionary<string, Func<TTarget, object>> Getters = new Dictionary<string, Func<TTarget, object>>();

			// Token: 0x04003AE2 RID: 15074
			[Nullable(new byte[] { 1, 1, 1, 1, 2 })]
			public readonly Dictionary<string, Action<TTarget, object>> Setters = new Dictionary<string, Action<TTarget, object>>();

			// Token: 0x04003AE3 RID: 15075
			[Nullable(new byte[] { 1, 1, 2 })]
			public readonly Dictionary<string, object> Data = new Dictionary<string, object>();

			// Token: 0x04003AE4 RID: 15076
			[Nullable(1)]
			public readonly HashSet<string> Disposable = new HashSet<string>();
		}
	}
}
