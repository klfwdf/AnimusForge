using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Customization;

internal sealed partial class ExecutionSiteBuilderController
{
    private ExecutionPresetTransform? _placementTransform;
    private float _placementHeightOffset;

    private void BeginRootPlacement(ExecutionSitePreset preset)
    {
        RemoveAllPreviews();
        _undo.Clear();
        _redo.Clear();
        _preset = preset;
        _rootPlaced = false;
        _rootPosition = _mission.MainAgent?.Position ?? Agent.Main?.Position ?? Vec3.Zero;
        var look = _mission.MainAgent?.LookDirection.AsVec2 ?? Agent.Main?.LookDirection.AsVec2 ?? Vec2.Forward;
        _rootForward = look.IsNonZero() ? look.Normalized() : Vec2.Forward;
        _rootHeightOffset = preset.RootAnchor.Z;
        _rootPosition.z += _rootHeightOffset;
        RotateRoot(preset.RootAnchor.Yaw, updatePreset: false);
        _movingSelected = false;
        _selectedPieceId = null;
        _selectedMarkerId = null;
        _state = ExecutionSiteBuilderState.Placing;
        RebuildPreview();
        Status("{=REX_Builder_Place_Root}Place the complete ghost layout. Mouse: X/Y; wheel: root yaw; Shift+wheel: root height; left click confirms.");
    }

    private void TickPlacementInput()
    {
        if (_preset is null || _view is null) return;
        if (Input.IsKeyPressed(InputKey.Escape) || Input.IsKeyPressed(InputKey.RightMouseButton))
        {
            CancelPendingPlacement();
            return;
        }
        if (_view.TryGetMouseSurfacePosition(out var target, out _))
        {
            if (!_rootPlaced)
            {
                _rootPosition = target;
                _rootPosition.z += _rootHeightOffset;
                UpdateAllPreviewFrames();
            }
            else if (_movingSelected && GetSelectedTransform() is { } selectedTransform)
            {
                SetRelativePosition(selectedTransform, target);
                UpdateAllPreviewFrames();
            }
            else if (_pendingPiece is not null)
            {
                SetRelativePosition(_pendingPiece.Transform, target);
                UpdatePendingPreview();
            }
            else if (_pendingMarker is not null)
            {
                SetRelativePosition(_pendingMarker.Transform, target);
                UpdatePendingPreview();
            }
        }
        HandleTransformInput();
        if (!Input.IsKeyPressed(InputKey.LeftMouseButton)) return;
        var point = GetPendingWorldPosition();
        if (!_mission.IsPositionInsideBoundaries(point.AsVec2))
        {
            Status("{=REX_Builder_Outside_Bounds}The selected point is outside the mission boundary.");
            return;
        }
        ConfirmPendingPlacement();
    }

    private void HandleTransformInput()
    {
        var step = Input.IsKeyPressed(InputKey.MouseScrollUp) ? 1 : Input.IsKeyPressed(InputKey.MouseScrollDown) ? -1 : 0;
        var shift = Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
        var control = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        if (step != 0)
        {
            if (!_rootPlaced)
            {
                if (shift)
                {
                    _rootHeightOffset += step * 0.10f;
                    _rootPosition.z += step * 0.10f;
                    if (_preset is not null) _preset.RootAnchor.Z = _rootHeightOffset;
                }
                else RotateRoot(step * 10f);
            }
            else if (GetActiveTransform() is { } transform)
            {
                if (control) transform.Scale = Clamp(transform.Scale + step * 0.05f, 0.10f, 5f);
                else if (shift) AdjustPlacementHeight(transform, step * 0.10f);
                else transform.Yaw += step * 10f;
            }
        }
        if (GetActiveTransform() is { } active)
        {
            if (Input.IsKeyPressed(InputKey.Q)) active.Pitch -= 5f;
            if (Input.IsKeyPressed(InputKey.E)) active.Pitch += 5f;
            if (Input.IsKeyPressed(InputKey.Z)) active.Roll -= 5f;
            if (Input.IsKeyPressed(InputKey.C)) active.Roll += 5f;
        }
        UpdatePendingPreview();
        if (!_rootPlaced || _movingSelected) UpdateAllPreviewFrames();
    }

    private void ConfirmPendingPlacement()
    {
        if (_preset is null) return;
        ResetPlacementHeight();
        if (!_rootPlaced)
        {
            _rootPlaced = true;
            _state = ExecutionSiteBuilderState.Editing;
            Status("{=REX_Builder_Root_Placed}Root placed. Press B to configure personnel, add decoration or finish deployment.");
            return;
        }
        if (_movingSelected)
        {
            _movingSelected = false;
        }
        else if (_pendingPiece is not null)
        {
            _preset.Pieces.Add(_pendingPiece);
            if (_pendingPreviewEntity is not null)
            {
                _piecePreviewEntities[_pendingPiece.Id] = _pendingPreviewEntity;
                _pendingPreviewEntity = null;
            }
            _selectedPieceId = _pendingPiece.Id;
            _pendingPiece = null;
        }
        else if (_pendingMarker is not null)
        {
            var duplicate = _preset.Markers.FirstOrDefault(marker => string.Equals(marker.RoleId, _pendingMarker.RoleId, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                RemovePreview(_markerPreviewEntities, duplicate.Id);
                _preset.Markers.Remove(duplicate);
            }
            _preset.Markers.Add(_pendingMarker);
            if (_pendingPreviewEntity is not null)
            {
                _markerPreviewEntities[_pendingMarker.Id] = _pendingPreviewEntity;
                _pendingPreviewEntity = null;
            }
            _selectedMarkerId = _pendingMarker.Id;
            _pendingMarker = null;
        }
        _state = ExecutionSiteBuilderState.Editing;
        Status("{=REX_Builder_Part_Placed}Part placed. Press B for more actions.");
    }

    private void CancelPendingPlacement()
    {
        RemovePendingPreview();
        ResetPlacementHeight();
        _pendingPiece = null;
        _pendingMarker = null;
        if (_movingSelected)
        {
            _movingSelected = false;
            if (_undo.Count > 0)
            {
                _preset = _undo.Pop();
                RebuildPreview();
            }
            _state = ExecutionSiteBuilderState.Editing;
        }
        else if (!_rootPlaced)
        {
            _preset = null;
            RemoveAllPreviews();
            _state = ExecutionSiteBuilderState.WaitingForBuilder;
        }
        else
        {
            if (_undo.Count > 0)
            {
                // Selecting a replacement functional piece removes its old
                // counterpart before placement, so cancellation restores it.
                _preset = _undo.Pop();
                RebuildPreview();
            }
            _state = ExecutionSiteBuilderState.Editing;
        }
        Status("{=REX_Builder_Placement_Cancelled}Current placement cancelled. No execution consequences were committed.");
    }

    private void TickEditingShortcuts()
    {
        if (_preset is null || !_rootPlaced) return;
        var control = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
        if (control && Input.IsKeyPressed(InputKey.Z)) { Undo(); return; }
        if (control && Input.IsKeyPressed(InputKey.Y)) { Redo(); return; }
        if (Input.IsKeyPressed(InputKey.Delete)) { DeleteSelected(); return; }
        if (Input.IsKeyPressed(InputKey.R) && GetSelectedTransform() is { } reset)
        {
            PushUndo(); reset.Pitch = reset.Yaw = reset.Roll = 0f; reset.Scale = 1f;
            UpdateAllPreviewFrames();
            Status("{=REX_Builder_Reset_Part}Selected part rotation and scale reset.");
            return;
        }
        var step = Input.IsKeyPressed(InputKey.MouseScrollUp) ? 1 : Input.IsKeyPressed(InputKey.MouseScrollDown) ? -1 : 0;
        var changed = step != 0 || Input.IsKeyPressed(InputKey.Q) || Input.IsKeyPressed(InputKey.E) || Input.IsKeyPressed(InputKey.Z) || Input.IsKeyPressed(InputKey.C);
        if (!changed || GetSelectedTransform() is not { } selected) return;
        PushUndo();
        var shift = Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
        if (step != 0)
        {
            if (control) selected.Scale = Clamp(selected.Scale + step * 0.05f, 0.10f, 5f);
            else if (shift) selected.Z += step * 0.10f;
            else selected.Yaw += step * 10f;
        }
        if (!control)
        {
            if (Input.IsKeyPressed(InputKey.Q)) selected.Pitch -= 5f;
            if (Input.IsKeyPressed(InputKey.E)) selected.Pitch += 5f;
            if (Input.IsKeyPressed(InputKey.Z)) selected.Roll -= 5f;
            if (Input.IsKeyPressed(InputKey.C)) selected.Roll += 5f;
        }
        UpdateAllPreviewFrames();
    }

    private void SelectNextEditable()
    {
        var ids = _preset!.Pieces.Select(piece => (piece.Id, Piece: true))
            .Concat(_preset.Markers.Select(marker => (marker.Id, Piece: false))).ToList();
        if (ids.Count == 0) { Status("{=REX_Builder_Nothing_To_Edit}There are no parts or markers to edit."); return; }
        var current = ids.FindIndex(item => item.Piece ? item.Id == _selectedPieceId : item.Id == _selectedMarkerId);
        var next = ids[(current + 1 + ids.Count) % ids.Count];
        _selectedPieceId = next.Piece ? next.Id : null;
        _selectedMarkerId = next.Piece ? null : next.Id;
        var label = next.Piece ? _preset.Pieces.First(x => x.Id == next.Id).ResourceKey : _preset.Markers.First(x => x.Id == next.Id).RoleId;
        PushUndo();
        ResetPlacementHeight();
        _movingSelected = true;
        _state = ExecutionSiteBuilderState.Placing;
        var text = new TaleWorlds.Localization.TextObject("{=REX_Builder_Selected}Selected: {ITEM}. Move the mouse for X/Y; use wheel/Q/E/Z/C/R/Delete, left click confirms, right click cancels.");
        text.SetTextVariable("ITEM", label); Status(text);
    }

    private void DeleteSelected()
    {
        if (_selectedPieceId is Guid pieceId)
        {
            var piece = _preset!.Pieces.FirstOrDefault(x => x.Id == pieceId);
            if (piece is null) return;
            PushUndo(); _preset.Pieces.Remove(piece); RemovePreview(_piecePreviewEntities, pieceId); _selectedPieceId = null;
        }
        else if (_selectedMarkerId is Guid markerId)
        {
            var marker = _preset!.Markers.FirstOrDefault(x => x.Id == markerId);
            if (marker is null) return;
            PushUndo(); _preset.Markers.Remove(marker); RemovePreview(_markerPreviewEntities, markerId); _selectedMarkerId = null;
        }
        Status("{=REX_Builder_Deleted}Selected part deleted.");
    }

    private void PushUndo()
    {
        if (_preset is null) return;
        _undo.Push(ClonePreset(_preset));
        if (_undo.Count > MaximumUndoSnapshots)
        {
            var snapshots = _undo.Take(MaximumUndoSnapshots).Reverse().ToArray();
            _undo.Clear(); foreach (var snapshot in snapshots) _undo.Push(snapshot);
        }
        _redo.Clear();
    }

    private void Undo()
    {
        if (_preset is null || _undo.Count == 0) { Status("{=REX_Builder_No_Undo}Nothing to undo."); return; }
        _redo.Push(ClonePreset(_preset)); _preset = _undo.Pop(); RebuildPreview(); Status("{=REX_Builder_Undone}Last change undone.");
    }

    private void Redo()
    {
        if (_preset is null || _redo.Count == 0) { Status("{=REX_Builder_No_Redo}Nothing to redo."); return; }
        _undo.Push(ClonePreset(_preset)); _preset = _redo.Pop(); RebuildPreview(); Status("{=REX_Builder_Redone}Change restored.");
    }

    private void RebuildPreview()
    {
        RemoveAllPreviews();
        if (_preset is null || !_rootPosition.IsValid) return;
        foreach (var piece in _preset.Pieces)
        {
            var entity = CreatePreviewEntity(piece);
            if (entity is not null) _piecePreviewEntities[piece.Id] = entity;
        }
        foreach (var marker in _preset.Markers)
        {
            var entity = CreatePreviewEntity("bd_pole_3m", ExecutionSiteResourceType.Prefab);
            if (entity is not null) _markerPreviewEntities[marker.Id] = entity;
        }
        UpdateAllPreviewFrames();
    }

    private GameEntity? CreatePreviewEntity(string key, ExecutionSiteResourceType type)
    {
        GameEntity? entity = null;
        try
        {
            if (type == ExecutionSiteResourceType.Prefab)
            {
                if (!GameEntity.PrefabExists(key)) return null;
                entity = BannerlordApiCompatibility.InstantiatePrefab(_mission.Scene, key, false, MatrixFrame.Identity, false);
            }
            else
            {
                var mesh = MetaMesh.GetCopy(key, false, true);
                if (mesh is null || !mesh.IsValid) return null;
                entity = GameEntity.CreateEmpty(_mission.Scene, false, false, false);
                entity?.AddMultiMesh(mesh);
            }
            if (entity is null) return null;
            try { entity.SetPhysicsState(false, true); } catch { }
            try { entity.RemovePhysics(false); } catch { }
            try { entity.SetAlpha(0.56f); } catch { }
            try { entity.SetVisibilityExcludeParents(true); } catch { }
            return entity;
        }
        catch (Exception exception)
        {
            RexLog.Warning($"Could not create preview for '{key}': {exception.Message}");
            try { entity?.Remove(0); } catch { }
            return null;
        }
    }

    private GameEntity? CreatePreviewEntity(ExecutionSitePiece piece)
    {
        if (string.Equals(
                piece.RoleId,
                ExecutionSiteCrucifixDecoration.RoleId,
                StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionSiteCrucifixDecoration.Create(
                _mission.Scene,
                createPhysics: false,
                preview: true);
        }

        return CreatePreviewEntity(piece.ResourceKey, piece.ResourceType);
    }

    private void UpdateAllPreviewFrames()
    {
        if (_preset is null || !_rootPosition.IsValid) return;
        foreach (var piece in _preset.Pieces)
        {
            if (!_piecePreviewEntities.TryGetValue(piece.Id, out var entity)) continue;
            entity.SetGlobalFrame(BuildFrame(piece.Transform, false));
        }
        foreach (var marker in _preset.Markers)
            if (_markerPreviewEntities.TryGetValue(marker.Id, out var entity)) entity.SetGlobalFrame(BuildFrame(marker.Transform, true));
    }

    private void UpdatePendingPreview()
    {
        var key = _pendingPiece?.ResourceKey ?? (_pendingMarker is null ? null : "bd_pole_3m");
        var type = _pendingPiece?.ResourceType ?? ExecutionSiteResourceType.Prefab;
        var transform = GetActiveTransform();
        if (key is null || transform is null) return;
        _pendingPreviewEntity ??= _pendingPiece is not null
            ? CreatePreviewEntity(_pendingPiece)
            : CreatePreviewEntity(key, type);
        _pendingPreviewEntity?.SetGlobalFrame(BuildFrame(transform, _pendingMarker is not null));
    }

    private MatrixFrame BuildFrame(ExecutionPresetTransform transform, bool marker)
    {
        var forward3 = new Vec3(_rootForward.x, _rootForward.y, 0f);
        var frame = new MatrixFrame(Mat3.CreateMat3WithForward(in forward3), ToWorldPosition(transform));
        frame.rotation.RotateAboutUp(transform.Yaw * DegreesToRadians);
        frame.rotation.RotateAboutSide(transform.Pitch * DegreesToRadians);
        frame.rotation.RotateAboutForward(transform.Roll * DegreesToRadians);
        frame.rotation.ApplyScaleLocal(marker ? 0.12f : Clamp(transform.Scale, 0.10f, 5f));
        return frame;
    }

    private Vec3 ToWorldPosition(ExecutionPresetTransform transform)
    {
        var right = new Vec2(_rootForward.y, -_rootForward.x);
        return _rootPosition + new Vec3(right.x * transform.X + _rootForward.x * transform.Y,
            right.y * transform.X + _rootForward.y * transform.Y, transform.Z);
    }

    private void SetRelativePosition(ExecutionPresetTransform transform, Vec3 world)
    {
        var delta = world - _rootPosition;
        if (!ReferenceEquals(_placementTransform, transform))
        {
            _placementTransform = transform;
            // Existing parts keep their elevation at the first cursor hit;
            // new parts retain their authored height above the hit surface.
            _placementHeightOffset = _movingSelected ? transform.Z - delta.z : transform.Z;
        }
        var right = new Vec2(_rootForward.y, -_rootForward.x);
        transform.X = delta.x * right.x + delta.y * right.y;
        transform.Y = delta.x * _rootForward.x + delta.y * _rootForward.y;
        transform.Z = delta.z + _placementHeightOffset;
    }

    private void AdjustPlacementHeight(ExecutionPresetTransform transform, float delta)
    {
        transform.Z += delta;
        if (ReferenceEquals(_placementTransform, transform)) _placementHeightOffset += delta;
    }

    private void ResetPlacementHeight()
    {
        _placementTransform = null;
        _placementHeightOffset = 0f;
    }

    private void RotateRoot(float degrees, bool updatePreset = true)
    {
        var angle = degrees * DegreesToRadians;
        var rotated = new Vec2(_rootForward.x * MathF.Cos(angle) - _rootForward.y * MathF.Sin(angle),
            _rootForward.x * MathF.Sin(angle) + _rootForward.y * MathF.Cos(angle));
        if (rotated.IsNonZero())
        {
            _rootForward = rotated.Normalized();
            if (updatePreset && _preset is not null) _preset.RootAnchor.Yaw += degrees;
        }
    }

    private ExecutionPresetTransform? GetActiveTransform() => _pendingPiece?.Transform ?? _pendingMarker?.Transform ?? (_movingSelected ? GetSelectedTransform() : null);
    private ExecutionPresetTransform? GetSelectedTransform()
    {
        if (_preset is null) return null;
        if (_selectedPieceId is Guid p) return _preset.Pieces.FirstOrDefault(x => x.Id == p)?.Transform;
        if (_selectedMarkerId is Guid m) return _preset.Markers.FirstOrDefault(x => x.Id == m)?.Transform;
        return null;
    }
    private Vec3 GetPendingWorldPosition() => GetActiveTransform() is { } transform ? ToWorldPosition(transform) : _rootPosition;
    private void RemovePendingPreview() { try { _pendingPreviewEntity?.Remove(0); } catch { } _pendingPreviewEntity = null; }
    private void RemoveAllPreviews()
    {
        ResetPlacementHeight();
        RemovePendingPreview();
        foreach (var entity in _piecePreviewEntities.Values.Concat(_markerPreviewEntities.Values).Distinct()) try { entity.Remove(0); } catch { }
        _piecePreviewEntities.Clear(); _markerPreviewEntities.Clear();
    }
    private static void RemovePreview(Dictionary<Guid, GameEntity> map, Guid id)
    {
        if (!map.TryGetValue(id, out var entity)) return;
        try { entity.Remove(0); } catch { } map.Remove(id);
    }
    private static bool IsMeshAvailable(string key)
    {
        try { var mesh = MetaMesh.GetCopy(key, false, true); return mesh is not null && mesh.IsValid; } catch { return false; }
    }
    private static ExecutionSitePreset ClonePreset(ExecutionSitePreset source) => new()
    {
        SchemaVersion = source.SchemaVersion, Id = source.Id, Name = source.Name, MethodId = source.MethodId,
        RootAnchor = source.RootAnchor.Clone(),
        Pieces = source.Pieces.Select(x => new ExecutionSitePiece { Id = x.Id, ResourceKey = x.ResourceKey, ResourceType = x.ResourceType, RoleId = x.RoleId, Collision = x.Collision, Transform = x.Transform.Clone() }).ToList(),
        Markers = source.Markers.Select(x => new ExecutionSiteMarker { Id = x.Id, RoleId = x.RoleId, Kind = x.Kind, Transform = x.Transform.Clone() }).ToList(),
        Troops = new ExecutionSiteTroopSelection { ExecutionerCharacterId = source.Troops.ExecutionerCharacterId, MeleeGuardCharacterId = source.Troops.MeleeGuardCharacterId, RangedGuardCharacterId = source.Troops.RangedGuardCharacterId }
    };
}
