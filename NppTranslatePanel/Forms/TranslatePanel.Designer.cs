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
        private System.Windows.Forms.ProgressBar progressTranslation;
        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.FlowLayoutPanel pnlActions;
        private System.Windows.Forms.Button btnOpenInNewTab;
        private System.Windows.Forms.Button btnSaveAs;
        private ScintillaOutput txtOutput;

        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressTranslation = new System.Windows.Forms.ProgressBar();
            this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
            this.pnlActions = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOpenInNewTab = new System.Windows.Forms.Button();
            this.btnSaveAs = new System.Windows.Forms.Button();
            this.txtOutput = new ScintillaOutput();
            this.layoutRoot.SuspendLayout();
            this.pnlActions.SuspendLayout();
            this.SuspendLayout();
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.AutoSize = false;
            this.lblStatus.Height = 20;
            this.lblStatus.Padding = new System.Windows.Forms.Padding(4, 3, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Ready";
            //
            // progressTranslation
            //
            this.progressTranslation.MarqueeAnimationSpeed = 30;
            this.progressTranslation.Margin = new System.Windows.Forms.Padding(8, 5, 0, 0);
            this.progressTranslation.Name = "progressTranslation";
            this.progressTranslation.Size = new System.Drawing.Size(56, 18);
            this.progressTranslation.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressTranslation.Visible = false;
            //
            // layoutRoot
            //
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.lblStatus, 0, 0);
            this.layoutRoot.Controls.Add(this.pnlActions, 0, 1);
            this.layoutRoot.Controls.Add(this.txtOutput, 0, 2);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 34F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //
            // pnlActions
            //
            this.pnlActions.Controls.Add(this.btnOpenInNewTab);
            this.pnlActions.Controls.Add(this.btnSaveAs);
            this.pnlActions.Controls.Add(this.progressTranslation);
            this.pnlActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlActions.Name = "pnlActions";
            this.pnlActions.Padding = new System.Windows.Forms.Padding(2, 2, 0, 0);
            this.pnlActions.WrapContents = false;
            //
            // btnOpenInNewTab
            //
            this.btnOpenInNewTab.AutoSize = true;
            this.btnOpenInNewTab.Name = "btnOpenInNewTab";
            this.btnOpenInNewTab.Text = "Open in New Tab";
            this.btnOpenInNewTab.UseVisualStyleBackColor = true;
            //
            // btnSaveAs
            //
            this.btnSaveAs.AutoSize = true;
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Text = "Save As...";
            this.btnSaveAs.UseVisualStyleBackColor = true;
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
            this.Controls.Add(this.layoutRoot);
            this.Name = "TranslatePanel";
            this.Text = "Translate";
            this.layoutRoot.ResumeLayout(false);
            this.pnlActions.ResumeLayout(false);
            this.pnlActions.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
