namespace Steam_Desktop_Authenticator
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.chkPeriodicChecking = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.numPeriodicInterval = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.chkCheckAll = new System.Windows.Forms.CheckBox();
            this.chkConfirmMarket = new System.Windows.Forms.CheckBox();
            this.chkConfirmTrades = new System.Windows.Forms.CheckBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.cmbLanguage = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numPeriodicInterval)).BeginInit();
            this.SuspendLayout();
            // 
            // chkPeriodicChecking
            // 
            this.chkPeriodicChecking.AutoSize = false;
            this.chkPeriodicChecking.Location = new System.Drawing.Point(12, 12);
            this.chkPeriodicChecking.Name = "chkPeriodicChecking";
            this.chkPeriodicChecking.Size = new System.Drawing.Size(287, 30);
            this.chkPeriodicChecking.TabIndex = 0;
            this.chkPeriodicChecking.Text = Strings.Get("SettingsForm_PeriodicChecking");
            this.chkPeriodicChecking.UseVisualStyleBackColor = true;
            this.chkPeriodicChecking.CheckedChanged += new System.EventHandler(this.chkPeriodicChecking_CheckedChanged);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            this.btnSave.Location = new System.Drawing.Point(12, 192);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(291, 38);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = Strings.Get("Common_Save");
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // numPeriodicInterval
            // 
            this.numPeriodicInterval.Location = new System.Drawing.Point(12, 51);
            this.numPeriodicInterval.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numPeriodicInterval.Name = "numPeriodicInterval";
            this.numPeriodicInterval.Size = new System.Drawing.Size(41, 22);
            this.numPeriodicInterval.TabIndex = 2;
            this.numPeriodicInterval.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = false;
            this.label1.Location = new System.Drawing.Point(59, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(240, 26);
            this.label1.TabIndex = 3;
            this.label1.Text = Strings.Get("SettingsForm_SecondsBetween");
            // 
            // chkCheckAll
            // 
            this.chkCheckAll.AutoSize = false;
            this.chkCheckAll.Location = new System.Drawing.Point(12, 81);
            this.chkCheckAll.Name = "chkCheckAll";
            this.chkCheckAll.Size = new System.Drawing.Size(287, 17);
            this.chkCheckAll.TabIndex = 4;
            this.chkCheckAll.Text = Strings.Get("SettingsForm_CheckAll");
            this.chkCheckAll.UseVisualStyleBackColor = true;
            // 
            // chkConfirmMarket
            // 
            this.chkConfirmMarket.AutoSize = false;
            this.chkConfirmMarket.Location = new System.Drawing.Point(12, 104);
            this.chkConfirmMarket.Name = "chkConfirmMarket";
            this.chkConfirmMarket.Size = new System.Drawing.Size(287, 17);
            this.chkConfirmMarket.TabIndex = 5;
            this.chkConfirmMarket.Text = Strings.Get("SettingsForm_AutoConfirmMarket");
            this.chkConfirmMarket.UseVisualStyleBackColor = true;
            this.chkConfirmMarket.CheckedChanged += new System.EventHandler(this.chkConfirmMarket_CheckedChanged);
            // 
            // chkConfirmTrades
            // 
            this.chkConfirmTrades.AutoSize = false;
            this.chkConfirmTrades.Location = new System.Drawing.Point(12, 127);
            this.chkConfirmTrades.Name = "chkConfirmTrades";
            this.chkConfirmTrades.Size = new System.Drawing.Size(287, 17);
            this.chkConfirmTrades.TabIndex = 6;
            this.chkConfirmTrades.Text = Strings.Get("SettingsForm_AutoConfirmTrades");
            this.chkConfirmTrades.UseVisualStyleBackColor = true;
            this.chkConfirmTrades.CheckedChanged += new System.EventHandler(this.chkConfirmTrades_CheckedChanged);
            // 
            // lblLanguage
            //
            this.lblLanguage.AutoSize = true;
            this.lblLanguage.Location = new System.Drawing.Point(12, 155);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(61, 13);
            this.lblLanguage.TabIndex = 7;
            this.lblLanguage.Text = Strings.Get("SettingsForm_Language");
            //
            // cmbLanguage
            //
            this.cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLanguage.FormattingEnabled = true;
            this.cmbLanguage.Location = new System.Drawing.Point(78, 152);
            this.cmbLanguage.Name = "cmbLanguage";
            this.cmbLanguage.Size = new System.Drawing.Size(221, 21);
            this.cmbLanguage.TabIndex = 8;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(311, 242);
            this.Controls.Add(this.cmbLanguage);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.chkConfirmTrades);
            this.Controls.Add(this.chkConfirmMarket);
            this.Controls.Add(this.chkCheckAll);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numPeriodicInterval);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chkPeriodicChecking);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Strings.Get("SettingsForm_Title");
            ((System.ComponentModel.ISupportInitialize)(this.numPeriodicInterval)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkPeriodicChecking;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.NumericUpDown numPeriodicInterval;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkCheckAll;
        private System.Windows.Forms.CheckBox chkConfirmMarket;
        private System.Windows.Forms.CheckBox chkConfirmTrades;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.ComboBox cmbLanguage;
    }
}