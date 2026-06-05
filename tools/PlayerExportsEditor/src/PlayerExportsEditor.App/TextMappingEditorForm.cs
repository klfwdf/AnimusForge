using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class TextMappingEditorForm : Form
{
    private readonly ConditionCatalog _catalog;
    private readonly TextBox _source = new();
    private readonly ComboBox _kind = new();
    private readonly ComboBox _targetId = new();
    private readonly NumericUpDown _ageMin = new();
    private readonly NumericUpDown _ageMax = new();
    private readonly TextBox _emptyValue = new();
    private readonly TextBox _trueText = new();
    private readonly TextBox _falseText = new();
    private readonly Label _targetLabel = new();
    private readonly Label _ageLabel = new();
    private bool _loading;

    public TextMappingEditorForm(LoreTextMapping? mapping, ConditionCatalog? conditionCatalog = null)
    {
        _catalog = conditionCatalog ?? ConditionCatalog.Empty;

        Text = "词汇映射";
        Width = 980;
        Height = 640;
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadMapping(mapping ?? new LoreTextMapping());
    }

    public LoreTextMapping Mapping
    {
        get
        {
            var kind = SelectedKindId();
            var usesAgeRange = TextMappingCatalog.IsAgeRangeKind(kind);
            return new LoreTextMapping
            {
                SourceText = _source.Text.Trim(),
                Kind = kind,
                TargetId = SelectedTargetId(kind),
                AgeMin = usesAgeRange ? (int)_ageMin.Value : null,
                AgeMax = usesAgeRange ? (int)_ageMax.Value : null,
                EmptyValueText = EmptyToNull(_emptyValue.Text),
                TrueText = EmptyToNull(_trueText.Text),
                FalseText = EmptyToNull(_falseText.Text)
            };
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 2,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        Controls.Add(root);

        AddLine(root, "要替换的词", _source, 0);
        AddCombo(root, "映射类型", _kind, 1);
        root.Controls.Add(_targetLabel, 0, 2);
        _targetLabel.Dock = DockStyle.Fill;
        _targetLabel.TextAlign = ContentAlignment.MiddleLeft;
        ConfigureCombo(_targetId);
        root.Controls.Add(_targetId, 1, 2);

        var agePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        ConfigureAge(_ageMin);
        ConfigureAge(_ageMax);
        agePanel.Controls.Add(new Label { Text = "最小", Width = 40, TextAlign = ContentAlignment.MiddleLeft });
        agePanel.Controls.Add(_ageMin);
        agePanel.Controls.Add(new Label { Text = "最大", Width = 40, TextAlign = ContentAlignment.MiddleLeft });
        agePanel.Controls.Add(_ageMax);
        root.Controls.Add(_ageLabel, 0, 3);
        _ageLabel.Text = "年龄范围";
        _ageLabel.Dock = DockStyle.Fill;
        _ageLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(agePanel, 1, 3);

        AddMultiline(root, "取不到时", _emptyValue, 4);
        AddMultiline(root, "判断为真时", _trueText, 5);
        AddMultiline(root, "判断为假时", _falseText, 6);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var ok = new Button { Text = "保存", Width = 100, DialogResult = DialogResult.OK };
        ok.Click += (_, _) =>
        {
            if (!ValidateEditor())
            {
                DialogResult = DialogResult.None;
            }
        };
        var cancel = new Button { Text = "取消", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 7);
        root.SetColumnSpan(buttons, 2);
        AcceptButton = ok;
        CancelButton = cancel;

        ConfigureCombo(_kind);
        _kind.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading)
            {
                PopulateTargetOptions(SelectedKindId(), null);
                UpdateTypeDependentControls();
            }
        };
        _kind.TextChanged += (_, _) =>
        {
            if (!_loading)
            {
                UpdateTypeDependentControls();
            }
        };
    }

    private void LoadMapping(LoreTextMapping mapping)
    {
        _loading = true;
        _source.Text = mapping.SourceText ?? "";
        PopulateKindOptions(mapping.Kind);
        PopulateTargetOptions(mapping.Kind, mapping.TargetId);
        _ageMin.Value = ClampAge(mapping.AgeMin ?? 0);
        _ageMax.Value = ClampAge(mapping.AgeMax ?? 120);
        _emptyValue.Text = mapping.EmptyValueText ?? "";
        _trueText.Text = mapping.TrueText ?? "";
        _falseText.Text = mapping.FalseText ?? "";
        _loading = false;
        UpdateTypeDependentControls();
    }

    private void PopulateKindOptions(string? selectedKind)
    {
        _kind.BeginUpdate();
        _kind.Items.Clear();
        foreach (var definition in TextMappingCatalog.AllKindDefinitions)
        {
            _kind.Items.Add(definition);
        }

        _kind.EndUpdate();
        SelectKind(selectedKind);
    }

    private void SelectKind(string? selectedKind)
    {
        var clean = (selectedKind ?? "").Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            _kind.SelectedIndex = -1;
            _kind.Text = "";
            return;
        }

        for (var i = 0; i < _kind.Items.Count; i++)
        {
            if (_kind.Items[i] is TextMappingKindDefinition definition &&
                definition.Kind.Equals(clean, StringComparison.OrdinalIgnoreCase))
            {
                _kind.SelectedIndex = i;
                return;
            }
        }

        _kind.SelectedIndex = -1;
        _kind.Text = clean;
    }

    private void PopulateTargetOptions(string? kind, string? selectedTargetId)
    {
        var targetRequirement = TextMappingCatalog.GetTargetRequirement(kind);
        _targetLabel.Text = targetRequirement.Label;
        _targetId.BeginUpdate();
        _targetId.Items.Clear();

        if (targetRequirement.Mode == TextMappingTargetMode.Auto)
        {
            var display = TextMappingCatalog.GetAutomaticTargetDisplayName(targetRequirement.AutoTargetId);
            _targetId.Items.Add(new TextMappingTargetOption(targetRequirement.AutoTargetId, display + " (" + targetRequirement.AutoTargetId + ")"));
            _targetId.Enabled = false;
            _targetId.SelectedIndex = 0;
            _targetId.EndUpdate();
            return;
        }

        _targetId.Enabled = true;
        foreach (var option in BuildTargetOptions(targetRequirement.Mode))
        {
            _targetId.Items.Add(option);
        }

        _targetId.EndUpdate();
        SelectTarget(selectedTargetId);
    }

    private IEnumerable<TextMappingTargetOption> BuildTargetOptions(TextMappingTargetMode mode)
    {
        IReadOnlyList<ConditionCandidate> source = mode switch
        {
            TextMappingTargetMode.Hero => _catalog.Heroes,
            TextMappingTargetMode.Kingdom => _catalog.Kingdoms,
            TextMappingTargetMode.Settlement => _catalog.Settlements,
            TextMappingTargetMode.Clan => _catalog.Clans.Count > 0 ? _catalog.Clans : _catalog.Kingdoms,
            _ => Array.Empty<ConditionCandidate>()
        };

        return source
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => string.IsNullOrWhiteSpace(x.Label) ? x.Id : x.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => new TextMappingTargetOption(x.Id, x.ToString()));
    }

    private void SelectTarget(string? selectedTargetId)
    {
        var clean = (selectedTargetId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            _targetId.SelectedIndex = -1;
            _targetId.Text = "";
            return;
        }

        for (var i = 0; i < _targetId.Items.Count; i++)
        {
            if (_targetId.Items[i] is TextMappingTargetOption option &&
                option.Id.Equals(clean, StringComparison.OrdinalIgnoreCase))
            {
                _targetId.SelectedIndex = i;
                return;
            }
        }

        _targetId.SelectedIndex = -1;
        _targetId.Text = clean;
    }

    private void UpdateTypeDependentControls()
    {
        var kind = SelectedKindId();
        var usesAgeRange = TextMappingCatalog.IsAgeRangeKind(kind);
        var usesStatusText = TextMappingCatalog.IsStatusKind(kind);

        _ageMin.Enabled = usesAgeRange;
        _ageMax.Enabled = usesAgeRange;
        _ageLabel.Enabled = usesAgeRange;
        _trueText.Enabled = usesStatusText;
        _falseText.Enabled = usesStatusText;

        if (usesAgeRange)
        {
            if (_ageMin.Value < 0)
            {
                _ageMin.Value = 0;
            }

            if (_ageMax.Value < 0)
            {
                _ageMax.Value = 120;
            }
        }
    }

    private bool ValidateEditor()
    {
        var kind = SelectedKindId();
        if (string.IsNullOrWhiteSpace(_source.Text))
        {
            MessageBox.Show(this, "请填写要替换的词。", "词汇映射", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _source.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            MessageBox.Show(this, "请选择映射类型。", "词汇映射", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _kind.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedTargetId(kind)))
        {
            MessageBox.Show(this, "请选择目标对象。", "词汇映射", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _targetId.Focus();
            return false;
        }

        if (TextMappingCatalog.IsAgeRangeKind(kind) && _ageMin.Value > _ageMax.Value)
        {
            MessageBox.Show(this, "年龄范围的最小值不能大于最大值。", "词汇映射", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _ageMin.Focus();
            return false;
        }

        if (TextMappingCatalog.IsStatusKind(kind) &&
            string.IsNullOrWhiteSpace(_trueText.Text) &&
            string.IsNullOrWhiteSpace(_falseText.Text))
        {
            MessageBox.Show(this, "状态判断至少需要填写“判断为真时”或“判断为假时”。", "词汇映射", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _trueText.Focus();
            return false;
        }

        return true;
    }

    private string SelectedKindId()
    {
        if (_kind.SelectedItem is TextMappingKindDefinition definition)
        {
            return definition.Kind.Trim();
        }

        var text = _kind.Text.Trim();
        var known = TextMappingCatalog.FindKind(text);
        if (known != null)
        {
            return known.Kind;
        }

        return TextMappingCatalog.ExtractIdFromDisplayText(text);
    }

    private string SelectedTargetId(string kind)
    {
        var requirement = TextMappingCatalog.GetTargetRequirement(kind);
        if (requirement.Mode == TextMappingTargetMode.Auto)
        {
            return requirement.AutoTargetId;
        }

        if (_targetId.SelectedItem is TextMappingTargetOption option)
        {
            return option.Id.Trim();
        }

        return TextMappingCatalog.ExtractIdFromDisplayText(_targetId.Text);
    }

    private static void AddLine(TableLayoutPanel root, string label, TextBox box, int row)
    {
        root.Controls.Add(Label(label), 0, row);
        box.Dock = DockStyle.Fill;
        root.Controls.Add(box, 1, row);
    }

    private static void AddCombo(TableLayoutPanel root, string label, ComboBox box, int row)
    {
        root.Controls.Add(Label(label), 0, row);
        ConfigureCombo(box);
        root.Controls.Add(box, 1, row);
    }

    private static void AddMultiline(TableLayoutPanel root, string label, TextBox box, int row)
    {
        root.Controls.Add(Label(label), 0, row);
        box.Dock = DockStyle.Fill;
        box.Multiline = true;
        box.ScrollBars = ScrollBars.Vertical;
        box.AcceptsReturn = true;
        root.Controls.Add(box, 1, row);
    }

    private static void ConfigureCombo(ComboBox box)
    {
        box.Dock = DockStyle.Fill;
        box.DropDownStyle = ComboBoxStyle.DropDown;
        box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        box.AutoCompleteSource = AutoCompleteSource.ListItems;
        box.MaxDropDownItems = 18;
    }

    private static void ConfigureAge(NumericUpDown box)
    {
        box.Minimum = 0;
        box.Maximum = 200;
        box.Width = 70;
    }

    private static decimal ClampAge(int value)
    {
        return Math.Max(0, Math.Min(200, value));
    }

    private static string? EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }
}
