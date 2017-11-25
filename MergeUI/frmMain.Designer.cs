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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tbAssembly = new AdamOneilSoftware.Controls.ToolStripSpringTextBox();
            this.btnExecute = new System.Windows.Forms.ToolStripButton();
            this.btnSaveAs = new System.Windows.Forms.ToolStripButton();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.btnSelectFile = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.pbMain = new System.Windows.Forms.ToolStripProgressBar();
            this.tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslAbout = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splcActions = new System.Windows.Forms.SplitContainer();
            this.tvwActions = new System.Windows.Forms.TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.lblErrors = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tbScript = new FastColoredTextBoxNS.FastColoredTextBox();
            this.tslSelectAssembly = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splcActions)).BeginInit();
            this.splcActions.Panel1.SuspendLayout();
            this.splcActions.Panel2.SuspendLayout();
            this.splcActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbScript)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.tbAssembly,
            this.btnExecute,
            this.btnSaveAs,
            this.btnRefresh,
            this.btnSelectFile});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(646, 25);
            this.toolStrip1.TabIndex = 0;
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
            this.tbAssembly.ReadOnly = true;
            this.tbAssembly.Size = new System.Drawing.Size(353, 25);
            // 
            // btnExecute
            // 
            this.btnExecute.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnExecute.Image = ((System.Drawing.Image)(resources.GetObject("btnExecute.Image")));
            this.btnExecute.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(67, 22);
            this.btnExecute.Text = "Execute";
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
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
            // btnRefresh
            // 
            this.btnRefresh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnRefresh.Image = ((System.Drawing.Image)(resources.GetObject("btnRefresh.Image")));
            this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(23, 22);
            this.btnRefresh.Text = "toolStripButton1";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnSelectFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnSelectFile.Image = ((System.Drawing.Image)(resources.GetObject("btnSelectFile.Image")));
            this.btnSelectFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(23, 22);
            this.btnSelectFile.Text = "...";
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pbMain,
            this.tslStatus,
            this.tslSelectAssembly,
            this.tslAbout});
            this.statusStrip1.Location = new System.Drawing.Point(0, 323);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(646, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // pbMain
            // 
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(100, 16);
            this.pbMain.Visible = false;
            // 
            // tslStatus
            // 
            this.tslStatus.Name = "tslStatus";
            this.tslStatus.Size = new System.Drawing.Size(39, 17);
            this.tslStatus.Text = "Ready";
            // 
            // tslAbout
            // 
            this.tslAbout.IsLink = true;
            this.tslAbout.Name = "tslAbout";
            this.tslAbout.Size = new System.Drawing.Size(301, 17);
            this.tslAbout.Spring = true;
            this.tslAbout.Text = "About";
            this.tslAbout.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.tslAbout.Click += new System.EventHandler(this.tslAbout_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splcActions);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tbScript);
            this.splitContainer1.Size = new System.Drawing.Size(646, 298);
            this.splitContainer1.SplitterDistance = 166;
            this.splitContainer1.TabIndex = 2;
            // 
            // splcActions
            // 
            this.splcActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splcActions.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splcActions.Location = new System.Drawing.Point(0, 0);
            this.splcActions.Name = "splcActions";
            this.splcActions.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splcActions.Panel1
            // 
            this.splcActions.Panel1.Controls.Add(this.tvwActions);
            // 
            // splcActions.Panel2
            // 
            this.splcActions.Panel2.Controls.Add(this.lblErrors);
            this.splcActions.Panel2.Controls.Add(this.pictureBox1);
            this.splcActions.Size = new System.Drawing.Size(166, 298);
            this.splcActions.SplitterDistance = 175;
            this.splcActions.TabIndex = 1;
            // 
            // tvwActions
            // 
            this.tvwActions.CheckBoxes = true;
            this.tvwActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvwActions.ImageIndex = 0;
            this.tvwActions.ImageList = this.imageList1;
            this.tvwActions.Location = new System.Drawing.Point(0, 0);
            this.tvwActions.Name = "tvwActions";
            this.tvwActions.SelectedImageIndex = 0;
            this.tvwActions.Size = new System.Drawing.Size(166, 175);
            this.tvwActions.TabIndex = 0;
            this.tvwActions.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.tvwActions_AfterCheck);
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
            this.imageList1.Images.SetKeyName(7, "Key");
            // 
            // lblErrors
            // 
            this.lblErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblErrors.Location = new System.Drawing.Point(54, 12);
            this.lblErrors.Name = "lblErrors";
            this.lblErrors.Size = new System.Drawing.Size(96, 97);
            this.lblErrors.TabIndex = 3;
            this.lblErrors.Text = "label1";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(36, 36);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // tbScript
            // 
            this.tbScript.AutoCompleteBracketsList = new char[] {
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
            this.tbScript.AutoIndentCharsPatterns = "";
            this.tbScript.AutoScrollMinSize = new System.Drawing.Size(25, 15);
            this.tbScript.BackBrush = null;
            this.tbScript.CharHeight = 15;
            this.tbScript.CharWidth = 7;
            this.tbScript.CommentPrefix = "--";
            this.tbScript.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.tbScript.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.tbScript.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbScript.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.tbScript.IsReplaceMode = false;
            this.tbScript.Language = FastColoredTextBoxNS.Language.SQL;
            this.tbScript.LeftBracket = '(';
            this.tbScript.Location = new System.Drawing.Point(0, 0);
            this.tbScript.Name = "tbScript";
            this.tbScript.Paddings = new System.Windows.Forms.Padding(0);
            this.tbScript.RightBracket = ')';
            this.tbScript.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.tbScript.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("tbScript.ServiceColors")));
            this.tbScript.Size = new System.Drawing.Size(476, 298);
            this.tbScript.TabIndex = 0;
            this.tbScript.Zoom = 100;
            // 
            // tslSelectAssembly
            // 
            this.tslSelectAssembly.IsLink = true;
            this.tslSelectAssembly.Name = "tslSelectAssembly";
            this.tslSelectAssembly.Size = new System.Drawing.Size(158, 17);
            this.tslSelectAssembly.Text = "Select assembly in solution...";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(646, 345);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Postulate Schema Merge";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splcActions.Panel1.ResumeLayout(false);
            this.splcActions.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splcActions)).EndInit();
            this.splcActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbScript)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private AdamOneilSoftware.Controls.ToolStripSpringTextBox tbAssembly;
        private System.Windows.Forms.ToolStripButton btnExecute;
        private System.Windows.Forms.ToolStripButton btnSaveAs;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripButton btnSelectFile;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar pbMain;
        private System.Windows.Forms.ToolStripStatusLabel tslStatus;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView tvwActions;
        private FastColoredTextBoxNS.FastColoredTextBox tbScript;
        private System.Windows.Forms.SplitContainer splcActions;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Label lblErrors;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolStripStatusLabel tslAbout;
        private System.Windows.Forms.ToolStripStatusLabel tslSelectAssembly;
    }
}

