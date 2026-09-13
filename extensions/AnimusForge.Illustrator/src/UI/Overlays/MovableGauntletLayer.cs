using System;
using TaleWorlds.Engine.GauntletUI;
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
        private bool _isDragging;
        private TaleWorlds.Library.Vec2 _lastMousePixel;

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

        protected override void Tick(float dt)
        {
            base.Tick(dt);

            if (_panelWidget == null)
            {
                return;
            }

            try
            {
                HandleDrag();
            }
            catch
            {
            }
        }

        private void HandleDrag()
        {
            var input = base.Input;
            if (input == null) return;

            if (_isDragging)
            {
                if (!input.IsKeyDown(InputKey.LeftMouseButton))
                {
                    _isDragging = false;
                    return;
                }

                TaleWorlds.Library.Vec2 mousePixel = input.GetMousePositionPixel();
                TaleWorlds.Library.Vec2 delta = mousePixel - _lastMousePixel;
                _lastMousePixel = mousePixel;

                if (MathF.Abs(delta.X) > 0.001f || MathF.Abs(delta.Y) > 0.001f)
                {
                    float scale = UIContext?.ScaleModifier ?? 1f;
                    if (scale <= 0.001f) scale = 1f;

                    _panelWidget.PositionXOffset += delta.X / scale;
                    _panelWidget.PositionYOffset += delta.Y / scale;
                }
            }
            else if (input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                if (CanStartDrag())
                {
                    _isDragging = true;
                    _lastMousePixel = input.GetMousePositionPixel();
                }
            }
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
            return widget.IsPointInsideMeasuredArea(UIContext.EventManager.MousePosition);
        }

        private bool IsMouseInPanelHeader(Widget panel, float headerHeight)
        {
            if (panel == null || UIContext?.EventManager == null) return false;
            var mousePos = UIContext.EventManager.MousePosition;
            var globalPos = panel.GlobalPosition;
            var size = panel.Size;

            float scaledHeader = headerHeight * (UIContext.ScaleModifier > 0.001f ? UIContext.ScaleModifier : 1f);

            return mousePos.X >= globalPos.X && mousePos.X <= globalPos.X + size.X &&
                   mousePos.Y >= globalPos.Y && mousePos.Y <= globalPos.Y + scaledHeader;
        }
    }
}
