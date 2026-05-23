using System;
using System.Text;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x0200028E RID: 654
	internal sealed class PropertyDefinition : PropertyReference, IMemberDefinition, ICustomAttributeProvider, IMetadataTokenProvider, IConstantProvider
	{
		// Token: 0x17000428 RID: 1064
		// (get) Token: 0x06001034 RID: 4148 RVA: 0x00031A88 File Offset: 0x0002FC88
		// (set) Token: 0x06001035 RID: 4149 RVA: 0x00031A90 File Offset: 0x0002FC90
		public PropertyAttributes Attributes
		{
			get
			{
				return (PropertyAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x17000429 RID: 1065
		// (get) Token: 0x06001036 RID: 4150 RVA: 0x00031A9C File Offset: 0x0002FC9C
		// (set) Token: 0x06001037 RID: 4151 RVA: 0x00031AEB File Offset: 0x0002FCEB
		public bool HasThis
		{
			get
			{
				if (this.has_this != null)
				{
					return this.has_this.Value;
				}
				if (this.GetMethod != null)
				{
					return this.get_method.HasThis;
				}
				return this.SetMethod != null && this.set_method.HasThis;
			}
			set
			{
				this.has_this = new bool?(value);
			}
		}

		// Token: 0x1700042A RID: 1066
		// (get) Token: 0x06001038 RID: 4152 RVA: 0x00031AF9 File Offset: 0x0002FCF9
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

		// Token: 0x1700042B RID: 1067
		// (get) Token: 0x06001039 RID: 4153 RVA: 0x00031B1E File Offset: 0x0002FD1E
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.Module);
			}
		}

		// Token: 0x1700042C RID: 1068
		// (get) Token: 0x0600103A RID: 4154 RVA: 0x00031B3C File Offset: 0x0002FD3C
		// (set) Token: 0x0600103B RID: 4155 RVA: 0x00031B59 File Offset: 0x0002FD59
		public MethodDefinition GetMethod
		{
			get
			{
				if (this.get_method != null)
				{
					return this.get_method;
				}
				this.InitializeMethods();
				return this.get_method;
			}
			set
			{
				this.get_method = value;
			}
		}

		// Token: 0x1700042D RID: 1069
		// (get) Token: 0x0600103C RID: 4156 RVA: 0x00031B62 File Offset: 0x0002FD62
		// (set) Token: 0x0600103D RID: 4157 RVA: 0x00031B7F File Offset: 0x0002FD7F
		public MethodDefinition SetMethod
		{
			get
			{
				if (this.set_method != null)
				{
					return this.set_method;
				}
				this.InitializeMethods();
				return this.set_method;
			}
			set
			{
				this.set_method = value;
			}
		}

		// Token: 0x1700042E RID: 1070
		// (get) Token: 0x0600103E RID: 4158 RVA: 0x00031B88 File Offset: 0x0002FD88
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

		// Token: 0x1700042F RID: 1071
		// (get) Token: 0x0600103F RID: 4159 RVA: 0x00031BB5 File Offset: 0x0002FDB5
		public Collection<MethodDefinition> OtherMethods
		{
			get
			{
				if (this.other_methods != null)
				{
					return this.other_methods;
				}
				this.InitializeMethods();
				if (this.other_methods != null)
				{
					return this.other_methods;
				}
				Interlocked.CompareExchange<Collection<MethodDefinition>>(ref this.other_methods, new Collection<MethodDefinition>(), null);
				return this.other_methods;
			}
		}

		// Token: 0x17000430 RID: 1072
		// (get) Token: 0x06001040 RID: 4160 RVA: 0x00031BF4 File Offset: 0x0002FDF4
		public bool HasParameters
		{
			get
			{
				this.InitializeMethods();
				if (this.get_method != null)
				{
					return this.get_method.HasParameters;
				}
				return this.set_method != null && this.set_method.HasParameters && this.set_method.Parameters.Count > 1;
			}
		}

		// Token: 0x17000431 RID: 1073
		// (get) Token: 0x06001041 RID: 4161 RVA: 0x00031C47 File Offset: 0x0002FE47
		public override Collection<ParameterDefinition> Parameters
		{
			get
			{
				this.InitializeMethods();
				if (this.get_method != null)
				{
					return PropertyDefinition.MirrorParameters(this.get_method, 0);
				}
				if (this.set_method != null)
				{
					return PropertyDefinition.MirrorParameters(this.set_method, 1);
				}
				return new Collection<ParameterDefinition>();
			}
		}

		// Token: 0x06001042 RID: 4162 RVA: 0x00031C80 File Offset: 0x0002FE80
		private static Collection<ParameterDefinition> MirrorParameters(MethodDefinition method, int bound)
		{
			Collection<ParameterDefinition> parameters = new Collection<ParameterDefinition>();
			if (!method.HasParameters)
			{
				return parameters;
			}
			Collection<ParameterDefinition> original_parameters = method.Parameters;
			int end = original_parameters.Count - bound;
			for (int i = 0; i < end; i++)
			{
				parameters.Add(original_parameters[i]);
			}
			return parameters;
		}

		// Token: 0x17000432 RID: 1074
		// (get) Token: 0x06001043 RID: 4163 RVA: 0x00031CC7 File Offset: 0x0002FEC7
		// (set) Token: 0x06001044 RID: 4164 RVA: 0x00031CEB File Offset: 0x0002FEEB
		public bool HasConstant
		{
			get
			{
				this.ResolveConstant(ref this.constant, this.Module);
				return this.constant != Mixin.NoValue;
			}
			set
			{
				if (!value)
				{
					this.constant = Mixin.NoValue;
				}
			}
		}

		// Token: 0x17000433 RID: 1075
		// (get) Token: 0x06001045 RID: 4165 RVA: 0x00031CFB File Offset: 0x0002FEFB
		// (set) Token: 0x06001046 RID: 4166 RVA: 0x00031D0D File Offset: 0x0002FF0D
		public object Constant
		{
			get
			{
				if (!this.HasConstant)
				{
					return null;
				}
				return this.constant;
			}
			set
			{
				this.constant = value;
			}
		}

		// Token: 0x17000434 RID: 1076
		// (get) Token: 0x06001047 RID: 4167 RVA: 0x00031D16 File Offset: 0x0002FF16
		// (set) Token: 0x06001048 RID: 4168 RVA: 0x00031D28 File Offset: 0x0002FF28
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

		// Token: 0x17000435 RID: 1077
		// (get) Token: 0x06001049 RID: 4169 RVA: 0x00031D41 File Offset: 0x0002FF41
		// (set) Token: 0x0600104A RID: 4170 RVA: 0x00031D53 File Offset: 0x0002FF53
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

		// Token: 0x17000436 RID: 1078
		// (get) Token: 0x0600104B RID: 4171 RVA: 0x00031D6C File Offset: 0x0002FF6C
		// (set) Token: 0x0600104C RID: 4172 RVA: 0x00031D7E File Offset: 0x0002FF7E
		public bool HasDefault
		{
			get
			{
				return this.attributes.GetAttributes(4096);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(4096, value);
			}
		}

		// Token: 0x17000437 RID: 1079
		// (get) Token: 0x0600104D RID: 4173 RVA: 0x0002A9E4 File Offset: 0x00028BE4
		// (set) Token: 0x0600104E RID: 4174 RVA: 0x0002A9F1 File Offset: 0x00028BF1
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

		// Token: 0x17000438 RID: 1080
		// (get) Token: 0x0600104F RID: 4175 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsDefinition
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000439 RID: 1081
		// (get) Token: 0x06001050 RID: 4176 RVA: 0x00031D98 File Offset: 0x0002FF98
		public override string FullName
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(base.PropertyType.ToString());
				builder.Append(' ');
				builder.Append(base.MemberFullName());
				builder.Append('(');
				if (this.HasParameters)
				{
					Collection<ParameterDefinition> parameters = this.Parameters;
					for (int i = 0; i < parameters.Count; i++)
					{
						if (i > 0)
						{
							builder.Append(',');
						}
						builder.Append(parameters[i].ParameterType.FullName);
					}
				}
				builder.Append(')');
				return builder.ToString();
			}
		}

		// Token: 0x06001051 RID: 4177 RVA: 0x00031E30 File Offset: 0x00030030
		public PropertyDefinition(string name, PropertyAttributes attributes, TypeReference propertyType)
			: base(name, propertyType)
		{
			this.attributes = (ushort)attributes;
			this.token = new MetadataToken(TokenType.Property);
		}

		// Token: 0x06001052 RID: 4178 RVA: 0x00031E5C File Offset: 0x0003005C
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
				if (this.get_method == null && this.set_method == null)
				{
					if (module.HasImage())
					{
						module.Read<PropertyDefinition>(this, delegate(PropertyDefinition property, MetadataReader reader)
						{
							reader.ReadMethods(property);
						});
					}
				}
			}
		}

		// Token: 0x06001053 RID: 4179 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override PropertyDefinition Resolve()
		{
			return this;
		}

		// Token: 0x04000566 RID: 1382
		private bool? has_this;

		// Token: 0x04000567 RID: 1383
		private ushort attributes;

		// Token: 0x04000568 RID: 1384
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x04000569 RID: 1385
		internal MethodDefinition get_method;

		// Token: 0x0400056A RID: 1386
		internal MethodDefinition set_method;

		// Token: 0x0400056B RID: 1387
		internal Collection<MethodDefinition> other_methods;

		// Token: 0x0400056C RID: 1388
		private object constant = Mixin.NotResolved;
	}
}
