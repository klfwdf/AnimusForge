# Courier route transport checks

`run.py` reads the production `RouteTransport`, `SessionTransport`, and
`GenerationLifecycle` partials. It guards route-key/progress refresh, target
mismatch repair, arrival-before-delivery, and the reviewed one-shot arrival
reservation introduced in `b2572623`: `DeliveryApplied` precedes payload,
payload/history receipts precede reply commit, and the reentry guard releases.
Bannerlord pathfinding, ports, naval runtime, and frame cost remain `NOT-RUN`.

`python -B tests/modules/AF.Module.Conversation/CourierRouteTransportTests/run_naval.py`
extracts the current production naval/route-plan methods and executes them in
a repository-local .NET 8 fixture. `NavalHarness.cs.txt` supplies explicit
game/model/ship-action leaf fakes; no historical inverse or production rewrite
is used. Checks cover actual DLC enablement vs installed resources, a missing
water model/campaign, reachable land with optional sea legs, disconnected land,
sea targets/return, temporary boat ownership/reuse/load protection/cleanup,
player fleet isolation, missing hulls, unsafe owners and assignment/model
failure. It also checks all five routed consumers provision before their native
go-to commands and forbids world scans in the DLC gate. Native pathfinding,
port transitions, actual save/load and live-game performance remain `NOT-RUN`.
