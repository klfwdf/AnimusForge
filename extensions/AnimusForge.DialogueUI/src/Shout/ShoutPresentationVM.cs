using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Library;

namespace AnimusForge.DialogueUI.Shout
{
    /// <summary>Owns only the record drawer and availability display. Host keeps the draft and callbacks.</summary>
    public sealed class ShoutPresentationVM : ViewModel
    {
        private readonly ShoutUiAdapter.ShoutContext _context;
        private readonly Action<object> _submit;
        private readonly Action<object> _cancel;
        private readonly Func<int, int, List<string>> _readHistory;
        private bool _historyLoaded;
        private bool _released;
        private bool _isHistoryVisible;
        private bool _canSubmit = true;
        private string _historyText = "";
        private string _statusText = DialogueUiLocalization.Text("AFD_UI_Shout_Hint", "Enter send · Shift+Enter new line · Esc close");
        private int _illustrationVersion = -1;
        private int _rewriteVersion;
        [DataSourceProperty] public bool IsIllustrationVisible => Host is ShoutTextInputPopupVM vm && vm.IsIllustrationVisible;
        [DataSourceProperty] public bool CanIllustrate => Host is ShoutTextInputPopupVM vm && vm.CanIllustrate;
        [DataSourceProperty] public string SceneHistoryTitleText => DialogueUiLocalization.Text("AFD_UI_Shout_Title", "Visible scene records for the current target");
        [DataSourceProperty] public string GalleryText => DialogueUiLocalization.Text("AFD_UI_Shout_Gallery", "Gallery");
        [DataSourceProperty] public string EncyclopediaText => DialogueUiLocalization.Text("AFD_UI_Shout_Encyclopedia", "Encyclopedia");
        [DataSourceProperty] public string CloseText => DialogueUiLocalization.Text("AFD_UI_Shout_Close", "Close");
        [DataSourceProperty] public string SendText => DialogueUiLocalization.Text("AFD_UI_Shout_Send", "Send");
        [DataSourceProperty] public bool IsRewriteVisible => Host is ShoutTextInputPopupVM vm && vm.IsRewriteVisible;
        [DataSourceProperty] public bool CanRewrite => _canSubmit && Host is ShoutTextInputPopupVM vm && vm.CanRewrite;
        [DataSourceProperty] public string RewriteText => DialogueUiLocalization.Text("AF_CONV_UI_Rewrite_Button", "Undo and edit");
        public void ExecuteRewrite() { if (RefreshAvailability() && CanRewrite && Host is ShoutTextInputPopupVM vm) vm.ExecuteRewrite(); }
        [DataSourceProperty] public string IllustrationButtonText => (Host as ShoutTextInputPopupVM)?.IllustrationButtonText ?? DialogueUiLocalization.Text("AFD_UI_Illustration_Button", "Illustrate");
        public void ExecuteIllustrate() { if (!_released && Host is ShoutTextInputPopupVM vm) vm.ExecuteIllustrate(); }
        public void ExecuteOpenGallery() { if (!_released && Host is ShoutTextInputPopupVM vm) vm.ExecuteOpenGallery(); }

        [DataSourceProperty] public ViewModel Host { get; private set; }
        [DataSourceProperty] public bool HasSubtitle { get; }
        [DataSourceProperty] public float HistoryBottomMargin => HasSubtitle ? 426f : 310f;
        [DataSourceProperty] public bool CanSubmit => _canSubmit;
        [DataSourceProperty] public bool IsHistoryVisible => _isHistoryVisible;
        [DataSourceProperty] public string HistoryButtonText => _isHistoryVisible ? DialogueUiLocalization.Text("AFD_UI_Shout_HideHistory", "Hide history") : DialogueUiLocalization.Text("AFD_UI_Shout_History", "History");
        [DataSourceProperty] public string HistoryText => _historyText;
        [DataSourceProperty] public string StatusText => _statusText;

        internal ShoutPresentationVM(ViewModel host, ShoutUiAdapter.ShoutContext context, string subtitle,
            Action<object> submit, Action<object> cancel, Func<int, int, List<string>> readHistory)
        {
            Host = host;
            _context = context;
            _submit = submit;
            _cancel = cancel;
            _readHistory = readHistory;
            HasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
        }

        public void ExecuteSubmit()
        {
            if (RefreshAvailability()) _submit(Host);
        }

        public void ExecuteCancel()
        {
            if (!_released && Host != null) _cancel(Host);
        }

        public void ExecuteToggleHistory()
        {
            if (_released) return;
            if (!_historyLoaded)
            {
                _historyLoaded = true;
                _historyText = ReadVisibleHistory();
                OnPropertyChangedWithValue(_historyText, nameof(HistoryText));
            }
            _isHistoryVisible = !_isHistoryVisible;
            OnPropertyChangedWithValue(_isHistoryVisible, nameof(IsHistoryVisible));
            OnPropertyChangedWithValue(HistoryButtonText, nameof(HistoryButtonText));
        }

        internal bool RefreshAvailability()
        {
            if (Host is ShoutTextInputPopupVM host)
            {
                host.RefreshRewriteAvailability();
                bool canRewrite = CanRewrite;
                if (_lastCanRewrite != canRewrite) { _lastCanRewrite = canRewrite; OnPropertyChanged(nameof(CanRewrite)); }
                if (_rewriteVersion != host.RewriteVersion)
                {
                    _rewriteVersion = host.RewriteVersion;
                    if (_historyLoaded) { _historyText = ReadVisibleHistory(); OnPropertyChanged(nameof(HistoryText)); }
                }
            }
            int illustrationVersion = ShoutBehavior.SceneIllustrationVersionForExternal;
            if (_illustrationVersion != illustrationVersion)
            {
                _illustrationVersion = illustrationVersion;
                OnPropertyChanged(nameof(IsIllustrationVisible)); OnPropertyChanged(nameof(CanIllustrate)); OnPropertyChanged(nameof(IllustrationButtonText));
            }
            bool available = !_released && Host != null && _context.IsCurrent();
            if (_canSubmit != available)
            {
                _canSubmit = available;
                _statusText = available ? DialogueUiLocalization.Text("AFD_UI_Shout_Hint", "Enter send · Shift+Enter new line · Esc close") : DialogueUiLocalization.Text("AFD_UI_Shout_TargetInvalid", "The current target is no longer available. Close and select again.");
                OnPropertyChangedWithValue(available, nameof(CanSubmit));
                OnPropertyChangedWithValue(_statusText, nameof(StatusText));
            }
            return available;
        }

        private bool _lastCanRewrite;

        private string ReadVisibleHistory()
        {
            if (!RefreshAvailability()) return DialogueUiLocalization.Text("AFD_UI_Shout_TargetInvalidHistory", "The current target is no longer available; history cannot be read.");
            try
            {
                List<string> lines = _readHistory(_context.AgentIndex, 260);
                if (lines == null || lines.Count == 0) return DialogueUiLocalization.Text("AFD_UI_Shout_NoHistory", "No visible conversation history in this scene.");
                var text = new StringBuilder();
                int start = Math.Max(0, lines.Count - 260);
                for (int i = start; i < lines.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    if (text.Length > 0) text.AppendLine().AppendLine();
                    text.Append(lines[i]);
                }
                return text.Length == 0 ? DialogueUiLocalization.Text("AFD_UI_Shout_NoHistory", "No visible conversation history in this scene.") : text.ToString();
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Scene history read failed: " + ex.Message);
                return DialogueUiLocalization.Text("AFD_UI_Shout_HistoryUnavailable", "History is temporarily unavailable.");
            }
        }

        public override void OnFinalize()
        {
            if (_released) return;
            _released = true;
            _context.Release();
            Host = null;
            _historyText = "";
            // Do not finalize Host: the original ShoutTextInputPopup owns that lifecycle.
            base.OnFinalize();
        }
    }
}
