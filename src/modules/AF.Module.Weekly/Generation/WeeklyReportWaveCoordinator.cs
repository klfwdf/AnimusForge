using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge;

// One invocation owns a generation's wave cursor and in-flight tasks. Game/UI
// access stays behind the host's main-thread launch queue and commit boundary.
internal static class WeeklyReportWaveCoordinator
{
	internal const int WaveIntervalMilliseconds = 60000;

	internal static async Task<TResult[]> RunAsync<TBatch, TResult>(
		IReadOnlyList<TBatch> batches,
		int burstSize,
		Func<TBatch, bool> isEligible,
		Func<string, bool> isCurrent,
		Func<List<TBatch>, int, int, int, Task<List<Task<TResult>>>> launch,
		Func<TBatch, int, TResult> notLaunched,
		Func<int, Task> delay = null)
	{
		if (batches == null) throw new ArgumentNullException(nameof(batches));
		if (isEligible == null) throw new ArgumentNullException(nameof(isEligible));
		if (isCurrent == null) throw new ArgumentNullException(nameof(isCurrent));
		if (launch == null) throw new ArgumentNullException(nameof(launch));
		if (notLaunched == null) throw new ArgumentNullException(nameof(notLaunched));
		delay = delay ?? Task.Delay;
		burstSize = Math.Max(1, burstSize);
		int totalWaves = Math.Max(1, (int)Math.Ceiling((double)batches.Count / burstSize));
		var running = new List<Task<TResult>>(batches.Count);
		for (int first = 0; first < batches.Count; first += burstSize)
		{
			if (!isCurrent("weekly_report_before_wave")) return null;
			int count = Math.Min(burstSize, batches.Count - first);
			var wave = new List<TBatch>(count);
			for (int offset = 0; offset < count; offset++)
			{
				TBatch batch = batches[first + offset];
				if (isEligible(batch)) wave.Add(batch);
			}
			if (wave.Count == 0) continue;
			List<Task<TResult>> launched = await launch(wave, first, first / burstSize + 1, totalWaves);
			if (!isCurrent("weekly_report_after_wave_launch")) return null;
			if (launched == null)
			{
				// A source-invalidated launch rejects this and every remaining wave.
				// Already launched requests still participate in final settlement.
				for (int index = first; index < batches.Count; index++)
					running.Add(Task.FromResult(notLaunched(batches[index], index)));
				break;
			}
			running.AddRange(launched);
			if (first + burstSize < batches.Count)
			{
				await delay(WaveIntervalMilliseconds);
				if (!isCurrent("weekly_report_after_wave_delay")) return null;
			}
		}
		TResult[] completed = await Task.WhenAll(running);
		return isCurrent("weekly_report_before_commit_enqueue") ? completed : null;
	}
}
