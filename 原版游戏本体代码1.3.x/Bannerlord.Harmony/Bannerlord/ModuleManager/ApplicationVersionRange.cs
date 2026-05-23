using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000014 RID: 20
	[NullableContext(1)]
	[Nullable(0)]
	internal class ApplicationVersionRange : IEquatable<ApplicationVersionRange>
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x000059BC File Offset: 0x00003BBC
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ApplicationVersionRange);
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000F4 RID: 244 RVA: 0x000059C8 File Offset: 0x00003BC8
		public static ApplicationVersionRange Empty
		{
			get
			{
				return new ApplicationVersionRange();
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x000059CF File Offset: 0x00003BCF
		// (set) Token: 0x060000F6 RID: 246 RVA: 0x000059D7 File Offset: 0x00003BD7
		public ApplicationVersion Min { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x000059E0 File Offset: 0x00003BE0
		// (set) Token: 0x060000F8 RID: 248 RVA: 0x000059E8 File Offset: 0x00003BE8
		public ApplicationVersion Max { get; set; }

		// Token: 0x060000F9 RID: 249 RVA: 0x000059F1 File Offset: 0x00003BF1
		public ApplicationVersionRange()
		{
			this.Min = ApplicationVersion.Empty;
			this.Max = ApplicationVersion.Empty;
			base..ctor();
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00005A11 File Offset: 0x00003C11
		public ApplicationVersionRange(ApplicationVersion min, ApplicationVersion max)
		{
			this.Min = ApplicationVersion.Empty;
			this.Max = ApplicationVersion.Empty;
			base..ctor();
			this.Max = max;
			this.Min = min;
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00005A41 File Offset: 0x00003C41
		[NullableContext(2)]
		public bool IsSame(ApplicationVersionRange other)
		{
			return this.Min.IsSame((other != null) ? other.Min : null) && this.Max.IsSame((other != null) ? other.Max : null);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00005A76 File Offset: 0x00003C76
		[NullableContext(2)]
		public bool IsSameWithChangeSet(ApplicationVersionRange other)
		{
			return this.Min.IsSameWithChangeSet((other != null) ? other.Min : null) && this.Max.IsSameWithChangeSet((other != null) ? other.Max : null);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00005AAC File Offset: 0x00003CAC
		public override string ToString()
		{
			bool flag = this.Min.ChangeSet == 0 && this.Max.ChangeSet == int.MaxValue;
			string result;
			if (flag)
			{
				result = this.Min.ToStringWithoutChangeset() + " - " + this.Max.ToStringWithoutChangeset();
			}
			else
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
				defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(this.Min);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(this.Max);
				result = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			return result;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00005B40 File Offset: 0x00003D40
		public static bool TryParse(string versionRangeAsString, out ApplicationVersionRange versionRange)
		{
			versionRange = ApplicationVersionRange.Empty;
			bool flag = string.IsNullOrWhiteSpace(versionRangeAsString);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				versionRangeAsString = versionRangeAsString.Replace(" ", string.Empty);
				int index = versionRangeAsString.IndexOf('-');
				bool flag2 = index < 0;
				if (flag2)
				{
					result = false;
				}
				else
				{
					string minAsString = versionRangeAsString.Substring(0, index);
					string maxAsString = versionRangeAsString.Substring(index + 1, versionRangeAsString.Length - 1 - index);
					ApplicationVersion min;
					ApplicationVersion max;
					bool flag3 = ApplicationVersion.TryParse(minAsString, out min, true) && ApplicationVersion.TryParse(maxAsString, out max, false);
					if (flag3)
					{
						versionRange = new ApplicationVersionRange
						{
							Min = min,
							Max = max
						};
						result = true;
					}
					else
					{
						result = false;
					}
				}
			}
			return result;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00005BF2 File Offset: 0x00003DF2
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Min = ");
			builder.Append(this.Min);
			builder.Append(", Max = ");
			builder.Append(this.Max);
			return true;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00005C2C File Offset: 0x00003E2C
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ApplicationVersionRange left, ApplicationVersionRange right)
		{
			return !(left == right);
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00005C38 File Offset: 0x00003E38
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ApplicationVersionRange left, ApplicationVersionRange right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00005C4E File Offset: 0x00003E4E
		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Min>k__BackingField)) * -1521134295 + EqualityComparer<ApplicationVersion>.Default.GetHashCode(this.<Max>k__BackingField);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00005C8E File Offset: 0x00003E8E
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ApplicationVersionRange);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00005C9C File Offset: 0x00003E9C
		[NullableContext(2)]
		[CompilerGenerated]
		public virtual bool Equals(ApplicationVersionRange other)
		{
			return this == other || (other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Min>k__BackingField, other.<Min>k__BackingField) && EqualityComparer<ApplicationVersion>.Default.Equals(this.<Max>k__BackingField, other.<Max>k__BackingField));
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00005CFF File Offset: 0x00003EFF
		[CompilerGenerated]
		protected ApplicationVersionRange(ApplicationVersionRange original)
		{
			this.Min = original.<Min>k__BackingField;
			this.Max = original.<Max>k__BackingField;
		}
	}
}
