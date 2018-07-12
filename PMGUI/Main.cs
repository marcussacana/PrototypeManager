using PrototypeManager;
using System;
using System.Data;


using System.Linq;
using System.Windows.Forms;

namespace PMGUI {
    public partial class Main : Form {
        public Main() {
            InitializeComponent();
        }

        PSB Editor;
        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Prototype PSB Scripts|*.psb";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            Editor = new PSB(System.IO.File.ReadAllBytes(fd.FileName));
            listBox1.Items.Clear();
            foreach (string str in Editor.Import())
                listBox1.Items.Add(str);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "All Prototype PSB Scripts|*.psb";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Strings = listBox1.Items.Cast<string>().ToArray();

            byte[] Output = Editor.Export(Strings);
            System.IO.File.WriteAllBytes(fd.FileName, Output);

            MessageBox.Show("Saved");
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\n' || e.KeyChar == '\r') {
                try {
                    listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
                } catch { }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                int i = listBox1.SelectedIndex;
                Text = "ID: " + i + "/" + listBox1.Items.Count;
                textBox1.Text = listBox1.Items[i].ToString();
            } catch { }
        }
        private void fontToolStripMenuItem_Click(object sender, EventArgs e) {

            Form2 f = new Form2();
            f.ShowDialog();
        }
    }
}
