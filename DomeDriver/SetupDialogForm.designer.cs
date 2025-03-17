using System;
using System.Windows.Forms;
namespace ASCOM.AshsanDomelatest.Dome
{
    partial class SetupDialogForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox comboBoxDomeComPort;
        private System.Windows.Forms.TextBox textBoxTelescopeProgID;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelTelescope;
        private System.Windows.Forms.Label labelDomeComPort;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.comboBoxDomeComPort = new System.Windows.Forms.ComboBox();
            this.textBoxTelescopeProgID = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelTelescope = new System.Windows.Forms.Label();
            this.labelDomeComPort = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBoxDomeComPort
            // 
            this.comboBoxDomeComPort.FormattingEnabled = true;
            this.comboBoxDomeComPort.Location = new System.Drawing.Point(127, 12);
            this.comboBoxDomeComPort.Name = "comboBoxDomeComPort";
            this.comboBoxDomeComPort.Size = new System.Drawing.Size(150, 21);
            this.comboBoxDomeComPort.TabIndex = 3;
            // 
            // textBoxTelescopeProgID
            // 
            this.textBoxTelescopeProgID.Location = new System.Drawing.Point(127, 39);
            this.textBoxTelescopeProgID.Name = "textBoxTelescopeProgID";
            this.textBoxTelescopeProgID.Size = new System.Drawing.Size(150, 20);
            this.textBoxTelescopeProgID.TabIndex = 5;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(50, 70);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(150, 70);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelTelescope
            // 
            this.labelTelescope.AutoSize = true;
            this.labelTelescope.Location = new System.Drawing.Point(12, 45);
            this.labelTelescope.Name = "labelTelescope";
            this.labelTelescope.Size = new System.Drawing.Size(96, 13);
            this.labelTelescope.TabIndex = 4;
            this.labelTelescope.Text = "Telescope ProgID:";
            // 
            // labelDomeComPort
            // 
            this.labelDomeComPort.AutoSize = true;
            this.labelDomeComPort.Location = new System.Drawing.Point(12, 15);
            this.labelDomeComPort.Name = "labelDomeComPort";
            this.labelDomeComPort.Size = new System.Drawing.Size(87, 13);
            this.labelDomeComPort.TabIndex = 2;
            this.labelDomeComPort.Text = "Dome COM Port:";
            // 
            // SetupDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(327, 104);
            this.Controls.Add(this.labelDomeComPort);
            this.Controls.Add(this.comboBoxDomeComPort);
            this.Controls.Add(this.labelTelescope);
            this.Controls.Add(this.textBoxTelescopeProgID);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SetupDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Setup";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}