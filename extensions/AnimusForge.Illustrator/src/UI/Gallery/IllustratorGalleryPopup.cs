using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using AnimusForge.Illustrator.UI.Overlays;

namespace AnimusForge.Illustrator.UI.Gallery
{
    public sealed class IllustratorGalleryPopup
    {
        private static IllustratorGalleryPopup _activeInstance;
        private readonly ScreenBase _screen;
        private readonly MovableGauntletLayer _layer;
        private readonly IllustratorGalleryPopupVM _dataSource;

        private IllustratorGalleryPopup(ScreenBase screen)
        {
            _screen = screen;
            _dataSource = new IllustratorGalleryPopupVM(Close);
            var layer = new MovableGauntletLayer("IllustratorGalleryPopup", 4020, false);
            var movieIdentifier = layer.LoadMovie("IllustratorGalleryPopup", _dataSource);
            layer.AutoAttachMovable(movieIdentifier?.Movie, "MainPanel", "TitleBar");
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer = layer;

            try
            {
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            }
            catch
            {
            }
        }

        public static bool IsOpen => _activeInstance != null;

        public static void Show(string focusKey = null)
        {
            ScreenBase topScreen = ScreenManager.TopScreen;
            if (topScreen == null)
            {
                return;
            }

            try
            {
                _activeInstance?.Close();
                var popup = new IllustratorGalleryPopup(topScreen);
                if (!string.IsNullOrWhiteSpace(focusKey))
                {
                    popup._dataSource.SelectByKey(focusKey);
                }
                topScreen.AddLayer(popup._layer);
                _activeInstance = popup;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Illustrator] Failed to open gallery popup: {ex.Message}");
            }
        }

        public void Close()
        {
            try
            {
                if (_screen != null && _layer != null)
                {
                    _screen.RemoveLayer(_layer);
                }
            }
            catch
            {
            }
            finally
            {
                if (_activeInstance == this)
                {
                    _activeInstance = null;
                }
            }
        }
    }
}
