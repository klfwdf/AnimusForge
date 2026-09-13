using System;
using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
    // Runtime-only derived index binding. Existing writers append via Record or
    // replace/clear the source during load/dev reset. Payload updates never edit
    // Day/StableKey. Preserve that contract; this is not a concurrent write API.
    private List<EventSourceMaterialEntry> _eventSourceMaterialIndexedSource;
    private Dictionary<string, EventSourceMaterialEntry> _eventSourceMaterialIndexedMap;
    private int _eventSourceMaterialIndexedCount;
    private List<EventSourceMaterialEntry>.Enumerator _eventSourceMaterialStructureProbe;

    private bool IsEventSourceMaterialIndexCurrent()
    {
        if (_eventSourceMaterialIndex == null
            || !ReferenceEquals(_eventSourceMaterialIndexedMap, _eventSourceMaterialIndex)
            || !ReferenceEquals(_eventSourceMaterialIndexedSource, _eventSourceMaterials)
            || _eventSourceMaterialIndexedCount != (_eventSourceMaterials?.Count ?? 0)) return false;
        if (_eventSourceMaterials == null) return true;
        try { _eventSourceMaterialStructureProbe.MoveNext(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    private void BindEventSourceMaterialIndex(List<EventSourceMaterialEntry> source,
        Dictionary<string, EventSourceMaterialEntry> index)
    {
        _eventSourceMaterialIndexedSource = source;
        _eventSourceMaterialIndexedMap = index;
        _eventSourceMaterialIndexedCount = source?.Count ?? 0;
        _eventSourceMaterialStructureProbe = source == null ? default(List<EventSourceMaterialEntry>.Enumerator) : source.GetEnumerator();
    }
}
