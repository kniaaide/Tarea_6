using System;
using System.Windows.Forms;

namespace Unidad6.Busquedas
{
    /// <summary>Punto de entrada de la aplicación Windows Forms (Tarea 6.2).</summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormBusquedas());
        }
    }
}
