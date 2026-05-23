using System;
using System.Security;

namespace System.Collections.Generic
{
	// Token: 0x020004C9 RID: 1225
	internal sealed class RandomizedStringEqualityComparer : IEqualityComparer<string>, IEqualityComparer, IWellKnownStringEqualityComparer
	{
		// Token: 0x06003AAA RID: 15018 RVA: 0x000DF7BC File Offset: 0x000DD9BC
		public RandomizedStringEqualityComparer()
		{
			this._entropy = HashHelpers.GetEntropy();
		}

		// Token: 0x06003AAB RID: 15019 RVA: 0x000DF7CF File Offset: 0x000DD9CF
		public bool Equals(object x, object y)
		{
			if (x == y)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			if (x is string && y is string)
			{
				return this.Equals((string)x, (string)y);
			}
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
			return false;
		}

		// Token: 0x06003AAC RID: 15020 RVA: 0x000DF809 File Offset: 0x000DDA09
		public bool Equals(string x, string y)
		{
			if (x != null)
			{
				return y != null && x.Equals(y);
			}
			return y == null;
		}

		// Token: 0x06003AAD RID: 15021 RVA: 0x000DF821 File Offset: 0x000DDA21
		[SecuritySafeCritical]
		public int GetHashCode(string obj)
		{
			if (obj == null)
			{
				return 0;
			}
			return string.InternalMarvin32HashString(obj, obj.Length, this._entropy);
		}

		// Token: 0x06003AAE RID: 15022 RVA: 0x000DF83C File Offset: 0x000DDA3C
		[SecuritySafeCritical]
		public int GetHashCode(object obj)
		{
			if (obj == null)
			{
				return 0;
			}
			string text = obj as string;
			if (text != null)
			{
				return string.InternalMarvin32HashString(text, text.Length, this._entropy);
			}
			return obj.GetHashCode();
		}

		// Token: 0x06003AAF RID: 15023 RVA: 0x000DF874 File Offset: 0x000DDA74
		public override bool Equals(object obj)
		{
			RandomizedStringEqualityComparer randomizedStringEqualityComparer = obj as RandomizedStringEqualityComparer;
			return randomizedStringEqualityComparer != null && this._entropy == randomizedStringEqualityComparer._entropy;
		}

		// Token: 0x06003AB0 RID: 15024 RVA: 0x000DF89B File Offset: 0x000DDA9B
		public override int GetHashCode()
		{
			return base.GetType().Name.GetHashCode() ^ (int)(this._entropy & 2147483647L);
		}

		// Token: 0x06003AB1 RID: 15025 RVA: 0x000DF8BC File Offset: 0x000DDABC
		IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer()
		{
			return new RandomizedStringEqualityComparer();
		}

		// Token: 0x06003AB2 RID: 15026 RVA: 0x000DF8C3 File Offset: 0x000DDAC3
		IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization()
		{
			return EqualityComparer<string>.Default;
		}

		// Token: 0x04001952 RID: 6482
		private long _entropy;
	}
}
