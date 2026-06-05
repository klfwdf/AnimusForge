using System.ComponentModel;
using System.Text.Json.Nodes;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class UnnamedPersonaEditorForm : Form
{
    private readonly NumericUpDown _version = new();
    private readonly BindingList<Entry> _entries = new();
    private readonly DataGridView _grid = new();

    public UnnamedPersonaEditorForm(string rawJson)
    {
        Text = "\u672a\u547d\u540d NPC \u8d44\u6599";
        Width = 1040;
        Height = 700;
        MinimumSize = new Size(820, 540);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout();
        LoadJson(rawJson);
    }

    public string Json => BuildJson();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill };
        top.Controls.Add(new Label { Text = "\u7248\u672c", Width = 70, TextAlign = ContentAlignment.MiddleLeft });
        _version.Minimum = 1;
        _version.Maximum = 999;
        _version.Width = 80;
        top.Controls.Add(_version);
        root.Controls.Add(top, 0, 0);

        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = SystemColors.Window;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Entry.Key), HeaderText = "\u952e", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Entry.Personality), HeaderText = "\u4e2a\u6027", Width = 380 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(Entry.Background), HeaderText = "\u80cc\u666f", Width = 420 });
        _grid.DataSource = _entries;
        root.Controls.Add(_grid, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void LoadJson(string rawJson)
    {
        var root = JsonNode.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson) as JsonObject ?? new JsonObject();
        if (root["Version"] != null && int.TryParse(root["Version"]?.ToString(), out var version))
        {
            _version.Value = Math.Clamp(version, (int)_version.Minimum, (int)_version.Maximum);
        }
        else
        {
            _version.Value = 1;
        }

        if (root["Profiles"] is not JsonObject profiles)
        {
            return;
        }

        foreach (var item in profiles)
        {
            var profile = item.Value as JsonObject;
            _entries.Add(new Entry
            {
                Key = item.Key,
                Personality = profile?["Personality"]?.GetValue<string>() ?? "",
                Background = profile?["Background"]?.GetValue<string>() ?? ""
            });
        }
    }

    private string BuildJson()
    {
        var profiles = new JsonObject();
        foreach (var entry in _entries)
        {
            var key = (entry.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            profiles[key] = new JsonObject
            {
                ["Personality"] = (entry.Personality ?? "").Trim(),
                ["Background"] = (entry.Background ?? "").Trim()
            };
        }

        var root = new JsonObject
        {
            ["Version"] = (int)_version.Value,
            ["Profiles"] = profiles
        };
        return root.ToJsonString(JsonFileStore.JsonOptions);
    }

    public sealed class Entry
    {
        public string Key { get; set; } = "";

        public string Personality { get; set; } = "";

        public string Background { get; set; } = "";
    }
}
