using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x02000057 RID: 87
	[Serializable]
	public struct DynamicBodyProperties
	{
		// Token: 0x060006F4 RID: 1780 RVA: 0x0001828E File Offset: 0x0001648E
		public DynamicBodyProperties(float age, float weight, float build)
		{
			this.Age = age;
			this.Weight = weight;
			this.Build = build;
		}

		// Token: 0x060006F5 RID: 1781 RVA: 0x000182A8 File Offset: 0x000164A8
		public static bool operator ==(DynamicBodyProperties a, DynamicBodyProperties b)
		{
			return a == b || (a != null && b != null && (a.Age == b.Age && a.Weight == b.Weight) && a.Build == b.Build);
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x00018303 File Offset: 0x00016503
		public static bool operator !=(DynamicBodyProperties a, DynamicBodyProperties b)
		{
			return !(a == b);
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x0001830F File Offset: 0x0001650F
		public bool Equals(DynamicBodyProperties other)
		{
			return this.Age.Equals(other.Age) && this.Weight.Equals(other.Weight) && this.Build.Equals(other.Build);
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x0001834A File Offset: 0x0001654A
		public override bool Equals(object obj)
		{
			return obj != null && obj is DynamicBodyProperties && this.Equals((DynamicBodyProperties)obj);
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x00018367 File Offset: 0x00016567
		public override int GetHashCode()
		{
			return (((this.Age.GetHashCode() * 397) ^ this.Weight.GetHashCode()) * 397) ^ this.Build.GetHashCode();
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x00018398 File Offset: 0x00016598
		public override string ToString()
		{
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(150, "ToString");
			mbstringBuilder.Append<string>("age=\"");
			mbstringBuilder.Append<string>(this.Age.ToString("0.##"));
			mbstringBuilder.Append<string>("\" weight=\"");
			mbstringBuilder.Append<string>(this.Weight.ToString("0.####"));
			mbstringBuilder.Append<string>("\" build=\"");
			mbstringBuilder.Append<string>(this.Build.ToString("0.####"));
			mbstringBuilder.Append<string>("\" ");
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x04000372 RID: 882
		public const float MaxAge = 128f;

		// Token: 0x04000373 RID: 883
		public const float MaxAgeTeenager = 21f;

		// Token: 0x04000374 RID: 884
		public float Age;

		// Token: 0x04000375 RID: 885
		public float Weight;

		// Token: 0x04000376 RID: 886
		public float Build;

		// Token: 0x04000377 RID: 887
		public static readonly DynamicBodyProperties Invalid;

		// Token: 0x04000378 RID: 888
		public static readonly DynamicBodyProperties Default = new DynamicBodyProperties(20f, 0.5f, 0.5f);
	}
}
