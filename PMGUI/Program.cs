using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PMGUI
{
    public class Program {

        [STAThread]
        public static void Main(string[] Args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            Application.Run(new Main());
        }
    }
}
