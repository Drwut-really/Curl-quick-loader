using CurlQuickLoader.Models;

namespace CurlQuickLoader.Forms;

public class HeadersEditorControl : UserControl
{
    private DataGridView _grid = null!;
    private Button _btnAdd = null!;
    private Button _btnRemove = null!;

    public HeadersEditorControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _grid = new DataGridView();
        _btnAdd = new Button();
        _btnRemove = new Button();

        SuspendLayout();

        // Grid
        _grid.Dock = DockStyle.Fill;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BorderStyle = BorderStyle.None;
        _grid.BackgroundColor = SystemColors.Window;

        var colKey = new DataGridViewTextBoxColumn
        {
            HeaderText = "Header Name",
            Name = "Key",
            FillWeight = 40
        };
        var colValue = new DataGridViewTextBoxColumn
        {
            HeaderText = "Value",
            Name = "Value",
            FillWeight = 60
        };
        _grid.Columns.Add(colKey);
        _grid.Columns.Add(colValue);

        // Buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 4, 0, 0)
        };

        _btnAdd.Text = "+ Add Header";
        _btnAdd.AutoSize = true;
        _btnAdd.Click += BtnAdd_Click;

        _btnRemove.Text = "Remove Selected";
        _btnRemove.AutoSize = true;
        _btnRemove.Click += BtnRemove_Click;

        buttonPanel.Controls.Add(_btnAdd);
        buttonPanel.Controls.Add(_btnRemove);

        Controls.Add(_grid);
        Controls.Add(buttonPanel);

        ResumeLayout(false);
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        _grid.Rows.Add("", "");
        // Focus the new row's Key cell
        int newRowIdx = _grid.Rows.Count - 1;
        _grid.CurrentCell = _grid.Rows[newRowIdx].Cells["Key"];
        _grid.BeginEdit(true);
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count > 0)
        {
            int idx = _grid.SelectedRows[0].Index;
            _grid.Rows.RemoveAt(idx);
        }
        else if (_grid.CurrentCell != null)
        {
            _grid.Rows.RemoveAt(_grid.CurrentCell.RowIndex);
        }
    }

    public List<Header> GetHeaders()
    {
        var headers = new List<Header>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            string key = row.Cells["Key"].Value?.ToString() ?? string.Empty;
            string value = row.Cells["Value"].Value?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
                headers.Add(new Header(key, value));
        }
        return headers;
    }

    public void SetHeaders(List<Header> headers)
    {
        _grid.Rows.Clear();
        foreach (var h in headers)
            _grid.Rows.Add(h.Key, h.Value);
    }
}
