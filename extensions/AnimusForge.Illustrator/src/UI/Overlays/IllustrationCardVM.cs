using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using AnimusForge.Illustrator.UI.Gallery;

namespace AnimusForge.Illustrator.UI.Overlays
{
    public sealed class IllustrationCardVM : ViewModel
    {
        private readonly Action _onClose;
        private readonly Action _onRegenerate;
        private readonly Action _onSceneProbe;
        private readonly Action _onRegenerateWithPrompt;
        internal Action OnRegenerateBasedOnImage;
        internal bool HasEditableImage;
        internal void SetEditableImage(bool value) { HasEditableImage = value; OnPropertyChanged(nameof(CanRegenerateBasedOnImage)); }
        [DataSourceProperty] public bool CanRegenerateBasedOnImage => HasEditableImage && HasIllustration && !IsLoading && OnRegenerateBasedOnImage != null;
        public void ExecuteRegenerateBasedOnImage()
        {
            if (!CanRegenerateBasedOnImage) return;
            try { OnRegenerateBasedOnImage(); } catch (Exception ex) { SetReady("重绘准备失败：" + ex.Message); }
        }
        private string _titleText = string.Empty;
        private string _statusText = string.Empty;
        private string _promptText = string.Empty;
        private string _spriteName = string.Empty;
        private string _currentKey = string.Empty;
        private bool _hasIllustration;
        private bool _isLoading;
        private bool _showPrompt;

        public IllustrationCardVM(Action onClose, Action onRegenerate, Action onSceneProbe = null)
            : this(onClose, onRegenerate, onSceneProbe, null)
        {
        }

        public IllustrationCardVM(Action onClose, Action onRegenerate, Action onSceneProbe, Action onRegenerateWithPrompt)
        {
            _onClose = onClose;
            _onRegenerate = onRegenerate;
            _onSceneProbe = onSceneProbe;
            _onRegenerateWithPrompt = onRegenerateWithPrompt;
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    OnPropertyChangedWithValue(value, nameof(StatusText));
                }
            }
        }

        [DataSourceProperty]
        public string PromptText
        {
            get => _promptText;
            set
            {
                if (value != _promptText)
                {
                    _promptText = value;
                    OnPropertyChangedWithValue(value, nameof(PromptText));
                }
            }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get => _spriteName;
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, nameof(SpriteName));
                }
            }
        }

        [DataSourceProperty]
        public bool HasIllustration
        {
            get => _hasIllustration;
            set
            {
                if (value != _hasIllustration)
                {
                    _hasIllustration = value;
                    OnPropertyChangedWithValue(value, nameof(HasIllustration));
                    OnPropertyChanged(nameof(CanRegenerateBasedOnImage));
                }
            }
        }

        [DataSourceProperty]
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value != _isLoading)
                {
                    _isLoading = value;
                    OnPropertyChangedWithValue(value, nameof(IsLoading));
                    OnPropertyChanged(nameof(CanExecuteSceneProbe));
                    OnPropertyChanged(nameof(CanRegenerateWithPrompt));
                    OnPropertyChanged(nameof(CanRegenerateBasedOnImage));
                }
            }
        }

        [DataSourceProperty]
        public bool CanProbeScene => _onSceneProbe != null;

        [DataSourceProperty]
        public bool CanExecuteSceneProbe => CanProbeScene && !IsLoading;

        [DataSourceProperty]
        public bool CanRegenerateWithPrompt => !IsLoading && _onRegenerateWithPrompt != null;

        [DataSourceProperty]
        public bool ShowPrompt
        {
            get => _showPrompt;
            set
            {
                if (value != _showPrompt)
                {
                    _showPrompt = value;
                    OnPropertyChangedWithValue(value, nameof(ShowPrompt));
                }
            }
        }

        public void SetIllustration(string key, string spriteName, string prompt)
        {
            _currentKey = key;
            PromptText = prompt ?? string.Empty;

            // 强制 Gauntlet 刷新：先清空并触发属性通知，再重新赋值，彻底解决同名 Sprite 在重画时不刷新画面的问题
            _spriteName = string.Empty;
            OnPropertyChanged(nameof(SpriteName));
            _spriteName = spriteName;
            OnPropertyChangedWithValue(spriteName, nameof(SpriteName));

            HasIllustration = !string.IsNullOrWhiteSpace(spriteName);
            IsLoading = false;
        }

        public void SetLoading(string status)
        {
            StatusText = status;
            IsLoading = true;
        }

        public void SetReady(string status)
        {
            StatusText = status;
            IsLoading = false;
        }

        public void ExecuteRegenerate()
        {
            if (IsLoading) return;
            try { _onRegenerate?.Invoke(); }
            catch (Exception ex) { SetReady("生成准备失败：" + ex.Message); }
        }

        public void ExecuteSceneProbe()
        {
            if (!CanExecuteSceneProbe) return;
            try { _onSceneProbe?.Invoke(); }
            catch (Exception ex) { SetReady("试采准备失败：" + ex.Message); }
        }

        public void ExecuteTogglePrompt()
        {
            ShowPrompt = !ShowPrompt;
        }

        public void ExecuteRegenerateWithPrompt()
        {
            if (!CanRegenerateWithPrompt) return;
            try { _onRegenerateWithPrompt.Invoke(); }
            catch (Exception ex) { SetReady("重绘准备失败：" + ex.Message); }
        }

        public void ExecuteCopyPrompt()
        {
            if (string.IsNullOrWhiteSpace(PromptText)) return;
            try
            {
                Input.SetClipboardText(PromptText);
                StatusText = "提示词已复制到剪贴板";
            }
            catch (Exception ex)
            {
                StatusText = "复制失败: " + ex.Message;
            }
        }

        public void ExecuteOpenGallery()
        {
            IllustratorGalleryPopup.Show(_currentKey);
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
    }
}
