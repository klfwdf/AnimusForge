using System;
using System.Collections.Generic;

namespace TaleWorlds.Localization.TextProcessor;

internal class CaseInsensitiveComparer : IEqualityComparer<string>
{
	public bool Equals(string x, string y)
	{
		return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
	}

	public int GetHashCode(string x)
	{
		return x.ToLowerInvariant().GetHashCode();
	}
}
