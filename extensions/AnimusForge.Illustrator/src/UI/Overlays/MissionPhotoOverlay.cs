using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge.Illustrator.UI.Overlays
{
    // Captures only UI visibility/focus owned by this short-lived camera session.
    internal sealed class MissionPhotoOverlay : IDisposable
    {
        private readonly ScreenBase _screen;
        private readonly ScreenLayer _priorFocus;
        private readonly GauntletLayer _layer;
        private readonly MissionPhotoVM _vm;
        private readonly List<Tuple<Widget, bool>> _hidden = new List<Tuple<Widget, bool>>();
        private readonly HashSet<Widget> _hiddenRoots = new HashSet<Widget>();
        private bool _closed;
        internal MissionPhotoOverlay(ScreenBase screen, Action generate, Action more, Action restart, Action cancel)
        {
            _screen = screen;
            _priorFocus = ScreenManager.FocusedLayer;
            _vm = new MissionPhotoVM(generate, more, restart, cancel);
            _layer = new GauntletLayer("AFMissionPhoto", 100500);
            try
            {
                _layer.LoadMovie("MissionPhotoOverlay", _vm);
                foreach (var layer in screen.Layers) Hide(layer);
                screen.OnAddLayer += Hide;
                _layer.IsFocusLayer = true;
                _layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
                screen.AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
            }
            catch { Dispose(); throw; }
        }
        private void Hide(ScreenLayer layer)
        {
            if (ReferenceEquals(layer, _layer)) return;
            var gauntlet = layer as GauntletLayer;
            var root = gauntlet?.UIContext?.Root;
            if (root == null) return;
            if (_hiddenRoots.Add(root)) _hidden.Add(Tuple.Create(root, root.IsVisible));
            root.IsVisible = false;
            gauntlet.TwoDimensionPlatform.Clear();
            gauntlet.TwoDimensionView.Clear();
        }
        internal void HideOtherUi()
        { foreach (var entry in _hidden) if (entry.Item1.IsVisible) entry.Item1.IsVisible = false; }
        internal void Aim(int count)
        {
            _vm.Set("Enter 回车截取第 " + (count + 1) + " 张 · Esc 取消", false, "", "", null, null);
            _layer.UIContext.Root.IsVisible = true;
            _layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
            ScreenManager.TrySetFocus(_layer);
        }
        internal void Capturing()
        {
            _layer.UIContext.Root.IsVisible = false;
            _layer.TwoDimensionPlatform.Clear();
            _layer.TwoDimensionView.Clear();
        }
        internal void Decision(int count)
        {
            _vm.Set("已截取 " + count + " 张，是否开始生成？", true, "生成", count == 1 ? "继续截取" : "重新截取",
                _vm.Generate, count == 1 ? _vm.More : _vm.Restart);
            ShowButtons();
        }
        internal void CancelChoice()
        {
            _vm.Set("已中止本次取景", true, "重新截取", "取消", _vm.Restart, _vm.Cancel);
            ShowButtons();
        }
        private void ShowButtons()
        {
            _layer.UIContext.Root.IsVisible = true;
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            ScreenManager.TrySetFocus(_layer);
        }
        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            bool restoreFocus = ReferenceEquals(ScreenManager.TopScreen, _screen)
                && ReferenceEquals(ScreenManager.FocusedLayer, _layer);
            _screen.OnAddLayer -= Hide;
            foreach (var entry in _hidden) RestorePart(() => entry.Item1.IsVisible = entry.Item2);
            _hidden.Clear();
            _hiddenRoots.Clear();
            RestorePart(() => _layer.InputRestrictions.ResetInputRestrictions());
            _layer.IsFocusLayer = false;
            RestorePart(() => ScreenManager.TryLoseFocus(_layer));
            if (!_screen.IsFinalized) RestorePart(() => _screen.RemoveLayer(_layer));
            if (restoreFocus && _priorFocus != null && !_priorFocus.IsFinalized && _priorFocus.IsActive
                && _screen.Layers.Contains(_priorFocus)) RestorePart(() => ScreenManager.TrySetFocus(_priorFocus));
            _vm.OnFinalize();
        }
        private static void RestorePart(Action action)
        {
            try { action(); }
            catch (Exception ex) { Debug.Print("[Illustrator] Photo UI restore: " + ex.GetType().Name); }
        }
    }

    internal sealed class MissionPhotoVM : ViewModel
    {
        internal readonly Action Generate, More, Restart, Cancel;
        private Action _first, _second;
        internal MissionPhotoVM(Action generate, Action more, Action restart, Action cancel)
        { Generate = generate; More = more; Restart = restart; Cancel = cancel; }
        [DataSourceProperty] public string Hint { get; private set; }
        [DataSourceProperty] public bool ChoicesVisible { get; private set; }
        [DataSourceProperty] public string FirstText { get; private set; }
        [DataSourceProperty] public string SecondText { get; private set; }
        internal void Set(string hint, bool choices, string first, string second, Action a, Action b)
        {
            Hint = hint; ChoicesVisible = choices; FirstText = first; SecondText = second; _first = a; _second = b;
            OnPropertyChanged(nameof(Hint)); OnPropertyChanged(nameof(ChoicesVisible));
            OnPropertyChanged(nameof(FirstText)); OnPropertyChanged(nameof(SecondText));
        }
        public void ExecuteFirst() { var action = _first; _first = _second = null; action?.Invoke(); }
        public void ExecuteSecond() { var action = _second; _first = _second = null; action?.Invoke(); }
    }
}
