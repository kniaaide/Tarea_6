using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unidad6.Busquedas
{
    /// <summary>Parámetros de un experimento.</summary>
    public class ConfigExperimento
    {
        /// <summary>Cantidad N de datos almacenados.</summary>
        public int N = 100000;
        /// <summary>Número de búsquedas de claves PRESENTES y, aparte, de claves AUSENTES.</summary>
        public int Busquedas = 1000;
        public int Semilla = 12345;
        public TipoFuncionHash Funcion = TipoFuncionHash.Division;
        /// <summary>Factor de carga objetivo del encadenamiento (n/m). m = primo &gt;= n/alfa.</summary>
        public double AlfaEncadenamiento = 1.0;
        /// <summary>Factor de carga objetivo del direccionamiento abierto (debe ser &lt; 1).</summary>
        public double AlfaAbierto = 0.5;
    }

    /// <summary>Una fila de resultados: un método, un N y un escenario (claves presentes o ausentes).</summary>
    public class FilaResultado
    {
        public string Metodo;
        public int N;
        public string Escenario;          // "Presentes" | "Ausentes"
        public int Busquedas;
        public int Encontradas;
        public long ComparacionesTotal;
        public long ComparacionesMin;
        public long ComparacionesMax;
        public double ComparacionesProm;
        public double IteracionesProm;
        public double TiempoTotalMs;
        public double TiempoPromUs;       // microsegundos por búsqueda
    }

    /// <summary>Costo de preparar cada estructura (ordenar el arreglo, construir cada tabla hash).</summary>
    public class InfoConstruccion
    {
        public string Estructura;
        public int N;
        public int Capacidad;
        public double FactorCarga;
        public long Colisiones;
        public int MaximaLongitud;
        public double TiempoMs;
    }

    /// <summary>Conjunto de datos y estructuras de un experimento (permite hacer búsquedas manuales después).</summary>
    public class ContextoBusqueda
    {
        public int[] Datos;               // orden aleatorio (para la búsqueda secuencial)
        public int[] Ordenados;           // copia ordenada (para la búsqueda binaria)
        public ITablaHash HashEncadenado;
        public ITablaHash HashAbierto;
    }

    public class ResultadoExperimento
    {
        public List<FilaResultado> Filas = new List<FilaResultado>();
        public List<InfoConstruccion> Construccion = new List<InfoConstruccion>();
        public ContextoBusqueda Contexto;
    }

    public static class Experimento
    {
        /// <summary>
        /// Genera los datos, prepara las estructuras y ejecuta las búsquedas.
        ///
        /// Generación de datos: se eligen N enteros DISTINTOS e IMPARES al azar
        /// (valor = 2*q + 1, con q distinto en [0, 32N)). Las claves AUSENTES son enteros PARES,
        /// por lo que se garantiza que no están en el conjunto sin necesidad de verificarlo.
        /// </summary>
        public static ResultadoExperimento Ejecutar(ConfigExperimento cfg)
        {
            if (cfg.N < 1 || cfg.N > 2000000) throw new ArgumentException("N debe estar entre 1 y 2 000 000.");
            if (cfg.Busquedas < 1) throw new ArgumentException("El número de búsquedas debe ser al menos 1.");
            if (cfg.AlfaAbierto <= 0 || cfg.AlfaAbierto >= 1) throw new ArgumentException("El factor de carga del direccionamiento abierto debe estar en (0, 1).");
            if (cfg.AlfaEncadenamiento <= 0) throw new ArgumentException("El factor de carga del encadenamiento debe ser positivo.");

            GeneradorAleatorio rng = new GeneradorAleatorio(cfg.Semilla);
            ResultadoExperimento res = new ResultadoExperimento();
            int n = cfg.N;

            // ---- 1) Datos: N valores distintos tomados al azar de un universo de 32*N posibilidades ----
            // Se descartan repetidos con un mapa de bits (rechazo). Un universo MUCHO mayor que N
            // evita que las claves queden demasiado "bien repartidas" y sesguen a las tablas hash.
            int universo = 32 * n;
            ulong[] usado = new ulong[(universo >> 6) + 1];
            int[] datos = new int[n];
            for (int i = 0; i < n; i++)
            {
                while (true)
                {
                    int v = rng.SiguienteEntero(universo);
                    ulong bit = 1UL << (v & 63);
                    if ((usado[v >> 6] & bit) == 0UL)
                    {
                        usado[v >> 6] |= bit;
                        datos[i] = 2 * v + 1;            // impar
                        break;
                    }
                }
            }
            usado = null;

            // ---- 2) Claves a buscar ----
            int[] presentes = new int[cfg.Busquedas];
            for (int i = 0; i < presentes.Length; i++) presentes[i] = datos[rng.SiguienteEntero(n)];
            int[] ausentes = new int[cfg.Busquedas];
            for (int i = 0; i < ausentes.Length; i++) ausentes[i] = 2 * rng.SiguienteEntero(universo);   // par: nunca está

            // ---- 3) Preparación de estructuras (se cronometra cada una) ----
            long t0 = Stopwatch.GetTimestamp();
            int[] ordenados = OrdenamientoMezcla.Ordenar(datos);
            double msOrden = Milisegundos(t0, Stopwatch.GetTimestamp());
            res.Construccion.Add(new InfoConstruccion { Estructura = "Arreglo ordenado (merge sort)", N = n, Capacidad = n, FactorCarga = 1.0, TiempoMs = msOrden });

            int capEnc = FuncionesHash.SiguientePrimo((int)Math.Ceiling(n / cfg.AlfaEncadenamiento));
            int capAbi = FuncionesHash.SiguientePrimo((int)Math.Ceiling(n / cfg.AlfaAbierto));
            ITablaHash enc = new TablaHashEncadenada(capEnc, cfg.Funcion);
            ITablaHash abi = new TablaHashAbierta(capAbi, cfg.Funcion);
            res.Construccion.Add(Construir(enc, datos));
            res.Construccion.Add(Construir(abi, datos));

            res.Contexto = new ContextoBusqueda { Datos = datos, Ordenados = ordenados, HashEncadenado = enc, HashAbierto = abi };

            // ---- 4) Búsquedas ----
            Func<int, ResultadoBusqueda> fSec = delegate (int k) { return BusquedaSecuencial.Buscar(datos, k); };
            Func<int, ResultadoBusqueda> fBin = delegate (int k) { return BusquedaBinaria.Buscar(ordenados, k); };
            Func<int, ResultadoBusqueda> fEnc = delegate (int k) { return enc.Buscar(k); };
            Func<int, ResultadoBusqueda> fAbi = delegate (int k) { return abi.Buscar(k); };

            AgregarMedicion(res, "Secuencial", n, presentes, ausentes, fSec);
            AgregarMedicion(res, "Binaria", n, presentes, ausentes, fBin);
            AgregarMedicion(res, "Hash (encadenamiento)", n, presentes, ausentes, fEnc);
            AgregarMedicion(res, "Hash (dir. abierto)", n, presentes, ausentes, fAbi);
            return res;
        }

        private static InfoConstruccion Construir(ITablaHash tabla, int[] datos)
        {
            long t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < datos.Length; i++) tabla.Insertar(datos[i]);
            double ms = Milisegundos(t0, Stopwatch.GetTimestamp());
            return new InfoConstruccion
            {
                Estructura = tabla.Nombre,
                N = tabla.Cantidad,
                Capacidad = tabla.Capacidad,
                FactorCarga = tabla.FactorCarga,
                Colisiones = tabla.Colisiones,
                MaximaLongitud = tabla.MaximaLongitud,
                TiempoMs = ms
            };
        }

        private static void AgregarMedicion(ResultadoExperimento res, string metodo, int n,
                                            int[] presentes, int[] ausentes, Func<int, ResultadoBusqueda> buscar)
        {
            res.Filas.Add(Medir(metodo, n, "Presentes", presentes, buscar));
            res.Filas.Add(Medir(metodo, n, "Ausentes", ausentes, buscar));
        }

        /// <summary>
        /// Ejecuta todas las búsquedas de un escenario. Primero hay un calentamiento (el JIT compila
        /// el código) y luego se cronometra SOLO el ciclo de búsquedas con Stopwatch; las métricas se
        /// guardan en un arreglo y se agregan DESPUÉS de detener el cronómetro, para no contaminar el tiempo.
        /// Nota: el tiempo incluye la llamada mediante delegado (igual para los cuatro métodos).
        /// </summary>
        private static FilaResultado Medir(string metodo, int n, string escenario, int[] claves,
                                           Func<int, ResultadoBusqueda> buscar)
        {
            int calentamiento = Math.Min(claves.Length, 50);
            for (int i = 0; i < calentamiento; i++) buscar(claves[i]);

            ResultadoBusqueda[] r = new ResultadoBusqueda[claves.Length];
            long t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < claves.Length; i++) r[i] = buscar(claves[i]);
            long t1 = Stopwatch.GetTimestamp();

            FilaResultado f = new FilaResultado();
            f.Metodo = metodo;
            f.N = n;
            f.Escenario = escenario;
            f.Busquedas = claves.Length;
            f.ComparacionesMin = long.MaxValue;
            long iter = 0;
            for (int i = 0; i < r.Length; i++)
            {
                if (r[i].Encontrado) f.Encontradas++;
                f.ComparacionesTotal += r[i].Comparaciones;
                if (r[i].Comparaciones < f.ComparacionesMin) f.ComparacionesMin = r[i].Comparaciones;
                if (r[i].Comparaciones > f.ComparacionesMax) f.ComparacionesMax = r[i].Comparaciones;
                iter += r[i].Iteraciones;
            }
            f.ComparacionesProm = (double)f.ComparacionesTotal / r.Length;
            f.IteracionesProm = (double)iter / r.Length;
            f.TiempoTotalMs = Milisegundos(t0, t1);
            f.TiempoPromUs = f.TiempoTotalMs * 1000.0 / r.Length;
            return f;
        }

        public static double Milisegundos(long inicio, long fin)
        {
            return (fin - inicio) * 1000.0 / Stopwatch.Frequency;
        }
    }
}
