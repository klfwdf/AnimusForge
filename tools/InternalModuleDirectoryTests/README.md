# Internal module directory contract tests

This executable links the actual production file `Refactor/Modules/InternalModuleDirectory.cs`. It does not copy the registry algorithm, load Bannerlord, install modules, execute handlers, call network services or change FeatureBridge switches.

## Run

From `G:\AFMOD\AF-REFACTOR`:

```powershell
$env:DOTNET_ROOT = 'G:\AFMOD\.dotnet-sdk'
$env:DOTNET_CLI_HOME = 'G:\AFMOD\AF-REFACTOR\.tmp\dotnet-cli'
$env:NUGET_PACKAGES = 'G:\AFMOD\AF-REFACTOR\.tmp\nuget-packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& 'G:\AFMOD\.dotnet-sdk\dotnet.exe' run --project tools/InternalModuleDirectoryTests/InternalModuleDirectoryTests.csproj -c Release
```

Target: `net8.0`, no package dependencies. The local test-only NuGet configuration has no remote sources. Production types remain `internal` inside the existing `AnimusForge.dll`; the test executable links them only to inspect and exercise their contract.

## Contract

- Definitions copy their source collections into read-only collections. IDs use ordinal, case-sensitive matching. Module/capability/dependency versions use exact positive integer contract versions, not SemVer ranges or game/assembly versions.
- Registration is explicit and bounded (64 modules / 256 total capabilities). A rejected duplicate declaration is not partly installed, cannot replace an accepted provider and must not be installed by the composition caller. Registration failures are returned immediately; rejected objects are not part of the frozen graph.
- `CompleteRegistration()` freezes the accepted definitions. Missing/version-incompatible/cyclic dependencies invalidate the affected module and its consumers; independent valid modules remain queryable even when the overall validation result is invalid.
- State starts `NotInitialized`. Only the composition/lifecycle owner reports `Ready`, `Unavailable` or `Failed`, after registration is complete. The snapshot separates definition validation from runtime binding state and preserves old snapshots unchanged.
- Capability `Available` means **an explicitly registered adapter has been reported ready, its dependency closure is ready and its existing FeatureBridge gates currently allow routing**. It does not mean a Campaign/Mission is active, any gameplay target is authorized, or live acceptance passed.
- `bridgeKnown` validates IDs against the existing, process-stable FeatureBridge catalog. `bridgeRejectionReason` queries those existing gates: **only null allows routing**; empty strings, failures and exceptions reject. Gate adapters execute outside the directory lock; binding state is rechecked afterwards. No replacement configuration, runtime enable switch or handler container is introduced.
- The directory does not intercept legacy `ForExternal` calls. Typed existing adapters still use their original gates and per-request generation/session/target/main-thread guards. This directory cannot retroactively authorize or block all historical code paths.
- Required-module readiness is not an implicit grant of every capability supplied by that dependency. Capability-specific gates are evaluated when querying that capability, not by scanning all unrelated providers.
- Dependency closures and validation are precomputed once; queries do not rediscover assemblies or rebuild graphs. Snapshots are cached until accepted registration, freeze or a real state/reason change.

## Coverage

44 cases: valid and invalid definitions, immutable source/snapshot data, duplicate atomicity, missing/version-incompatible dependencies, self/ring/diamond graphs, transitive readiness and unrelated-module availability, initialization distinction, freeze/idempotency, precise query statuses, gate precedence/exception/empty rejection, lock-free injected callbacks, reentrant state change, bounded catalogs, concurrent duplicate attempts and no unrelated query work.

No gameplay or current adapter binding is proven by this suite. The composition root and public query mapping require their own integration verification. No new API surface or module business implementation is supplied here.
