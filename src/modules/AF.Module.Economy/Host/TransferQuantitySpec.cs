using System;

namespace AnimusForge;

internal readonly struct TransferQuantitySpec
{
	internal const string AllToken = "ALL";

	internal bool IsAll { get; }

	internal int Amount { get; }

	private TransferQuantitySpec(bool isAll, int amount)
	{
		IsAll = isAll;
		Amount = amount;
	}

	internal static bool TryParse(string token, out TransferQuantitySpec value)
	{
		string text = (token ?? "").Trim();
		if (string.Equals(text, AllToken, StringComparison.OrdinalIgnoreCase))
		{
			value = new TransferQuantitySpec(isAll: true, 0);
			return true;
		}
		if (int.TryParse(text, out var amount) && amount > 0)
		{
			value = new TransferQuantitySpec(isAll: false, amount);
			return true;
		}
		value = default;
		return false;
	}

	internal static bool IsAllValue(string token)
	{
		return string.Equals((token ?? "").Trim(), AllToken, StringComparison.OrdinalIgnoreCase);
	}

	internal static long AddProduct(long total, int count, int unitValue)
	{
		if (total >= long.MaxValue || count <= 0 || unitValue <= 0)
		{
			return Math.Max(0L, total);
		}
		long product;
		try
		{
			product = checked((long)count * unitValue);
		}
		catch (OverflowException)
		{
			return long.MaxValue;
		}
		return total > long.MaxValue - product ? long.MaxValue : total + product;
	}

	internal static long AddValue(long total, long value)
	{
		long safeTotal = Math.Max(0L, total);
		long safeValue = Math.Max(0L, value);
		return safeTotal > long.MaxValue - safeValue ? long.MaxValue : safeTotal + safeValue;
	}
}
