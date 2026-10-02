using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace AnimusForge.Illustrator.UI.Overlays
{
    public class MovableGauntletLayer : GauntletLayer
    {
        private Widget _panelWidget;
        private Widget _dragHandleWidget;
        private Widget _fractionPanel;
        private float _fractionPanelHeight;
        private bool _isDragging;
        private bool _isResizing;
        private TaleWorlds.Library.Vec2 _lastMousePixel;
        private float _dragStartX, _dragStartY;

        public MovableGauntletLayer(string name, int localOrder, bool shouldClear = false)
            : base(name, localOrder, shouldClear)
        {
        }

        public void AttachMovable(Widget panelWidget, Widget dragHandleWidget = null)
        {
            _panelWidget = panelWidget;
            _dragHandleWidget = dragHandleWidget ?? panelWidget;
        }

        public void AutoAttachMovable(IGauntletMovie movie, string panelId = null, string handleId = "TitleBar")
        {
            if (movie?.RootWidget == null) return;
            Widget root = movie.RootWidget;

            Widget panel = null;
            if (!string.IsNullOrEmpty(panelId))
            {
                panel = (root.Id == panelId) ? root : root.FindChild(panelId, includeAllChildren: true);
            }

            if (panel == null)
            {
                panel = root.FindChild("CardPanel", includeAllChildren: true)
                     ?? root.FindChild("MainPanel", includeAllChildren: true);
            }

            if (panel == null)
            {
                panel = (root.ChildCount > 0) ? root.GetChild(0) : root;
            }

            Widget handle = null;
            if (!string.IsNullOrEmpty(handleId))
            {
                handle = root.FindChild(handleId, includeAllChildren: true);
            }

            AttachMovable(panel, handle);
        }

        public void AttachTopFraction(Widget panelWidget, float heightFraction)
        {
            _fractionPanel = panelWidget;
            _fractionPanelHeight = MathF.Clamp(heightFraction, 0.1f, 1f);
        }

        protected override void Tick(float dt)
        {
            if (_fractionPanel != null && UIContext?.EventManager != null)
            {
                float scale = UIContext.CustomScale > 0.001f ? UIContext.CustomScale : 1f;
                float targetHeight = UIContext.EventManager.PageSize.Y / scale * _fractionPanelHeight;
                if (targetHeight > 0f && MathF.Abs(_fractionPanel.SuggestedHeight - targetHeight) > 1f)
                    _fractionPanel.SuggestedHeight = targetHeight;
            }

            if (_panelWidget == null)
            {
                base.Tick(dt);
                return;
            }

            try
            {
                HandleDrag();
            }
            catch
            {
                _isDragging = false;
                _isResizing = false;
            }
            // Offsets must be applied before Gauntlet's layout/update, not one frame later.
            base.Tick(dt);
        }

        private void HandleDrag()
        {
            var input = base.Input;
            if (input == null) return;

            if (_isResizing)
            {
                if (!input.IsKeyDown(InputKey.LeftMouseButton))
                {
                    _isResizing = false;
                    return;
                }

                TaleWorlds.Library.Vec2 mousePixel = input.GetMousePositionPixel();
                TaleWorlds.Library.Vec2 delta = mousePixel - _lastMousePixel;
                _lastMousePixel = mousePixel;
                float scale = UIContext?.CustomScale ?? 1f;
                if (scale <= 0.001f) scale = 1f;
                _panelWidget.SuggestedWidth = MathF.Clamp(_panelWidget.SuggestedWidth + delta.X / scale, 320f, 1600f);
                _panelWidget.SuggestedHeight = MathF.Clamp(_panelWidget.SuggestedHeight + delta.Y / scale, 360f, 1100f);
                return;
            }

            if (_isDragging)
            {
                if (!input.IsKeyDown(InputKey.LeftMouseButton))
                {
                    _isDragging = false;
                    return;
                }

                TaleWorlds.Library.Vec2 mousePixel = input.GetMousePositionPixel();
                TaleWorlds.Library.Vec2 delta = mousePixel - _lastMousePixel;

                float scale = UIContext?.CustomScale ?? 1f;
                if (scale <= 0.001f) scale = 1f;
                // Absolute press anchor also restores the exact starting position when delta returns to zero.
                _panelWidget.PositionXOffset = _dragStartX + delta.X / scale;
                _panelWidget.PositionYOffset = _dragStartY + delta.Y / scale;
            }
            else if (input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                if (CanStartResize())
                {
                    _isResizing = true;
                    _lastMousePixel = input.GetMousePositionPixel();
                }
                else if (CanStartDrag())
                {
                    _isDragging = true;
                    _lastMousePixel = input.GetMousePositionPixel();
                    _dragStartX = _panelWidget.PositionXOffset;
                    _dragStartY = _panelWidget.PositionYOffset;
                }
            }
        }

        private bool CanStartResize()
        {
            if (_panelWidget == null || UIContext?.EventManager == null) return false;
            if (_panelWidget.WidthSizePolicy != SizePolicy.Fixed || _panelWidget.HeightSizePolicy != SizePolicy.Fixed) return false;
            var mousePos = Input.GetMousePositionPixel();
            var globalPos = _panelWidget.GlobalPosition;
            var size = _panelWidget.Size;
            const float resizeGrip = 24f;
            return mousePos.X >= globalPos.X + size.X - resizeGrip && mousePos.X <= globalPos.X + size.X &&
                   mousePos.Y >= globalPos.Y + size.Y - resizeGrip && mousePos.Y <= globalPos.Y + size.Y;
        }

        private bool CanStartDrag()
        {
            if (_dragHandleWidget != null && IsMouseInsideWidget(_dragHandleWidget))
            {
                return true;
            }

            if (_panelWidget != null && IsMouseInPanelHeader(_panelWidget, 72f))
            {
                return true;
            }

            return false;
        }

        private bool IsMouseInsideWidget(Widget widget)
        {
            if (widget == null || UIContext?.EventManager == null) return false;
            var mouse = Input.GetMousePositionPixel();
            // Infer the game's Vector2 type: the 1.3 reference overlay also contains
            // a Numerics compatibility assembly, so explicitly naming it is ambiguous.
            var point = UIContext.EventManager.MousePosition;
            point.X = mouse.X;
            point.Y = mouse.Y;
            return widget.IsPointInsideMeasuredArea(point);
        }

        private bool IsMouseInPanelHeader(Widget panel, float headerHeight)
        {
            if (panel == null || UIContext?.EventManager == null) return false;
            var mousePos = Input.GetMousePositionPixel();
            var globalPos = panel.GlobalPosition;
            var size = panel.Size;

            float scaledHeader = headerHeight * (UIContext.CustomScale > 0.001f ? UIContext.CustomScale : 1f);

            return mousePos.X >= globalPos.X && mousePos.X <= globalPos.X + size.X &&
                   mousePos.Y >= globalPos.Y && mousePos.Y <= globalPos.Y + scaledHeader;
        }
    }
}
