using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.ExpeditionParade.Campaign;

internal static class ParadeSampleAllocator
{
	internal static int[] Allocate(IReadOnlyList<int> availableCounts, int displayLimit)
	{
		if (availableCounts == null)
		{
			throw new ArgumentNullException(nameof(availableCounts));
		}

		int[] available = availableCounts.Select(count => Math.Max(0, count)).ToArray();
		long totalAvailable = available.Aggregate(0L, (total, count) => total + count);
		int cappedLimit = (int)Math.Max(0L, Math.Min(displayLimit, totalAvailable));
		if (cappedLimit == totalAvailable)
		{
			return available;
		}
		if (cappedLimit == 0)
		{
			return new int[available.Length];
		}

		int[] allocated = new int[available.Length];
		double[] remainders = new double[available.Length];
		int allocatedTotal = 0;
		for (int index = 0; index < available.Length; index++)
		{
			double exact = (double)available[index] * cappedLimit / totalAvailable;
			int floor = Math.Min(available[index], (int)Math.Floor(exact));
			allocated[index] = floor;
			remainders[index] = exact - floor;
			allocatedTotal += floor;
		}

		int remaining = cappedLimit - allocatedTotal;
		foreach (int index in Enumerable.Range(0, available.Length)
			.OrderByDescending(index => remainders[index])
			.ThenByDescending(index => available[index])
			.ThenBy(index => index))
		{
			if (remaining <= 0)
			{
				break;
			}
			if (allocated[index] < available[index])
			{
				allocated[index]++;
				remaining--;
			}
		}

		return allocated;
	}
}
