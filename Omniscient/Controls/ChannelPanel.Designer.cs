﻿namespace Omniscient
{
    partial class ChannelPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SymbolComboBox = new System.Windows.Forms.ComboBox();
            this.Chart4CheckBox = new System.Windows.Forms.CheckBox();
            this.Chart3CheckBox = new System.Windows.Forms.CheckBox();
            this.Chart2CheckBox = new System.Windows.Forms.CheckBox();
            this.Chart1CheckBox = new System.Windows.Forms.CheckBox();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.NameToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // SymbolComboBox
            // 
            this.SymbolComboBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.SymbolComboBox.FormattingEnabled = true;
            this.SymbolComboBox.Location = new System.Drawing.Point(240, 0);
            this.SymbolComboBox.Name = "SymbolComboBox";
            this.SymbolComboBox.Size = new System.Drawing.Size(50, 21);
            this.SymbolComboBox.TabIndex = 12;
            // 
            // Chart4CheckBox
            // 
            this.Chart4CheckBox.AutoSize = true;
            this.Chart4CheckBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.Chart4CheckBox.Location = new System.Drawing.Point(215, 0);
            this.Chart4CheckBox.Name = "Chart4CheckBox";
            this.Chart4CheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Chart4CheckBox.Size = new System.Drawing.Size(25, 24);
            this.Chart4CheckBox.TabIndex = 11;
            this.Chart4CheckBox.Tag = "4";
            this.Chart4CheckBox.UseVisualStyleBackColor = true;
            this.Chart4CheckBox.CheckedChanged += new System.EventHandler(this.Chart4CheckBox_CheckedChanged);
            // 
            // Chart3CheckBox
            // 
            this.Chart3CheckBox.AutoSize = true;
            this.Chart3CheckBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.Chart3CheckBox.Location = new System.Drawing.Point(190, 0);
            this.Chart3CheckBox.Name = "Chart3CheckBox";
            this.Chart3CheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Chart3CheckBox.Size = new System.Drawing.Size(25, 24);
            this.Chart3CheckBox.TabIndex = 10;
            this.Chart3CheckBox.Tag = "3";
            this.Chart3CheckBox.UseVisualStyleBackColor = true;
            this.Chart3CheckBox.CheckedChanged += new System.EventHandler(this.Chart3CheckBox_CheckedChanged);
            // 
            // Chart2CheckBox
            // 
            this.Chart2CheckBox.AutoSize = true;
            this.Chart2CheckBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.Chart2CheckBox.Location = new System.Drawing.Point(165, 0);
            this.Chart2CheckBox.Name = "Chart2CheckBox";
            this.Chart2CheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Chart2CheckBox.Size = new System.Drawing.Size(25, 24);
            this.Chart2CheckBox.TabIndex = 9;
            this.Chart2CheckBox.Tag = "2";
            this.Chart2CheckBox.UseVisualStyleBackColor = true;
            this.Chart2CheckBox.CheckedChanged += new System.EventHandler(this.Chart2CheckBox_CheckedChanged);
            // 
            // Chart1CheckBox
            // 
            this.Chart1CheckBox.AutoSize = true;
            this.Chart1CheckBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.Chart1CheckBox.Location = new System.Drawing.Point(140, 0);
            this.Chart1CheckBox.Name = "Chart1CheckBox";
            this.Chart1CheckBox.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.Chart1CheckBox.Size = new System.Drawing.Size(25, 24);
            this.Chart1CheckBox.TabIndex = 8;
            this.Chart1CheckBox.Tag = "1";
            this.Chart1CheckBox.UseVisualStyleBackColor = true;
            this.Chart1CheckBox.CheckedChanged += new System.EventHandler(this.Chart1CheckBox_CheckedChanged);
            // 
            // NameTextBox
            // 
            this.NameTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.NameTextBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.NameTextBox.Location = new System.Drawing.Point(0, 0);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(140, 20);
            this.NameTextBox.TabIndex = 7;
            // 
            // NameToolTip
            // 
            this.NameToolTip.AutomaticDelay = 125;
            this.NameToolTip.AutoPopDelay = 5000;
            this.NameToolTip.InitialDelay = 125;
            this.NameToolTip.ReshowDelay = 25;
            this.NameToolTip.ShowAlways = true;
            // 
            // ChannelPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SymbolComboBox);
            this.Controls.Add(this.Chart4CheckBox);
            this.Controls.Add(this.Chart3CheckBox);
            this.Controls.Add(this.Chart2CheckBox);
            this.Controls.Add(this.Chart1CheckBox);
            this.Controls.Add(this.NameTextBox);
            this.Name = "ChannelPanel";
            this.Size = new System.Drawing.Size(306, 24);
            this.Load += new System.EventHandler(this.ChannelPanel_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox SymbolComboBox;
        public System.Windows.Forms.CheckBox Chart4CheckBox;
        public System.Windows.Forms.CheckBox Chart3CheckBox;
        public System.Windows.Forms.CheckBox Chart2CheckBox;
        public System.Windows.Forms.CheckBox Chart1CheckBox;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.ToolTip NameToolTip;
    }
}