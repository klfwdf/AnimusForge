# Courier route transport checks

`run.py` reads the production `RouteTransport`, `SessionTransport`, and
`GenerationLifecycle` partials. It guards route-key/progress refresh, target
mismatch repair, arrival-before-delivery, and `DeliveryApplied`-before-commit
ordering. Bannerlord pathfinding, ports, naval runtime, and frame cost remain
`NOT-RUN`.
