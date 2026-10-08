# Localization Inventory

Offline, read-only extraction for `AF-ENGLISH`. It does not translate or rewrite product files.

## Run

Requirements: Python 3, Git, a .NET 8 SDK with its bundled Roslyn assemblies. No new package dependency is used.

```powershell
python tools/LocalizationInventory/extract_text.py --dotnet G:\AFMOD\.dotnet-sdk\dotnet.exe
$env:AF_INVENTORY_DOTNET='G:\AFMOD\.dotnet-sdk\dotnet.exe'
python -m unittest discover -s tools/LocalizationInventory -p test_extract_text.py -v
```

The extractor evaluates MSBuild compile/embedded inputs for both API lines and Bootstrap, parses C# using Roslyn, and inventories mapped and explicitly identified secondary text resources. SDK scratch/build outputs remain under this tool's `obj`/`bin` folders.

## Outputs

Written to `docs/localization/AF-ENGLISH/inventory/`:

- `occurrences.jsonl`: all extracted strings, including existing English, technical keys, comments and conditional trivia.
- `han-review.jsonl`: decoded records containing Han characters. These are **not** all safe translation candidates.
- `unique-han-texts.jsonl`: exact-text deduplication retaining every occurrence ID and category.
- `han-source-lines.tsv`: independent raw Han line audit; source-level Chinese identifiers/directives and resource scaffolding stay visible here.
- `manifest.json`: selected source hashes, sizes and surface classification.
- `excluded-worldbook.json`: deferred worldbook paths and hashes, **no book body text**.
- `summary.json`: source revision, counts and limitations.

The four large streams also have deterministic `.gz` versions. Git tracks those complete compressed streams, not the oversized plain views; ordinary local files remain available after a run. For example, `gzip.open(path, 'rt', encoding='utf-8')` reads a tracked JSONL stream without a separate extraction step. Compression is inventory evidence storage, not a game release package.

Coordinates are one-based lines/columns; offsets are zero-based. Roslyn offsets use UTF-16 code units; Python resource offsets use Unicode code points; `offset_unit` identifies the representation. JSON context is an RFC 6901-style pointer. XML context is lexical, not a semantic XPath.

The exclusion is intentionally narrow: the `knowledge` subtree and `KnowledgeRules.json`, not all `PlayerExports`, not all world-related modules, and not the world's book editor/loader UI code.

Classification is conservative, advisory and static. All rows require review. Runtime reachability, image/OCR text, native binaries, private player data, semantic translation and game acceptance are outside this tool's claims. Disabled C# trivia is preserved as review-only block text; currently active 1.3/1.4 strings are tokenized separately. JSON string decoding preserves literal control characters/newlines found in Json.NET-style baseline presets; extraction is not strict JSON validity certification.
