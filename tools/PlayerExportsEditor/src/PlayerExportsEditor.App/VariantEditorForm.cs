using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class VariantEditorForm : Form
{
    private readonly ConditionCatalog _catalog;
    private readonly NumericUpDown _priority = new();
    private readonly TextBox _content = new();
    private readonly ComboBox _conditionType = new();
    private readonly ComboBox _conditionValue = new();
    private readonly NumericUpDown _skillValue = new();
    private readonly ComboBox _gender = new();
    private readonly ComboBox _clanLeader = new();
    private readonly DataGridView _conditionsGrid = new();
    private readonly Button _setSkillValueButton = new();
    private readonly List<ConditionEntry> _conditions = new();

    public VariantEditorForm(LoreVariant? variant, ConditionCatalog? catalog = null)
    {
        _catalog = catalog ?? ConditionCatalog.Empty;
        Text = "\u63d0\u793a\u8bcd\u53d8\u4f53";
        Width = 1120;
        Height = 840;
        MinimumSize = new Size(940, 740);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadVariant(variant ?? new LoreVariant { Priority = 0, When = null, Content = "" });
    }

    public LoreVariant Variant => BuildVariant();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.Controls.Add(Label("\u4f18\u5148\u7ea7"), 0, 0);
        _priority.Minimum = -1000;
        _priority.Maximum = 1000;
        _priority.Dock = DockStyle.Left;
        top.Controls.Add(_priority, 1, 0);
        root.Controls.Add(top, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(body, 0, 1);

        _content.Dock = DockStyle.Fill;
        _content.Multiline = true;
        _content.AcceptsReturn = true;
        _content.ScrollBars = ScrollBars.Vertical;
        _content.Margin = new Padding(0, 0, 0, 8);
        body.Controls.Add(_content, 0, 0);

        body.Controls.Add(BuildConditionsPanel(), 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private Control BuildConditionsPanel()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(0, 8, 0, 0) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var addBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6 };
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 134));
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        addBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        addBar.Controls.Add(Label("\u6dfb\u52a0\u6761\u4ef6"), 0, 0);

        _conditionType.Dock = DockStyle.Fill;
        _conditionType.DropDownStyle = ComboBoxStyle.DropDownList;
        _conditionType.Items.AddRange(BuildTypeOptions().Cast<object>().ToArray());
        _conditionType.SelectedIndexChanged += (_, _) => RefreshCandidateChoices();
        addBar.Controls.Add(_conditionType, 1, 0);

        ConfigureCandidateCombo(_conditionValue);
        addBar.Controls.Add(_conditionValue, 2, 0);

        addBar.Controls.Add(Label("\u6570\u503c"), 3, 0);
        _skillValue.Minimum = 0;
        _skillValue.Maximum = 999;
        _skillValue.Value = 50;
        _skillValue.Dock = DockStyle.Fill;
        addBar.Controls.Add(_skillValue, 4, 0);

        var add = new Button { Text = "\u6dfb\u52a0", Dock = DockStyle.Fill };
        add.Click += (_, _) => AddSelectedCondition();
        addBar.Controls.Add(add, 5, 0);
        root.Controls.Add(addBar, 0, 0);

        var toggles = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7 };
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        toggles.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        toggles.Controls.Add(Label("\u6027\u522b"), 0, 0);
        ConfigureFixedCombo(_gender, new[] { "\u4e0d\u9650", "\u7537", "\u5973" });
        _gender.SelectedIndexChanged += (_, _) => RefreshConditionsGrid();
        toggles.Controls.Add(_gender, 1, 0);
        toggles.Controls.Add(Label("\u662f\u5426\u65cf\u957f"), 2, 0);
        ConfigureFixedCombo(_clanLeader, new[] { "\u4e0d\u9650", "\u662f", "\u5426" });
        _clanLeader.SelectedIndexChanged += (_, _) => RefreshConditionsGrid();
        toggles.Controls.Add(_clanLeader, 3, 0);
        root.Controls.Add(toggles, 0, 1);

        ConfigureConditionsGrid();
        root.Controls.Add(_conditionsGrid, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
        _setSkillValueButton.Text = "\u8bbe\u7f6e\u6280\u80fd\u503c";
        _setSkillValueButton.Width = 120;
        _setSkillValueButton.Click += (_, _) => SetSelectedSkillValue();
        actions.Controls.Add(_setSkillValueButton);
        var remove = new Button { Text = "\u79fb\u9664\u9009\u4e2d\u6761\u4ef6", Width = 130 };
        remove.Click += (_, _) => RemoveSelectedCondition();
        actions.Controls.Add(remove);
        var clear = new Button { Text = "\u6e05\u7a7a\u6761\u4ef6", Width = 110 };
        clear.Click += (_, _) => ClearConditions();
        actions.Controls.Add(clear);
        root.Controls.Add(actions, 0, 3);

        if (_conditionType.Items.Count > 0)
        {
            _conditionType.SelectedIndex = 0;
        }

        return root;
    }

    private void LoadVariant(LoreVariant variant)
    {
        _conditions.Clear();
        _priority.Value = Math.Clamp(variant.Priority, (int)_priority.Minimum, (int)_priority.Maximum);
        _content.Text = variant.Content ?? "";

        var when = variant.When;
        AddExisting(ConditionKind.Hero, when?.HeroIds);
        AddExisting(ConditionKind.Culture, when?.Cultures);
        AddExisting(ConditionKind.Kingdom, when?.KingdomIds);
        AddExisting(ConditionKind.Settlement, when?.SettlementIds);
        AddExisting(ConditionKind.Role, when?.Roles);
        AddExisting(ConditionKind.Identity, when?.IdentityIds);
        if (when?.SkillMin != null)
        {
            foreach (var item in when.SkillMin)
            {
                AddOrUpdateCondition(ConditionKind.Skill, item.Key, item.Value);
            }
        }

        _gender.SelectedIndex = when?.IsFemale == null ? 0 : when.IsFemale.Value ? 2 : 1;
        _clanLeader.SelectedIndex = when?.IsClanLeader == null ? 0 : when.IsClanLeader.Value ? 1 : 2;
        RefreshConditionsGrid();
    }

    private LoreVariant BuildVariant()
    {
        var when = new LoreWhen
        {
            HeroIds = ValuesFor(ConditionKind.Hero),
            Cultures = ValuesFor(ConditionKind.Culture),
            KingdomIds = ValuesFor(ConditionKind.Kingdom),
            SettlementIds = ValuesFor(ConditionKind.Settlement),
            Roles = ValuesFor(ConditionKind.Role),
            IdentityIds = ValuesFor(ConditionKind.Identity),
            SkillMin = _conditions
                .Where(x => x.Kind == ConditionKind.Skill)
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Max(y => y.SkillValue ?? 0), StringComparer.OrdinalIgnoreCase)
        };

        when.IsFemale = _gender.SelectedIndex switch { 1 => false, 2 => true, _ => null };
        when.IsClanLeader = _clanLeader.SelectedIndex switch { 1 => true, 2 => false, _ => null };
        if (IsEmptyWhen(when))
        {
            when = null;
        }

        return new LoreVariant { Priority = (int)_priority.Value, When = when, Content = _content.Text.Trim() };
    }

    private void AddSelectedCondition()
    {
        if (_conditionType.SelectedItem is not TypeOption option)
        {
            return;
        }

        var id = SelectedCandidateId(_conditionValue);
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        AddOrUpdateCondition(option.Kind, id, option.Kind == ConditionKind.Skill ? (int)_skillValue.Value : null);
        RefreshConditionsGrid();
    }

    private void SetSelectedSkillValue()
    {
        if (SelectedEntry() is not { Kind: ConditionKind.Skill } entry)
        {
            return;
        }

        entry.SkillValue = (int)_skillValue.Value;
        RefreshConditionsGrid();
    }

    private void RemoveSelectedCondition()
    {
        var tag = SelectedTag();
        if (tag is ConditionEntry entry)
        {
            _conditions.Remove(entry);
        }
        else if (tag is string pseudo && pseudo == "gender")
        {
            _gender.SelectedIndex = 0;
        }
        else if (tag is string pseudoClan && pseudoClan == "clan_leader")
        {
            _clanLeader.SelectedIndex = 0;
        }

        RefreshConditionsGrid();
    }

    private void ClearConditions()
    {
        _conditions.Clear();
        _gender.SelectedIndex = 0;
        _clanLeader.SelectedIndex = 0;
        RefreshConditionsGrid();
    }

    private void AddExisting(ConditionKind kind, IEnumerable<string>? values)
    {
        if (values == null)
        {
            return;
        }

        foreach (var value in values)
        {
            AddOrUpdateCondition(kind, value, null);
        }
    }

    private void AddOrUpdateCondition(ConditionKind kind, string value, int? skillValue)
    {
        var id = ExtractCandidateId(value);
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var existing = _conditions.FirstOrDefault(x => x.Kind == kind && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (kind == ConditionKind.Skill)
            {
                existing.SkillValue = skillValue ?? existing.SkillValue ?? 0;
            }

            return;
        }

        _conditions.Add(new ConditionEntry { Kind = kind, Id = id, SkillValue = kind == ConditionKind.Skill ? skillValue ?? 0 : null });
    }

    private List<string> ValuesFor(ConditionKind kind)
    {
        return _conditions
            .Where(x => x.Kind == kind)
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsEmptyWhen(LoreWhen when)
    {
        return IsEmpty(when.HeroIds) &&
               IsEmpty(when.Cultures) &&
               IsEmpty(when.KingdomIds) &&
               IsEmpty(when.SettlementIds) &&
               IsEmpty(when.Roles) &&
               IsEmpty(when.IdentityIds) &&
               (when.SkillMin == null || when.SkillMin.Count == 0) &&
               !when.IsFemale.HasValue &&
               !when.IsClanLeader.HasValue;
    }

    private static bool IsEmpty(List<string>? values) => values == null || values.Count == 0;

    private void RefreshCandidateChoices()
    {
        if (_conditionType.SelectedItem is not TypeOption option)
        {
            return;
        }

        _conditionValue.Items.Clear();
        foreach (var candidate in option.Candidates)
        {
            _conditionValue.Items.Add(candidate);
        }

        _skillValue.Enabled = option.Kind == ConditionKind.Skill;
        _conditionValue.Text = "";
        if (_conditionValue.Items.Count > 0)
        {
            _conditionValue.SelectedIndex = 0;
        }
    }

    private void RefreshConditionsGrid()
    {
        _conditionsGrid.Rows.Clear();
        foreach (var entry in _conditions.OrderBy(x => KindOrder(x.Kind)).ThenBy(x => DisplayName(x.Kind, x.Id), StringComparer.OrdinalIgnoreCase))
        {
            var rowIndex = _conditionsGrid.Rows.Add(KindLabel(entry.Kind), DisplayName(entry.Kind, entry.Id), entry.Id, entry.Kind == ConditionKind.Skill ? (entry.SkillValue ?? 0).ToString() : "");
            var row = _conditionsGrid.Rows[rowIndex];
            row.Tag = entry;
            row.Cells[0].ReadOnly = true;
            row.Cells[1].ReadOnly = true;
            row.Cells[2].ReadOnly = true;
            row.Cells[3].ReadOnly = entry.Kind != ConditionKind.Skill;
        }

        if (_gender.SelectedIndex > 0)
        {
            var rowIndex = _conditionsGrid.Rows.Add("\u6027\u522b", _gender.SelectedIndex == 1 ? "\u7537" : "\u5973", "", "");
            var row = _conditionsGrid.Rows[rowIndex];
            row.Tag = "gender";
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.ReadOnly = true;
            }
        }

        if (_clanLeader.SelectedIndex > 0)
        {
            var rowIndex = _conditionsGrid.Rows.Add("\u65cf\u957f", _clanLeader.SelectedIndex == 1 ? "\u662f" : "\u5426", "", "");
            var row = _conditionsGrid.Rows[rowIndex];
            row.Tag = "clan_leader";
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.ReadOnly = true;
            }
        }

        UpdateSkillButtonState();
    }

    private void ConfigureConditionsGrid()
    {
        _conditionsGrid.AllowUserToAddRows = false;
        _conditionsGrid.AllowUserToDeleteRows = false;
        _conditionsGrid.AutoGenerateColumns = false;
        _conditionsGrid.BackgroundColor = SystemColors.Window;
        _conditionsGrid.Dock = DockStyle.Fill;
        _conditionsGrid.MultiSelect = false;
        _conditionsGrid.RowHeadersVisible = false;
        _conditionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _conditionsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _conditionsGrid.Columns.Add(Column("\u6761\u4ef6", 95, 16));
        _conditionsGrid.Columns.Add(Column("\u540d\u79f0", 220, 38));
        _conditionsGrid.Columns.Add(Column("ID", 180, 34));
        _conditionsGrid.Columns.Add(Column("\u6280\u80fd\u503c", 86, 12));
        _conditionsGrid.SelectionChanged += (_, _) => UpdateSkillButtonState();
        _conditionsGrid.CellEndEdit += (_, args) => CommitSkillGridEdit(args.RowIndex, args.ColumnIndex);
    }

    private void CommitSkillGridEdit(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || columnIndex != 3 || _conditionsGrid.Rows[rowIndex].Tag is not ConditionEntry { Kind: ConditionKind.Skill } entry)
        {
            return;
        }

        var text = Convert.ToString(_conditionsGrid.Rows[rowIndex].Cells[columnIndex].Value) ?? "";
        if (int.TryParse(text.Trim(), out var value))
        {
            entry.SkillValue = Math.Clamp(value, 0, 999);
        }

        RefreshConditionsGrid();
    }

    private void UpdateSkillButtonState()
    {
        var entry = SelectedEntry();
        var isSkill = entry?.Kind == ConditionKind.Skill;
        _setSkillValueButton.Enabled = isSkill;
        if (isSkill && entry?.SkillValue != null)
        {
            _skillValue.Value = Math.Clamp(entry.SkillValue.Value, (int)_skillValue.Minimum, (int)_skillValue.Maximum);
        }
    }

    private object? SelectedTag()
    {
        return _conditionsGrid.CurrentRow?.Tag;
    }

    private ConditionEntry? SelectedEntry()
    {
        return SelectedTag() as ConditionEntry;
    }

    private static DataGridViewTextBoxColumn Column(string header, int minimumWidth, float fillWeight)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            MinimumWidth = minimumWidth,
            FillWeight = fillWeight
        };
    }

    private List<TypeOption> BuildTypeOptions()
    {
        return new List<TypeOption>
        {
            new("\u89d2\u8272ID", ConditionKind.Hero, _catalog.Heroes),
            new("\u6587\u5316", ConditionKind.Culture, _catalog.Cultures),
            new("\u52bf\u529b/\u738b\u56fd", ConditionKind.Kingdom, _catalog.Kingdoms),
            new("\u5b9a\u5c45\u70b9", ConditionKind.Settlement, _catalog.Settlements),
            new("\u8eab\u4efd\u5927\u7c7b", ConditionKind.Role, _catalog.Roles),
            new("\u7ec6\u5206\u8eab\u4efd", ConditionKind.Identity, _catalog.Identities),
            new("\u6280\u80fd\u6761\u4ef6", ConditionKind.Skill, _catalog.Skills)
        };
    }

    private static void ConfigureCandidateCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDown;
        combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        combo.MaxDropDownItems = 18;
    }

    private static void ConfigureFixedCombo(ComboBox combo, string[] values)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Items.AddRange(values);
        combo.SelectedIndex = 0;
    }

    private string DisplayName(ConditionKind kind, string id)
    {
        var candidate = CandidatesFor(kind).FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(candidate?.Label) ? id : candidate.Label.Trim();
    }

    private IReadOnlyList<ConditionCandidate> CandidatesFor(ConditionKind kind)
    {
        return kind switch
        {
            ConditionKind.Hero => _catalog.Heroes,
            ConditionKind.Culture => _catalog.Cultures,
            ConditionKind.Kingdom => _catalog.Kingdoms,
            ConditionKind.Settlement => _catalog.Settlements,
            ConditionKind.Role => _catalog.Roles,
            ConditionKind.Identity => _catalog.Identities,
            ConditionKind.Skill => _catalog.Skills,
            _ => Array.Empty<ConditionCandidate>()
        };
    }

    private static string SelectedCandidateId(ComboBox combo)
    {
        if (combo.SelectedItem is ConditionCandidate candidate)
        {
            return candidate.Id.Trim();
        }

        return ExtractCandidateId(combo.Text);
    }

    private static string ExtractCandidateId(string value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var open = text.LastIndexOf('(');
        if (open >= 0)
        {
            var close = text.IndexOf(')', open + 1);
            if (close > open + 1)
            {
                return text[(open + 1)..close].Trim();
            }
        }

        var separator = text.IndexOf(" - ", StringComparison.Ordinal);
        if (separator > 0)
        {
            text = text[..separator].Trim();
        }

        return text;
    }

    private static string KindLabel(ConditionKind kind)
    {
        return kind switch
        {
            ConditionKind.Hero => "\u89d2\u8272ID",
            ConditionKind.Culture => "\u6587\u5316",
            ConditionKind.Kingdom => "\u52bf\u529b/\u738b\u56fd",
            ConditionKind.Settlement => "\u5b9a\u5c45\u70b9",
            ConditionKind.Role => "\u8eab\u4efd\u5927\u7c7b",
            ConditionKind.Identity => "\u7ec6\u5206\u8eab\u4efd",
            ConditionKind.Skill => "\u6280\u80fd",
            _ => ""
        };
    }

    private static int KindOrder(ConditionKind kind)
    {
        return kind switch
        {
            ConditionKind.Hero => 0,
            ConditionKind.Culture => 1,
            ConditionKind.Kingdom => 2,
            ConditionKind.Settlement => 3,
            ConditionKind.Role => 4,
            ConditionKind.Identity => 5,
            ConditionKind.Skill => 6,
            _ => 99
        };
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }

    private enum ConditionKind
    {
        Hero,
        Culture,
        Kingdom,
        Settlement,
        Role,
        Identity,
        Skill
    }

    private sealed class ConditionEntry
    {
        public required ConditionKind Kind { get; init; }

        public required string Id { get; init; }

        public int? SkillValue { get; set; }
    }

    private sealed class TypeOption
    {
        public TypeOption(string label, ConditionKind kind, IReadOnlyList<ConditionCandidate> candidates)
        {
            Label = label;
            Kind = kind;
            Candidates = candidates;
        }

        public string Label { get; }

        public ConditionKind Kind { get; }

        public IReadOnlyList<ConditionCandidate> Candidates { get; }

        public override string ToString() => Label;
    }
}
