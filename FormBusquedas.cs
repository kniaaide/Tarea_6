using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Unidad6.Busquedas
{
    /// <summary>
    /// Interfaz de la Tarea 6.2. Pestañas:
    ///   1) Experimento  : parámetros, ejecución y tabla de resultados.
    ///   2) Gráficas     : comparaciones, iteraciones o tiempo vs N (escala log-log).
    ///   3) Construcción : costo de ordenar y de construir las tablas hash (colisiones, factor de carga).
    ///   4) Búsqueda manual : buscar una clave concreta con cada método y ver sus métricas.
    ///   5) Pruebas      : verificación automática PASA/FALLA de los algoritmos.
    /// Los experimentos se ejecutan en un hilo de fondo para que la ventana no se congele.
    /// </summary>
    public class FormBusquedas : Form
    {
        private static readonly string[] Metodos =
            { "Secuencial", "Binaria", "Hash (encadenamiento)", "Hash (dir. abierto)" };
        private static readonly Color[] Colores =
            { Color.FromArgb(192, 0, 0), Color.FromArgb(31, 78, 121), Color.FromArgb(0, 128, 0), Color.FromArgb(237, 125, 49) };
        private static readonly string[] Metricas =
        {
            "Comparaciones promedio - claves presentes", "Comparaciones promedio - claves ausentes",
            "Iteraciones promedio - claves presentes", "Iteraciones promedio - claves ausentes",
            "Tiempo promedio por búsqueda (µs) - claves presentes", "Tiempo promedio por búsqueda (µs) - claves ausentes"
        };

        private readonly List<FilaResultado> _filas = new List<FilaResultado>();
        private readonly List<InfoConstruccion> _construccion = new List<InfoConstruccion>();
        private ContextoBusqueda _contexto;

        private NumericUpDown _numN, _numK, _numSemilla, _numAlfaEnc, _numAlfaAbi;
        private ComboBox _cmbFuncion, _cmbMetrica;
        private Button _btnEjecutar, _btnSerie, _btnCsv, _btnLimpiar;
        private Label _lblEstado;
        private DataGridView _grid;
        private PanelGrafica _grafica;
        private TextBox _txtConstruccion, _txtClave, _txtManual, _txtPruebas;

        public FormBusquedas()
        {
            Inicializar();
        }

        // ==================================================================
        // Construcción de la interfaz
        // ==================================================================
        private void Inicializar()
        {
            Text = "Unidad 6 - Tarea 6.2: Evaluación empírica de métodos de búsqueda";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1150, 720);
            MinimumSize = new Size(900, 600);

            TabControl pestanas = new TabControl();
            pestanas.Dock = DockStyle.Fill;
            pestanas.TabPages.Add(CrearPestanaExperimento());
            pestanas.TabPages.Add(CrearPestanaGraficas());
            pestanas.TabPages.Add(CrearPestanaConstruccion());
            pestanas.TabPages.Add(CrearPestanaManual());
            pestanas.TabPages.Add(CrearPestanaPruebas());
            Controls.Add(pestanas);
        }

        private TabPage CrearPestanaExperimento()
        {
            TabPage p = new TabPage("Experimento");
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 3;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel barra = new FlowLayoutPanel();
            barra.Dock = DockStyle.Fill;
            barra.AutoSize = true;
            barra.WrapContents = true;
            barra.Padding = new Padding(4);

            _numN = Numero(barra, "N (datos):", 10, 2000000, 100000, 0, 10000);
            _numK = Numero(barra, "Búsquedas por escenario:", 1, 200000, 1000, 0, 100);
            _numSemilla = Numero(barra, "Semilla:", 0, 2000000000, 12345, 0, 1);

            barra.Controls.Add(Etiqueta("Función hash:"));
            _cmbFuncion = new ComboBox();
            _cmbFuncion.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFuncion.Items.Add("División: k mod m");
            _cmbFuncion.Items.Add("Multiplicativa (Knuth)");
            _cmbFuncion.SelectedIndex = 0;
            _cmbFuncion.Width = 160;
            barra.Controls.Add(_cmbFuncion);

            _numAlfaEnc = Numero(barra, "α encadenamiento:", 0.1m, 10m, 1.0m, 2, 0.1m);
            _numAlfaAbi = Numero(barra, "α dir. abierto (<1):", 0.05m, 0.95m, 0.5m, 2, 0.05m);

            FlowLayoutPanel botones = new FlowLayoutPanel();
            botones.Dock = DockStyle.Fill;
            botones.AutoSize = true;
            botones.Padding = new Padding(4);

            _btnEjecutar = Boton(botones, "Ejecutar experimento (N actual)", OnEjecutar);
            _btnSerie = Boton(botones, "Ejecutar serie N = 1 000 · 10 000 · 100 000 · 1 000 000", OnSerie);
            _btnCsv = Boton(botones, "Exportar CSV", OnExportarCsv);
            _btnLimpiar = Boton(botones, "Limpiar resultados", OnLimpiar);
            _lblEstado = new Label();
            _lblEstado.AutoSize = true;
            _lblEstado.Margin = new Padding(12, 9, 3, 3);
            _lblEstado.Text = "Listo.";
            botones.Controls.Add(_lblEstado);

            _grid = new DataGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            string[] cols = { "Método", "N", "Escenario", "Búsquedas", "Encontradas", "Comp. total", "Comp. mín", "Comp. máx",
                              "Comp. promedio", "Iter. promedio", "Tiempo total (ms)", "Tiempo prom. (µs)" };
            for (int i = 0; i < cols.Length; i++) _grid.Columns.Add("c" + i, cols[i]);

            t.Controls.Add(barra, 0, 0);
            t.Controls.Add(botones, 0, 1);
            t.Controls.Add(_grid, 0, 2);
            p.Controls.Add(t);
            return p;
        }

        private TabPage CrearPestanaGraficas()
        {
            TabPage p = new TabPage("Gráficas");
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel barra = new FlowLayoutPanel();
            barra.Dock = DockStyle.Fill;
            barra.AutoSize = true;
            barra.Padding = new Padding(4);
            barra.Controls.Add(Etiqueta("Métrica:"));
            _cmbMetrica = new ComboBox();
            _cmbMetrica.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbMetrica.Width = 380;
            for (int i = 0; i < Metricas.Length; i++) _cmbMetrica.Items.Add(Metricas[i]);
            _cmbMetrica.SelectedIndex = 1;
            _cmbMetrica.SelectedIndexChanged += delegate { ActualizarGrafica(); };
            barra.Controls.Add(_cmbMetrica);
            Label nota = new Label();
            nota.AutoSize = true;
            nota.Margin = new Padding(12, 9, 3, 3);
            nota.Text = "Se grafica un punto por cada N ejecutado (use la serie para ver la tendencia).";
            barra.Controls.Add(nota);

            _grafica = new PanelGrafica();
            _grafica.Dock = DockStyle.Fill;

            t.Controls.Add(barra, 0, 0);
            t.Controls.Add(_grafica, 0, 1);
            p.Controls.Add(t);
            return p;
        }

        private TabPage CrearPestanaConstruccion()
        {
            TabPage p = new TabPage("Construcción de estructuras");
            _txtConstruccion = CajaTexto();
            _txtConstruccion.Text = "Ejecute un experimento para ver el costo de ordenar el arreglo y de construir las tablas hash.";
            p.Controls.Add(_txtConstruccion);
            return p;
        }

        private TabPage CrearPestanaManual()
        {
            TabPage p = new TabPage("Búsqueda manual");
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel barra = new FlowLayoutPanel();
            barra.Dock = DockStyle.Fill;
            barra.AutoSize = true;
            barra.Padding = new Padding(4);
            barra.Controls.Add(Etiqueta("Clave (entero ≥ 0):"));
            _txtClave = new TextBox();
            _txtClave.Width = 110;
            barra.Controls.Add(_txtClave);
            Boton(barra, "Secuencial", delegate { BuscarManual(0); });
            Boton(barra, "Binaria", delegate { BuscarManual(1); });
            Boton(barra, "Hash encadenamiento", delegate { BuscarManual(2); });
            Boton(barra, "Hash dir. abierto", delegate { BuscarManual(3); });
            Boton(barra, "Los cuatro", delegate { BuscarManual(-1); });
            Boton(barra, "Tomar una clave presente al azar", delegate { TomarClavePresente(); });

            _txtManual = CajaTexto();
            _txtManual.Text = "Primero ejecute un experimento: la búsqueda manual usa el último conjunto de datos generado." + Environment.NewLine +
                              "Las claves presentes son IMPARES; los números PARES nunca están en el conjunto.";
            t.Controls.Add(barra, 0, 0);
            t.Controls.Add(_txtManual, 0, 1);
            p.Controls.Add(t);
            return p;
        }

        private TabPage CrearPestanaPruebas()
        {
            TabPage p = new TabPage("Pruebas");
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            FlowLayoutPanel barra = new FlowLayoutPanel();
            barra.Dock = DockStyle.Fill;
            barra.AutoSize = true;
            barra.Padding = new Padding(4);
            Boton(barra, "Ejecutar pruebas automáticas", delegate
            {
                _txtPruebas.Text = PruebasBusqueda.Ejecutar().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                _txtPruebas.SelectionStart = 0;
            });
            _txtPruebas = CajaTexto();
            t.Controls.Add(barra, 0, 0);
            t.Controls.Add(_txtPruebas, 0, 1);
            p.Controls.Add(t);
            return p;
        }

        // ---- fábricas de controles ----
        private static Label Etiqueta(string texto)
        {
            Label l = new Label();
            l.Text = texto;
            l.AutoSize = true;
            l.Margin = new Padding(8, 9, 3, 3);
            return l;
        }

        private static NumericUpDown Numero(Control padre, string etiqueta, decimal min, decimal max, decimal valor,
                                            int decimales, decimal paso)
        {
            padre.Controls.Add(Etiqueta(etiqueta));
            NumericUpDown n = new NumericUpDown();
            n.Minimum = min;
            n.Maximum = max;
            n.DecimalPlaces = decimales;
            n.Increment = paso;
            n.Value = valor;
            n.Width = 95;
            n.ThousandsSeparator = decimales == 0;
            padre.Controls.Add(n);
            return n;
        }

        private static Button Boton(Control padre, string texto, EventHandler accion)
        {
            Button b = new Button();
            b.Text = texto;
            b.AutoSize = true;
            b.Padding = new Padding(6, 2, 6, 2);
            b.Click += accion;
            padre.Controls.Add(b);
            return b;
        }

        private static TextBox CajaTexto()
        {
            TextBox t = new TextBox();
            t.Multiline = true;
            t.ReadOnly = true;
            t.ScrollBars = ScrollBars.Both;
            t.WordWrap = false;
            t.Font = new Font("Consolas", 10f);
            t.Dock = DockStyle.Fill;
            t.BackColor = Color.White;
            return t;
        }

        // ==================================================================
        // Ejecución de experimentos
        // ==================================================================
        private async void OnEjecutar(object sender, EventArgs e)
        {
            await Correr(new int[] { (int)_numN.Value });
        }

        private async void OnSerie(object sender, EventArgs e)
        {
            await Correr(new int[] { 1000, 10000, 100000, 1000000 });
        }

        private ConfigExperimento LeerConfig(int n)
        {
            ConfigExperimento c = new ConfigExperimento();
            c.N = n;
            c.Busquedas = (int)_numK.Value;
            c.Semilla = (int)_numSemilla.Value;
            c.Funcion = _cmbFuncion.SelectedIndex == 0 ? TipoFuncionHash.Division : TipoFuncionHash.Multiplicativa;
            c.AlfaEncadenamiento = (double)_numAlfaEnc.Value;
            c.AlfaAbierto = (double)_numAlfaAbi.Value;
            return c;
        }

        private async Task Correr(int[] tamanos)
        {
            Bloquear(true);
            try
            {
                for (int i = 0; i < tamanos.Length; i++)
                {
                    ConfigExperimento cfg = LeerConfig(tamanos[i]);
                    _lblEstado.Text = "Ejecutando N = " + cfg.N.ToString("N0") + " ...";
                    ResultadoExperimento r = await Task.Run(() => Experimento.Ejecutar(cfg));
                    Acumular(r, cfg.N);
                }
                _lblEstado.Text = "Listo. Resultados acumulados: " + _filas.Count + " filas.";
            }
            catch (Exception ex)
            {
                _lblEstado.Text = "Error.";
                MessageBox.Show(ex.Message, "No se pudo ejecutar el experimento", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Bloquear(false);
            }
        }

        private void Bloquear(bool ocupado)
        {
            _btnEjecutar.Enabled = !ocupado;
            _btnSerie.Enabled = !ocupado;
            _btnCsv.Enabled = !ocupado;
            _btnLimpiar.Enabled = !ocupado;
            UseWaitCursor = ocupado;
        }

        /// <summary>Guarda los resultados de un N (reemplaza los anteriores del mismo N) y refresca la interfaz.</summary>
        private void Acumular(ResultadoExperimento r, int n)
        {
            _filas.RemoveAll(f => f.N == n);
            _construccion.RemoveAll(c => c.N == n);
            _filas.AddRange(r.Filas);
            _construccion.AddRange(r.Construccion);
            _contexto = r.Contexto;

            _filas.Sort((a, b) =>
            {
                int c = a.N.CompareTo(b.N);
                if (c != 0) return c;
                c = Array.IndexOf(Metodos, a.Metodo).CompareTo(Array.IndexOf(Metodos, b.Metodo));
                return c != 0 ? c : string.CompareOrdinal(a.Escenario, b.Escenario);
            });

            LlenarTabla();
            LlenarConstruccion();
            ActualizarGrafica();
        }

        private void LlenarTabla()
        {
            _grid.Rows.Clear();
            for (int i = 0; i < _filas.Count; i++)
            {
                FilaResultado f = _filas[i];
                _grid.Rows.Add(f.Metodo, f.N.ToString("N0"), f.Escenario, f.Busquedas, f.Encontradas,
                               f.ComparacionesTotal.ToString("N0"), f.ComparacionesMin, f.ComparacionesMax,
                               f.ComparacionesProm.ToString("N2"), f.IteracionesProm.ToString("N2"),
                               f.TiempoTotalMs.ToString("N3"), f.TiempoPromUs.ToString("N3"));
            }
        }

        private void LlenarConstruccion()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("COSTO DE PREPARAR LAS ESTRUCTURAS (último experimento de cada N)");
            sb.AppendLine(new string('=', 100));
            for (int i = 0; i < _construccion.Count; i++)
            {
                InfoConstruccion c = _construccion[i];
                sb.AppendLine(c.Estructura);
                sb.AppendLine("   N = " + c.N.ToString("N0") + "   capacidad = " + c.Capacidad.ToString("N0") +
                              "   factor de carga = " + c.FactorCarga.ToString("F3"));
                if (c.Estructura.StartsWith("Hash"))
                    sb.AppendLine("   colisiones en la inserción = " + c.Colisiones.ToString("N0") +
                                  "   longitud máxima (cadena / sondeos) = " + c.MaximaLongitud);
                sb.AppendLine("   tiempo de construcción = " + c.TiempoMs.ToString("N3") + " ms");
                sb.AppendLine();
            }
            sb.AppendLine("Nota: la búsqueda binaria solo es válida si el arreglo se ordena antes; ese costo O(n log n)");
            sb.AppendLine("se paga una vez y se amortiza si se realizan muchas búsquedas.");
            _txtConstruccion.Text = sb.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        // ==================================================================
        // Gráfica
        // ==================================================================
        private double Valor(FilaResultado f, int metrica)
        {
            int tipo = metrica / 2;
            if (tipo == 0) return f.ComparacionesProm;
            if (tipo == 1) return f.IteracionesProm;
            return f.TiempoPromUs;
        }

        private void ActualizarGrafica()
        {
            if (_grafica == null || _cmbMetrica == null) return;
            int metrica = _cmbMetrica.SelectedIndex;
            string escenario = (metrica % 2 == 0) ? "Presentes" : "Ausentes";

            List<SerieGrafica> series = new List<SerieGrafica>();
            for (int m = 0; m < Metodos.Length; m++)
            {
                List<double> xs = new List<double>();
                List<double> ys = new List<double>();
                for (int i = 0; i < _filas.Count; i++)   // _filas ya está ordenada por N
                {
                    if (_filas[i].Metodo == Metodos[m] && _filas[i].Escenario == escenario)
                    {
                        xs.Add(_filas[i].N);
                        ys.Add(Valor(_filas[i], metrica));
                    }
                }
                if (xs.Count > 0)
                {
                    SerieGrafica s = new SerieGrafica();
                    s.Nombre = Metodos[m];
                    s.Color = Colores[m];
                    s.X = xs.ToArray();
                    s.Y = ys.ToArray();
                    series.Add(s);
                }
            }
            _grafica.Titulo = Metricas[metrica];
            _grafica.EtiquetaX = "N";
            _grafica.EtiquetaY = metrica >= 4 ? "µs" : "operaciones";
            _grafica.Series = series.ToArray();
            _grafica.Invalidate();
        }

        // ==================================================================
        // Búsqueda manual
        // ==================================================================
        private void TomarClavePresente()
        {
            if (_contexto == null) { _txtManual.Text = "Primero ejecute un experimento."; return; }
            GeneradorAleatorio g = new GeneradorAleatorio(Environment.TickCount);
            _txtClave.Text = _contexto.Datos[g.SiguienteEntero(_contexto.Datos.Length)].ToString();
        }

        private void BuscarManual(int metodo)
        {
            if (_contexto == null) { _txtManual.Text = "Primero ejecute un experimento."; return; }
            int clave;
            if (!int.TryParse(_txtClave.Text.Trim(), out clave) || clave < 0)
            {
                _txtManual.Text = "Ingrese una clave entera mayor o igual que 0.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Clave buscada: " + clave + "   (N = " + _contexto.Datos.Length.ToString("N0") + ")");
            sb.AppendLine(new string('-', 80));
            for (int m = 0; m < 4; m++)
            {
                if (metodo != -1 && metodo != m) continue;
                long t0 = Stopwatch.GetTimestamp();
                ResultadoBusqueda r = Ejecutar(m, clave);
                long t1 = Stopwatch.GetTimestamp();
                sb.AppendLine(Metodos[m]);
                sb.AppendLine("   resultado     : " + (r.Encontrado ? "ENCONTRADA en la posición " + r.Posicion : "NO ENCONTRADA"));
                sb.AppendLine("   comparaciones : " + r.Comparaciones.ToString("N0"));
                sb.AppendLine("   iteraciones   : " + r.Iteraciones.ToString("N0"));
                sb.AppendLine("   tiempo        : " + (Experimento.Milisegundos(t0, t1) * 1000.0).ToString("N3") +
                              " µs (una sola búsqueda: poco fiable; use los experimentos para comparar tiempos)");
                sb.AppendLine();
            }
            _txtManual.Text = sb.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        private ResultadoBusqueda Ejecutar(int metodo, int clave)
        {
            switch (metodo)
            {
                case 0: return BusquedaSecuencial.Buscar(_contexto.Datos, clave);
                case 1: return BusquedaBinaria.Buscar(_contexto.Ordenados, clave);
                case 2: return _contexto.HashEncadenado.Buscar(clave);
                default: return _contexto.HashAbierto.Buscar(clave);
            }
        }

        // ==================================================================
        // Exportar / limpiar
        // ==================================================================
        private void OnExportarCsv(object sender, EventArgs e)
        {
            if (_filas.Count == 0) { MessageBox.Show("No hay resultados para exportar."); return; }
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV (*.csv)|*.csv";
                dlg.FileName = "resultados_busqueda.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                CultureInfo inv = CultureInfo.InvariantCulture;
                using (StreamWriter w = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
                {
                    w.WriteLine("Metodo,N,Escenario,Busquedas,Encontradas,CompTotal,CompMin,CompMax,CompPromedio,IterPromedio,TiempoTotalMs,TiempoPromUs");
                    for (int i = 0; i < _filas.Count; i++)
                    {
                        FilaResultado f = _filas[i];
                        w.WriteLine(string.Join(",", new string[] {
                            "\"" + f.Metodo + "\"", f.N.ToString(inv), f.Escenario, f.Busquedas.ToString(inv), f.Encontradas.ToString(inv),
                            f.ComparacionesTotal.ToString(inv), f.ComparacionesMin.ToString(inv), f.ComparacionesMax.ToString(inv),
                            f.ComparacionesProm.ToString("F4", inv), f.IteracionesProm.ToString("F4", inv),
                            f.TiempoTotalMs.ToString("F4", inv), f.TiempoPromUs.ToString("F4", inv) }));
                    }
                }
                _lblEstado.Text = "Exportado: " + dlg.FileName;
            }
        }

        private void OnLimpiar(object sender, EventArgs e)
        {
            _filas.Clear();
            _construccion.Clear();
            _contexto = null;
            _grid.Rows.Clear();
            _txtConstruccion.Text = "";
            ActualizarGrafica();
            _lblEstado.Text = "Resultados borrados.";
        }
    }
}
