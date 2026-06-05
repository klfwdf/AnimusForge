using System.Text.Json;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class KnowledgeRuleEditorForm : Form
{
    private readonly TextBox _idBox = new();
    private readonly TextBox _keywordsBox = new();
    private readonly TextBox _ragBox = new();
    private readonly DataGridView _variantGrid = new();
    private readonly DataGridView _mappingGrid = new();
    private readonly List<LoreVariant> _variants;
    private readonly List<LoreTextMapping> _mappings;
    private readonly ConditionCatalog _conditionCatalog;

    public KnowledgeRuleEditorForm(LoreRule? rule, ConditionCatalog? conditionCatalog = null)
    {
        var source = Clone(rule ?? CreateDefaultRule());
        _variants = source.Variants ?? new List<LoreVariant>();
        _mappings = source.TextMappings ?? new List<LoreTextMapping>();
        _conditionCatalog = conditionCatalog ?? ConditionCatalog.Empty;

        Text = "\u77e5\u8bc6\u89c4\u5219\u7f16\u8f91\u5668";
        Width = 1120;
        Height = 780;
        MinimumSize = new Size(940, 660);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadRule(source);
        RefreshVariants();
        RefreshMappings();
    }

    public LoreRule Rule => BuildRule();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var basic = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 2 };
        basic.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        basic.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        basic.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        basic.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        basic.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.Controls.Add(basic, 0, 0);

        basic.Controls.Add(Label("RuleId"), 0, 0);
        _idBox.Dock = DockStyle.Fill;
        basic.Controls.Add(_idBox, 1, 0);

        basic.Controls.Add(Label("\u5173\u952e\u8bcd\uff08\u6bcf\u884c\u4e00\u4e2a\uff09"), 0, 1);
        ConfigureMultiline(_keywordsBox);
        basic.Controls.Add(_keywordsBox, 1, 1);

        basic.Controls.Add(Label("RAG \u77ed\u53e5"), 0, 2);
        ConfigureMultiline(_ragBox);
        basic.Controls.Add(_ragBox, 1, 2);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        root.Controls.Add(tabs, 0, 1);
        tabs.TabPages.Add(BuildVariantsTab());
        tabs.TabPages.Add(BuildMappingsTab());

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private TabPage BuildVariantsTab()
    {
        var tab = new TabPage("\u63d0\u793a\u8bcd\u53d8\u4f53");
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        tab.Controls.Add(panel);

        ConfigureGrid(_variantGrid);
        _variantGrid.Columns.Add(Column("Priority", "\u4f18\u5148\u7ea7", 70));
        _variantGrid.Columns.Add(Column("Condition", "\u6761\u4ef6", 360));
        _variantGrid.Columns.Add(Column("Preview", "\u5185\u5bb9\u9884\u89c8", 560));
        panel.Controls.Add(_variantGrid, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        AddButton(buttons, "\u65b0\u589e\u53d8\u4f53", (_, _) => EditVariant(null));
        AddButton(buttons, "\u7f16\u8f91\u53d8\u4f53", (_, _) => EditSelectedVariant());
        AddButton(buttons, "\u5220\u9664\u53d8\u4f53", (_, _) => DeleteSelectedVariant());
        panel.Controls.Add(buttons, 0, 1);
        _variantGrid.CellDoubleClick += (_, _) => EditSelectedVariant();
        return tab;
    }

    private TabPage BuildMappingsTab()
    {
        var tab = new TabPage("\u8bcd\u6c47\u6620\u5c04");
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        tab.Controls.Add(panel);

        ConfigureGrid(_mappingGrid);
        _mappingGrid.Columns.Add(Column("SourceText", "\u8981\u66ff\u6362\u7684\u8bcd", 180));
        _mappingGrid.Columns.Add(Column("Kind", "\u6620\u5c04\u7c7b\u578b", 260));
        _mappingGrid.Columns.Add(Column("TargetId", "\u76ee\u6807\u5bf9\u8c61", 220));
        _mappingGrid.Columns.Add(Column("Preview", "\u8f93\u51fa / \u515c\u5e95", 420));
        panel.Controls.Add(_mappingGrid, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        AddButton(buttons, "\u65b0\u589e\u6620\u5c04", (_, _) => EditMapping(null));
        AddButton(buttons, "\u7f16\u8f91\u6620\u5c04", (_, _) => EditSelectedMapping());
        AddButton(buttons, "\u5220\u9664\u6620\u5c04", (_, _) => DeleteSelectedMapping());
        panel.Controls.Add(buttons, 0, 1);
        _mappingGrid.CellDoubleClick += (_, _) => EditSelectedMapping();
        return tab;
    }

    private void LoadRule(LoreRule rule)
    {
        _idBox.Text = rule.Id ?? "";
        _keywordsBox.Text = string.Join(Environment.NewLine, rule.Keywords ?? new List<string>());
        _ragBox.Text = string.Join(Environment.NewLine, rule.RagShortTexts ?? new List<string>());
    }

    private LoreRule BuildRule()
    {
        return new LoreRule
        {
            Id = _idBox.Text.Trim(),
            Keywords = Lines(_keywordsBox.Text),
            RagShortTexts = Lines(_ragBox.Text),
            SemanticPrototypes = new List<string>(),
            Variants = _variants,
            TextMappings = _mappings
        };
    }

    private void EditSelectedVariant()
    {
        if (_variantGrid.CurrentRow?.DataBoundItem is VariantRow row)
        {
            EditVariant(row.Variant);
        }
    }

    private void EditVariant(LoreVariant? existing)
    {
        var index = existing == null ? -1 : _variants.IndexOf(existing);
        using var dialog = new VariantEditorForm(existing, _conditionCatalog);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (index >= 0)
        {
            _variants[index] = dialog.Variant;
        }
        else
        {
            _variants.Add(dialog.Variant);
        }

        RefreshVariants();
    }

    private void DeleteSelectedVariant()
    {
        if (_variantGrid.CurrentRow?.DataBoundItem is VariantRow row)
        {
            _variants.Remove(row.Variant);
            RefreshVariants();
        }
    }

    private void EditSelectedMapping()
    {
        if (_mappingGrid.CurrentRow?.DataBoundItem is MappingRow row)
        {
            EditMapping(row.Mapping);
        }
    }

    private void EditMapping(LoreTextMapping? existing)
    {
        var index = existing == null ? -1 : _mappings.IndexOf(existing);
        using var dialog = new TextMappingEditorForm(existing, _conditionCatalog);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (index >= 0)
        {
            _mappings[index] = dialog.Mapping;
        }
        else
        {
            _mappings.Add(dialog.Mapping);
        }

        RefreshMappings();
    }

    private void DeleteSelectedMapping()
    {
        if (_mappingGrid.CurrentRow?.DataBoundItem is MappingRow row)
        {
            _mappings.Remove(row.Mapping);
            RefreshMappings();
        }
    }

    private void RefreshVariants()
    {
        _variantGrid.DataSource = _variants.Select(x => new VariantRow(x)).ToList();
    }

    private void RefreshMappings()
    {
        _mappingGrid.DataSource = _mappings.Select(x => new MappingRow(x, _conditionCatalog)).ToList();
    }

    private static LoreRule CreateDefaultRule()
    {
        return new LoreRule
        {
            Id = "rule_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            Keywords = new List<string>(),
            RagShortTexts = new List<string>(),
            Variants = new List<LoreVariant>
            {
                new() { Priority = 0, When = null, Content = "" }
            },
            TextMappings = new List<LoreTextMapping>()
        };
    }

    private static LoreRule Clone(LoreRule rule)
    {
        var json = JsonSerializer.Serialize(rule, JsonFileStore.JsonOptions);
        return JsonSerializer.Deserialize<LoreRule>(json, JsonFileStore.JsonOptions) ?? CreateDefaultRule();
    }

    internal static List<string> Lines(string text)
    {
        return (text ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string Preview(string? text, int max = 110)
    {
        var value = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length <= max ? value : value[..max] + "...";
    }

    internal static string WhenSummary(LoreWhen? when)
    {
        if (when == null)
        {
            return "\u901a\u7528";
        }

        var parts = new List<string>();
        AddList(parts, "hero", when.HeroIds);
        AddList(parts, "culture", when.Cultures);
        AddList(parts, "kingdom", when.KingdomIds);
        AddList(parts, "settlement", when.SettlementIds);
        AddList(parts, "role", when.Roles);
        AddList(parts, "identity", when.IdentityIds);
        if (when.IsFemale.HasValue)
        {
            parts.Add(when.IsFemale.Value ? "\u5973\u6027" : "\u7537\u6027");
        }

        if (when.IsClanLeader.HasValue)
        {
            parts.Add(when.IsClanLeader.Value ? "\u5bb6\u65cf\u65cf\u957f" : "\u975e\u5bb6\u65cf\u65cf\u957f");
        }

        if (when.SkillMin != null && when.SkillMin.Count > 0)
        {
            parts.Add("\u6280\u80fd " + string.Join(", ", when.SkillMin.Select(x => x.Key + ">=" + x.Value)));
        }

        return parts.Count == 0 ? "\u901a\u7528" : string.Join("; ", parts);
    }

    private static void AddList(List<string> parts, string name, List<string>? values)
    {
        if (values != null && values.Count > 0)
        {
            parts.Add(name + "=" + string.Join("|", values));
        }
    }

    private static void ConfigureMultiline(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.Multiline = true;
        box.ScrollBars = ScrollBars.Vertical;
        box.AcceptsReturn = true;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.Dock = DockStyle.Fill;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    private static DataGridViewTextBoxColumn Column(string property, string header, int width)
    {
        return new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = header, Width = width };
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }

    private static void AddButton(FlowLayoutPanel panel, string text, EventHandler handler)
    {
        var button = new Button { Text = text, Width = 120 };
        button.Click += handler;
        panel.Controls.Add(button);
    }

    private sealed class VariantRow
    {
        public VariantRow(LoreVariant variant)
        {
            Variant = variant;
        }

        public LoreVariant Variant { get; }

        public int Priority => Variant.Priority;

        public string Condition => WhenSummary(Variant.When);

        public string Preview => KnowledgeRuleEditorForm.Preview(Variant.Content);
    }

    private sealed class MappingRow
    {
        private readonly ConditionCatalog _catalog;

        public MappingRow(LoreTextMapping mapping, ConditionCatalog catalog)
        {
            Mapping = mapping;
            _catalog = catalog;
        }

        public LoreTextMapping Mapping { get; }

        public string SourceText => Mapping.SourceText ?? "";

        public string Kind => TextMappingCatalog.GetKindDisplayName(Mapping.Kind);

        public string TargetId => TextMappingCatalog.GetTargetDisplayName(Mapping.TargetId, _catalog);

        public string Preview
        {
            get
            {
                var parts = new List<string>();
                if (TextMappingCatalog.IsAgeRangeKind(Mapping.Kind))
                {
                    parts.Add("\u5e74\u9f84 " + (Mapping.AgeMin ?? 0) + "-" + (Mapping.AgeMax ?? 120));
                }

                if (TextMappingCatalog.IsStatusKind(Mapping.Kind))
                {
                    parts.Add("\u771f:" + (string.IsNullOrWhiteSpace(Mapping.TrueText) ? "\u7a7a" : Mapping.TrueText));
                    parts.Add("\u5047:" + (string.IsNullOrWhiteSpace(Mapping.FalseText) ? "\u7a7a" : Mapping.FalseText));
                }

                if (!string.IsNullOrWhiteSpace(Mapping.EmptyValueText))
                {
                    parts.Add("\u53d6\u4e0d\u5230:" + Mapping.EmptyValueText);
                }

                return string.Join(" / ", parts.Select(x => KnowledgeRuleEditorForm.Preview(x, 60)));
            }
        }
    }
}
