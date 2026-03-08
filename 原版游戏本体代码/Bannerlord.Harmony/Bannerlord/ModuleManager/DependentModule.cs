using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000016 RID: 22
	[NullableContext(1)]
	[Nullable(0)]
	internal class DependentModule : IEquatable<DependentModule>
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000107 RID: 263 RVA: 0x00005D21 File Offset: 0x00003F21
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(DependentModule);
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x06000108 RID: 264 RVA: 0x00005D2D File Offset: 0x00003F2D
		// (set) Token: 0x06000109 RID: 265 RVA: 0x00005D35 File Offset: 0x00003F35
		public string Id { get; set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600010A RID: 266 RVA: 0x00005D3E File Offset: 0x00003F3E
		// (set) Token: 0x0600010B RID: 267 RVA: 0x00005D46 File Offset: 0x00003F46
		public ApplicationVersion Version { get; set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x0600010C RID: 268 RVA: 0x00005D4F File Offset: 0x00003F4F
		// (set) Token: 0x0600010D RID: 269 RVA: 0x00005D57 File Offset: 0x00003F57
		public bool IsOptional { get; set; }

		// Token: 0x0600010E RID: 270 RVA: 0x00005D60 File Offset: 0x00003F60
		public DependentModule()
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.IsOptional = false;
			base..ctor();
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00005D87 File Offset: 0x00003F87
		public DependentModule(string id, ApplicationVersion version, bool isOptional)
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.IsOptional = false;
			base..ctor();
			this.Id = id;
			this.Version = version;
			this.IsOptional = isOptional;
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00005DC8 File Offset: 0x00003FC8
		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("DependentModule");
			stringBuilder.Append(" { ");
			if (this.PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00005E14 File Offset: 0x00004014
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Id = ");
			builder.Append(this.Id);
			builder.Append(", Version = ");
			builder.Append(this.Version);
			builder.Append(", IsOptional = ");
			builder.Append(this.IsOptional.ToString());
			return true;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00005E80 File Offset: 0x00004080
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(DependentModule left, DependentModule right)
		{
			return !(left == right);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00005E8C File Offset: 0x0000408C
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(DependentModule left, DependentModule right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00005EA4 File Offset: 0x000040A4
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return ((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Id>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Version>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsOptional>k__BackingField);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00005F06 File Offset: 0x00004106
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as DependentModule);
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00005F14 File Offset: 0x00004114
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(DependentModule other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(this.<Id>k__BackingField, other.<Id>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Version>k__BackingField, other.<Version>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsOptional>k__BackingField, other.<IsOptional>k__BackingField));
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00005F8F File Offset: 0x0000418F
		[CompilerGenerated]
		protected DependentModule(DependentModule original)
		{
			this.Id = original.<Id>k__BackingField;
			this.Version = original.<Version>k__BackingField;
			this.IsOptional = original.<IsOptional>k__BackingField;
		}
	}
}
