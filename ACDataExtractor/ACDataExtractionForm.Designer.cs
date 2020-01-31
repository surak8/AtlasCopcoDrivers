namespace ACDataExtractor {
	partial class ACDataExtractionForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.msMainMenu = new System.Windows.Forms.MenuStrip();
			this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
			this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.msMainMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// msMainMenu
			// 
			this.msMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile});
			this.msMainMenu.Location = new System.Drawing.Point(0, 0);
			this.msMainMenu.Name = "msMainMenu";
			this.msMainMenu.Size = new System.Drawing.Size(370, 24);
			this.msMainMenu.TabIndex = 0;
			this.msMainMenu.Text = "menuStrip1";
			// 
			// tsmiFile
			// 
			this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.exitToolStripMenuItem});
			this.tsmiFile.Name = "tsmiFile";
			this.tsmiFile.Size = new System.Drawing.Size(37, 20);
			this.tsmiFile.Text = "File";
			// 
			// tsmiOpen
			// 
			this.tsmiOpen.Name = "tsmiOpen";
			this.tsmiOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.tsmiOpen.Size = new System.Drawing.Size(180, 22);
			this.tsmiOpen.Text = "Open...";
			this.tsmiOpen.Click += new System.EventHandler(this.tsmiOpen_Click);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.exitToolStripMenuItem.Text = "Exit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// ACDataExtractionForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(370, 339);
			this.Controls.Add(this.msMainMenu);
			this.MainMenuStrip = this.msMainMenu;
			this.Name = "ACDataExtractionForm";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.ACDataExtractionForm_Load);
			this.msMainMenu.ResumeLayout(false);
			this.msMainMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip msMainMenu;
		private System.Windows.Forms.ToolStripMenuItem tsmiFile;
		private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
	}
}

