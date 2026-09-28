using System.Diagnostics;
using PreprocessTopicPromptLab.Core;

namespace PreprocessTopicPromptLab.App;

public sealed class MainForm : Form
{
    private readonly PreprocessTopicLabService _service = new();
    private readonly JsonFileStore _json = new();

    private readonly TextBox _repoRootBox = new();
    private readonly TextBox _caseFileBox = new();
    private readonly TextBox _apiUrlBox = new();
    private readonly TextBox _apiKeyBox = new();
    private readonly TextBox _modelBox = new();
    private readonly NumericUpDown _temperatureBox = new();
    private readonly NumericUpDown _maxTokensBox = new();
    private readonly CheckBox _thinkingBox = new();
    private readonly ComboBox _reasoningBox = new();
    private readonly TextBox _promptVersionBox = new();
    private readonly ListBox _caseList = new();
    private readonly ListBox _topicList = new();
    private readonly TextBox _caseIdBox = new();
    private readonly TextBox _titleBox = new();
    private readonly TextBox _playerTextBox = new();
    private readonly TextBox _npcReplyBox = new();
    private readonly TextBox _historyBox = new();
    private readonly TextBox _afefPlayerBox = new();
    private readonly TextBox _afefNpcBox = new();
    private readonly TextBox _runtimeBox = new();
    private readonly TextBox _expectedBox = new();
    private readonly TextBox _allowedBox = new();
    private readonly TextBox _forbiddenBox = new();
    private readonly TextBox _notesBox = new();
    private readonly TextBox _systemPromptBox = new();
    private readonly TextBox _userTemplateBox = new();
    private readonly NumericUpDown _ruleMaxCharsBox = new();
    private readonly TextBox _promptPreviewBox = new();
    private readonly TextBox _requestBox = new();
    private readonly TextBox _responseBox = new();
    private readonly TextBox _scoreBox = new();
    private readonly TextBox _injectedRulesBox = new();
    private readonly TextBox _filesBox = new();
    private readonly Label _statusLabel = new();

    private PromptCatalog _catalog = new();
    private PreprocessPromptConfig _promptConfig = new();
    private PreprocessLabSettings _settings = new();
    private readonly List<PreprocessLabCase> _cases = new();
    private string _labRoot = "";
    private string _lastRunDir = "";
    private bool _loadingCase;

    private sealed class LocalSettings
    {
        public string RepoRoot { get; set; } = "";
        public string CaseFilePath { get; set; } = "";
        public string PromptVersionPath { get; set; } = "";
        public PreprocessLabSettings Api { get; set; } = new();
        public PreprocessPromptConfig Prompt { get; set; } = new();
    }

    public MainForm()
    {
        Text = "AnimusForge 前处理话题原链路测试器";
        Width = 1380;
        Height = 900;
        MinimumSize = new Size(1100, 740);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        LoadDefaults();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        Controls.Add(root);

        var repoBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 8, Padding = new Padding(8, 6, 8, 4) };
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        repoBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        root.Controls.Add(repoBar, 0, 0);

        repoBar.Controls.Add(Label("仓库"), 0, 0);
        _repoRootBox.Dock = DockStyle.Fill;
        repoBar.Controls.Add(_repoRootBox, 1, 0);
        var browseRepoButton = Button("浏览");
        var reloadButton = Button("刷新");
        var saveSettingsButton = Button("保存");
        var openRunsButton = Button("运行目录", 90);
        var loadPromptButton = Button("原链路只读", 92);
        loadPromptButton.Enabled = false;
        repoBar.Controls.Add(browseRepoButton, 2, 0);
        repoBar.Controls.Add(reloadButton, 3, 0);
        repoBar.Controls.Add(saveSettingsButton, 4, 0);
        repoBar.Controls.Add(openRunsButton, 5, 0);
        repoBar.Controls.Add(loadPromptButton, 6, 0);
        _promptVersionBox.Dock = DockStyle.Fill;
        _promptVersionBox.PlaceholderText = "前处理基准固定使用 mod 原链路结构；旧提示词版本不会参与运行";
        repoBar.Controls.Add(_promptVersionBox, 7, 0);

        var apiBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 14, Padding = new Padding(8, 4, 8, 4) };
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        apiBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        root.Controls.Add(apiBar, 0, 1);

        apiBar.Controls.Add(Label("接口"), 0, 0);
        _apiUrlBox.Dock = DockStyle.Fill;
        _apiUrlBox.PlaceholderText = "OpenAI: https://.../v1/chat/completions；Anthropic: https://api.deepseek.com/anthropic";
        apiBar.Controls.Add(_apiUrlBox, 1, 0);
        apiBar.Controls.Add(Label("密钥"), 2, 0);
        _apiKeyBox.Dock = DockStyle.Fill;
        _apiKeyBox.UseSystemPasswordChar = true;
        apiBar.Controls.Add(_apiKeyBox, 3, 0);
        apiBar.Controls.Add(Label("模型"), 4, 0);
        _modelBox.Dock = DockStyle.Fill;
        apiBar.Controls.Add(_modelBox, 5, 0);
        apiBar.Controls.Add(Label("温度"), 6, 0);
        _temperatureBox.DecimalPlaces = 2;
        _temperatureBox.Increment = 0.05m;
        _temperatureBox.Maximum = 2;
        _temperatureBox.Dock = DockStyle.Fill;
        apiBar.Controls.Add(_temperatureBox, 7, 0);
        apiBar.Controls.Add(Label("输出"), 8, 0);
        _maxTokensBox.Minimum = 16;
        _maxTokensBox.Maximum = 200000;
        _maxTokensBox.Increment = 128;
        _maxTokensBox.Dock = DockStyle.Fill;
        apiBar.Controls.Add(_maxTokensBox, 9, 0);
        apiBar.Controls.Add(Label("思考"), 10, 0);
        _thinkingBox.Text = "开启";
        _thinkingBox.Dock = DockStyle.Fill;
        apiBar.Controls.Add(_thinkingBox, 11, 0);
        apiBar.Controls.Add(Label("强度"), 12, 0);
        _reasoningBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _reasoningBox.Dock = DockStyle.Fill;
        foreach (var effort in PreprocessTopicLabService.GetReasoningEffortOptions())
        {
            _reasoningBox.Items.Add(effort);
        }
        apiBar.Controls.Add(_reasoningBox, 13, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 340 };
        root.Controls.Add(split, 0, 2);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, Padding = new Padding(8, 4, 4, 4) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        split.Panel1.Controls.Add(left);

        var caseFilePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        caseFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        caseFilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        _caseFileBox.Dock = DockStyle.Fill;
        caseFilePanel.Controls.Add(_caseFileBox, 0, 0);
        var browseCaseButton = Button("案例");
        caseFilePanel.Controls.Add(browseCaseButton, 1, 0);
        left.Controls.Add(caseFilePanel, 0, 0);

        _caseList.Dock = DockStyle.Fill;
        _caseList.DisplayMember = nameof(PreprocessLabCase.DisplayName);
        left.Controls.Add(_caseList, 0, 1);

        left.Controls.Add(Label("全部话题"), 0, 2);
        _topicList.Dock = DockStyle.Fill;
        left.Controls.Add(_topicList, 0, 3);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 4, 0, 0) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        var newCaseButton = Button("新建");
        var saveCaseButton = Button("保存案例");
        var renderButton = Button("生成请求");
        var runButton = Button("运行当前");
        var runAllButton = Button("批量运行全部");
        buttons.Controls.Add(newCaseButton, 0, 0);
        buttons.Controls.Add(saveCaseButton, 1, 0);
        buttons.Controls.Add(renderButton, 0, 1);
        buttons.Controls.Add(runButton, 1, 1);
        buttons.Controls.Add(runAllButton, 0, 2);
        buttons.SetColumnSpan(runAllButton, 2);
        left.Controls.Add(buttons, 0, 4);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        split.Panel2.Controls.Add(tabs);
        tabs.TabPages.Add(BuildCaseTab());
        tabs.TabPages.Add(BuildPromptTab());
        tabs.TabPages.Add(BuildResultTab());

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(8, 0, 0, 0);
        root.Controls.Add(_statusLabel, 0, 3);

        browseRepoButton.Click += (_, _) => BrowseFolder(_repoRootBox);
        browseCaseButton.Click += (_, _) => BrowseCaseFile();
        reloadButton.Click += (_, _) => ReloadAll();
        saveSettingsButton.Click += (_, _) => SaveLocalSettings();
        openRunsButton.Click += (_, _) => OpenLastRunDir();
        loadPromptButton.Click += (_, _) => LoadPromptVersionFromPath();
        newCaseButton.Click += (_, _) => NewCase();
        saveCaseButton.Click += (_, _) => SaveCurrentCase();
        renderButton.Click += (_, _) => RenderCurrent();
        runButton.Click += async (_, _) => await RunCurrentAsync();
        runAllButton.Click += async (_, _) => await RunAllAsync();
        _caseList.SelectedIndexChanged += (_, _) => LoadSelectedCase();
    }

    private TabPage BuildCaseTab()
    {
        var page = new TabPage("案例");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 8, Padding = new Padding(8) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 12));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 8));
        page.Controls.Add(grid);

        grid.Controls.Add(Label("案例ID"), 0, 0);
        _caseIdBox.Dock = DockStyle.Fill;
        grid.Controls.Add(_caseIdBox, 1, 0);
        grid.Controls.Add(Label("标题"), 2, 0);
        _titleBox.Dock = DockStyle.Fill;
        grid.Controls.Add(_titleBox, 3, 0);

        AddLabeledText(grid, "玩家本轮", _playerTextBox, 0, 1);
        AddLabeledText(grid, "NPC回复", _npcReplyBox, 2, 1);
        AddLabeledText(grid, "普通历史", _historyBox, 0, 2);
        AddLabeledText(grid, "运行时事实", _runtimeBox, 2, 2);
        AddLabeledText(grid, "AFEF玩家", _afefPlayerBox, 0, 3);
        AddLabeledText(grid, "AFEF NPC", _afefNpcBox, 2, 3);
        AddLabeledText(grid, "期望话题", _expectedBox, 0, 4);
        AddLabeledText(grid, "允许额外", _allowedBox, 2, 4);
        AddLabeledText(grid, "禁止话题", _forbiddenBox, 0, 5);
        AddLabeledText(grid, "备注", _notesBox, 2, 5);

        var hint = Label("话题每行一个或用逗号分隔；AFEF事实每行一条。");
        grid.Controls.Add(hint, 0, 7);
        grid.SetColumnSpan(hint, 4);
        return page;
    }

    private TabPage BuildPromptTab()
    {
        var page = new TabPage("提示词与请求");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        page.Controls.Add(split);

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(8) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel1.Controls.Add(top);

        top.Controls.Add(Label("mod system"), 0, 0);
        top.Controls.Add(Label("mod user结构"), 1, 0);
        top.Controls.Add(Label("返回上限"), 2, 0);
        _systemPromptBox.Dock = DockStyle.Fill;
        _systemPromptBox.Multiline = true;
        _systemPromptBox.ScrollBars = ScrollBars.Vertical;
        _systemPromptBox.ReadOnly = true;
        _userTemplateBox.Dock = DockStyle.Fill;
        _userTemplateBox.Multiline = true;
        _userTemplateBox.ScrollBars = ScrollBars.Vertical;
        _userTemplateBox.ReadOnly = true;
        _ruleMaxCharsBox.Minimum = 1;
        _ruleMaxCharsBox.Maximum = 12;
        _ruleMaxCharsBox.Increment = 1;
        _ruleMaxCharsBox.Dock = DockStyle.Top;
        _ruleMaxCharsBox.Enabled = false;
        top.Controls.Add(_systemPromptBox, 0, 1);
        top.Controls.Add(_userTemplateBox, 1, 1);
        top.Controls.Add(_ruleMaxCharsBox, 2, 1);

        var bottom = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
        split.Panel2.Controls.Add(bottom);
        _promptPreviewBox.Dock = DockStyle.Fill;
        _promptPreviewBox.Multiline = true;
        _promptPreviewBox.ScrollBars = ScrollBars.Both;
        _promptPreviewBox.WordWrap = false;
        _requestBox.Dock = DockStyle.Fill;
        _requestBox.Multiline = true;
        _requestBox.ScrollBars = ScrollBars.Both;
        _requestBox.WordWrap = false;
        bottom.Panel1.Controls.Add(_promptPreviewBox);
        bottom.Panel2.Controls.Add(_requestBox);
        return page;
    }

    private TabPage BuildResultTab()
    {
        var page = new TabPage("结果");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
        page.Controls.Add(split);
        var top = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
        split.Panel1.Controls.Add(top);
        _responseBox.Dock = DockStyle.Fill;
        _responseBox.Multiline = true;
        _responseBox.ScrollBars = ScrollBars.Both;
        _responseBox.WordWrap = false;
        _scoreBox.Dock = DockStyle.Fill;
        _scoreBox.Multiline = true;
        _scoreBox.ScrollBars = ScrollBars.Both;
        _scoreBox.WordWrap = false;
        top.Panel1.Controls.Add(_responseBox);
        top.Panel2.Controls.Add(_scoreBox);

        var bottom = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
        split.Panel2.Controls.Add(bottom);
        _injectedRulesBox.Dock = DockStyle.Fill;
        _injectedRulesBox.Multiline = true;
        _injectedRulesBox.ScrollBars = ScrollBars.Both;
        _injectedRulesBox.WordWrap = false;
        _filesBox.Dock = DockStyle.Fill;
        _filesBox.Multiline = true;
        _filesBox.ScrollBars = ScrollBars.Both;
        _filesBox.WordWrap = false;
        bottom.Panel1.Controls.Add(_injectedRulesBox);
        bottom.Panel2.Controls.Add(_filesBox);
        return page;
    }

    private void LoadDefaults()
    {
        var repoRoot = _service.FindDefaultRepoRoot(Directory.GetCurrentDirectory());
        _repoRootBox.Text = repoRoot;
        _labRoot = _service.GetLabRoot(repoRoot);
        var manualCasesPath = Path.Combine(_labRoot, "cases", "training_20260705_topics_200.manual.jsonl");
        _caseFileBox.Text = File.Exists(manualCasesPath)
            ? manualCasesPath
            : Path.Combine(_labRoot, "cases", "sample_cases.jsonl");
        _settings = new PreprocessLabSettings();
        _promptConfig = _service.GetDefaultPromptConfig();
        LoadLocalSettings();
        ApplySettingsToUi();
        ReloadAll();
    }

    private void ReloadAll()
    {
        try
        {
            _catalog = _service.LoadCatalog(_repoRootBox.Text.Trim());
            _labRoot = _service.GetLabRoot(_catalog.RepoRoot);
            LoadTopicList();
            LoadCases();
            SetStatus("已载入规则 " + _catalog.Rules.Count + " 个。");
        }
        catch (Exception ex)
        {
            SetStatus("载入失败：" + ex.Message);
        }
    }

    private void LoadTopicList()
    {
        _topicList.Items.Clear();
        foreach (var rule in _catalog.Rules)
        {
            _topicList.Items.Add(rule.DisplayName);
        }
    }

    private void LoadCases()
    {
        _cases.Clear();
        _caseList.Items.Clear();
        foreach (var item in _service.LoadCases(_caseFileBox.Text.Trim()))
        {
            _cases.Add(item);
            _caseList.Items.Add(item);
        }

        if (_caseList.Items.Count > 0)
        {
            _caseList.SelectedIndex = 0;
        }
    }

    private void LoadSelectedCase()
    {
        if (_loadingCase || _caseList.SelectedItem is not PreprocessLabCase labCase)
        {
            return;
        }

        _loadingCase = true;
        try
        {
            _caseIdBox.Text = labCase.CaseId;
            _titleBox.Text = labCase.Title;
            _playerTextBox.Text = labCase.PlayerText;
            _npcReplyBox.Text = labCase.NpcReplyText;
            _historyBox.Text = string.Join(Environment.NewLine, labCase.HistoryLines);
            _afefPlayerBox.Text = string.Join(Environment.NewLine, labCase.AfefFacts.Where(x => !string.Equals(x.Kind, "npc", StringComparison.OrdinalIgnoreCase)).Select(x => x.Text));
            _afefNpcBox.Text = string.Join(Environment.NewLine, labCase.AfefFacts.Where(x => string.Equals(x.Kind, "npc", StringComparison.OrdinalIgnoreCase)).Select(x => x.Text));
            _runtimeBox.Text = labCase.RuntimeContext;
            _expectedBox.Text = string.Join(Environment.NewLine, labCase.ExpectedTopics);
            _allowedBox.Text = string.Join(Environment.NewLine, labCase.AllowedExtraTopics);
            _forbiddenBox.Text = string.Join(Environment.NewLine, labCase.ForbiddenTopics);
            _notesBox.Text = labCase.Notes;
        }
        finally
        {
            _loadingCase = false;
        }
    }

    private PreprocessLabCase CurrentCaseFromUi()
    {
        var facts = new List<AfefFact>();
        facts.AddRange(ReadLines(_afefPlayerBox.Text).Select(x => new AfefFact { Kind = "player", Text = x }));
        facts.AddRange(ReadLines(_afefNpcBox.Text).Select(x => new AfefFact { Kind = "npc", Text = x }));
        return new PreprocessLabCase
        {
            CaseId = string.IsNullOrWhiteSpace(_caseIdBox.Text) ? "case_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") : _caseIdBox.Text.Trim(),
            Title = _titleBox.Text.Trim(),
            PlayerText = _playerTextBox.Text.Trim(),
            NpcReplyText = _npcReplyBox.Text.Trim(),
            HistoryLines = ReadLines(_historyBox.Text),
            AfefFacts = facts,
            RuntimeContext = _runtimeBox.Text.Trim(),
            ExpectedTopics = ReadTopics(_expectedBox.Text),
            AllowedExtraTopics = ReadTopics(_allowedBox.Text),
            ForbiddenTopics = ReadTopics(_forbiddenBox.Text),
            Notes = _notesBox.Text.Trim()
        };
    }

    private PreprocessLabSettings CurrentSettingsFromUi()
    {
        return new PreprocessLabSettings
        {
            ApiProtocol = "auto",
            ApiUrl = _apiUrlBox.Text.Trim(),
            ApiKey = _apiKeyBox.Text,
            Model = _modelBox.Text.Trim(),
            Temperature = (float)_temperatureBox.Value,
            MaxTokens = (int)_maxTokensBox.Value,
            ThinkingEnabled = _thinkingBox.Checked,
            ReasoningEffort = (_reasoningBox.SelectedItem as string) ?? "low",
            PromptVersionPath = _promptVersionBox.Text.Trim()
        };
    }

    private PreprocessPromptConfig CurrentPromptConfigFromUi()
    {
        return _service.GetDefaultPromptConfig();
    }

    private void NewCase()
    {
        var item = new PreprocessLabCase
        {
            CaseId = "case_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            Title = "新案例"
        };
        _cases.Add(item);
        _caseList.Items.Add(item);
        _caseList.SelectedItem = item;
    }

    private void SaveCurrentCase()
    {
        var item = CurrentCaseFromUi();
        var index = _caseList.SelectedIndex;
        if (index >= 0 && index < _cases.Count)
        {
            _cases[index] = item;
            _caseList.Items[index] = item;
        }
        else
        {
            _cases.Add(item);
            _caseList.Items.Add(item);
            _caseList.SelectedItem = item;
        }

        _service.SaveCases(_caseFileBox.Text.Trim(), _cases);
        SetStatus("案例已保存。");
    }

    private void RenderCurrent()
    {
        try
        {
            _settings = CurrentSettingsFromUi();
            _promptConfig = _service.GetDefaultPromptConfig();
            var labCase = CurrentCaseFromUi();
            var rendered = _service.RenderPrompt(_catalog, labCase, _settings, _promptConfig);
            _promptPreviewBox.Text = _service.FormatRenderedPromptText(rendered);
            _requestBox.Text = rendered.RequestJson;
            var score = _service.ScoreTopics(labCase, Array.Empty<string>());
            _scoreBox.Text = FormatScore(score);
            SetStatus("请求已生成。");
        }
        catch (Exception ex)
        {
            SetStatus("生成失败：" + ex.Message);
        }
    }

    private async Task RunCurrentAsync()
    {
        try
        {
            _settings = CurrentSettingsFromUi();
            _promptConfig = _service.GetDefaultPromptConfig();
            var labCase = CurrentCaseFromUi();
            _lastRunDir = _service.CreateRunDirectory(_labRoot);
            var artifact = await _service.RunCaseAsync(_lastRunDir, 1, _catalog, labCase, _settings, _promptConfig);
            ShowArtifact(artifact);
            SetStatus("运行完成：" + artifact.Score.ExactMatch);
        }
        catch (Exception ex)
        {
            SetStatus("运行失败：" + ex.Message);
        }
    }

    private async Task RunAllAsync()
    {
        if (_cases.Count == 0)
        {
            SetStatus("没有案例。");
            return;
        }

        try
        {
            _settings = CurrentSettingsFromUi();
            _promptConfig = _service.GetDefaultPromptConfig();
            _lastRunDir = _service.CreateRunDirectory(_labRoot);
            var exact = 0;
            for (var i = 0; i < _cases.Count; i++)
            {
                SetStatus("运行 " + (i + 1) + " / " + _cases.Count + "：" + _cases[i].CaseId);
                var artifact = await _service.RunCaseAsync(_lastRunDir, i + 1, _catalog, _cases[i], _settings, _promptConfig);
                if (artifact.Score.ExactMatch)
                {
                    exact++;
                }

                ShowArtifact(artifact);
                Application.DoEvents();
            }

            SetStatus("批量完成 exact=" + exact + "/" + _cases.Count + "，目录：" + _lastRunDir);
        }
        catch (Exception ex)
        {
            SetStatus("批量失败：" + ex.Message);
        }
    }

    private void ShowArtifact(RunArtifact artifact)
    {
        _responseBox.Text = artifact.Result.AssistantText;
        _scoreBox.Text = FormatScore(artifact.Score);
        _injectedRulesBox.Text = File.Exists(artifact.InjectedRulesPath) ? _json.ReadUtf8(artifact.InjectedRulesPath) : "";
        _filesBox.Text = string.Join(Environment.NewLine, new[]
        {
            artifact.PromptPath,
            artifact.RequestPath,
            artifact.ResponsePath,
            artifact.InjectedRulesPath,
            artifact.MetaPath
        });
        if (File.Exists(artifact.RequestPath))
        {
            _requestBox.Text = _json.ReadUtf8(artifact.RequestPath);
        }
        if (File.Exists(artifact.PromptPath))
        {
            _promptPreviewBox.Text = _json.ReadUtf8(artifact.PromptPath);
        }
    }

    private string FormatScore(TopicScoreResult score)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "完全匹配: " + score.ExactMatch,
            "Recall: " + score.Recall,
            "Precision: " + score.Precision,
            "期望: " + string.Join(", ", score.ExpectedTopics),
            "允许额外: " + string.Join(", ", score.AllowedExtraTopics),
            "禁止: " + string.Join(", ", score.ForbiddenTopics),
            "实际: " + string.Join(", ", score.ActualTopics),
            "漏掉: " + string.Join(", ", score.MissingTopics),
            "多出: " + string.Join(", ", score.UnexpectedTopics),
            "禁中: " + string.Join(", ", score.ForbiddenHits)
        });
    }

    private void LoadPromptVersionFromPath()
    {
        SetStatus("前处理基准固定使用 mod 原链路结构，不能载入自创提示词版本。");
    }

    private void SaveLocalSettings()
    {
        try
        {
            _settings = CurrentSettingsFromUi();
            _promptConfig = _service.GetDefaultPromptConfig();
            var local = new LocalSettings
            {
                RepoRoot = _repoRootBox.Text.Trim(),
                CaseFilePath = _caseFileBox.Text.Trim(),
                PromptVersionPath = _promptVersionBox.Text.Trim(),
                Api = _settings,
                Prompt = _promptConfig
            };
            Directory.CreateDirectory(_labRoot);
            _json.WriteUtf8(Path.Combine(_labRoot, "local.settings.json"), _json.ToJson(local));
            SetStatus("本地设置已保存。");
        }
        catch (Exception ex)
        {
            SetStatus("保存设置失败：" + ex.Message);
        }
    }

    private void LoadLocalSettings()
    {
        try
        {
            var path = Path.Combine(_labRoot, "local.settings.json");
            if (!File.Exists(path))
            {
                return;
            }

            var local = _json.Deserialize<LocalSettings>(_json.ReadUtf8(path));
            if (local == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(local.RepoRoot))
            {
                _repoRootBox.Text = local.RepoRoot;
                _labRoot = _service.GetLabRoot(local.RepoRoot);
            }

            if (!string.IsNullOrWhiteSpace(local.CaseFilePath))
            {
                _caseFileBox.Text = local.CaseFilePath;
            }

            _promptVersionBox.Text = local.PromptVersionPath ?? "";
            _settings = local.Api ?? new PreprocessLabSettings();
            _promptConfig = local.Prompt ?? _service.GetDefaultPromptConfig();
        }
        catch
        {
        }
    }

    private void ApplySettingsToUi()
    {
        _apiUrlBox.Text = _settings.ApiUrl;
        _apiKeyBox.Text = _settings.ApiKey;
        _modelBox.Text = _settings.Model;
        _temperatureBox.Value = ClampDecimal((decimal)_settings.Temperature, _temperatureBox.Minimum, _temperatureBox.Maximum);
        _maxTokensBox.Value = ClampDecimal(_settings.MaxTokens <= 0 ? PreprocessTopicLabService.LabSafeAuxiliaryRouterMaxTokens : _settings.MaxTokens, _maxTokensBox.Minimum, _maxTokensBox.Maximum);
        _thinkingBox.Checked = _settings.ThinkingEnabled;
        _reasoningBox.SelectedItem = PreprocessTopicLabService.NormalizeReasoningEffortSelection(_settings.ReasoningEffort);
        if (_reasoningBox.SelectedIndex < 0 && _reasoningBox.Items.Count > 0)
        {
            _reasoningBox.SelectedIndex = 0;
        }

        ApplyPromptConfigToUi();
    }

    private void ApplyPromptConfigToUi()
    {
        _systemPromptBox.Text = PreprocessTopicLabService.DefaultSystemPrompt;
        _userTemplateBox.Text = "由 content/modules/AF.Module.Prompt/ModuleData/PreprocessPrompts.json 的 TopicRouting.UserPromptTemplate 生成；完整动态提示词请点击“生成请求”后查看下方预览。";
        _ruleMaxCharsBox.Value = ClampDecimal(PreprocessTopicLabService.ModGuardrailReturnCap, _ruleMaxCharsBox.Minimum, _ruleMaxCharsBox.Maximum);
    }

    private void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private void BrowseCaseFile()
    {
        using var dialog = new OpenFileDialog { Filter = "JSONL (*.jsonl)|*.jsonl|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _caseFileBox.Text = dialog.FileName;
            LoadCases();
        }
    }

    private void OpenLastRunDir()
    {
        var dir = !string.IsNullOrWhiteSpace(_lastRunDir) ? _lastRunDir : Path.Combine(_labRoot, "runs");
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
    }

    private static void AddLabeledText(TableLayoutPanel grid, string label, TextBox box, int col, int row)
    {
        grid.Controls.Add(Label(label), col, row);
        box.Dock = DockStyle.Fill;
        box.Multiline = true;
        box.ScrollBars = ScrollBars.Vertical;
        grid.Controls.Add(box, col + 1, row);
    }

    private static Label Label(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
    }

    private static Button Button(string text, int width = 68)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Dock = DockStyle.Fill,
            Margin = new Padding(3)
        };
    }

    private static List<string> ReadLines(string text)
    {
        return (text ?? "")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private static List<string> ReadTopics(string text)
    {
        return (text ?? "")
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split(new[] { '\n', ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = DateTime.Now.ToString("HH:mm:ss") + " " + text;
    }
}
