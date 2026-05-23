using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000017 RID: 23
	[NullableContext(1)]
	[Nullable(0)]
	internal class DependentModuleMetadata : IEquatable<DependentModuleMetadata>
	{
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000119 RID: 281 RVA: 0x00005FBD File Offset: 0x000041BD
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(DependentModuleMetadata);
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x0600011A RID: 282 RVA: 0x00005FC9 File Offset: 0x000041C9
		// (set) Token: 0x0600011B RID: 283 RVA: 0x00005FD1 File Offset: 0x000041D1
		public string Id { get; set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x0600011C RID: 284 RVA: 0x00005FDA File Offset: 0x000041DA
		// (set) Token: 0x0600011D RID: 285 RVA: 0x00005FE2 File Offset: 0x000041E2
		public LoadType LoadType { get; set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x0600011E RID: 286 RVA: 0x00005FEB File Offset: 0x000041EB
		// (set) Token: 0x0600011F RID: 287 RVA: 0x00005FF3 File Offset: 0x000041F3
		public bool IsOptional { get; set; }

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00005FFC File Offset: 0x000041FC
		// (set) Token: 0x06000121 RID: 289 RVA: 0x00006004 File Offset: 0x00004204
		public bool IsIncompatible { get; set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000122 RID: 290 RVA: 0x0000600D File Offset: 0x0000420D
		// (set) Token: 0x06000123 RID: 291 RVA: 0x00006015 File Offset: 0x00004215
		public ApplicationVersion Version { get; set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000124 RID: 292 RVA: 0x0000601E File Offset: 0x0000421E
		// (set) Token: 0x06000125 RID: 293 RVA: 0x00006026 File Offset: 0x00004226
		public ApplicationVersionRange VersionRange { get; set; }

		// Token: 0x06000126 RID: 294 RVA: 0x0000602F File Offset: 0x0000422F
		public DependentModuleMetadata()
		{
			this.Id = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.VersionRange = ApplicationVersionRange.Empty;
			base..ctor();
		}

		// Token: 0x06000127 RID: 295 RVA: 0x0000605C File Offset: 0x0000425C
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

		// Token: 0x06000128 RID: 296 RVA: 0x000060C8 File Offset: 0x000042C8
		public static string GetLoadType(LoadType loadType)
		{
			if (!true)
			{
			}
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
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00006114 File Offset: 0x00004314
		public static string GetVersion([Nullable(2)] ApplicationVersion av)
		{
			string result;
			if (av == null || !av.IsSameWithChangeSet(ApplicationVersion.Empty))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(av);
				result = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				result = "";
			}
			return result;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00006160 File Offset: 0x00004360
		public static string GetVersionRange([Nullable(2)] ApplicationVersionRange avr)
		{
			string result;
			if (!(avr == ApplicationVersionRange.Empty))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersionRange>(avr);
				result = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				result = "";
			}
			return result;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x000061A7 File Offset: 0x000043A7
		public static string GetOptional(bool isOptional)
		{
			return isOptional ? " Optional" : "";
		}

		// Token: 0x0600012C RID: 300 RVA: 0x000061B8 File Offset: 0x000043B8
		public static string GetIncompatible(bool isOptional)
		{
			return isOptional ? "Incompatible " : "";
		}

		// Token: 0x0600012D RID: 301 RVA: 0x000061CC File Offset: 0x000043CC
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

		// Token: 0x0600012E RID: 302 RVA: 0x00006234 File Offset: 0x00004434
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

		// Token: 0x0600012F RID: 303 RVA: 0x00006307 File Offset: 0x00004507
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(DependentModuleMetadata left, DependentModuleMetadata right)
		{
			return !(left == right);
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00006313 File Offset: 0x00004513
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(DependentModuleMetadata left, DependentModuleMetadata right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000632C File Offset: 0x0000452C
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (((((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Id>k__BackingField)) * -1521134295 + EqualityComparer<LoadType>.Default.GetHashCode(this.<LoadType>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsOptional>k__BackingField)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(this.<IsIncompatible>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Version>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersionRange>.Default.GetHashCode(this.<VersionRange>k__BackingField);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000063D3 File Offset: 0x000045D3
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as DependentModuleMetadata);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000063E4 File Offset: 0x000045E4
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(DependentModuleMetadata other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(this.<Id>k__BackingField, other.<Id>k__BackingField) && EqualityComparer<LoadType>.Default.Equals(this.<LoadType>k__BackingField, other.<LoadType>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsOptional>k__BackingField, other.<IsOptional>k__BackingField) && EqualityComparer<bool>.Default.Equals(this.<IsIncompatible>k__BackingField, other.<IsIncompatible>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Version>k__BackingField, other.<Version>k__BackingField) && EqualityComparer<ApplicationVersionRange>.Default.Equals(this.<VersionRange>k__BackingField, other.<VersionRange>k__BackingField));
		}

		// Token: 0x06000135 RID: 309 RVA: 0x000064B0 File Offset: 0x000046B0
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
