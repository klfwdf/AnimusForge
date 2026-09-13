using System;
using System.Diagnostics;
using System.IO;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Engine;

namespace AnimusForge.Illustrator.UI.Gallery
{
    public sealed class IllustrationItemVM : ViewModel
    {
        private readonly CachedIllustrationItem _item;
        private readonly Action<IllustrationItemVM> _onSelect;
        private bool _isSelected;

        public CachedIllustrationItem Item => _item;

        public IllustrationItemVM(CachedIllustrationItem item, Action<IllustrationItemVM> onSelect)
        {
            _item = item;
            _onSelect = onSelect;
            // 纹理材质采用延迟按需加载 (Lazy Loading)：仅在用户选中具体条目时才读取文件并注册纹理，避免打开画廊瞬间阻塞主线程与显存膨胀
        }

        [DataSourceProperty]
        public string Title => string.IsNullOrWhiteSpace(_item.Title) ? "卡拉迪亚纪事画卷" : _item.Title;

        [DataSourceProperty]
        public string DateText => _item.CreatedTime.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

        [DataSourceProperty]
        public string SpriteName => _item.Key;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        public void ExecuteSelect()
        {
            _onSelect?.Invoke(this);
        }
    }

    public sealed class IllustratorGalleryPopupVM : ViewModel
    {
        private readonly Action _onClose;
        private MBBindingList<IllustrationItemVM> _items = new MBBindingList<IllustrationItemVM>();
        private IllustrationItemVM _selectedItem;
        private bool _hasSelection;
        private string _selectedSpriteName = string.Empty;
        private string _selectedTitle = string.Empty;
        private string _selectedPrompt = string.Empty;
        private string _selectedDate = string.Empty;
        private string _statusText = "欢迎查阅卡拉迪亚纪事画廊";

        public IllustratorGalleryPopupVM(Action onClose)
        {
            _onClose = onClose;
            RefreshItems();
        }

        [DataSourceProperty]
        public MBBindingList<IllustrationItemVM> Items
        {
            get => _items;
            set
            {
                if (value != _items)
                {
                    _items = value;
                    OnPropertyChangedWithValue(value, nameof(Items));
                }
            }
        }

        [DataSourceProperty]
        public bool HasSelection
        {
            get => _hasSelection;
            set
            {
                if (value != _hasSelection)
                {
                    _hasSelection = value;
                    OnPropertyChangedWithValue(value, nameof(HasSelection));
                    OnPropertyChanged(nameof(HasNoSelection));
                }
            }
        }

        [DataSourceProperty]
        public bool HasNoSelection => !_hasSelection;

        [DataSourceProperty]
        public string SelectedSpriteName
        {
            get => _selectedSpriteName;
            set
            {
                if (value != _selectedSpriteName)
                {
                    _selectedSpriteName = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedSpriteName));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedTitle
        {
            get => _selectedTitle;
            set
            {
                if (value != _selectedTitle)
                {
                    _selectedTitle = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedTitle));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedPrompt
        {
            get => _selectedPrompt;
            set
            {
                if (value != _selectedPrompt)
                {
                    _selectedPrompt = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedPrompt));
                }
            }
        }

        [DataSourceProperty]
        public string SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (value != _selectedDate)
                {
                    _selectedDate = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedDate));
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

        public void RefreshItems()
        {
            Items.Clear();
            var cached = DiskImageCacheManager.GetAllCachedIllustrations();
            foreach (var item in cached)
            {
                Items.Add(new IllustrationItemVM(item, HandleItemSelect));
            }

            if (Items.Count > 0)
            {
                HandleItemSelect(Items[0]);
                StatusText = $"共收录 {Items.Count} 幅历史画卷";
            }
            else
            {
                HasSelection = false;
                StatusText = "暂无收录画卷，将在周报生成时自动为您绘制";
            }
        }

        private void HandleItemSelect(IllustrationItemVM selected)
        {
            foreach (var item in Items)
            {
                item.IsSelected = (item == selected);
            }

            _selectedItem = selected;
            if (selected != null)
            {
                HasSelection = true;
                if (!GauntletTextureLoader.TryGetSprite(selected.SpriteName, out _))
                {
                    if (File.Exists(selected.Item?.FilePath))
                    {
                        byte[] bytes = File.ReadAllBytes(selected.Item.FilePath);
                        GauntletTextureLoader.LoadOrRegisterPngBytes(selected.SpriteName, bytes);
                    }
                }
                SelectedSpriteName = selected.SpriteName;
                SelectedTitle = selected.Title;
                SelectedPrompt = selected.Item?.Prompt ?? string.Empty;
                SelectedDate = selected.DateText;
            }
            else
            {
                HasSelection = false;
            }
        }

        public void SelectByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            foreach (var item in Items)
            {
                if (string.Equals(item.SpriteName, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Item?.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    HandleItemSelect(item);
                    break;
                }
            }
        }

        public void ExecuteOpenFolder()
        {
            try
            {
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string cacheDir = Path.Combine(docsDir, "Mount and Blade II Bannerlord", "AnimusForge", "IllustratorCache");
                if (Directory.Exists(cacheDir))
                {
                    Process.Start("explorer.exe", cacheDir);
                }
            }
            catch (Exception ex)
            {
                StatusText = "打开目录失败: " + ex.Message;
            }
        }

        public void ExecuteClose()
        {
            _onClose?.Invoke();
        }
    }
}
