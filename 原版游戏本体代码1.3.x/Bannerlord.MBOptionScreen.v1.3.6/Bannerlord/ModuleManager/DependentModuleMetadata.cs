using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000059 RID: 89
	[NullableContext(1)]
	[Nullable(0)]
	internal class DependentModuleMetadata : IEquatable<DependentModuleMetadata>
	{
		// Token: 0x170000EE RID: 238
		// (get) Token: 0x06000321 RID: 801 RVA: 0x0000B009 File Offset: 0x00009209
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(DependentModuleMetadata);
			}
		}

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x06000322 RID: 802 RVA: 0x0000B015 File Offset: 0x00009215
		// (set) Token: 0x06000323 RID: 803 RVA: 0x0000B01D File Offset: 0x0000921D
		public string Id { get; set; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x06000324 RID: 804 RVA: 0x0000B026 File Offset: 0x00009226
		// (set) Token: 0x06000325 RID: 805 RVA: 0x0000B02E File Offset: 0x0000922E
		public LoadType LoadType { get; set; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x06000326 RID: 806 RVA: 0x0000B037 File Offset: 0x00009237
		// (set) Token: 0x06000327 RID: 807 RVA: 0x0000B03F File Offset: 0x0000923F
		public bool IsOptional { get; set; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x06000328 RID: 808 RVA: 0x0000B048 File Offset: 0x00009248
		// (set) Token: 0x06000329 RID: 809 RVA: 0x0000B050 File Offset: 0x00009250
		public bool IsIncompatible { get; set; }

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x0600032A RID: 810 RVA: 0x0000B059 File Offset: 0x00009259
		// (set) Token: 0x0600032B RID: 811 RVA: 0x0000B061 File Offset: 0x00009261
		public ApplicationVersion Version { get; set; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x0600032C RID: 812 RVA: 0x0000B06A File Offset: 0x0000926A
		// (set) Token: 0x0600032D RID: 813 RVA: 0x0000B072 File Offset: 0x00009272
		public ApplicationVersionRange VersionRange { get; set; }

		// Token: 0x0600032E RID: 814 RVA: 0x0000B07B File Offset: 0x0000927B
		public DependentModuleMetadata()
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.VersionRange = ApplicationVersionRange.Empty;
			base..ctor();
		}

		// Token: 0x0600032F RID: 815 RVA: 0x0000B0A4 File Offset: 0x000092A4
		public DependentModuleMetadata(string id, LoadType loadType, bool isOptional, bool isIncompatible, ApplicationVersion version, ApplicationVersionRange versionRange)
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.VersionRange = ApplicationVersionRange.Empty;
			base..ctor();
			this.Id = id;
			this.LoadType = loadType;
			this.IsOptional = isOptional;
			this.IsIncompatible = isIncompatible;
			this.Version = version;
			this.VersionRange = versionRange;
		}

		// Token: 0x06000330 RID: 816 RVA: 0x0000B108 File Offset: 0x00009308
		public static string GetLoadType(LoadType loadType)
		{
			string result;
			switch (loadType)
			{
			case LoadType.None:
				result = "";
				break;
			case LoadType.LoadAfterThis:
				result = "Before       ";
				break;
			case LoadType.LoadBeforeThis:
				result = "After        ";
				break;
			default:
				result = "ERROR        ";
				break;
			}
			return result;
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0000B148 File Offset: 0x00009348
		public static string GetVersion([Nullable(2)] ApplicationVersion av)
		{
			if (av == null || !av.IsSameWithChangeSet(ApplicationVersion.Empty))
			{
				return string.Format(" {0}", av);
			}
			return "";
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0000B16B File Offset: 0x0000936B
		public static string GetVersionRange([Nullable(2)] ApplicationVersionRange avr)
		{
			if (!(avr == ApplicationVersionRange.Empty))
			{
				return string.Format(" {0}", avr);
			}
			return "";
		}

		// Token: 0x06000333 RID: 819 RVA: 0x0000B18B File Offset: 0x0000938B
		public static string GetOptional(bool isOptional)
		{
			if (!isOptional)
			{
				return "";
			}
			return " Optional";
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0000B19B File Offset: 0x0000939B
		public static string GetIncompatible(bool isOptional)
		{
			if (!isOptional)
			{
				return "";
			}
			return "Incompatible ";
		}

		// Token: 0x06000335 RID: 821 RVA: 0x0000B1AC File Offset: 0x000093AC
		public override string ToString()
		{
			return string.Concat(new string[]
			{
				DependentModuleMetadata.GetLoadType(this.LoadType),
				DependentModuleMetadata.GetIncompatible(this.IsIncompatible),
				this.Id,
				DependentModuleMetadata.GetVersion(this.Version),
				DependentModuleMetadata.GetVersionRange(this.VersionRange),
				DependentModuleMetadata.GetOptional(this.IsOptional)
			});
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0000B214 File Offset: 0x00009414
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Id = ");
			builder.Append(this.Id);
			builder.Append(", LoadType = ");
			builder.Append(this.LoadType.ToString());
			builder.Append(", IsOptional = ");
			builder.Append(this.IsOptional.ToString());
			builder.Append(", IsIncompatible = ");
			builder.Append(this.IsIncompatible.ToString());
			builder.Append(", Version = ");
			builder.Append(this.Version);
			builder.Append(", VersionRange = ");
			builder.Append(this.VersionRange);
			return true;
		}

		// Token: 0x06000337 RID: 823 RVA: 0x0000B2E7 File Offset: 0x000094E7
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(DependentModuleMetadata left, DependentModuleMetadata right)
		{
			return !(left == right);
		}

		// Token: 0x06000338 RID: 824 RVA: 0x0000B2F3 File Offset: 0x000094F3
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(DependentModuleMetadata left, DependentModuleMetadata right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000339 RID: 825 RVA: 0x0000B308 File Offset: 0x00009508
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (((((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Id>k__BackingField)) * -1521134295 + EqualityComparer<LoadType>.Default.GetHashCode(this.<LoadType>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsOptional>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsIncompatible>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Version>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<VersionRange>k__BackingField);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0000B3AF File Offset: 0x000095AF
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as DependentModuleMetadata);
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0000B3C0 File Offset: 0x000095C0
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(DependentModuleMetadata other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(this.<Id>k__BackingField, other.<Id>k__BackingField) && EqualityComparer<LoadType>.Default.Equals(this.<LoadType>k__BackingField, other.<LoadType>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsOptional>k__BackingField, other.<IsOptional>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsIncompatible>k__BackingField, other.<IsIncompatible>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Version>k__BackingField, other.<Version>k__BackingField) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<VersionRange>k__BackingField, other.<VersionRange>k__BackingField));
		}

		// Token: 0x0600033D RID: 829 RVA: 0x0000B48C File Offset: 0x0000968C
		[CompilerGenerated]
		protected DependentModuleMetadata(DependentModuleMetadata original)
		{
			this.Id = original.<Id>k__BackingField;
			this.LoadType = original.<LoadType>k__BackingField;
			this.IsOptional = original.<IsOptional>k__BackingField;
			this.IsIncompatible = original.<IsIncompatible>k__BackingField;
			this.Version = original.<Version>k__BackingField;
			this.VersionRange = original.<VersionRange>k__BackingField;
		}
	}
}
