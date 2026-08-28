namespace NppTranslatePanel.Forms
{
    partial class TranslatePanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private System.Windows.Forms.Label lblStatus;
        private ScintillaOutput txtOutput;

        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtOutput = new ScintillaOutput();
            this.SuspendLayout();
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStatus.AutoSize = false;
            this.lblStatus.Height = 20;
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4, 3, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Ready";
            //
            // txtOutput
            //
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.TabStop = false;
            //
            // TranslatePanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 500);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.lblStatus);
            this.Name = "TranslatePanel";
            this.Text = "Translate";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
