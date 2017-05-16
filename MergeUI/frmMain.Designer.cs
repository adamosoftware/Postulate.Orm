namespace Postulate.MergeUI
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvwActions = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.tbSQL = new FastColoredTextBoxNS.FastColoredTextBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tbAssembly = new System.Windows.Forms.ToolStripTextBox();
            this.btnSelectAssembly = new System.Windows.Forms.ToolStripButton();
            this.btnExecute = new System.Windows.Forms.ToolStripButton();
            this.btnSaveAs = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSQL)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvwActions);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tbSQL);
            this.splitContainer1.Size = new System.Drawing.Size(643, 286);
            this.splitContainer1.SplitterDistance = 275;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 0;
            // 
            // tvwActions
            // 
            this.tvwActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvwActions.ImageIndex = 0;
            this.tvwActions.ImageList = this.imageList1;
            this.tvwActions.Location = new System.Drawing.Point(0, 0);
            this.tvwActions.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tvwActions.Name = "tvwActions";
            this.tvwActions.SelectedImageIndex = 0;
            this.tvwActions.Size = new System.Drawing.Size(275, 286);
            this.tvwActions.TabIndex = 0;
            this.tvwActions.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvwActions_AfterSelect);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Alter");
            this.imageList1.Images.SetKeyName(1, "Create");
            this.imageList1.Images.SetKeyName(2, "Drop");
            this.imageList1.Images.SetKeyName(3, "Table");
            this.imageList1.Images.SetKeyName(4, "Column");
            this.imageList1.Images.SetKeyName(5, "Database");
            this.imageList1.Images.SetKeyName(6, "ForeignKey");
            // 
            // tbSQL
            // 
            this.tbSQL.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.tbSQL.AutoIndentCharsPatterns = "";
            this.tbSQL.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            this.tbSQL.BackBrush = null;
            this.tbSQL.CharHeight = 14;
            this.tbSQL.CharWidth = 8;
            this.tbSQL.CommentPrefix = "--";
            this.tbSQL.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tbSQL.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.tbSQL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbSQL.IsReplaceMode = false;
            this.tbSQL.Language = FastColoredTextBoxNS.Language.SQL;
            this.tbSQL.LeftBracket = '(';
            this.tbSQL.Location = new System.Drawing.Point(0, 0);
            this.tbSQL.Name = "tbSQL";
            this.tbSQL.Paddings = new System.Windows.Forms.Padding(0);
            this.tbSQL.ReadOnly = true;
            this.tbSQL.RightBracket = ')';
            this.tbSQL.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.tbSQL.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("tbSQL.ServiceColors")));
            this.tbSQL.Size = new System.Drawing.Size(363, 286);
            this.tbSQL.TabIndex = 0;
            this.tbSQL.Zoom = 100;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tbAssembly,
            this.btnSelectAssembly,
            this.btnExecute,
            this.btnSaveAs});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(643, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(61, 22);
            this.toolStripLabel1.Text = "Assembly:";
            // 
            // tbAssembly
            // 
            this.tbAssembly.Name = "tbAssembly";
            this.tbAssembly.Size = new System.Drawing.Size(349, 25);
            // 
            // btnSelectAssembly
            // 
            this.btnSelectAssembly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSelectAssembly.Image = ((System.Drawing.Image)(resources.GetObject("btnSelectAssembly.Image")));
            this.btnSelectAssembly.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSelectAssembly.Name = "btnSelectAssembly";
            this.btnSelectAssembly.Size = new System.Drawing.Size(23, 22);
            this.btnSelectAssembly.Text = "...";
            this.btnSelectAssembly.Click += new System.EventHandler(this.btnSelectAssembly_Click);
            // 
            // btnExecute
            // 
            this.btnExecute.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnExecute.Image = ((System.Drawing.Image)(resources.GetObject("btnExecute.Image")));
            this.btnExecute.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(67, 22);
            this.btnExecute.Text = "Execute";
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnSaveAs.Image = ((System.Drawing.Image)(resources.GetObject("btnSaveAs.Image")));
            this.btnSaveAs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(76, 22);
            this.btnSaveAs.Text = "Save As...";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(643, 311);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Schema Merge";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResizeEnd += new System.EventHandler(this.frmMain_ResizeEnd);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tbSQL)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox tbAssembly;
        private System.Windows.Forms.ToolStripButton btnSelectAssembly;
        private System.Windows.Forms.TreeView tvwActions;
        private System.Windows.Forms.ToolStripButton btnExecute;
        private System.Windows.Forms.ImageList imageList1;
        private FastColoredTextBoxNS.FastColoredTextBox tbSQL;
        private System.Windows.Forms.ToolStripButton btnSaveAs;
    }
}

