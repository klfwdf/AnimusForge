using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

[Serializable]
public struct DynamicBodyProperties
{
	public const float MaxAge = 128f;

	public const float MaxAgeTeenager = 21f;

	public float Age;

	public float Weight;

	public float Build;

	public static readonly DynamicBodyProperties Invalid;

	public static readonly DynamicBodyProperties Default = new DynamicBodyProperties(20f, 0.5f, 0.5f);

	public DynamicBodyProperties(float age, float weight, float build)
	{
		Age = age;
		Weight = weight;
		Build = build;
	}

	public static bool operator ==(DynamicBodyProperties a, DynamicBodyProperties b)
	{
		if ((object)a == (object)b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.Age == b.Age && a.Weight == b.Weight)
		{
			return a.Build == b.Build;
		}
		return false;
	}

	public static bool operator !=(DynamicBodyProperties a, DynamicBodyProperties b)
	{
		return !(a == b);
	}

	public bool Equals(DynamicBodyProperties other)
	{
		if (Age.Equals(other.Age) && Weight.Equals(other.Weight))
		{
			return Build.Equals(other.Build);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj is DynamicBodyProperties)
		{
			return Equals((DynamicBodyProperties)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((Age.GetHashCode() * 397) ^ Weight.GetHashCode()) * 397) ^ Build.GetHashCode();
	}

	public override string ToString()
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(150, "ToString");
		mBStringBuilder.Append("age=\"");
		mBStringBuilder.Append(Age.ToString("0.##"));
		mBStringBuilder.Append("\" weight=\"");
		mBStringBuilder.Append(Weight.ToString("0.####"));
		mBStringBuilder.Append("\" build=\"");
		mBStringBuilder.Append(Build.ToString("0.####"));
		mBStringBuilder.Append("\" ");
		return mBStringBuilder.ToStringAndRelease();
	}
}
