using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PrototypeManager;
using System.Windows.Forms;

namespace PMGUI {
    public partial class Form2 : Form {
        public Form2() {
            InitializeComponent();

            MessageBox.Show("Welcome, Well... This is my 3º Tool with a Font Editor, I really don't like works with fonts but this time created using the xmoe info as base, Thank you xmoe!\n\nHow to use:\nBasically Open a font, type the a Windows Font Name at the last textbox and in the textbox in the center type all characters that you want replace, after this press update and save!\n\nThe Load Remap:\nSelect a plain text with lines in this format: é=#\nThe First char is the \"Fake\" and the Second char is the physical character.", "PMGUI - Font Editor");
        }

        int FontWidth => Glyphs.First().Texture.Width;
        int FontHeight => Glyphs.First().Texture.Height;

        Dictionary<char, char> Remap = new Dictionary<char, char>();

        Glyph[] Glyphs;
        new FNT Font;
        private void button1_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Prototype Font Files|*.fnt;*.pro";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string FN = Path.GetDirectoryName(fd.FileName) + "\\" + Path.GetFileNameWithoutExtension(fd.FileName);

            byte[] FNTD = File.ReadAllBytes(FN+".fnt");
            byte[] PROD = File.ReadAllBytes(FN+".pro");

            Font = new FNT(FNTD, PROD);
            Glyphs = Font.GetGlyphs();
            textBox4.Text = (FontWidth - (FontWidth/8)) + ",0";
            PreviewText();
        }

        private void textBox2_TextChanged(object sender, EventArgs e) {
            timer1.Stop();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            PreviewText();
            timer1.Stop();
        }

        private void PreviewText() {
            if (textBox2.Text.Length == 0)
                return;

            Bitmap Texture = new Bitmap(FontWidth * textBox2.Text.Length, FontHeight);
            Graphics g = Graphics.FromImage(Texture);
            for (int i = 0, X = 0; i < textBox2.Text.Length; i++) {
                char c = textBox2.Text[i];
                Glyph Glyph = (from x in Glyphs where x.Char == c select x).FirstOrDefault();
                if (Glyph.Char == '\x0') {
                    continue;
                }
                g.DrawImageUnscaled(Glyph.Texture, new Point(X, 0));
                X += Glyph.Texture.Width;
            }

            g.Dispose();
            pictureBox1.Image = Texture;
        }

        private void button3_Click(object sender, EventArgs e) {
            if (textBox1.Text.Length > Glyphs.Length)
                throw new Exception("Too many glyphs in the list");

            var Font = new Font(textBox3.Text, float.Parse(textBox4.Text), FontStyle.Regular, GraphicsUnit.Pixel);
            int Missed = 1;
            for (int i = 0; i < textBox1.Text.Length; i++) {
                char c = textBox1.Text[i];
                int x = GetGlyphIndex(c);
                if (x == -1) {
                    x = Glyphs.Length - Missed++;
                }
                Glyphs[x].Char = c;
                var Buffer = new Bitmap(FontWidth, FontHeight);
                Graphics g = Graphics.FromImage(Buffer);
                if (Remap.ContainsKey(c))
                    c = Remap[c];

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                var Size = g.MeasureString(c.ToString(), Font, new SizeF(FontWidth, FontHeight));
                g.DrawString(c.ToString(), Font, Brushes.White, new Rectangle(0, 0, FontWidth, FontHeight));
                g.Flush();
                g.Dispose();


                Glyphs[x].Changed = true;
                Glyphs[x].Texture = Buffer.Clone(new Rectangle(new Point(0, 0), new Size((int)Size.Width, (int)Size.Height)), PixelFormat.Format32bppArgb);
            }

            PreviewText();

        }

        private int GetGlyphIndex(char c) {
            for (int i = 0; i < Glyphs.Length; i++) {
                if (Glyphs[i].Char == c)
                    return i;
            }
            return -1;
        }

        private void button2_Click(object sender, EventArgs e) {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "All Prototype Font Files|*.fnt;*.pro";
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string FN = Path.GetDirectoryName(fd.FileName) + "\\" + Path.GetFileNameWithoutExtension(fd.FileName);


            Font.UpdateGlyphs(Glyphs, out byte[] FNT, out byte[] PRO);

            File.WriteAllBytes(FN + ".fnt", FNT);
            File.WriteAllBytes(FN + ".pro", PRO);

            MessageBox.Show("Saved");
        }

        private void button4_Click(object sender, EventArgs e) {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "String Reloader Chars Remap|Chars.lst";
            Remap = new Dictionary<char, char>();
            if (fd.ShowDialog() != DialogResult.OK)
                return;

            string[] Lines = File.ReadAllLines(fd.FileName);

            foreach (string Line in Lines) {
                if (Line.Trim().Length != 3 || Line[1] != '=')
                    continue;

                Remap.Add(Line[0], Line[2]);
            }
        }
    }
}
            
