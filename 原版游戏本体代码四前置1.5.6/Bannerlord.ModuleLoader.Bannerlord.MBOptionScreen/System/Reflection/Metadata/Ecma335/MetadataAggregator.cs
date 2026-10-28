using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class MetadataAggregator
{
	internal struct RowCounts : IComparable<RowCounts>
	{
		public int AggregateInserts;

		public int Updates;

		public int CompareTo(RowCounts other)
		{
			return AggregateInserts - other.AggregateInserts;
		}

		public override string ToString()
		{
			return $"+0x{AggregateInserts:x} ~0x{Updates:x}";
		}
	}

	private readonly System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<int>> _heapSizes;

	private readonly System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<RowCounts>> _rowCounts;

	public MetadataAggregator(System.Reflection.Metadata.MetadataReader baseReader, IReadOnlyList<System.Reflection.Metadata.MetadataReader> deltaReaders)
		: this(baseReader, null, null, deltaReaders)
	{
	}

	public MetadataAggregator(IReadOnlyList<int>? baseTableRowCounts, IReadOnlyList<int>? baseHeapSizes, IReadOnlyList<System.Reflection.Metadata.MetadataReader>? deltaReaders)
		: this(null, baseTableRowCounts, baseHeapSizes, deltaReaders)
	{
	}

	private MetadataAggregator(System.Reflection.Metadata.MetadataReader baseReader, IReadOnlyList<int> baseTableRowCounts, IReadOnlyList<int> baseHeapSizes, IReadOnlyList<System.Reflection.Metadata.MetadataReader> deltaReaders)
	{
		if (baseTableRowCounts == null)
		{
			if (baseReader == null)
			{
				System.Reflection.Throw.ArgumentNull("baseReader");
			}
			if (baseReader.GetTableRowCount(System.Reflection.Metadata.Ecma335.TableIndex.EncMap) != 0)
			{
				throw new ArgumentException(System.SR.BaseReaderMustBeFullMetadataReader, "baseReader");
			}
			CalculateBaseCounts(baseReader, out baseTableRowCounts, out baseHeapSizes);
		}
		else
		{
			if (baseTableRowCounts.Count != System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount)
			{
				throw new ArgumentException(System.SR.Format(System.SR.ExpectedListOfSize, System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount), "baseTableRowCounts");
			}
			if (baseHeapSizes == null)
			{
				System.Reflection.Throw.ArgumentNull("baseHeapSizes");
			}
			if (baseHeapSizes.Count != System.Reflection.Metadata.Ecma335.MetadataTokens.HeapCount)
			{
				throw new ArgumentException(System.SR.Format(System.SR.ExpectedListOfSize, System.Reflection.Metadata.Ecma335.MetadataTokens.HeapCount), "baseTableRowCounts");
			}
		}
		if (deltaReaders == null || deltaReaders.Count == 0)
		{
			throw new ArgumentException(System.SR.ExpectedNonEmptyList, "deltaReaders");
		}
		for (int i = 0; i < deltaReaders.Count; i++)
		{
			if (deltaReaders[i].GetTableRowCount(System.Reflection.Metadata.Ecma335.TableIndex.EncMap) == 0 || !deltaReaders[i].IsMinimalDelta)
			{
				throw new ArgumentException(System.SR.ReadersMustBeDeltaReaders, "deltaReaders");
			}
		}
		_heapSizes = CalculateHeapSizes(baseHeapSizes, deltaReaders);
		_rowCounts = CalculateRowCounts(baseTableRowCounts, deltaReaders);
	}

	internal MetadataAggregator(RowCounts[][] rowCounts, int[][] heapSizes)
	{
		_rowCounts = ToImmutable(rowCounts);
		_heapSizes = ToImmutable(heapSizes);
	}

	private static void CalculateBaseCounts(System.Reflection.Metadata.MetadataReader baseReader, out IReadOnlyList<int> baseTableRowCounts, out IReadOnlyList<int> baseHeapSizes)
	{
		int[] array = new int[System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount];
		int[] array2 = new int[System.Reflection.Metadata.Ecma335.MetadataTokens.HeapCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = baseReader.GetTableRowCount((System.Reflection.Metadata.Ecma335.TableIndex)i);
		}
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = baseReader.GetHeapSize((HeapIndex)j);
		}
		baseTableRowCounts = array;
		baseHeapSizes = array2;
	}

	private static System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<int>> CalculateHeapSizes(IReadOnlyList<int> baseSizes, IReadOnlyList<System.Reflection.Metadata.MetadataReader> deltaReaders)
	{
		int num = 1 + deltaReaders.Count;
		int[] array = new int[num];
		int[] array2 = new int[num];
		int[] array3 = new int[num];
		int[] array4 = new int[num];
		array[0] = baseSizes[0];
		array2[0] = baseSizes[1];
		array3[0] = baseSizes[2];
		array4[0] = baseSizes[3] / 16;
		for (int i = 0; i < deltaReaders.Count; i++)
		{
			array[i + 1] = array[i] + deltaReaders[i].GetHeapSize(HeapIndex.UserString);
			array2[i + 1] = array2[i] + deltaReaders[i].GetHeapSize(HeapIndex.String);
			array3[i + 1] = array3[i] + deltaReaders[i].GetHeapSize(HeapIndex.Blob);
			array4[i + 1] = array4[i] + deltaReaders[i].GetHeapSize(HeapIndex.Guid) / 16;
		}
		return System.Collections.Immutable.ImmutableArray.Create(((IEnumerable<int>)array).ToImmutableArray(), ((IEnumerable<int>)array2).ToImmutableArray(), ((IEnumerable<int>)array3).ToImmutableArray(), ((IEnumerable<int>)array4).ToImmutableArray());
	}

	private static System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<RowCounts>> CalculateRowCounts(IReadOnlyList<int> baseRowCounts, IReadOnlyList<System.Reflection.Metadata.MetadataReader> deltaReaders)
	{
		RowCounts[][] baseRowCounts2 = GetBaseRowCounts(baseRowCounts, 1 + deltaReaders.Count);
		for (int i = 1; i <= deltaReaders.Count; i++)
		{
			CalculateDeltaRowCountsForGeneration(baseRowCounts2, i, ref deltaReaders[i - 1].EncMapTable);
		}
		return ToImmutable(baseRowCounts2);
	}

	private static System.Collections.Immutable.ImmutableArray<System.Collections.Immutable.ImmutableArray<T>> ToImmutable<T>(T[][] array) where T : notnull
	{
		System.Collections.Immutable.ImmutableArray<T>[] array2 = new System.Collections.Immutable.ImmutableArray<T>[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = ((IEnumerable<T>)array[i]).ToImmutableArray();
		}
		return ((IEnumerable<System.Collections.Immutable.ImmutableArray<T>>)array2).ToImmutableArray();
	}

	internal static RowCounts[][] GetBaseRowCounts(IReadOnlyList<int> baseRowCounts, int generations)
	{
		RowCounts[][] array = new RowCounts[System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount][];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new RowCounts[generations];
			array[i][0].AggregateInserts = baseRowCounts[i];
		}
		return array;
	}

	internal static void CalculateDeltaRowCountsForGeneration(RowCounts[][] rowCounts, int generation, ref EnCMapTableReader encMapTable)
	{
		foreach (RowCounts[] array in rowCounts)
		{
			array[generation].AggregateInserts = array[generation - 1].AggregateInserts;
		}
		int numberOfRows = encMapTable.NumberOfRows;
		for (int j = 1; j <= numberOfRows; j++)
		{
			uint token = encMapTable.GetToken(j);
			int num = (int)(token & 0xFFFFFF);
			RowCounts[] array2 = rowCounts[token >> 24];
			if (num > array2[generation].AggregateInserts)
			{
				if (num != array2[generation].AggregateInserts + 1)
				{
					throw new BadImageFormatException(System.SR.EnCMapNotSorted);
				}
				array2[generation].AggregateInserts = num;
			}
			else
			{
				array2[generation].Updates++;
			}
		}
	}

	public System.Reflection.Metadata.Handle GetGenerationHandle(System.Reflection.Metadata.Handle handle, out int generation)
	{
		if (handle.IsVirtual)
		{
			throw new NotSupportedException();
		}
		if (handle.IsHeapHandle)
		{
			int offset = handle.Offset;
			System.Reflection.Metadata.Ecma335.MetadataTokens.TryGetHeapIndex(handle.Kind, out var index);
			System.Collections.Immutable.ImmutableArray<int> array = _heapSizes[(int)index];
			int num = ((handle.Type == 114) ? (offset - 1) : offset);
			generation = array.BinarySearch(num);
			if (generation >= 0)
			{
				do
				{
					generation++;
				}
				while (generation < array.Length && array[generation] == num);
			}
			else
			{
				generation = ~generation;
			}
			if (generation >= array.Length)
			{
				throw new ArgumentException(System.SR.HandleBelongsToFutureGeneration, "handle");
			}
			int value = ((handle.Type == 114 || generation == 0) ? offset : (offset - array[generation - 1]));
			return new System.Reflection.Metadata.Handle((byte)handle.Type, value);
		}
		int rowId = handle.RowId;
		System.Collections.Immutable.ImmutableArray<RowCounts> array2 = _rowCounts[(int)handle.Type];
		generation = array2.BinarySearch(new RowCounts
		{
			AggregateInserts = rowId
		});
		if (generation >= 0)
		{
			while (generation > 0 && array2[generation - 1].AggregateInserts == rowId)
			{
				generation--;
			}
		}
		else
		{
			generation = ~generation;
			if (generation >= array2.Length)
			{
				throw new ArgumentException(System.SR.HandleBelongsToFutureGeneration, "handle");
			}
		}
		int value2 = ((generation == 0) ? rowId : (rowId - array2[generation - 1].AggregateInserts + array2[generation].Updates));
		return new System.Reflection.Metadata.Handle((byte)handle.Type, value2);
	}
}
