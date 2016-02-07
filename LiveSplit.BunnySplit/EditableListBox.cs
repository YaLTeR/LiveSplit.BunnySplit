using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace LiveSplit.BunnySplit
{
    // Original EditableListBox by Fatalis at https://github.com/fatalis.
    
    /// <summary>
    /// A DataGridView that emulates the look of a ListBox and can be edited.
    /// </summary>
    partial class EditableListBox : DataGridView
    {
        private ContextMenu menuRemove;

        public EditableListBox()
        {
            this.menuRemove = new ContextMenu();
            var delete = new MenuItem("Remove Selected");
            delete.Click += delete_Click;
            this.menuRemove.MenuItems.Add(delete);

            this.AllowUserToResizeRows = false;
            this.RowHeadersVisible = false;
            this.ColumnHeadersVisible = false;
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            this.CellBorderStyle = DataGridViewCellBorderStyle.None;
            this.BorderStyle = BorderStyle.Fixed3D;
            this.BackgroundColor = SystemColors.Window;

            this.RowTemplate.Height = base.Font.Height + 1;
        }

        void delete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in this.SelectedRows)
            {
                if (!row.IsNewRow)
                    this.Rows.Remove(row);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            if (!this.Enabled)
                this.CurrentCell = null;
            this.DefaultCellStyle.BackColor = this.Enabled ? SystemColors.Window : SystemColors.Control;
            this.DefaultCellStyle.ForeColor = this.Enabled ? SystemColors.ControlText : SystemColors.GrayText;
            this.BackgroundColor = this.Enabled ? SystemColors.Window : SystemColors.Control;
        }

        protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseUp(e);

            if (e.Button == MouseButtons.Right)
                this.menuRemove.Show(this, e.Location);
        }

        public List<string> GetValues()
        {
            var ret = new List<string>();
            foreach (DataGridViewRow row in this.Rows)
            {
                if (row.IsNewRow || (this.CurrentRow == row && this.IsCurrentRowDirty))
                    continue;

                string value = (string)row.Cells[0].Value;
                if (value == null)
                    continue;

                value = value.Trim().Replace("|", String.Empty);
                if (value.Length > 0)
                    ret.Add(value);
            }
            return ret;
        }
    }
}