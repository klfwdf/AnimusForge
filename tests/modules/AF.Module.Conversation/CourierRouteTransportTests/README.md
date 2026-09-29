# Courier route transport checks

`run.py` reads the production `RouteTransport`, `SessionTransport`, and
`GenerationLifecycle` partials. It guards route-key/progress refresh, target
mismatch repair, arrival-before-delivery, and the reviewed one-shot arrival
reservation introduced in `b2572623`: `DeliveryApplied` precedes payload,
payload/history receipts precede reply commit, and the reentry guard releases.
Bannerlord pathfinding, ports, naval runtime, and frame cost remain `NOT-RUN`.
