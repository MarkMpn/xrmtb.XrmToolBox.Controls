namespace xrmtb.XrmToolBox.Controls
{
    partial class LookupControl
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
            this.txtLookup = new System.Windows.Forms.TextBox();
            this.btnLookup = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // txtLookup
            // 
            this.txtLookup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLookup.Location = new System.Drawing.Point(0, 0);
            this.txtLookup.Name = "txtLookup";
            this.txtLookup.Size = new System.Drawing.Size(350, 20);
            this.txtLookup.TabIndex = 0;
            this.txtLookup.Click += new System.EventHandler(this.txtLookup_Click);
            this.txtLookup.TextChanged += new System.EventHandler(this.txtLookup_TextChanged);
            this.txtLookup.Enter += new System.EventHandler(this.txtLookup_Enter);
            this.txtLookup.Leave += new System.EventHandler(this.txtLookup_Leave);
            // 
            // btnLookup
            // 
            this.btnLookup.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnLookup.Location = new System.Drawing.Point(350, 0);
            this.btnLookup.Name = "btnLookup";
            this.btnLookup.Size = new System.Drawing.Size(24, 20);
            this.btnLookup.TabIndex = 1;
            this.btnLookup.Text = "...";
            this.btnLookup.UseVisualStyleBackColor = true;
            this.btnLookup.Click += new System.EventHandler(this.btnLookup_Click);
            // 
            // errorProvider
            // 
            this.errorProvider.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.NeverBlink;
            this.errorProvider.ContainerControl = this;
            // 
            // LookupControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtLookup);
            this.Controls.Add(this.btnLookup);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "LookupControl";
            this.Size = new System.Drawing.Size(374, 20);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLookup;
        private System.Windows.Forms.Button btnLookup;
        private System.Windows.Forms.ErrorProvider errorProvider;
    }
}
