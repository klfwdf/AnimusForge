using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class MemoryEditorPage<T>
{
 internal readonly List<T> Matches;
 internal readonly int Page, PageCount;
 internal MemoryEditorPage(List<T> matches, int page)
 {
  Matches = matches;
  PageCount = Math.Max(1, (matches.Count + 39) / 40);
  Page = Math.Min(Math.Max(0, page), PageCount - 1);
 }
 internal IEnumerable<T> Visible => Matches.Skip(Page * 40).Take(40);
}

// UI-owned navigation only; source records are provided by the Memory domain per opening.
internal sealed class MemoryEditorController
{
 private long _generation = long.MinValue;
 internal string HistoryQuery = string.Empty, DailyQuery = string.Empty, CompressedQuery = string.Empty;
 internal int DailyPage, CompressedPage;
 internal void SynchronizeGeneration(long generation)
 {
  if (_generation == generation) return;
  _generation = generation;
  HistoryQuery = DailyQuery = CompressedQuery = string.Empty;
  DailyPage = CompressedPage = 0;
 }
 internal MemoryEditorPage<DailyMemoryDraft> OpenDailyPage(IEnumerable<DailyMemoryDraft> drafts, int page, string query, MemoryEditorDisplayPort display)
 {
  DailyQuery = (query ?? "").Trim();
  string[] terms = MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(display, DailyQuery);
  var result = new MemoryEditorPage<DailyMemoryDraft>(drafts.Where(x => MemoryEditorProjection.IsDevDailyMemoryDraftMatch(display, x, terms)).ToList(), page);
  DailyPage = result.Page;
  return result;
 }
 internal MemoryEditorPage<Tuple<int, CompressedMemoryBlock>> OpenCompressedPage(IReadOnlyList<CompressedMemoryBlock> blocks, int page, string query, MemoryEditorDisplayPort display)
 {
  CompressedQuery = (query ?? "").Trim();
  string[] terms = MemoryEditorProjection.SplitDevCompressedMemorySearchTerms(display, CompressedQuery);
  var filtered = new List<Tuple<int, CompressedMemoryBlock>>();
  for (int i = 0; i < blocks.Count; i++)
   if (MemoryEditorProjection.IsDevCompressedMemoryBlockMatch(display, blocks[i], terms)) filtered.Add(Tuple.Create(i + 1, blocks[i]));
  var result = new MemoryEditorPage<Tuple<int, CompressedMemoryBlock>>(filtered, page);
  CompressedPage = result.Page;
  return result;
 }
}
