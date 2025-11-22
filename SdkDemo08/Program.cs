using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SdkDemo08
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fatal al iniciar la aplicación:\n\n" + ex.Message + "\n\n" + ex.StackTrace, 
                    "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
       
    }
    
}
