using System.ComponentModel;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class MainForm : Form
{
    private readonly PlayerExportsService _service = new();
    private readonly PlayerExportsValidator _validator = new();

    private readonly TextBox _rootBox = new();
    private readonly ListBox _packageList = new();
    private readonly Label _summaryLabel = new();
    private readonly TabControl _tabs = new();
    private readonly DataGridView _knowledgeGrid = new();
    private readonly DataGridView _personaGrid = new();
    private readonly DataGridView _eventGrid = new();
    private readonly DataGridView _issuesGrid = new();
    private readonly TextBox _rawEditor = new();
    private readonly Label _rawFileLabel = new();
    private readonly Label _statusLabel = new();

    private PlayerExportsPackageData? _currentPackage;
    private string? _activeJsonFile;
    private IReadOnlyList<ValidationIssue> _currentIssues = Array.Empty<ValidationIssue>();
    private ConditionCatalog _conditionCatalog = ConditionCatalog.Empty;

    public MainForm()
    {
        Text = "AnimusForge PlayerExports \u7f16\u8f91\u5668";
        Width = 1320;
        Height = 850;
        MinimumSize = new Size(1040, 700);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        ConfigureGrids();
        WireEvents();
        LoadDefaultRoot();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        Controls.Add(root);

        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, Padding = new Padding(8, 6, 8, 4) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        root.Controls.Add(toolbar, 0, 0);

        toolbar.Controls.Add(Label("\u6839\u76ee\u5f55"), 0, 0);
        _rootBox.Dock = DockStyle.Fill;
        toolbar.Controls.Add(_rootBox, 1, 0);
        var browseButton = Button("\u6d4f\u89c8");
        var reloadButton = Button("\u5237\u65b0");
        var newPackageButton = Button("\u65b0\u5efa\u6570\u636e\u5305");
        var deletePackageButton = Button("\u5220\u9664\u6570\u636e\u5305");
        var openFolderButton = Button("\u6253\u5f00\u76ee\u5f55");
        toolbar.Controls.Add(browseButton, 2, 0);
        toolbar.Controls.Add(reloadButton, 3, 0);
        toolbar.Controls.Add(newPackageButton, 4, 0);
        toolbar.Controls.Add(deletePackageButton, 5, 0);
        toolbar.Controls.Add(openFolderButton, 6, 0);

        var mainSplit = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 270 };
        root.Controls.Add(mainSplit, 0, 1);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(Label("\u6570\u636e\u5305"), 0, 0);
        _packageList.Dock = DockStyle.Fill;
        left.Controls.Add(_packageList, 0, 1);
        mainSplit.Panel1.Controls.Add(left);

        var rightSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 440 };
        mainSplit.Panel2.Controls.Add(rightSplit);

        _tabs.Dock = DockStyle.Fill;
        rightSplit.Panel1.Controls.Add(_tabs);
        _tabs.TabPages.Add(BuildSummaryTab());
        _tabs.TabPages.Add(BuildGridTab("\u77e5\u8bc6", _knowledgeGrid));
        _tabs.TabPages.Add(BuildGridTab("\u4eba\u7269\u8d44\u6599", _personaGrid));
        _tabs.TabPages.Add(BuildGridTab("\u58f0\u97f3\u6620\u5c04", CenterLabel("\u70b9\u51fb\u7f16\u8f91\u5b57\u6bb5\u4fee\u6539\u58f0\u97f3\u5206\u7ec4\u548c fallback\u3002")));
        _tabs.TabPages.Add(BuildGridTab("\u4e8b\u4ef6\u6570\u636e", _eventGrid));
        _tabs.TabPages.Add(BuildGridTab("\u672a\u547d\u540dNPC", CenterLabel("\u70b9\u51fb\u7f16\u8f91\u5b57\u6bb5\u4fee\u6539 UnnamedNpcProfiles.json\u3002")));
        _tabs.TabPages.Add(BuildGridTab("\u95ee\u9898", _issuesGrid));

        var rawPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        rawPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        rawPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rawPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        rightSplit.Panel2.Controls.Add(rawPanel);

        _rawFileLabel.Dock = DockStyle.Fill;
        _rawFileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _rawFileLabel.Padding = new Padding(8, 0, 8, 0);
        rawPanel.Controls.Add(_rawFileLabel, 0, 0);

        _rawEditor.Dock = DockStyle.Fill;
        _rawEditor.Multiline = true;
        _rawEditor.AcceptsReturn = true;
        _rawEditor.AcceptsTab = true;
        _rawEditor.ScrollBars = ScrollBars.Both;
        _rawEditor.WordWrap = false;
        _rawEditor.Font = new Font(FontFamily.GenericMonospace, 10f);
        rawPanel.Controls.Add(_rawEditor, 0, 1);

        var rawButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(4) };
        var saveJsonButton = Button("\u4fdd\u5b58JSON", 100);
        var formatJsonButton = Button("\u683c\u5f0f\u5316", 90);
        var editFieldsButton = Button("\u7f16\u8f91\u5b57\u6bb5", 100);
        var newRuleButton = Button("\u65b0\u5efa\u77e5\u8bc6", 90);
        var deleteFileButton = Button("\u5220\u9664\u6587\u4ef6", 90);
        var deleteTypeButton = Button("\u6e05\u7a7a\u5f53\u524d\u7c7b\u578b", 120);
        rawButtons.Controls.Add(saveJsonButton);
        rawButtons.Controls.Add(formatJsonButton);
        rawButtons.Controls.Add(editFieldsButton);
        rawButtons.Controls.Add(newRuleButton);
        rawButtons.Controls.Add(deleteFileButton);
        rawButtons.Controls.Add(deleteTypeButton);
        rawPanel.Controls.Add(rawButtons, 0, 2);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Padding = new Padding(8, 0, 8, 0);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 2);

        browseButton.Click += (_, _) => BrowseRoot();
        reloadButton.Click += (_, _) => LoadPackages();
        newPackageButton.Click += (_, _) => CreatePackage();
        deletePackageButton.Click += (_, _) => DeleteSelectedPackage();
        openFolderButton.Click += (_, _) => OpenRootInExplorer();
        saveJsonButton.Click += (_, _) => SaveActiveRawJson();
        formatJsonButton.Click += (_, _) => FormatActiveRawJson();
        editFieldsButton.Click += (_, _) => EditActiveStructuredDocument();
        newRuleButton.Click += (_, _) => CreateKnowledgeRule();
        deleteFileButton.Click += (_, _) => DeleteActiveJsonFile();
        deleteTypeButton.Click += (_, _) => DeleteCurrentDataType();
    }

    private TabPage BuildSummaryTab()
    {
        var tab = new TabPage("\u6982\u89c8");
        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.Padding = new Padding(12);
        _summaryLabel.TextAlign = ContentAlignment.TopLeft;
        tab.Controls.Add(_summaryLabel);
        return tab;
    }

    private static TabPage BuildGridTab(string title, Control content)
    {
        var tab = new TabPage(title);
        content.Dock = DockStyle.Fill;
        tab.Controls.Add(content);
        return tab;
    }

    private void ConfigureGrids()
    {
        ConfigureGrid(_knowledgeGrid);
        _knowledgeGrid.Columns.Add(Column("RuleId", "RuleId", 190));
        _knowledgeGrid.Columns.Add(Column("FirstKeyword", "\u5173\u952e\u8bcd", 190));
        _knowledgeGrid.Columns.Add(Column("RagShortTextCount", "RAG", 55));
        _knowledgeGrid.Columns.Add(Column("VariantCount", "\u63d0\u793a\u8bcd", 70));
        _knowledgeGrid.Columns.Add(Column("TextMappingCount", "\u6620\u5c04", 76));
        _knowledgeGrid.Columns.Add(Column("FileName", "\u6587\u4ef6", 300));

        ConfigureGrid(_personaGrid);
        _personaGrid.Columns.Add(Column("EntityId", "ID", 190));
        _personaGrid.Columns.Add(Column("DisplayName", "\u540d\u79f0", 160));
        _personaGrid.Columns.Add(Column("HasPersonality", "\u4e2a\u6027", 86));
        _personaGrid.Columns.Add(Column("HasBackground", "\u80cc\u666f", 88));
        _personaGrid.Columns.Add(Column("VoiceId", "VoiceId", 230));
        _personaGrid.Columns.Add(Column("FileName", "\u6587\u4ef6", 300));

        ConfigureGrid(_eventGrid);
        _eventGrid.Columns.Add(Column("FileName", "\u6587\u4ef6", 280));
        _eventGrid.Columns.Add(Column("Kind", "\u7c7b\u578b", 120));
        _eventGrid.Columns.Add(Column("Size", "\u5b57\u7b26\u6570", 90));

        ConfigureGrid(_issuesGrid);
        _issuesGrid.Columns.Add(Column("Severity", "\u7ea7\u522b", 80));
        _issuesGrid.Columns.Add(Column("Area", "\u5206\u533a", 100));
        _issuesGrid.Columns.Add(Column("FileName", "\u6587\u4ef6", 240));
        _issuesGrid.Columns.Add(Column("Message", "\u95ee\u9898", 680));
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.Dock = DockStyle.Fill;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    private void WireEvents()
    {
        _packageList.SelectedIndexChanged += (_, _) => LoadSelectedPackage();
        _knowledgeGrid.SelectionChanged += (_, _) => LoadSelectedKnowledgeRaw();
        _personaGrid.SelectionChanged += (_, _) => LoadSelectedPersonaRaw();
        _eventGrid.SelectionChanged += (_, _) => LoadSelectedEventRaw();
        _knowledgeGrid.CellDoubleClick += (_, _) => EditActiveStructuredDocument();
        _personaGrid.CellDoubleClick += (_, _) => EditActiveStructuredDocument();
        _eventGrid.CellDoubleClick += (_, _) => LoadSelectedEventRaw();
        _tabs.SelectedIndexChanged += (_, _) => LoadRawForCurrentTab();
    }

    private void LoadDefaultRoot()
    {
        var defaultRoot = _service.FindDefaultPlayerExportsRoot(AppContext.BaseDirectory) ??
                          _service.FindDefaultPlayerExportsRoot(Directory.GetCurrentDirectory());
        if (!string.IsNullOrWhiteSpace(defaultRoot))
        {
            _rootBox.Text = defaultRoot;
            LoadPackages();
        }
    }

    private void BrowseRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "\u9009\u62e9 PlayerExports \u6839\u76ee\u5f55",
            SelectedPath = Directory.Exists(_rootBox.Text) ? _rootBox.Text : Directory.GetCurrentDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _rootBox.Text = dialog.SelectedPath;
            LoadPackages();
        }
    }

    private void LoadPackages()
    {
        try
        {
            var packages = _service.ListPackages(_rootBox.Text).ToList();
            _packageList.DataSource = packages;
            _packageList.DisplayMember = nameof(PlayerExportsPackageInfo.Name);
            SetStatus("\u627e\u5230 " + packages.Count + " \u4e2a\u6570\u636e\u5305\u3002");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u52a0\u8f7d\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CreatePackage()
    {
        var name = PromptDialog.Show(this, "\u65b0\u5efa\u6570\u636e\u5305", "\u6570\u636e\u5305\u540d\u79f0\uff1a", "NewPackage");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            _service.CreatePackage(_rootBox.Text, name);
            LoadPackages();
            SetStatus("\u5df2\u65b0\u5efa\u6570\u636e\u5305\uff1a" + name);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u65b0\u5efa\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteSelectedPackage()
    {
        if (_packageList.SelectedItem is not PlayerExportsPackageInfo package)
        {
            return;
        }

        var result = MessageBox.Show(this, "\u5c06\u8fd9\u4e2a\u6570\u636e\u5305\u79fb\u52a8\u5230 .deleted_packages\uff1f\r\n" + package.FullPath, "\u5220\u9664\u6570\u636e\u5305", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var deletedPath = _service.MovePackageToDeleted(_rootBox.Text, package.FullPath);
            _currentPackage = null;
            ClearViews();
            LoadPackages();
            SetStatus("\u5df2\u79fb\u52a8\u5230\uff1a" + deletedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u5220\u9664\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenRootInExplorer()
    {
        if (!Directory.Exists(_rootBox.Text))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _rootBox.Text, UseShellExecute = true });
    }

    private void LoadSelectedPackage()
    {
        if (_packageList.SelectedItem is not PlayerExportsPackageInfo package)
        {
            return;
        }

        try
        {
            _currentPackage = _service.LoadPackage(package.FullPath);
            _currentIssues = _validator.Validate(_currentPackage);
            _conditionCatalog = BuildConditionCatalog(_currentPackage);
            RefreshViews();
            SetStatus("\u5df2\u6253\u5f00\uff1a" + package.Name + " | \u6761\u4ef6\u5019\u9009\uff1a" + _conditionCatalog.Summary);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u6253\u5f00\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshViews()
    {
        if (_currentPackage == null)
        {
            ClearViews();
            return;
        }

        var errorCount = _currentIssues.Count(x => x.Severity == ValidationSeverity.Error);
        var warningCount = _currentIssues.Count(x => x.Severity == ValidationSeverity.Warning);
        _summaryLabel.Text =
            "\u6570\u636e\u5305\uff1a" + _currentPackage.Info.Name + Environment.NewLine +
            "\u8def\u5f84\uff1a" + _currentPackage.Info.FullPath + Environment.NewLine +
            "\u77e5\u8bc6\u89c4\u5219\uff1a" + _currentPackage.KnowledgeRules.Count + Environment.NewLine +
            "\u4eba\u7269\u8d44\u6599\uff1a" + _currentPackage.Personas.Count + Environment.NewLine +
            "\u4e8b\u4ef6\u6587\u4ef6\uff1a" + _currentPackage.EventFiles.Count + Environment.NewLine +
            "\u58f0\u97f3\u6620\u5c04\uff1a" + (_currentPackage.VoiceMapping == null ? "\u7f3a\u5931" : "\u5df2\u52a0\u8f7d") + Environment.NewLine +
            "\u672a\u547d\u540d NPC\uff1a" + (_currentPackage.UnnamedPersona == null ? "\u7f3a\u5931" : "\u5df2\u52a0\u8f7d") + Environment.NewLine +
            "\u6761\u4ef6\u5019\u9009\uff1a" + _conditionCatalog.Summary + Environment.NewLine +
            "\u95ee\u9898\uff1aError " + errorCount + " / Warning " + warningCount;

        _knowledgeGrid.DataSource = _currentPackage.KnowledgeRules.Select(x => new KnowledgeRow(x)).ToList();
        _personaGrid.DataSource = _currentPackage.Personas.Select(x => new PersonaRow(x)).ToList();
        _eventGrid.DataSource = _currentPackage.EventFiles.Select(x => new JsonFileRow(x)).ToList();
        _issuesGrid.DataSource = _currentIssues.Select(x => new IssueRow(x)).ToList();
        LoadRawForCurrentTab();
    }

    private void ClearViews()
    {
        _summaryLabel.Text = "";
        _knowledgeGrid.DataSource = null;
        _personaGrid.DataSource = null;
        _eventGrid.DataSource = null;
        _issuesGrid.DataSource = null;
        _conditionCatalog = ConditionCatalog.Empty;
        LoadRaw(null, "");
    }

    private void LoadRawForCurrentTab()
    {
        if (_currentPackage == null)
        {
            return;
        }

        switch (_tabs.SelectedTab?.Text)
        {
            case "\u77e5\u8bc6":
                LoadSelectedKnowledgeRaw();
                break;
            case "\u4eba\u7269\u8d44\u6599":
                LoadSelectedPersonaRaw();
                break;
            case "\u58f0\u97f3\u6620\u5c04":
                LoadJsonRaw(_currentPackage.VoiceMapping);
                break;
            case "\u4e8b\u4ef6\u6570\u636e":
                LoadSelectedEventRaw();
                break;
            case "\u672a\u547d\u540dNPC":
                LoadJsonRaw(_currentPackage.UnnamedPersona);
                break;
        }
    }

    private void LoadSelectedKnowledgeRaw()
    {
        if (_knowledgeGrid.CurrentRow?.DataBoundItem is KnowledgeRow row)
        {
            LoadRaw(row.Document.FilePath, row.Document.RawJson);
        }
    }

    private void LoadSelectedPersonaRaw()
    {
        if (_personaGrid.CurrentRow?.DataBoundItem is PersonaRow row)
        {
            LoadRaw(row.Document.FilePath, row.Document.RawJson);
        }
    }

    private void LoadSelectedEventRaw()
    {
        if (_eventGrid.CurrentRow?.DataBoundItem is JsonFileRow row)
        {
            LoadJsonRaw(row.Document);
        }
    }

    private void LoadJsonRaw(JsonFileDocument? document)
    {
        if (document == null)
        {
            LoadRaw(null, "");
            return;
        }

        LoadRaw(document.FilePath, document.RawJson);
    }

    private void LoadRaw(string? filePath, string raw)
    {
        _activeJsonFile = filePath;
        _rawFileLabel.Text = string.IsNullOrWhiteSpace(filePath) ? "\u672a\u9009\u62e9 JSON \u6587\u4ef6" : filePath;
        _rawEditor.Text = raw;
    }

    private void SaveActiveRawJson()
    {
        if (_currentPackage == null || string.IsNullOrWhiteSpace(_activeJsonFile))
        {
            return;
        }

        try
        {
            var backup = _service.SaveJsonDocument(_currentPackage.Info.FullPath, _activeJsonFile, _rawEditor.Text);
            ReloadCurrentPackage();
            SetStatus(string.IsNullOrWhiteSpace(backup) ? "\u5df2\u4fdd\u5b58\u3002" : "\u5df2\u4fdd\u5b58\uff0c\u5907\u4efd\uff1a" + backup);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u4fdd\u5b58\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FormatActiveRawJson()
    {
        try
        {
            _rawEditor.Text = _service.FormatJson(_rawEditor.Text);
            SetStatus("JSON \u5df2\u683c\u5f0f\u5316\u3002");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u683c\u5f0f\u5316\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EditActiveStructuredDocument()
    {
        if (_currentPackage == null || string.IsNullOrWhiteSpace(_activeJsonFile))
        {
            return;
        }

        try
        {
            if (TryEditKnowledge())
            {
                return;
            }

            if (TryEditPersona())
            {
                return;
            }

            if (TryEditVoiceMapping())
            {
                return;
            }

            if (TryEditWorldSummary())
            {
                return;
            }

            if (TryEditKingdomSummaries())
            {
                return;
            }

            if (TryEditUnnamedPersona())
            {
                return;
            }

            MessageBox.Show(this, "\u8fd9\u4e2a\u6587\u4ef6\u6682\u65f6\u6ca1\u6709\u4e13\u7528\u8868\u5355\uff0c\u53ef\u4ee5\u7ee7\u7eed\u4f7f\u7528\u539f\u59cb JSON \u7f16\u8f91\u3002", "\u7f16\u8f91\u5b57\u6bb5", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u7f16\u8f91\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool TryEditKnowledge()
    {
        if (_currentPackage == null || _knowledgeGrid.CurrentRow?.DataBoundItem is not KnowledgeRow row || !SameFile(row.Document.FilePath, _activeJsonFile))
        {
            return false;
        }

        if (row.Document.Rule == null)
        {
            MessageBox.Show(this, "\u8fd9\u4e2a\u77e5\u8bc6 JSON \u65e0\u6cd5\u89e3\u6790\u4e3a\u77e5\u8bc6\u89c4\u5219\u3002", "\u7f16\u8f91\u5b57\u6bb5", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }

        using var dialog = new KnowledgeRuleEditorForm(row.Document.Rule, _conditionCatalog);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SaveKnowledgeRule(_currentPackage.Info.FullPath, row.Document.FilePath, dialog.Rule);
            ReloadCurrentPackage();
            SetStatus("\u77e5\u8bc6\u89c4\u5219\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private bool TryEditPersona()
    {
        if (_currentPackage == null || _personaGrid.CurrentRow?.DataBoundItem is not PersonaRow row || !SameFile(row.Document.FilePath, _activeJsonFile))
        {
            return false;
        }

        using var dialog = new PersonaEditorForm(row.Document.EntityId, row.Document.DisplayName, row.Document.Profile);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SavePersonaProfile(_currentPackage.Info.FullPath, row.Document.FilePath, dialog.Profile);
            ReloadCurrentPackage();
            SetStatus("\u4eba\u7269\u8d44\u6599\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private bool TryEditVoiceMapping()
    {
        if (_currentPackage?.VoiceMapping == null || !SameFile(_currentPackage.VoiceMapping.FilePath, _activeJsonFile))
        {
            return false;
        }

        using var dialog = new VoiceMappingEditorForm(_currentPackage.VoiceMapping.RawJson);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SaveJsonDocument(_currentPackage.Info.FullPath, _currentPackage.VoiceMapping.FilePath, dialog.Json);
            ReloadCurrentPackage();
            SetStatus("\u58f0\u97f3\u6620\u5c04\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private bool TryEditWorldSummary()
    {
        if (_currentPackage == null || string.IsNullOrWhiteSpace(_activeJsonFile) ||
            !Path.GetFileName(_activeJsonFile).Equals("WorldOpeningSummary.json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using var dialog = new WorldSummaryEditorForm(_rawEditor.Text);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SaveJsonDocument(_currentPackage.Info.FullPath, _activeJsonFile, dialog.Json);
            ReloadCurrentPackage();
            SetStatus("\u4e16\u754c\u5f00\u5c40\u6982\u8981\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private bool TryEditKingdomSummaries()
    {
        if (_currentPackage == null || string.IsNullOrWhiteSpace(_activeJsonFile) ||
            !Path.GetFileName(_activeJsonFile).Equals("KingdomOpeningSummaries.json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using var dialog = new StringMapEditorForm("\u738b\u56fd\u5f00\u5c40\u6982\u8981", "KingdomId", "\u6982\u8981", _rawEditor.Text);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SaveJsonDocument(_currentPackage.Info.FullPath, _activeJsonFile, dialog.Json);
            ReloadCurrentPackage();
            SetStatus("\u738b\u56fd\u5f00\u5c40\u6982\u8981\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private bool TryEditUnnamedPersona()
    {
        if (_currentPackage?.UnnamedPersona == null || !SameFile(_currentPackage.UnnamedPersona.FilePath, _activeJsonFile))
        {
            return false;
        }

        using var dialog = new UnnamedPersonaEditorForm(_currentPackage.UnnamedPersona.RawJson);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _service.SaveJsonDocument(_currentPackage.Info.FullPath, _currentPackage.UnnamedPersona.FilePath, dialog.Json);
            ReloadCurrentPackage();
            SetStatus("\u672a\u547d\u540d NPC \u8d44\u6599\u5df2\u4fdd\u5b58\u3002");
        }

        return true;
    }

    private void CreateKnowledgeRule()
    {
        if (_currentPackage == null)
        {
            return;
        }

        using var dialog = new KnowledgeRuleEditorForm(null, _conditionCatalog);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var filePath = _service.CreateKnowledgeRule(_currentPackage.Info.FullPath, dialog.Rule);
            ReloadCurrentPackage();
            SetStatus("\u5df2\u65b0\u5efa\u77e5\u8bc6\uff1a" + filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u65b0\u5efa\u77e5\u8bc6\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteActiveJsonFile()
    {
        if (_currentPackage == null || string.IsNullOrWhiteSpace(_activeJsonFile))
        {
            return;
        }

        var result = MessageBox.Show(this, "\u5c06\u8fd9\u4e2a JSON \u6587\u4ef6\u79fb\u52a8\u5230 .deleted_files\uff1f\r\n" + _activeJsonFile, "\u5220\u9664\u6587\u4ef6", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var deletedPath = _service.MoveJsonFileToDeleted(_currentPackage.Info.FullPath, _activeJsonFile);
            ReloadCurrentPackage();
            SetStatus("\u6587\u4ef6\u5df2\u79fb\u52a8\u5230\uff1a" + deletedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u5220\u9664\u6587\u4ef6\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteCurrentDataType()
    {
        if (_currentPackage == null)
        {
            return;
        }

        var target = GetCurrentDataTypeDeleteTarget();
        if (!target.HasValue)
        {
            MessageBox.Show(this, "\u8bf7\u5148\u5207\u6362\u5230\u77e5\u8bc6\u3001\u4eba\u7269\u8d44\u6599\u3001\u58f0\u97f3\u6620\u5c04\u3001\u4e8b\u4ef6\u6570\u636e\u6216\u672a\u547d\u540dNPC\u9875\u7b7e\u3002", "\u6e05\u7a7a\u5f53\u524d\u7c7b\u578b", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var files = _service.ListDataTypeJsonFiles(_currentPackage.Info.FullPath, target.Value.DataType);
            if (files.Count == 0)
            {
                MessageBox.Show(this, "\u5f53\u524d\u7c7b\u578b\u6ca1\u6709\u53ef\u5220\u9664\u7684 JSON \u6570\u636e\u6587\u4ef6\u3002", "\u6e05\u7a7a\u5f53\u524d\u7c7b\u578b", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = "\u5c06\u5f53\u524d\u7c7b\u578b\u7684\u6240\u6709 JSON \u6570\u636e\u6587\u4ef6\u79fb\u52a8\u5230 .deleted_files\uff1f\r\n" +
                          "\u7c7b\u578b\uff1a" + target.Value.Label + "\r\n" +
                          "\u6587\u4ef6\u6570\uff1a" + files.Count + "\r\n\r\n" +
                          BuildDeletePreview(_currentPackage.Info.FullPath, files);
            var result = MessageBox.Show(this, message, "\u6e05\u7a7a\u5f53\u524d\u7c7b\u578b", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            var deleted = _service.MoveDataTypeToDeleted(_currentPackage.Info.FullPath, target.Value.DataType);
            ReloadCurrentPackage();
            SetStatus(target.Value.Label + "\u5df2\u6e05\u7a7a\uff1a" + deleted.MovedFiles.Count + "\u4e2a\u6587\u4ef6\uff0c\u5df2\u79fb\u52a8\u5230\uff1a" + deleted.DeletedRoot);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u6e05\u7a7a\u5f53\u524d\u7c7b\u578b\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private (PlayerExportsDataType DataType, string Label)? GetCurrentDataTypeDeleteTarget()
    {
        return _tabs.SelectedTab?.Text switch
        {
            "\u77e5\u8bc6" => (PlayerExportsDataType.Knowledge, "\u77e5\u8bc6"),
            "\u4eba\u7269\u8d44\u6599" => (PlayerExportsDataType.PersonalityBackground, "\u4eba\u7269\u8d44\u6599"),
            "\u58f0\u97f3\u6620\u5c04" => (PlayerExportsDataType.VoiceMapping, "\u58f0\u97f3\u6620\u5c04"),
            "\u4e8b\u4ef6\u6570\u636e" => (PlayerExportsDataType.EventData, "\u4e8b\u4ef6\u6570\u636e"),
            "\u672a\u547d\u540dNPC" => (PlayerExportsDataType.UnnamedPersona, "\u672a\u547d\u540dNPC"),
            _ => null
        };
    }

    private static string BuildDeletePreview(string packageRoot, IReadOnlyList<string> files)
    {
        var preview = files
            .Take(12)
            .Select(file => " - " + SafeRelativePath(packageRoot, file))
            .ToList();
        if (files.Count > 12)
        {
            preview.Add(" - ...");
        }

        return string.Join(Environment.NewLine, preview);
    }

    private static string SafeRelativePath(string root, string file)
    {
        try
        {
            return Path.GetRelativePath(root, file);
        }
        catch
        {
            return file;
        }
    }

    private void ReloadCurrentPackage()
    {
        if (_currentPackage == null)
        {
            return;
        }

        var packagePath = _currentPackage.Info.FullPath;
        _currentPackage = _service.LoadPackage(packagePath);
        _currentIssues = _validator.Validate(_currentPackage);
        _conditionCatalog = BuildConditionCatalog(_currentPackage);
        RefreshViews();
    }

    private static ConditionCatalog BuildConditionCatalog(PlayerExportsPackageData package)
    {
        return new ConditionCatalogBuilder().Build(package, AppContext.BaseDirectory);
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = DateTime.Now.ToString("HH:mm:ss") + "  " + text;
    }

    private static bool SameFile(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) };
    }

    private static Label CenterLabel(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    }

    private static Button Button(string text, int width = 0)
    {
        return new Button { Text = text, Dock = width <= 0 ? DockStyle.Fill : DockStyle.None, Width = width <= 0 ? 75 : width };
    }

    private static DataGridViewTextBoxColumn Column(string property, string header, int width)
    {
        return new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = header, Width = width };
    }

    private sealed class KnowledgeRow
    {
        public KnowledgeRow(KnowledgeRuleDocument document)
        {
            Document = document;
        }

        [Browsable(false)]
        public KnowledgeRuleDocument Document { get; }

        public string RuleId => Document.RuleId;

        public string FirstKeyword => Document.FirstKeyword;

        public int RagShortTextCount => Document.RagShortTextCount;

        public int VariantCount => Document.VariantCount;

        public int TextMappingCount => Document.TextMappingCount;

        public string FileName => Document.FileName;
    }

    private sealed class PersonaRow
    {
        public PersonaRow(NpcPersonaDocument document)
        {
            Document = document;
        }

        [Browsable(false)]
        public NpcPersonaDocument Document { get; }

        public string EntityId => Document.EntityId;

        public string DisplayName => Document.DisplayName;

        public string HasPersonality => string.IsNullOrWhiteSpace(Document.Profile?.Personality) ? "" : "Yes";

        public string HasBackground => string.IsNullOrWhiteSpace(Document.Profile?.Background) ? "" : "Yes";

        public string VoiceId => Document.Profile?.VoiceId ?? "";

        public string FileName => Document.FileName;
    }

    private sealed class JsonFileRow
    {
        public JsonFileRow(JsonFileDocument document)
        {
            Document = document;
        }

        [Browsable(false)]
        public JsonFileDocument Document { get; }

        public string FileName => Document.FileName;

        public string Kind => Document.Root?.GetType().Name.Replace("Json", "", StringComparison.Ordinal) ?? "Invalid";

        public int Size => Document.RawJson.Length;
    }

    private sealed class IssueRow
    {
        public IssueRow(ValidationIssue issue)
        {
            Severity = issue.Severity.ToString();
            Area = issue.Area;
            FileName = issue.FileName;
            Message = issue.Message;
        }

        public string Severity { get; }

        public string Area { get; }

        public string FileName { get; }

        public string Message { get; }
    }
}
