using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.Engine
{
    /// <summary>
    /// Bounded, immutable description of authored geometry. This reader only touches files
    /// and managed value types; the caller owns native asset lookup and runtime validation.
    /// </summary>
    internal sealed class SceneResourceGeometryPlan
    {
        internal const int MaximumFileBytes = 8 * 1024 * 1024;
        // File work, live DOM memory and compact geometry plans are different budgets.
        internal const int MaximumXmlElements = 524288;
        internal const int MaximumSubtreeElements = 32768;
        internal const int MaximumDepth = 64;
        internal const int MaximumEntries = 32768;
        private const int MaximumSamples = 24;
        private const int MaximumAssetNameLength = 512;
        private static readonly object CacheLock = new object();
        private static SceneResourceGeometryPlan _cachedPlan;
        private readonly long _fileLength;
        private readonly DateTime _fileWriteTimeUtc;

        internal sealed class Entry
        {
            internal string SourceId { get; }
            internal string ParentSourceId { get; }
            internal string SourceName { get; }
            internal string PrefabName { get; }
            internal string MeshName { get; }
            internal MatrixFrame WorldFrame { get; }
            internal uint LevelMask { get; }

            internal Entry(string sourceId, string parentSourceId, string sourceName,
                string prefabName, string meshName, MatrixFrame worldFrame, uint levelMask)
            {
                SourceId = sourceId;
                ParentSourceId = parentSourceId;
                SourceName = sourceName;
                PrefabName = prefabName;
                MeshName = meshName;
                WorldFrame = worldFrame;
                LevelMask = levelMask;
            }
        }

        internal string ResolvedSceneFile { get; }
        internal uint ActiveMask { get; }
        internal uint ImplicitBaseMask { get; }
        internal IReadOnlyList<Entry> Entries { get; }
        internal int InspectedNodes { get; }
        internal int XmlElementCount { get; }
        internal int PeakSubtreeElements { get; }
        internal int SkippedByLevel { get; }
        internal int SkippedUnsupported { get; }
        internal int SkippedInvisible { get; }
        internal IReadOnlyDictionary<string, int> UnsupportedReasons { get; }
        internal IReadOnlyList<string> UnsupportedSamples { get; }

        private SceneResourceGeometryPlan(string path, long length, DateTime writeTime, uint activeMask, Reader reader)
        {
            ResolvedSceneFile = path;
            _fileLength = length;
            _fileWriteTimeUtc = writeTime;
            ActiveMask = activeMask;
            ImplicitBaseMask = reader.BaseMask;
            Entries = new ReadOnlyCollection<Entry>(reader.Entries.ToArray());
            InspectedNodes = reader.EntityCount;
            XmlElementCount = reader.ElementCount;
            PeakSubtreeElements = reader.PeakSubtreeElements;
            SkippedByLevel = reader.SkippedByLevel;
            SkippedUnsupported = reader.SkippedUnsupported;
            SkippedInvisible = reader.SkippedInvisible;
            UnsupportedReasons = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(reader.Reasons));
            UnsupportedSamples = new ReadOnlyCollection<string>(reader.Samples.ToArray());
        }

        internal static SceneResourceGeometryPlan Load(string resolvedSceneFile, uint activeMask, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(resolvedSceneFile))
                throw new InvalidDataException("未取得当前场景资源文件的实际路径。");
            try
            {
                string path = Path.GetFullPath(resolvedSceneFile);
                var before = new FileInfo(path);
                if (!before.Exists) throw new FileNotFoundException("当前场景资源文件不存在。", path);
                long length = before.Length;
                DateTime writeTime = before.LastWriteTimeUtc;
                if (length <= 0 || length > MaximumFileBytes)
                    throw new InvalidDataException("场景资源为空或超过8MiB读取预算。");
                lock (CacheLock)
                {
                    token.ThrowIfCancellationRequested();
                    if (_cachedPlan != null && _cachedPlan.ActiveMask == activeMask &&
                        _cachedPlan._fileLength == length && _cachedPlan._fileWriteTimeUtc == writeTime &&
                        string.Equals(_cachedPlan.ResolvedSceneFile, path, StringComparison.OrdinalIgnoreCase))
                        return _cachedPlan;
                }

                var reader = new Reader(activeMask, token);
                // No native objects or callbacks are reachable from this worker-side reader.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length != length) throw new InvalidDataException("场景资源在读取前发生了变化，请重新采集。");
                    reader.Read(stream);
                }
                token.ThrowIfCancellationRequested();
                var after = new FileInfo(path);
                if (!after.Exists || after.Length != length || after.LastWriteTimeUtc != writeTime)
                    throw new InvalidDataException("场景资源在读取期间发生了变化，请重新采集。");
                var plan = new SceneResourceGeometryPlan(path, length, writeTime, activeMask, reader);
                lock (CacheLock)
                {
                    token.ThrowIfCancellationRequested();
                    _cachedPlan = plan;
                }
                return plan;
            }
            catch (OperationCanceledException) { throw; }
            catch (XmlException ex) { throw new InvalidDataException("无法解析当前场景XML资源：" + ex.Message, ex); }
            catch (UnauthorizedAccessException ex) { throw new InvalidDataException("无法读取当前场景资源文件，访问被拒绝。", ex); }
            catch (IOException ex)
            {
                throw new InvalidDataException("读取当前场景资源失败：" + ex.Message, ex);
            }
        }

        private sealed class Reader
        {
            internal readonly List<Entry> Entries = new List<Entry>();
            internal readonly Dictionary<XmlElement, string> EntityIds = new Dictionary<XmlElement, string>();
            internal readonly Dictionary<string, int> Reasons = new Dictionary<string, int>(StringComparer.Ordinal);
            internal readonly List<string> Samples = new List<string>();
            internal int ElementCount;
            internal int EntityCount;
            internal int PeakSubtreeElements;
            internal int SkippedByLevel;
            internal int SkippedUnsupported;
            internal int SkippedInvisible;
            internal uint BaseMask;
            private readonly uint _activeMask;
            private readonly CancellationToken _token;
            private readonly Dictionary<string, uint> _levels = new Dictionary<string, uint>(StringComparer.Ordinal);

            internal Reader(uint activeMask, CancellationToken token) { _activeMask = activeMask; _token = token; }

            internal void Read(Stream stream)
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreWhitespace = true,
                    MaxCharactersInDocument = MaximumFileBytes,
                    CloseInput = false
                };
                // First pass stores only the global level definitions. They may occur
                // after entities in third-party files; never assume XML section order.
                bool sawScene = false, sawEntities = false, sawLevels = false;
                using (XmlReader xml = XmlReader.Create(stream, settings))
                {
                    while (ReadNext(xml))
                    {
                        if (xml.NodeType != XmlNodeType.Element) continue;
                        if (xml.Depth == 0)
                        {
                            if (xml.Name != "scene") throw new InvalidDataException("当前资源不是有效的scene XML。");
                            sawScene = true;
                        }
                        if (xml.Depth == 1 && xml.Name == "entities")
                        {
                            if (sawEntities) throw new InvalidDataException("场景资源包含重复的entities节点。");
                            sawEntities = true;
                        }
                        if (xml.Depth == 1 && xml.Name == "levels")
                        {
                            if (sawLevels) throw new InvalidDataException("场景资源包含重复的levels节点。");
                            sawLevels = true;
                            XmlElement levels = ReadSubtree(xml);
                            var root = levels.OwnerDocument.CreateElement("scene");
                            levels.OwnerDocument.RemoveChild(levels);
                            root.AppendChild(levels);
                            ReadLevels(root);
                            EntityIds.Clear();
                        }
                    }
                }
                if (!sawScene || !sawEntities) throw new InvalidDataException("当前场景资源缺少scene或entities节点。");

                // Rewind this same read-locked file. Each root subtree is validated by
                // the existing hierarchy/override rules, then released immediately.
                // No whole-scene XmlDocument or dictionary of all XmlElements survives.
                stream.Position = 0;
                ElementCount = 0;
                EntityCount = 0;
                bool inEntities = false;
                using (XmlReader xml = XmlReader.Create(stream, settings))
                {
                    while (ReadNext(xml))
                    {
                        if (xml.Depth == 1 && xml.Name == "entities")
                            inEntities = xml.NodeType == XmlNodeType.Element && !xml.IsEmptyElement;
                        if (!inEntities || xml.Depth != 2 || xml.NodeType != XmlNodeType.Element) continue;
                        if (xml.Name != "game_entity")
                        { Unsupported("entities", string.Empty, "unsupported_root_entity_node"); continue; }
                        XmlElement entity = ReadSubtree(xml);
                        Visit(entity, MatrixFrame.Identity, uint.MaxValue, false, string.Empty);
                        EntityIds.Clear();
                    }
                }
            }

            private bool ReadNext(XmlReader xml)
            {
                _token.ThrowIfCancellationRequested();
                if (!xml.Read()) return false;
                if (xml.Depth > MaximumDepth) throw new InvalidDataException("场景XML层级超过64层预算。");
                if (xml.NodeType == XmlNodeType.Element)
                {
                    if (++ElementCount > MaximumXmlElements)
                        throw new InvalidDataException("场景XML流式扫描超过524288个元素预算。");
                    if (!string.IsNullOrEmpty(xml.NamespaceURI))
                        throw new InvalidDataException("场景XML使用了不支持的命名空间。");
                    if (xml.Name == "game_entity") EntityCount++;
                }
                return true;
            }

            // Leaves xml on the subtree's end element (or its empty start element).
            private XmlElement ReadSubtree(XmlReader xml)
            {
                int startDepth = xml.Depth, elements = 0;
                var document = new XmlDocument { XmlResolver = null };
                XmlElement current = null;
                do
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (++elements > MaximumSubtreeElements)
                            throw new InvalidDataException("单个场景实体子树超过32768个元素内存预算。");
                        PeakSubtreeElements = Math.Max(PeakSubtreeElements, elements);
                        var element = document.CreateElement(xml.Name);
                        if (xml.HasAttributes)
                        {
                            while (xml.MoveToNextAttribute()) element.SetAttribute(xml.Name, xml.Value);
                            xml.MoveToElement();
                        }
                        if (current == null) document.AppendChild(element);
                        else current.AppendChild(element);
                        if (element.Name == "game_entity")
                            EntityIds.Add(element, "xml:" + ElementCount.ToString(CultureInfo.InvariantCulture));
                        if (!xml.IsEmptyElement) current = element;
                        else if (xml.Depth == startDepth) return document.DocumentElement;
                    }
                    else if (xml.NodeType == XmlNodeType.EndElement)
                    {
                        if (xml.Depth == startDepth) return document.DocumentElement;
                        current = current?.ParentNode as XmlElement;
                    }
                    else if (xml.NodeType == XmlNodeType.Text || xml.NodeType == XmlNodeType.CDATA)
                        current?.AppendChild(document.CreateTextNode(xml.Value));
                } while (ReadNext(xml));
                throw new InvalidDataException("场景实体子树意外结束。");
            }

            private void ReadLevels(XmlElement root)
            {
                XmlElement levels = SingleChild(root, "levels");
                if (levels == null) return;
                foreach (XmlNode child in levels.ChildNodes)
                {
                    _token.ThrowIfCancellationRequested();
                    if (!(child is XmlElement level) || level.Name != "level")
                        throw new InvalidDataException("场景升级层级定义无效。");
                    string name = level.GetAttribute("name");
                    if (string.IsNullOrWhiteSpace(name) || !uint.TryParse(level.GetAttribute("mask"),
                        NumberStyles.None, CultureInfo.InvariantCulture, out uint mask) || mask == 0 || _levels.ContainsKey(name))
                        throw new InvalidDataException("场景升级层级名称或mask定义无效。");
                    _levels.Add(name, mask);
                    if (string.Equals(name, "base", StringComparison.OrdinalIgnoreCase)) BaseMask = mask;
                }
            }

            private void Visit(XmlElement entity, MatrixFrame parentFrame, uint parentMask, bool parentHasLevels, string parentId)
            {
                _token.ThrowIfCancellationRequested();
                string id = EntityIds[entity];
                string name = entity.GetAttribute("name");
                string prefab = entity.GetAttribute("prefab");
                string sourceName = Short(string.IsNullOrEmpty(name) ? prefab : name, 160);
                if (entity.HasAttribute("visible"))
                {
                    if (!bool.TryParse(entity.GetAttribute("visible"), out bool visible))
                    { Unsupported(id, sourceName, "invalid_visibility"); return; }
                    if (!visible) { SkippedInvisible++; return; }
                }
                string mobility = entity.GetAttribute("mobility");
                if (!string.IsNullOrEmpty(mobility) && mobility != "0" && !string.Equals(mobility, "stationary", StringComparison.OrdinalIgnoreCase))
                { Unsupported(id, sourceName, "non_static_mobility"); return; }
                foreach (XmlAttribute attribute in entity.Attributes)
                {
                    switch (attribute.Name)
                    {
                        case "name": case "prefab": case "old_prefab_name": case "guid": case "_index_":
                        case "visible": case "mobility": case "occlusion_body_name": break;
                        default: Unsupported(id, sourceName, "unknown_entity_attribute"); return;
                    }
                }
                foreach (XmlNode child in entity.ChildNodes)
                {
                    if (!(child is XmlElement part)) continue;
                    switch (part.Name)
                    {
                        case "transform": case "levels": case "components": case "children":
                        case "physics": case "tags": case "edit_mode_data": break;
                        // Scripts and render/animation flags cannot safely be replayed from
                        // authored data when their current runtime state is unavailable.
                        default: Unsupported(id, sourceName, "unsupported_entity_section"); return;
                    }
                }
                if (!TryLevelMask(entity, parentMask, parentHasLevels, out uint mask, out bool hasLevels))
                { Unsupported(id, sourceName, "unknown_or_invalid_levels"); return; }
                // Mission masks include the implicit base layer (observed 21=1|4|16),
                // while visible authored civilian furniture declares 30=2|4|8|16.
                // Match the selected variant bits, not a mandatory explicit base tag.
                uint requiredVariants = _activeMask & ~BaseMask;
                if ((_activeMask == 0 && hasLevels) ||
                    (hasLevels && mask != uint.MaxValue &&
                     (requiredVariants == 0 || (mask & requiredVariants) != requiredVariants)))
                { SkippedByLevel++; return; }
                if (!TryFrame(entity, parentFrame, out MatrixFrame world))
                { Unsupported(id, sourceName, "invalid_or_unsupported_transform"); return; }

                XmlElement components = SingleChild(entity, "components");
                XmlElement children = SingleChild(entity, "children");
                if (entity.HasAttribute("prefab"))
                {
                    // A linked prefab may contain an arbitrary tree. Its source asset is
                    // resolved by the native loader; do not guess instance-override merges.
                    if (!ValidAssetName(prefab)) { Unsupported(id, sourceName, "invalid_prefab_name"); return; }
                    if (components != null || children != null)
                    { Unsupported(id, sourceName, "prefab_instance_override"); return; }
                    Add(new Entry(id, parentId, sourceName, prefab, null, world, mask));
                    return;
                }
                if (components != null)
                {
                    foreach (XmlNode child in components.ChildNodes)
                    {
                        if (!(child is XmlElement component)) continue;
                        if (component.Name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            component.Name.IndexOf("animation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            component.Name.IndexOf("cloth", StringComparison.OrdinalIgnoreCase) >= 0)
                        { Unsupported(id, sourceName, "animated_or_cloth_component"); return; }
                    }
                    int componentIndex = 0;
                    foreach (XmlNode child in components.ChildNodes)
                    {
                        _token.ThrowIfCancellationRequested();
                        if (!(child is XmlElement component)) continue;
                        componentIndex++;
                        string meshId = id + "/mesh:" + componentIndex.ToString(CultureInfo.InvariantCulture);
                        if (component.Name != "meta_mesh_component" || component.Attributes.Count != 1 ||
                            !component.HasAttribute("name") || component.HasChildNodes || !ValidAssetName(component.GetAttribute("name")))
                        { Unsupported(meshId, sourceName, "unsupported_mesh_component"); continue; }
                        Add(new Entry(meshId, parentId, sourceName, null, component.GetAttribute("name"), world, mask));
                    }
                }
                if (children == null) return;
                foreach (XmlNode child in children.ChildNodes)
                {
                    if (child is XmlElement nested && nested.Name == "game_entity")
                        Visit(nested, world, mask, hasLevels, id);
                    else if (child is XmlElement) Unsupported(id, sourceName, "unsupported_child_node");
                }
            }

            private bool TryLevelMask(XmlElement entity, uint parentMask, bool parentHasLevels, out uint mask, out bool hasLevels)
            {
                mask = parentMask;
                XmlElement levels = SingleChild(entity, "levels");
                hasLevels = parentHasLevels || levels != null;
                if (levels == null) return true;
                uint own = 0;
                foreach (XmlNode child in levels.ChildNodes)
                {
                    if (!(child is XmlElement level) || level.Name != "level" || !_levels.TryGetValue(level.GetAttribute("name"), out uint bit))
                        return false;
                    own |= bit;
                }
                if (own == 0) return false;
                // An explicitly base-only container is common to all variants;
                // its children may still select their own civilian/siege/level bits.
                if (BaseMask != 0 && own == BaseMask) own = uint.MaxValue;
                mask &= own;
                return true;
            }

            private static bool TryFrame(XmlElement entity, MatrixFrame parent, out MatrixFrame world)
            {
                world = MatrixFrame.Identity;
                XmlElement transform = SingleChild(entity, "transform");
                MatrixFrame local = MatrixFrame.Identity;
                if (transform != null)
                {
                    if (transform.HasChildNodes) return false;
                    foreach (XmlAttribute attribute in transform.Attributes)
                        if (attribute.Name != "position" && attribute.Name != "rotation_euler" && attribute.Name != "scale") return false;
                    if (transform.HasAttribute("position"))
                    {
                        if (!TryVector(transform.GetAttribute("position"), out Vec3 position)) return false;
                        local.origin = position;
                    }
                    if (transform.HasAttribute("rotation_euler"))
                    {
                        if (!TryVector(transform.GetAttribute("rotation_euler"), out Vec3 angles)) return false;
                        local.rotation.ApplyEulerAngles(angles);
                    }
                    if (transform.HasAttribute("scale"))
                    {
                        if (!TryVector(transform.GetAttribute("scale"), out Vec3 scale) || scale.x == 0 || scale.y == 0 || scale.z == 0) return false;
                        local.rotation.ApplyScaleLocal(scale);
                    }
                }
                world = parent.TransformToParent(local);
                return Finite(world.origin) && Finite(world.rotation.s) && Finite(world.rotation.f) && Finite(world.rotation.u);
            }

            private void Add(Entry entry)
            {
                if (Entries.Count >= MaximumEntries) throw new InvalidDataException("场景静态几何计划超过32768项预算。");
                Entries.Add(entry);
            }

            private void Unsupported(string id, string name, string reason)
            {
                SkippedUnsupported++;
                Reasons.TryGetValue(reason, out int count);
                Reasons[reason] = count + 1;
                if (Samples.Count < MaximumSamples) Samples.Add(id + " " + Short(name, 160) + " " + reason);
            }

            private static XmlElement SingleChild(XmlElement parent, string name)
            {
                XmlElement found = null;
                foreach (XmlNode child in parent.ChildNodes)
                {
                    if (!(child is XmlElement element) || element.Name != name) continue;
                    if (found != null) throw new InvalidDataException("场景资源包含重复的" + name + "节点。");
                    found = element;
                }
                return found;
            }
        }

        private static bool TryVector(string text, out Vec3 value)
        {
            value = Vec3.Zero;
            string[] parts = text.Split(',');
            if (parts.Length != 3 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) return false;
            value = new Vec3(x, y, z);
            return Finite(value);
        }

        private static bool Finite(Vec3 value)
            => !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static bool ValidAssetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumAssetNameLength) return false;
            foreach (char character in name) if (char.IsControl(character)) return false;
            return true;
        }

        private static string Short(string value, int maximum) => value == null ? string.Empty : value.Length <= maximum ? value : value.Substring(0, maximum);
    }
}
