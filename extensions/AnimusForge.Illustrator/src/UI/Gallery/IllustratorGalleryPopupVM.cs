using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Core;
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
        public string Title => (string.IsNullOrWhiteSpace(_item.Title) ? "卡拉迪亚纪事画卷" : _item.Title) + (_item.IsDefault ? "【默认】" : string.Empty) + (_item.IsFavorite ? "【收藏】" : string.Empty);

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

        internal void RefreshTitle() => OnPropertyChanged(nameof(Title));
    }

    public sealed class IllustratorGalleryPopupVM : ViewModel
    {
        private readonly Action _onClose;
        private readonly string _campaignKey;
        private MBBindingList<IllustrationItemVM> _items = new MBBindingList<IllustrationItemVM>();
        private IllustrationItemVM _selectedItem;
        private bool _hasSelection;
        private string _selectedSpriteName = string.Empty;
        private string _selectedTitle = string.Empty;
        private string _selectedDate = string.Empty;
        private string _selectedTheme = string.Empty;
        private string _statusText = "欢迎查阅卡拉迪亚纪事画廊";
        private string _loadedPreviewSpriteName;
        private int _previewCounter;
        private readonly GalleryPreviewLoader _previewLoader;
        private bool _disposed;
        private int _refreshVersion;
        private readonly Func<bool> _isCurrent;
        private bool _rpConversionOpen;
        private bool _redrawing, _settingDefault;
        internal Action<CachedIllustrationItem> RedrawSelected;
        private string _pendingSelectionKey;
        internal CachedIllustrationItem SelectedImage => _selectedItem?.Item;
        [DataSourceProperty] public bool CanRedrawBasedOnImage => !_disposed && !_redrawing && !_rpConversionOpen && HasSelection && !string.IsNullOrEmpty(SelectedSpriteName);
        internal void SetRedrawing(bool value) { _redrawing = value; OnPropertyChanged(nameof(CanRedrawBasedOnImage)); }
        public void ExecuteRedrawBasedOnImage()
        {
            if (CanRedrawBasedOnImage && _isCurrent()) RedrawSelected?.Invoke(_selectedItem.Item);
        }
        // Last directory snapshot; the favorites tab filters it in memory instead of re-reading disk.
        private System.Collections.Generic.List<CachedIllustrationItem> _cached = new System.Collections.Generic.List<CachedIllustrationItem>();
        private bool _showFavorites;

        public IllustratorGalleryPopupVM(Action onClose, string campaignKey)
            : this(onClose, campaignKey, () => string.Equals(campaignKey, IllustratorRuntime.CampaignKey, StringComparison.Ordinal))
        {
        }

        internal IllustratorGalleryPopupVM(Action onClose, string campaignKey, Func<bool> isCurrent)
        {
            _onClose = onClose;
            _campaignKey = campaignKey;
            _isCurrent = isCurrent;
            _previewLoader = new GalleryPreviewLoader(() => !_disposed && isCurrent(),
                IllustratorRuntime.PostCritical, GauntletTextureLoader.ReadPreparedImageForUi);
            RefreshItems(false);
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
                    OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
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
                    OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
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
        public bool IsFavoritesTab => _showFavorites;

        [DataSourceProperty]
        public bool IsAllTab => !_showFavorites;

        [DataSourceProperty]
        public string FavoriteButtonText => _selectedItem?.Item?.IsFavorite == true ? "取消收藏" : "收藏";

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
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (value != _selectedTheme)
                {
                    _selectedTheme = value;
                    OnPropertyChangedWithValue(value, nameof(SelectedTheme));
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
            RefreshItems(true);
        }

        private void RefreshItems(bool forceRefresh)
        {
            IllustratorRuntime.AssertMainThread();
            string keepKey = _pendingSelectionKey ?? _selectedItem?.Item?.Key;
            int version = ++_refreshVersion;
            _previewLoader.Cancel();
            ReleasePreviewSprite();
            SelectedSpriteName = string.Empty;
            _selectedItem = null;
            Items.Clear();
            HasSelection = false;
            StatusText = "正在读取画廊目录…";

            if (!IllustratorRuntime.Start(
                () => Task.Run(() => DiskImageCacheManager.GetAllCachedIllustrations(_campaignKey, forceRefresh)),
                (cached, error) =>
                {
                    if (_disposed || version != _refreshVersion) return;
                    if (error != null)
                    {
                        StatusText = "画廊操作失败：" + error.Message;
                        return;
                    }
                    try { ApplyCachedItems(cached, _pendingSelectionKey ?? keepKey); _pendingSelectionKey = null; }
                    catch (Exception ex)
                    {
                        HasSelection = false;
                        SelectedSpriteName = string.Empty;
                        StatusText = "画廊操作失败：" + ex.Message;
                    }
                }))
            {
                StatusText = "画廊后台读取忙碌，请稍后刷新。";
            }
        }

        private void ApplyCachedItems(System.Collections.Generic.List<CachedIllustrationItem> cached, string keepKey = null)
        {
            _cached = cached ?? new System.Collections.Generic.List<CachedIllustrationItem>();
            RebuildVisibleItems(keepKey);
        }

        private void RebuildVisibleItems(string keepKey)
        {
            _previewLoader.Cancel();
            ReleasePreviewSprite();
            SelectedSpriteName = string.Empty;
            _selectedItem = null;
            Items.Clear();
            foreach (var item in _cached)
            {
                if (_showFavorites && !item.IsFavorite) continue;
                Items.Add(new IllustrationItemVM(item, HandleItemSelect));
            }

            if (Items.Count > 0)
            {
                StatusText = _showFavorites ? $"共收藏 {Items.Count} 幅画卷" : $"共收录 {Items.Count} 幅历史画卷";
                IllustrationItemVM target = Items[0];
                if (!string.IsNullOrEmpty(keepKey))
                    foreach (var item in Items)
                        if (string.Equals(item.Item?.Key, keepKey, StringComparison.Ordinal)) { target = item; break; }
                HandleItemSelect(target);
            }
            else
            {
                HasSelection = false;
                OnPropertyChanged(nameof(FavoriteButtonText));
                StatusText = _showFavorites ? "暂无收藏画卷，可在“全部”中选中画卷后点击“收藏”" : "暂无收录画卷，将在周报生成时自动为您绘制";
            }
        }

        public void ExecuteShowAll() => SwitchTab(false);

        public void ExecuteShowFavorites() => SwitchTab(true);

        private void SwitchTab(bool favorites)
        {
            IllustratorRuntime.AssertMainThread();
            if (_disposed || favorites == _showFavorites) return;
            _showFavorites = favorites;
            OnPropertyChanged(nameof(IsFavoritesTab));
            OnPropertyChanged(nameof(IsAllTab));
            try { RebuildVisibleItems(_selectedItem?.Item?.Key); }
            catch (Exception ex) { HasSelection = false; StatusText = "画廊操作失败：" + ex.Message; }
        }

        public void ExecuteToggleFavorite()
        {
            IllustratorRuntime.AssertMainThread();
            var item = _selectedItem?.Item;
            if (_disposed || item == null) return;
            bool favorite = !item.IsFavorite;
            if (!DiskImageCacheManager.SetFavorite(item, _campaignKey, favorite))
            {
                StatusText = favorite ? "收藏失败" : "取消收藏失败";
                return;
            }
            // Patch the in-memory snapshot rather than re-scanning the cache directory.
            foreach (var cachedItem in _cached)
                if (string.Equals(cachedItem.Key, item.Key, StringComparison.Ordinal)) cachedItem.IsFavorite = favorite;
            item.IsFavorite = favorite;
            string key = item.Key;
            string title = _selectedItem.Title;
            if (_showFavorites && !favorite)
            {
                RebuildVisibleItems(null);
                StatusText = "已取消收藏：" + title;
                return;
            }
            _selectedItem.RefreshTitle();
            SelectedTitle = _selectedItem.Title;
            OnPropertyChanged(nameof(FavoriteButtonText));
            StatusText = (favorite ? "已收藏：" : "已取消收藏：") + title;
        }

        private void HandleItemSelect(IllustrationItemVM selected)
        {
            try { HandleItemSelectCore(selected); }
            catch (Exception ex) { HasSelection = false; SelectedSpriteName = string.Empty; StatusText = "画廊操作失败：" + ex.Message; }
        }

        private void HandleItemSelectCore(IllustrationItemVM selected)
        {
            IllustratorRuntime.AssertMainThread();
            foreach (var item in Items)
            {
                item.IsSelected = (item == selected);
            }

            _selectedItem = selected;
            _pendingSelectionKey = null;
            OnPropertyChanged(nameof(FavoriteButtonText));
            _previewLoader.Cancel();
            ReleasePreviewSprite();
            SelectedSpriteName = string.Empty;
            if (selected != null)
            {
                HasSelection = true;
                // 预览 sprite 名必须每次唯一：旧纹理已释放，同名复用会让控件继续持有失效对象而不触发属性通知
                string spriteName = "Gallery_" + selected.Item.Key + "_" + (++_previewCounter);
                SelectedTitle = selected.Title;
                SelectedDate = selected.DateText;
                SelectedTheme = selected.Item.DisplayStatusText;
                StatusText = "正在载入画卷…";
                _previewLoader.Select(selected.Item.FilePath, prepared =>
                {
                    var sprite = GauntletTextureLoader.LoadOrRegisterPreparedImage(spriteName, prepared);
                    if (sprite == null)
                    {
                        HasSelection = false;
                        StatusText = "图片纹理加载失败，请刷新画廊。";
                        return;
                    }
                    _loadedPreviewSpriteName = spriteName;
                    SelectedSpriteName = spriteName;
                    StatusText = selected.Item.DisplayStatusText;
                }, error =>
                {
                    HasSelection = false;
                    StatusText = "画卷载入失败：" + error;
                });
            }
            else
            {
                HasSelection = false;
            }
        }

        public void SelectByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _pendingSelectionKey = key;
            foreach (var item in Items)
            {
                if (string.Equals(item.SpriteName, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Item?.Key, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Item?.SubjectKey, key, StringComparison.OrdinalIgnoreCase))
                {
                    HandleItemSelect(item);
                    break;
                }
            }
        }

        [DataSourceProperty]
        public bool CanConvertToRpItem => !_disposed && !_rpConversionOpen && HasSelection && !string.IsNullOrEmpty(SelectedSpriteName);

        public void ExecuteConvertToRpItem()
        {
            IllustratorRuntime.AssertMainThread();
            if (!CanConvertToRpItem || !_isCurrent() || _selectedItem?.Item == null) return;
            var image = _selectedItem.Item.CopyMetadata();
            var campaign = TaleWorlds.CampaignSystem.Campaign.Current;
            var rewardOwner = RewardSystemBehavior.Instance;
            _rpConversionOpen = true;
            OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
            bool opened = CourierLetterInputPopup.Show("转为 RP 物品", GalleryRpItemConverter.ItemName(image),
                "确认画卷介绍后加入背包。向 NPC 展示时会读取这段介绍；可在此修正画面内容。",
                GalleryRpItemConverter.BuildIntroduction(image), description =>
                {
                    _rpConversionOpen = false;
                    OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
                    if (_disposed || !_isCurrent() || !ReferenceEquals(campaign, TaleWorlds.CampaignSystem.Campaign.Current) ||
                        !ReferenceEquals(rewardOwner, RewardSystemBehavior.Instance)) return;
                    try
                    {
                        GalleryRpItemConverter.TryConvert(image, _campaignKey, description, out string result);
                        StatusText = result;
                    }
                    catch (Exception ex) { StatusText = "转换未完成，请检查背包后再试：" + ex.Message; }
                }, () =>
                {
                    _rpConversionOpen = false;
                    OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
                    if (!_disposed) StatusText = "已取消转换，未添加物品。";
                });
            if (!opened)
            {
                _rpConversionOpen = false;
                OnPropertyChanged(nameof(CanConvertToRpItem));
                    OnPropertyChanged(nameof(CanRedrawBasedOnImage));
                StatusText = "无法打开画卷介绍编辑框。";
            }
        }

        public void ExecuteSetDefault()
        {
            try { ExecuteSetDefaultCore(); }
            catch (Exception ex) { HasSelection = false; SelectedSpriteName = string.Empty; StatusText = "画廊操作失败：" + ex.Message; }
        }

        private void ExecuteSetDefaultCore()
        {
            if (_disposed || !_isCurrent() || _settingDefault || _selectedItem?.Item == null) return;
            var selected = _selectedItem.Item.CopyMetadata();
            _settingDefault = true;
            StatusText = "正在重新读取本地图并设置默认…";
            if (!IllustratorRuntime.Start(() => Task.Run(() => {
                // Re-read the selected file even when its path/key is unchanged after an external replacement.
                selected.ImageData = ImagePayload.ReadFile(selected.FilePath);
                return selected;
            }), (loaded, error) => {
                _settingDefault = false;
                if (_disposed || !_isCurrent()) return;
                if (error != null) { StatusText = "读取本地图失败，默认图未变更：" + error.Message; return; }
                try
                {
                    if (!DiskImageCacheManager.SetDefault(loaded, _campaignKey)) { StatusText = "设为默认失败，请保留图片原名及同名 JSON。"; return; }
                    IllustratorRuntime.PublishDefaultImageChanged(loaded);
                    _pendingSelectionKey = loaded.Key;
                    RefreshItems();
                }
                catch (Exception ex) { StatusText = "设为默认失败：" + ex.Message; }
            })) { _settingDefault = false; StatusText = "图片读取忙碌，请稍后重试。"; }
        }

        public void ExecuteDelete()
        {
            if (_selectedItem?.Item == null) return;
            string title = _selectedItem.Title;
            if (DiskImageCacheManager.DeleteItem(_selectedItem.Item, _campaignKey))
            {
                StatusText = $"已删除：{title}（文件已移入回收区）";
                RefreshItems();
            }
            else
            {
                StatusText = "删除失败";
            }
        }

        public void DisposeVisuals()
        {
            _disposed = true;
            _previewLoader.Dispose();
            SelectedSpriteName = string.Empty;
            ReleasePreviewSprite();
        }

        private void ReleasePreviewSprite()
        {
            if (string.IsNullOrWhiteSpace(_loadedPreviewSpriteName)) return;
            GauntletTextureLoader.ReleaseSprite(_loadedPreviewSpriteName);
            _loadedPreviewSpriteName = null;
        }

        public void ExecuteOpenFolder()
        {
            try
            {
                string cacheDir = Path.Combine(DiskImageCacheManager.CacheRoot, DiskImageCacheManager.SanitizeKey(_campaignKey));
                Directory.CreateDirectory(cacheDir);
                Process.Start("explorer.exe", cacheDir);
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
