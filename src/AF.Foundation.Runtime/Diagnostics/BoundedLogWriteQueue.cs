using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal sealed class BoundedLogWriteQueue
{
	internal sealed class WorkItem
	{
		internal string Path;
		internal string Content;
		internal bool IsVerbose;
	}

	private const int MaxLogWriteQueueItems = 4096;
	private const int HardMaxLogWriteQueueItems = 8192;
	private const int LogBatchFlushItemCount = 256;
	private const int DroppedLogSummaryIntervalSeconds = 10;
	private readonly ConcurrentQueue<WorkItem> _queue = new ConcurrentQueue<WorkItem>();
	private readonly Func<string, bool> _isPathEnabled;
	private readonly Action<Dictionary<string, StringBuilder>> _flushBatches;
	private readonly Action<WorkItem> _writeImmediate;
	private readonly Action _processQueue;
	private int _running;
	private int _count;
	private long _droppedVerbose;
	private long _droppedNormal;
	private long _lastDroppedSummaryUtcTicks;

	internal BoundedLogWriteQueue(Func<string, bool> isPathEnabled,
		Action<Dictionary<string, StringBuilder>> flushBatches, Action<WorkItem> writeImmediate)
	{
		_isPathEnabled = isPathEnabled ?? throw new ArgumentNullException(nameof(isPathEnabled));
		_flushBatches = flushBatches ?? throw new ArgumentNullException(nameof(flushBatches));
		_writeImmediate = writeImmediate ?? throw new ArgumentNullException(nameof(writeImmediate));
		_processQueue = ProcessQueue;
	}

	internal int Count => Volatile.Read(ref _count);
	internal int WriterRunning => Volatile.Read(ref _running);
	internal long DroppedVerbose => Interlocked.Read(ref _droppedVerbose);
	internal long DroppedNormal => Interlocked.Read(ref _droppedNormal);

	internal bool Enqueue(string path, string content, bool isVerbose = false, bool bypassBackpressure = false)
	{
		if (string.IsNullOrWhiteSpace(path) || content == null) return true;
		if (!bypassBackpressure)
		{
			int queued = Volatile.Read(ref _count);
			if (queued >= MaxLogWriteQueueItems && isVerbose)
			{
				Interlocked.Increment(ref _droppedVerbose);
				return false;
			}
			if (queued >= HardMaxLogWriteQueueItems)
			{
				if (isVerbose) Interlocked.Increment(ref _droppedVerbose);
				else Interlocked.Increment(ref _droppedNormal);
				return false;
			}
		}
		Interlocked.Increment(ref _count);
		_queue.Enqueue(new WorkItem { Path = path, Content = content, IsVerbose = isVerbose });
		TryStartWriter();
		return true;
	}

	internal bool TryTakeDroppedSummary(long nowUtcTicks, out long verbose, out long normal)
	{
		verbose = 0L;
		normal = 0L;
		long last = Interlocked.Read(ref _lastDroppedSummaryUtcTicks);
		if (nowUtcTicks - last < TimeSpan.FromSeconds(DroppedLogSummaryIntervalSeconds).Ticks) return false;
		if (Interlocked.CompareExchange(ref _lastDroppedSummaryUtcTicks, nowUtcTicks, last) != last) return false;
		verbose = Interlocked.Exchange(ref _droppedVerbose, 0L);
		normal = Interlocked.Exchange(ref _droppedNormal, 0L);
		return verbose > 0L || normal > 0L;
	}

	internal void Drain()
	{
		while (_queue.TryDequeue(out WorkItem item))
		{
			Interlocked.Decrement(ref _count);
			_writeImmediate(item);
		}
	}

	private void TryStartWriter()
	{
		if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
		Task.Run(_processQueue);
	}

	private void ProcessQueue()
	{
		try
		{
			while (true)
			{
				Dictionary<string, StringBuilder> batches = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
				int batchCount = 0;
				while (_queue.TryDequeue(out WorkItem item))
				{
					Interlocked.Decrement(ref _count);
					AppendToBatch(item, batches, ref batchCount);
					if (batchCount >= LogBatchFlushItemCount)
					{
						_flushBatches(batches);
						batches.Clear();
						batchCount = 0;
					}
				}
				_flushBatches(batches);
				Interlocked.Exchange(ref _running, 0);
				if (_queue.IsEmpty || Interlocked.CompareExchange(ref _running, 1, 0) != 0) break;
			}
		}
		catch
		{
			Interlocked.Exchange(ref _running, 0);
			if (!_queue.IsEmpty) TryStartWriter();
		}
	}

	private void AppendToBatch(WorkItem item, Dictionary<string, StringBuilder> batches, ref int batchCount)
	{
		try
		{
			if (item == null || string.IsNullOrWhiteSpace(item.Path) || item.Content == null || !_isPathEnabled(item.Path) || batches == null) return;
			if (!batches.TryGetValue(item.Path, out StringBuilder builder) || builder == null)
			{
				builder = new StringBuilder();
				batches[item.Path] = builder;
			}
			builder.Append(item.Content);
			batchCount++;
		}
		catch
		{
		}
	}
}
