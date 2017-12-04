namespace Postulate.MergeUI
{
    partial class frmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmAbout));
            this.btnOK = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.btnInstallUpdate = new System.Windows.Forms.Button();
            this.pbMain = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.lblCoreVersion = new System.Windows.Forms.Label();
            this.lblUIVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOK.Location = new System.Drawing.Point(367, 136);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(41, 39);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(59, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(198, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Postulate ORM Schema Merge";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(59, 30);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(107, 13);
            this.linkLabel1.TabIndex = 3;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Source on GitHub";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // btnInstallUpdate
            // 
            this.btnInstallUpdate.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnInstallUpdate.Location = new System.Drawing.Point(62, 136);
            this.btnInstallUpdate.Name = "btnInstallUpdate";
            this.btnInstallUpdate.Size = new System.Drawing.Size(135, 23);
            this.btnInstallUpdate.TabIndex = 5;
            this.btnInstallUpdate.Text = "Install Update";
            this.btnInstallUpdate.UseVisualStyleBackColor = true;
            this.btnInstallUpdate.Click += new System.EventHandler(this.btnInstallUpdate_Click);
            // 
            // pbMain
            // 
            this.pbMain.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.pbMain.Location = new System.Drawing.Point(203, 136);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(158, 23);
            this.pbMain.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pbMain.TabIndex = 6;
            this.pbMain.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(59, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(247, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Adam O\'Neil | adamosoftware@gmail.com";
            // 
            // lblCoreVersion
            // 
            this.lblCoreVersion.AutoSize = true;
            this.lblCoreVersion.Location = new System.Drawing.Point(59, 73);
            this.lblCoreVersion.Name = "lblCoreVersion";
            this.lblCoreVersion.Size = new System.Drawing.Size(191, 13);
            this.lblCoreVersion.TabIndex = 8;
            this.lblCoreVersion.Text = "Postulate.Orm.Core version {0}";
            // 
            // lblUIVersion
            // 
            this.lblUIVersion.AutoSize = true;
            this.lblUIVersion.Location = new System.Drawing.Point(59, 96);
            this.lblUIVersion.Name = "lblUIVersion";
            this.lblUIVersion.Size = new System.Drawing.Size(91, 13);
            this.lblUIVersion.TabIndex = 9;
            this.lblUIVersion.Text = "UI version {0}";
            // 
            // frmAbout
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnOK;
            this.ClientSize = new System.Drawing.Size(454, 171);
            this.Controls.Add(this.lblUIVersion);
            this.Controls.Add(this.lblCoreVersion);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pbMain);
            this.Controls.Add(this.btnInstallUpdate);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnOK);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAbout";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            this.Load += new System.EventHandler(this.frmAbout_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button btnInstallUpdate;
        private System.Windows.Forms.ProgressBar pbMain;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblCoreVersion;
        private System.Windows.Forms.Label lblUIVersion;
    }
}