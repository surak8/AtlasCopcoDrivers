using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ACDataExtractor {
	public partial class ACDataExtractionForm : Form {
		public ACDataExtractionForm() {
			InitializeComponent();
		}

		  void tsmiOpen_Click(object sender, EventArgs e) {

		}

		  void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			CancelEventArgs cea;

			Application.Exit(cea=new CancelEventArgs());
			if (!cea.Cancel)
				return;
			Application.Exit();
		}

		  void ACDataExtractionForm_Load(object sender, EventArgs e) {

		}
	}
}
