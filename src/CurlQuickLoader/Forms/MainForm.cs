using CurlQuickLoader.Models;
using CurlQuickLoader.Services;

namespace CurlQuickLoader.Forms;

public class MainForm : Form
{
    private readonly PresetRepository _repo;
    private readonly CurlRunner _runner;

    // Left panel
    private TextBox _txtSearch = null!;
    private ListView _listView = null!;

    // Right panel
    private TextBox _txtCommand = null!;
    private RichTextBox _txtOutput = null!;
    private Button _btnCopy = null!;
    private Button _btnRun = null!;

    // Toolbar buttons
    private Button _btnNew = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnDuplicate = null!;

    // Main left/right split — stored so distances can be set after layout
    private SplitContainer _split = null!;
    // Right-panel command-preview / output split (horizontal, user-draggable)
    private SplitContainer _rightSplit = null!;

    private List<CurlPreset> _allPresets = new();

    public MainForm()
    {
        Logger.Info("MainForm constructor start");
        _repo = new PresetRepository();
        _runner = new CurlRunner();
        InitializeComponent();
        Logger.Info("MainForm constructor complete");
    }

    private void InitializeComponent()
    {
        Logger.Info("InitializeComponent start");

        Text = "Curl Quick Loader";
        Size = new Size(1000, 680);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Load += MainForm_Load;

        // ── Toolbar ──────────────────────────────────────────────────────────
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6, 4, 6, 4),
            BackColor = SystemColors.ControlLight
        };

        _btnNew = MakeToolbarButton("New", BtnNew_Click);
        _btnEdit = MakeToolbarButton("Edit", BtnEdit_Click);
        _btnDelete = MakeToolbarButton("Delete", BtnDelete_Click);
        _btnDuplicate = MakeToolbarButton("Duplicate", BtnDuplicate_Click);
        toolbar.Controls.AddRange(new Control[] { _btnNew, _btnEdit, _btnDelete, _btnDuplicate });

        // ── Main split ───────────────────────────────────────────────────────
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill
        };

        // LEFT: search + list
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(4)
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search presets…",
            Margin = new Padding(0, 0, 0, 4)
        };
        _txtSearch.TextChanged += (_, _) => FilterPresets();

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            MultiSelect = false,
            HideSelection = false,
            Sorting = SortOrder.Ascending
        };
        _listView.Columns.Add("Name", 160);
        _listView.Columns.Add("Method", 65);
        _listView.Columns.Add("URL", 120);
        _listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
        _listView.MouseDoubleClick += (_, _) => BtnEdit_Click(null, EventArgs.Empty);
        _listView.KeyDown += ListView_KeyDown;

        leftPanel.Controls.Add(_txtSearch, 0, 0);
        leftPanel.Controls.Add(_listView, 0, 1);
        _split.Panel1.Controls.Add(leftPanel);

        // RIGHT: command preview + actions + output
        // A horizontal SplitContainer lets the user drag the divider between
        // the command-preview area (top) and the output terminal (bottom).
        var rightOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };

        var cmdLabel = new Label
        {
            Text = "Generated curl command:",
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font(Font.FontFamily, 8f, FontStyle.Bold)
        };

        _rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal  // splitter bar runs left→right, divides top/bottom
        };

        // ── Top pane: command textbox + buttons ──────────────────────────────
        _txtCommand = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(245, 245, 245),
            Font = new Font("Consolas", 9f),
            ForeColor = Color.DarkGreen
        };

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 4)
        };

        _btnCopy = new Button { Text = "Copy Command", Width = 120, Height = 32 };
        _btnCopy.Click += BtnCopy_Click;

        _btnRun = new Button { Text = "Run Now", Width = 100, Height = 32 };
        _btnRun.Click += BtnRun_Click;

        actionPanel.Controls.Add(_btnCopy);
        actionPanel.Controls.Add(_btnRun);

        _rightSplit.Panel1.Controls.Add(_txtCommand);
        _rightSplit.Panel1.Controls.Add(actionPanel);

        // ── Bottom pane: output terminal ─────────────────────────────────────
        _txtOutput = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.LightGreen,
            Font = new Font("Consolas", 9f),
            ScrollBars = RichTextBoxScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle
        };

        var outputLabel = new Label
        {
            Text = "Output:",
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 8f, FontStyle.Bold),
            Padding = new Padding(0, 4, 0, 2)
        };

        var outputWrapper = new Panel { Dock = DockStyle.Fill };
        outputWrapper.Controls.Add(_txtOutput);
        outputWrapper.Controls.Add(outputLabel);

        _rightSplit.Panel2.Controls.Add(outputWrapper);

        // Add cmdLabel last so DockStyle.Top is processed after Fill (_rightSplit)
        rightOuter.Controls.Add(_rightSplit);
        rightOuter.Controls.Add(cmdLabel);

        _split.Panel2.Controls.Add(rightOuter);

        Controls.Add(_split);
        Controls.Add(toolbar);
        // Menu must be added last so WinForms docking processes it first,
        // placing it at the top above the toolbar (Windows standard layout).
        BuildMenu();

        UpdateButtonStates();
        Logger.Info("InitializeComponent complete");
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        Logger.Info("MainForm_Load start");
        try
        {
            // Set min sizes and splitter distances after layout so the containers
            // have real dimensions — avoids InvalidOperationException on startup.
            _split.Panel1MinSize = 260;
            _split.Panel2MinSize = 300;
            _split.SplitterDistance = Math.Min(370, _split.Width - _split.Panel2MinSize - _split.SplitterWidth);

            _rightSplit.Panel1MinSize = 60;
            _rightSplit.Panel2MinSize = 60;
            // Default: command preview takes ~35% of the right panel height
            _rightSplit.SplitterDistance = Math.Max(
                _rightSplit.Panel1MinSize,
                (int)(_rightSplit.Height * 0.35) - _rightSplit.SplitterWidth);

            CheckCurlAvailable();
            LoadPresets();
            Logger.Info("MainForm_Load complete");
        }
        catch (Exception ex)
        {
            Logger.Error("Exception in MainForm_Load", ex);
            throw;
        }
    }

    private void BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        var exportItem = new ToolStripMenuItem("Export Presets…");
        exportItem.Click += ExportPresets_Click;
        var importItem = new ToolStripMenuItem("Import Presets…");
        importItem.Click += ImportPresets_Click;
        var sep = new ToolStripSeparator();
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Close();
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { exportItem, importItem, sep, exitItem });

        var helpMenu = new ToolStripMenuItem("Help");
        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (_, _) => MessageBox.Show(
            "Curl Quick Loader\n\nCreate and manage preset curl commands.\nRun presets from the GUI or CLI.\n\nCLI usage:\n  CurlQuickLoader.exe --list\n  CurlQuickLoader.exe --run \"PresetName\"\n  CurlQuickLoader.exe --export \"PresetName\"",
            "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        helpMenu.DropDownItems.Add(aboutItem);

        menu.Items.Add(fileMenu);
        menu.Items.Add(helpMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private static Button MakeToolbarButton(string text, EventHandler handler)
    {
        var btn = new Button { Text = text, AutoSize = true, Height = 28, Margin = new Padding(2, 0, 2, 0) };
        btn.Click += handler;
        return btn;
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    private void LoadPresets()
    {
        Logger.Info("LoadPresets start");
        _allPresets = _repo.Load();
        FilterPresets();
        Logger.Info("LoadPresets complete");
    }

    private void FilterPresets()
    {
        string filter = _txtSearch.Text.Trim().ToLowerInvariant();
        var visible = string.IsNullOrEmpty(filter)
            ? _allPresets
            : _allPresets.Where(p =>
                p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.Url.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                p.Method.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        _listView.BeginUpdate();
        _listView.Items.Clear();
        foreach (var p in visible)
        {
            var item = new ListViewItem(p.Name) { Tag = p };
            item.SubItems.Add(p.Method);
            item.SubItems.Add(p.Url);
            _listView.Items.Add(item);
        }
        _listView.EndUpdate();
        UpdateButtonStates();
        UpdateCommandPreview();
    }

    private CurlPreset? SelectedPreset =>
        _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag as CurlPreset : null;

    private void UpdateCommandPreview()
    {
        var preset = SelectedPreset;
        if (preset == null)
        {
            _txtCommand.Text = string.Empty;
            return;
        }
        _txtCommand.Text = CurlCommandBuilder.Build(preset);
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = SelectedPreset != null;
        _btnEdit.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
        _btnDuplicate.Enabled = hasSelection;
        _btnCopy.Enabled = hasSelection;
        _btnRun.Enabled = hasSelection;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateCommandPreview();
        UpdateButtonStates();
    }

    private void ListView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
            BtnDelete_Click(null, EventArgs.Empty);
        else if (e.KeyCode == Keys.Enter)
            BtnEdit_Click(null, EventArgs.Empty);
        else if (e.KeyCode == Keys.F2)
            BtnEdit_Click(null, EventArgs.Empty);
    }

    private void BtnNew_Click(object? sender, EventArgs e)
    {
        Logger.Info("New preset");
        using var form = new PresetEditForm(_repo);
        if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
        {
            _repo.Add(form.Result);
            LoadPresets();
            SelectPresetById(form.Result.Id);
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset == null) return;
        Logger.Info($"Edit preset: {preset.Name}");

        using var form = new PresetEditForm(_repo, preset);
        if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
        {
            _repo.Update(form.Result);
            LoadPresets();
            SelectPresetById(form.Result.Id);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset == null) return;

        var answer = MessageBox.Show(
            $"Delete preset \"{preset.Name}\"?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (answer == DialogResult.Yes)
        {
            Logger.Info($"Delete preset: {preset.Name}");
            _repo.Delete(preset.Id);
            LoadPresets();
        }
    }

    private void BtnDuplicate_Click(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset == null) return;

        var clone = preset.Clone();
        clone.Name = GenerateUniqueName(preset.Name);
        Logger.Info($"Duplicate preset: {preset.Name} → {clone.Name}");

        using var form = new PresetEditForm(_repo, clone);
        if (form.ShowDialog(this) == DialogResult.OK && form.Result != null)
        {
            _repo.Add(form.Result);
            LoadPresets();
            SelectPresetById(form.Result.Id);
        }
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        string cmd = _txtCommand.Text;
        if (string.IsNullOrEmpty(cmd)) return;
        Clipboard.SetText(cmd);
        AppendOutput("[Copied to clipboard]");
    }

    private void BtnRun_Click(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset == null) return;

        _txtOutput.Clear();
        string cmd = CurlCommandBuilder.Build(preset);
        AppendOutput($"> {cmd}");
        AppendOutput(string.Empty);
        Logger.Info($"Run preset: {preset.Name}");

        _btnRun.Enabled = false;
        _btnRun.Text = "Running…";

        Task.Run(() =>
        {
            try
            {
                var result = _runner.Run(preset);
                Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(result.Output))
                        AppendOutput(result.Output.TrimEnd());

                    if (!string.IsNullOrWhiteSpace(result.Error))
                    {
                        AppendOutput("[stderr]", Color.Yellow);
                        AppendOutput(result.Error.TrimEnd(), Color.Yellow);
                    }

                    string exitMsg = $"[Exit code: {result.ExitCode}]";
                    AppendOutput(exitMsg, result.ExitCode == 0 ? Color.LightGreen : Color.OrangeRed);
                    Logger.Info($"Preset run finished. Exit code: {result.ExitCode}");

                    _btnRun.Enabled = true;
                    _btnRun.Text = "Run Now";
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Exception running preset", ex);
                Invoke(() =>
                {
                    AppendOutput($"[Error: {ex.Message}]", Color.OrangeRed);
                    _btnRun.Enabled = true;
                    _btnRun.Text = "Run Now";
                });
            }
        });
    }

    private void AppendOutput(string text, Color? color = null)
    {
        _txtOutput.SelectionStart = _txtOutput.TextLength;
        _txtOutput.SelectionLength = 0;
        _txtOutput.SelectionColor = color ?? Color.LightGreen;
        _txtOutput.AppendText(text + "\n");
        _txtOutput.ScrollToCaret();
    }

    private void ExportPresets_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export Presets",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = "curl-presets-export.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var presets = _repo.Load();
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(presets, options);
            File.WriteAllText(dialog.FileName, json);
            Logger.Info($"Exported {presets.Count} preset(s) to {dialog.FileName}");
            MessageBox.Show($"Exported {presets.Count} preset(s) to:\n{dialog.FileName}",
                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("Export failed", ex);
            MessageBox.Show($"Export failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportPresets_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import Presets",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<CurlPreset>>(json, options);
            if (imported == null || imported.Count == 0)
            {
                MessageBox.Show("No presets found in file.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var existing = _repo.Load();
            int added = 0, skipped = 0;

            foreach (var p in imported)
            {
                if (existing.Any(e => string.Equals(e.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    skipped++;
                }
                else
                {
                    p.Id = Guid.NewGuid();
                    existing.Add(p);
                    added++;
                }
            }

            _repo.Save(existing);
            LoadPresets();
            Logger.Info($"Import: added={added}, skipped={skipped}");
            MessageBox.Show($"Import complete.\n  Added: {added}\n  Skipped (name conflict): {skipped}",
                "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("Import failed", ex);
            MessageBox.Show($"Import failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SelectPresetById(Guid id)
    {
        foreach (ListViewItem item in _listView.Items)
        {
            if (item.Tag is CurlPreset p && p.Id == id)
            {
                item.Selected = true;
                item.EnsureVisible();
                _listView.Focus();
                return;
            }
        }
    }

    private string GenerateUniqueName(string baseName)
    {
        string candidate = $"{baseName} (copy)";
        int counter = 2;
        while (_repo.NameExists(candidate))
            candidate = $"{baseName} (copy {counter++})";
        return candidate;
    }

    private void CheckCurlAvailable()
    {
        Logger.Info("Checking curl availability");
        if (!CurlRunner.IsCurlAvailable())
        {
            Logger.Warn("curl not found on PATH");
            MessageBox.Show(
                "curl was not found on your system PATH.\n\n" +
                "Windows 10 (1803+) and Windows 11 include curl in System32.\n" +
                "If curl is missing, download it from https://curl.se/windows/",
                "curl Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else
        {
            Logger.Info("curl found on PATH");
        }
    }
}
