using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000233 RID: 563
	internal sealed class EventDefinition : EventReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider
	{
		// Token: 0x17000277 RID: 631
		// (get) Token: 0x06000C10 RID: 3088 RVA: 0x0002A864 File Offset: 0x00028A64
		// (set) Token: 0x06000C11 RID: 3089 RVA: 0x0002A86C File Offset: 0x00028A6C
		public EventAttributes Attributes
		{
			get
			{
				return (EventAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x17000278 RID: 632
		// (get) Token: 0x06000C12 RID: 3090 RVA: 0x0002A875 File Offset: 0x00028A75
		// (set) Token: 0x06000C13 RID: 3091 RVA: 0x0002A892 File Offset: 0x00028A92
		public MethodDefinition AddMethod
		{
			get
			{
				if (this.add_method != null)
				{
					return this.add_method;
				}
				this.InitializeMethods();
				return this.add_method;
			}
			set
			{
				this.add_method = value;
			}
		}

		// Token: 0x17000279 RID: 633
		// (get) Token: 0x06000C14 RID: 3092 RVA: 0x0002A89B File Offset: 0x00028A9B
		// (set) Token: 0x06000C15 RID: 3093 RVA: 0x0002A8B8 File Offset: 0x00028AB8
		public MethodDefinition InvokeMethod
		{
			get
			{
				if (this.invoke_method != null)
				{
					return this.invoke_method;
				}
				this.InitializeMethods();
				return this.invoke_method;
			}
			set
			{
				this.invoke_method = value;
			}
		}

		// Token: 0x1700027A RID: 634
		// (get) Token: 0x06000C16 RID: 3094 RVA: 0x0002A8C1 File Offset: 0x00028AC1
		// (set) Token: 0x06000C17 RID: 3095 RVA: 0x0002A8DE File Offset: 0x00028ADE
		public MethodDefinition RemoveMethod
		{
			get
			{
				if (this.remove_method != null)
				{
					return this.remove_method;
				}
				this.InitializeMethods();
				return this.remove_method;
			}
			set
			{
				this.remove_method = value;
			}
		}

		// Token: 0x1700027B RID: 635
		// (get) Token: 0x06000C18 RID: 3096 RVA: 0x0002A8E7 File Offset: 0x00028AE7
		public bool HasOtherMethods
		{
			get
			{
				if (this.other_methods != null)
				{
					return this.other_methods.Count > 0;
				}
				this.InitializeMethods();
				return !this.other_methods.IsNullOrEmpty<MethodDefinition>();
			}
		}

		// Token: 0x1700027C RID: 636
		// (get) Token: 0x06000C19 RID: 3097 RVA: 0x0002A914 File Offset: 0x00028B14
		public Collection<MethodDefinition> OtherMethods
		{
			get
			{
				if (this.other_methods != null)
				{
					return this.other_methods;
				}
				this.InitializeMethods();
				if (this.other_methods == null)
				{
					Interlocked.CompareExchange<Collection<MethodDefinition>>(ref this.other_methods, new Collection<MethodDefinition>(), null);
				}
				return this.other_methods;
			}
		}

		// Token: 0x1700027D RID: 637
		// (get) Token: 0x06000C1A RID: 3098 RVA: 0x0002A94B File Offset: 0x00028B4B
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.Module);
			}
		}

		// Token: 0x1700027E RID: 638
		// (get) Token: 0x06000C1B RID: 3099 RVA: 0x0002A970 File Offset: 0x00028B70
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x1700027F RID: 639
		// (get) Token: 0x06000C1C RID: 3100 RVA: 0x0002A98E File Offset: 0x00028B8E
		// (set) Token: 0x06000C1D RID: 3101 RVA: 0x0002A9A0 File Offset: 0x00028BA0
		public bool IsSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(512);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(512, value);
			}
		}

		// Token: 0x17000280 RID: 640
		// (get) Token: 0x06000C1E RID: 3102 RVA: 0x0002A9B9 File Offset: 0x00028BB9
		// (set) Token: 0x06000C1F RID: 3103 RVA: 0x0002A9CB File Offset: 0x00028BCB
		public bool IsRuntimeSpecialName
		{
			get
			{
				return this.attributes.GetAttributes(1024);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1024, value);
			}
		}

		// Token: 0x17000281 RID: 641
		// (get) Token: 0x06000C20 RID: 3104 RVA: 0x0002A9E4 File Offset: 0x00028BE4
		// (set) Token: 0x06000C21 RID: 3105 RVA: 0x0002A9F1 File Offset: 0x00028BF1
		public new TypeDefinition DeclaringType
		{
			get
			{
				return (TypeDefinition)base.DeclaringType;
			}
			set
			{
				base.DeclaringType = value;
			}
		}

		// Token: 0x17000282 RID: 642
		// (get) Token: 0x06000C22 RID: 3106 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsDefinition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x0002A9FA File Offset: 0x00028BFA
		public EventDefinition(string name, EventAttributes attributes, TypeReference eventType)
			: base(name, eventType)
		{
			this.attributes = (ushort)attributes;
			this.token = new MetadataToken(TokenType.Event);
		}

		// Token: 0x06000C24 RID: 3108 RVA: 0x0002AA1C File Offset: 0x00028C1C
		private void InitializeMethods()
		{
			ModuleDefinition module = this.Module;
			if (module == null)
			{
				return;
			}
			object syncRoot = module.SyncRoot;
			lock (syncRoot)
			{
				if (this.add_method == null && this.invoke_method == null && this.remove_method == null)
				{
					if (module.HasImage())
					{
						module.Read<EventDefinition>(this, delegate(EventDefinition @event, MetadataReader reader)
						{
							reader.ReadMethods(@event);
						});
					}
				}
			}
		}

		// Token: 0x06000C25 RID: 3109 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override EventDefinition Resolve()
		{
			return this;
		}

		// Token: 0x040003A7 RID: 935
		private ushort attributes;

		// Token: 0x040003A8 RID: 936
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x040003A9 RID: 937
		internal MethodDefinition add_method;

		// Token: 0x040003AA RID: 938
		internal MethodDefinition invoke_method;

		// Token: 0x040003AB RID: 939
		internal MethodDefinition remove_method;

		// Token: 0x040003AC RID: 940
		internal Collection<MethodDefinition> other_methods;
	}
}
