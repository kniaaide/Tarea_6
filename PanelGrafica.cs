using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Unidad6.Busquedas
{
    /// <summary>Una serie de la gráfica: puntos (X, Y) de un método.</summary>
    public class SerieGrafica
    {
        public string Nombre;
        public Color Color;
        public double[] X;
        public double[] Y;
    }

    /// <summary>
    /// Gráfica de líneas con ambos ejes en escala logarítmica (base 10), dibujada con GDI+
    /// (no depende de bibliotecas externas). La escala log-log permite ver juntos métodos
    /// de órdenes de crecimiento muy distintos: O(n) aparece como recta de pendiente 1,
    /// O(log n) casi plana y O(1) como horizontal.
    /// </summary>
    public class PanelGrafica : Panel
    {
        public string Titulo { get; set; }
        public string EtiquetaX { get; set; }
        public string EtiquetaY { get; set; }
        public SerieGrafica[] Series { get; set; }

        public PanelGrafica()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.White;
            Titulo = "";
            EtiquetaX = "N";
            EtiquetaY = "";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Font fTitulo = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (Font fEje = new Font("Segoe UI", 8.5f))
            {
                g.DrawString(Titulo, fTitulo, Brushes.Black, 10, 6);

                if (Series == null || Series.Length == 0)
                {
                    g.DrawString("Ejecute un experimento para ver la gráfica.", fEje, Brushes.Gray, 20, 40);
                    return;
                }

                // ---- rangos de datos (solo valores positivos, por la escala log) ----
                double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
                for (int s = 0; s < Series.Length; s++)
                {
                    for (int i = 0; i < Series[s].X.Length; i++)
                    {
                        double x = Series[s].X[i], y = Series[s].Y[i];
                        if (x <= 0 || y <= 0) continue;
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
                }
                if (minX == double.MaxValue) { g.DrawString("Sin datos positivos para graficar.", fEje, Brushes.Gray, 20, 40); return; }

                double lx0 = Math.Floor(Math.Log10(minX)), lx1 = Math.Ceiling(Math.Log10(maxX));
                double ly0 = Math.Floor(Math.Log10(minY)), ly1 = Math.Ceiling(Math.Log10(maxY));
                if (lx1 <= lx0) lx1 = lx0 + 1;
                if (ly1 <= ly0) ly1 = ly0 + 1;

                Rectangle area = new Rectangle(70, 34, Math.Max(50, Width - 70 - 190), Math.Max(50, Height - 34 - 50));

                // ---- cuadrícula y marcas (potencias de 10) ----
                using (Pen rejilla = new Pen(Color.FromArgb(225, 225, 225)))
                using (Pen eje = new Pen(Color.Black, 1.5f))
                {
                    for (double lx = lx0; lx <= lx1; lx++)
                    {
                        float px = (float)(area.Left + (lx - lx0) / (lx1 - lx0) * area.Width);
                        g.DrawLine(rejilla, px, area.Top, px, area.Bottom);
                        g.DrawString(Formato(Math.Pow(10, lx)), fEje, Brushes.Black, px - 14, area.Bottom + 4);
                    }
                    for (double ly = ly0; ly <= ly1; ly++)
                    {
                        float py = (float)(area.Bottom - (ly - ly0) / (ly1 - ly0) * area.Height);
                        g.DrawLine(rejilla, area.Left, py, area.Right, py);
                        g.DrawString(Formato(Math.Pow(10, ly)), fEje, Brushes.Black, 6, py - 8);
                    }
                    g.DrawRectangle(eje, area);
                }
                g.DrawString(EtiquetaX + " (escala logarítmica)", fEje, Brushes.Black, area.Left + area.Width / 2 - 70, area.Bottom + 24);
                g.DrawString(EtiquetaY + " (escala log.)", fEje, Brushes.Black, 6, 20);

                // ---- series ----
                int yLeyenda = area.Top + 6;
                for (int s = 0; s < Series.Length; s++)
                {
                    SerieGrafica serie = Series[s];
                    PointF[] pts = new PointF[serie.X.Length];
                    int cuenta = 0;
                    for (int i = 0; i < serie.X.Length; i++)
                    {
                        if (serie.X[i] <= 0 || serie.Y[i] <= 0) continue;
                        float px = (float)(area.Left + (Math.Log10(serie.X[i]) - lx0) / (lx1 - lx0) * area.Width);
                        float py = (float)(area.Bottom - (Math.Log10(serie.Y[i]) - ly0) / (ly1 - ly0) * area.Height);
                        pts[cuenta++] = new PointF(px, py);
                    }
                    using (Pen lapiz = new Pen(serie.Color, 2.2f))
                    using (SolidBrush pincel = new SolidBrush(serie.Color))
                    {
                        for (int i = 0; i + 1 < cuenta; i++) g.DrawLine(lapiz, pts[i], pts[i + 1]);
                        for (int i = 0; i < cuenta; i++) g.FillEllipse(pincel, pts[i].X - 3.5f, pts[i].Y - 3.5f, 7, 7);

                        g.FillRectangle(pincel, area.Right + 12, yLeyenda + 3, 14, 4);
                        g.DrawString(serie.Nombre, fEje, Brushes.Black, area.Right + 30, yLeyenda - 3);
                        yLeyenda += 20;
                    }
                }
            }
        }

        private static string Formato(double v)
        {
            if (v >= 1000000) return (v / 1000000).ToString("0.#") + "M";
            if (v >= 1000) return (v / 1000).ToString("0.#") + "k";
            if (v >= 1) return v.ToString("0.#");
            return v.ToString("0.###");
        }
    }
}
