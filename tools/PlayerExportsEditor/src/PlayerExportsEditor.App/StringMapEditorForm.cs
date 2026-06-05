using System.ComponentModel;
using System.Text.Json.Nodes;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class StringMapEditorForm : Form
{
    private readonly BindingList<Entry> _entries = new();
    private readonly DataGridView _grid = new();

    public StringMapEditorForm(string title, string keyHeader, string valueHeader, string rawJson)
    {
        Text = title;
        Width = 980;
        Height = 680;
        MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout(keyHeader, valueHeader);
        LoadJson(rawJson);
    }

    public string Json => BuildJson();

    private void BuildLayout(string keyHeader, string valueHeader)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Entry.Key), HeaderText = keyHeader, Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Entry.Value), HeaderText = valueHeader, Width = 720 });
        _grid.DataSource = _entries;
        root.Controls.Add(_grid, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 1);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void LoadJson(string rawJson)
    {
        var root = JsonNode.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson) as JsonObject ?? new JsonObject();
        foreach (var item in root)
        {
            _entries.Add(new Entry { Key = item.Key, Value = item.Value?.GetValue<string>() ?? "" });
        }
    }

    private string BuildJson()
    {
        var root = new JsonObject();
        foreach (var entry in _entries)
        {
            var key = (entry.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            root[key] = (entry.Value ?? "").Trim();
        }

        return root.ToJsonString(JsonFileStore.JsonOptions);
    }

    public sealed class Entry
    {
        public string Key { get; set; } = "";

        public string Value { get; set; } = "";
    }
}
