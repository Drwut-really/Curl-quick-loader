using CurlQuickLoader.Models;
using CurlQuickLoader.Services;

namespace CurlQuickLoader.Forms;

public class PresetEditForm : Form
{
    // Inputs
    private TextBox _txtName = null!;
    private ComboBox _cmbMethod = null!;
    private TextBox _txtUrl = null!;
    private HeadersEditorControl _headersEditor = null!;
    private HeadersEditorControl _formDataEditor = null!;
    private TextBox _txtBody = null!;
    private TextBox _txtExtraFlags = null!;
    private TextBox _txtPreview = null!;

    // Buttons
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    private readonly PresetRepository _repo;
    private readonly CurlPreset? _existingPreset;

    public CurlPreset? Result { get; private set; }

    private static readonly string[] Methods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

    public PresetEditForm(PresetRepository repo, CurlPreset? existingPreset = null)
    {
        _repo = repo;
        _existingPreset = existingPreset;
        InitializeComponent();
        PopulateFields();
    }

    private void InitializeComponent()
    {
        Text = _existingPreset == null ? "New Preset" : "Edit Preset";
        Size = new Size(700, 820);
        MinimumSize = new Size(600, 700);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(12),
            AutoSize = false
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Row heights
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // Name
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // Method
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // URL
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // Headers label
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));    // Headers grid
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // Form Data label
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));    // Form Data grid
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15));    // Body
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // Extra Flags
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15));    // Preview

        // Name
        mainPanel.Controls.Add(MakeLabel("Name:"), 0, 0);
        _txtName = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _txtName.TextChanged += OnFieldChanged;
        mainPanel.Controls.Add(_txtName, 1, 0);

        // Method
        mainPanel.Controls.Add(MakeLabel("Method:"), 0, 1);
        _cmbMethod = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 4, 0, 4)
        };
        _cmbMethod.Items.AddRange(Methods);
        _cmbMethod.SelectedIndex = 0;
        _cmbMethod.SelectedIndexChanged += OnFieldChanged;
        mainPanel.Controls.Add(_cmbMethod, 1, 1);

        // URL
        mainPanel.Controls.Add(MakeLabel("URL:"), 0, 2);
        _txtUrl = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _txtUrl.TextChanged += OnFieldChanged;
        mainPanel.Controls.Add(_txtUrl, 1, 2);

        // Headers label
        mainPanel.Controls.Add(MakeLabel("Headers:"), 0, 3);
        mainPanel.Controls.Add(new Label
        {
            Text = "Enter request headers (key / value pairs):",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8f)
        }, 1, 3);

        // Headers editor (spans both columns)
        _headersEditor = new HeadersEditorControl("Header Name", "+ Add Header")
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4)
        };
        _headersEditor.SetHeaders(new List<Header>());
        mainPanel.SetColumnSpan(_headersEditor, 2);
        mainPanel.Controls.Add(_headersEditor, 0, 4);

        // Form Data label
        mainPanel.Controls.Add(MakeLabel("Form Data:"), 0, 5);
        mainPanel.Controls.Add(new Label
        {
            Text = "Key / value pairs sent as --form-string arguments:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font.FontFamily, 8f)
        }, 1, 5);

        // Form Data editor (spans both columns)
        _formDataEditor = new HeadersEditorControl("Field Name", "+ Add Field")
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4)
        };
        _formDataEditor.SetHeaders(new List<Header>());
        mainPanel.SetColumnSpan(_formDataEditor, 2);
        mainPanel.Controls.Add(_formDataEditor, 0, 6);

        // Wire up change events for live preview
        _headersEditor.OnChanged += OnFieldChanged;
        _formDataEditor.OnChanged += OnFieldChanged;

        // Body
        mainPanel.Controls.Add(MakeLabel("Body:"), 0, 7);
        _txtBody = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f),
            Margin = new Padding(0, 0, 0, 4)
        };
        _txtBody.TextChanged += OnFieldChanged;
        mainPanel.Controls.Add(_txtBody, 1, 7);

        // Extra flags
        mainPanel.Controls.Add(MakeLabel("Extra Flags:"), 0, 8);
        _txtExtraFlags = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _txtExtraFlags.TextChanged += OnFieldChanged;
        mainPanel.Controls.Add(_txtExtraFlags, 1, 8);

        // Preview (spans both columns)
        var previewPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };
        var previewLabel = new Label
        {
            Text = "Command Preview:",
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 8f, FontStyle.Bold)
        };
        _txtPreview = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(245, 245, 245),
            Font = new Font("Consolas", 9f),
            ForeColor = Color.DarkGreen
        };
        previewPanel.Controls.Add(_txtPreview);
        previewPanel.Controls.Add(previewLabel);
        mainPanel.SetColumnSpan(previewPanel, 2);
        mainPanel.Controls.Add(previewPanel, 0, 9);

        // Button row at bottom
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8, 6, 8, 8)
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 90,
            Height = 46
        };

        _btnOk = new Button
        {
            Text = "Save",
            Width = 90,
            Height = 46
        };
        _btnOk.Click += BtnOk_Click;

        buttonPanel.Controls.Add(_btnCancel);
        buttonPanel.Controls.Add(_btnOk);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private static Label MakeLabel(string text) => new Label
    {
        Text = text,
        TextAlign = ContentAlignment.MiddleRight,
        Dock = DockStyle.Fill,
        Padding = new Padding(0, 0, 6, 0)
    };

    private void PopulateFields()
    {
        if (_existingPreset == null) return;

        _txtName.Text = _existingPreset.Name;
        _cmbMethod.SelectedItem = _existingPreset.Method;
        if (_cmbMethod.SelectedIndex < 0) _cmbMethod.SelectedIndex = 0;
        _txtUrl.Text = _existingPreset.Url;
        _headersEditor.SetHeaders(_existingPreset.Headers);
        _formDataEditor.SetHeaders(_existingPreset.FormData);
        _txtBody.Text = _existingPreset.Body ?? string.Empty;
        _txtExtraFlags.Text = _existingPreset.ExtraFlags ?? string.Empty;
        UpdatePreview();
    }

    private void OnFieldChanged(object? sender, EventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        var temp = BuildPresetFromFields();
        _txtPreview.Text = CurlCommandBuilder.Build(temp);
    }

    private CurlPreset BuildPresetFromFields()
    {
        return new CurlPreset
        {
            Id = _existingPreset?.Id ?? Guid.NewGuid(),
            Name = _txtName.Text.Trim(),
            Method = _cmbMethod.SelectedItem?.ToString() ?? "GET",
            Url = _txtUrl.Text.Trim(),
            Headers = _headersEditor.GetHeaders(),
            FormData = _formDataEditor.GetHeaders(),
            Body = string.IsNullOrEmpty(_txtBody.Text) ? null : _txtBody.Text,
            ExtraFlags = string.IsNullOrEmpty(_txtExtraFlags.Text) ? null : _txtExtraFlags.Text.Trim(),
            CreatedAt = _existingPreset?.CreatedAt ?? DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        string name = _txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Preset name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtUrl.Text))
        {
            MessageBox.Show("URL cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtUrl.Focus();
            return;
        }

        Guid? excludeId = _existingPreset?.Id;
        if (_repo.NameExists(name, excludeId))
        {
            MessageBox.Show($"A preset named \"{name}\" already exists. Choose a different name.",
                "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        Result = BuildPresetFromFields();
        DialogResult = DialogResult.OK;
        Close();
    }
}
