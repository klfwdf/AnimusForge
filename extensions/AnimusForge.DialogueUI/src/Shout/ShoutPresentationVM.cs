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
        private string _statusText = "Enter 发送 · Shift+Enter 换行 · Esc 收起";
        private int _illustrationVersion = -1;
        [DataSourceProperty] public bool IsIllustrationVisible => Host is ShoutTextInputPopupVM vm && vm.IsIllustrationVisible;
        [DataSourceProperty] public bool CanIllustrate => Host is ShoutTextInputPopupVM vm && vm.CanIllustrate;
        [DataSourceProperty] public string IllustrationButtonText => (Host as ShoutTextInputPopupVM)?.IllustrationButtonText ?? "生图";
        public void ExecuteIllustrate() { if (!_released && Host is ShoutTextInputPopupVM vm) vm.ExecuteIllustrate(); }

        [DataSourceProperty] public ViewModel Host { get; private set; }
        [DataSourceProperty] public bool HasSubtitle { get; }
        [DataSourceProperty] public float HistoryBottomMargin => HasSubtitle ? 426f : 310f;
        [DataSourceProperty] public bool CanSubmit => _canSubmit;
        [DataSourceProperty] public bool IsHistoryVisible => _isHistoryVisible;
        [DataSourceProperty] public string HistoryButtonText => _isHistoryVisible ? "收起记录" : "记录";
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
                _statusText = available ? "Enter 发送 · Shift+Enter 换行 · Esc 收起" : "当前交流对象已失效，请收起后重新框选。";
                OnPropertyChangedWithValue(available, nameof(CanSubmit));
                OnPropertyChangedWithValue(_statusText, nameof(StatusText));
            }
            return available;
        }

        private string ReadVisibleHistory()
        {
            if (!RefreshAvailability()) return "当前交流对象已失效，无法读取记录。";
            try
            {
                List<string> lines = _readHistory(_context.AgentIndex, 260);
                if (lines == null || lines.Count == 0) return "当前场景暂无可见对话记录。";
                var text = new StringBuilder();
                int start = Math.Max(0, lines.Count - 260);
                for (int i = start; i < lines.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    if (text.Length > 0) text.AppendLine().AppendLine();
                    text.Append(lines[i]);
                }
                return text.Length == 0 ? "当前场景暂无可见对话记录。" : text.ToString();
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Scene history read failed: " + ex.Message);
                return "记录暂时不可用。";
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
