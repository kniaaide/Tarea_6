using System;

namespace Unidad6.Busquedas
{
    /// <summary>
    /// Resultado de UNA búsqueda, con las métricas exactas que la instrumentan.
    ///
    /// Convención de conteo (la misma en todos los métodos, para comparar de forma justa):
    ///   * Comparaciones: veces que se compara la clave buscada contra una clave almacenada.
    ///     Cada comparación cuenta 1, aunque produzca un resultado de tres vías (menor/igual/mayor).
    ///   * Iteraciones: pasos del ciclo principal del método
    ///     (secuencial: elementos examinados; binaria: particiones; hash: nodos o casillas examinadas).
    ///   * El cálculo de la función hash NO se cuenta como comparación (es O(1) y no compara claves).
    /// </summary>
    public struct ResultadoBusqueda
    {
        public bool Encontrado;
        /// <summary>Posición (índice del arreglo o de la casilla); -1 si no se encontró.</summary>
        public int Posicion;
        public long Comparaciones;
        public long Iteraciones;

        public ResultadoBusqueda(bool encontrado, int posicion, long comparaciones, long iteraciones)
        {
            Encontrado = encontrado;
            Posicion = posicion;
            Comparaciones = comparaciones;
            Iteraciones = iteraciones;
        }
    }

    /// <summary>
    /// Generador pseudoaleatorio xorshift64* implementado a mano. Se usa en lugar de
    /// System.Random para que, con la misma semilla, los datos sean idénticos en cualquier
    /// versión de .NET y en cualquier lenguaje (el reporte contrasta los conteos con una
    /// simulación independiente que usa exactamente este mismo algoritmo).
    /// </summary>
    public class GeneradorAleatorio
    {
        private ulong _estado;

        public GeneradorAleatorio(int semilla)
        {
            unchecked
            {
                _estado = (ulong)(long)semilla + 0x9E3779B97F4A7C15UL;
                if (_estado == 0UL) _estado = 88172645463325252UL;
            }
        }

        public ulong Siguiente()
        {
            unchecked
            {
                _estado ^= _estado >> 12;
                _estado ^= _estado << 25;
                _estado ^= _estado >> 27;
                return _estado * 2685821657736338717UL;
            }
        }

        /// <summary>Entero uniforme en [0, limite).</summary>
        public int SiguienteEntero(int limite)
        {
            if (limite <= 0) throw new ArgumentOutOfRangeException("limite");
            return (int)((Siguiente() >> 11) % (ulong)limite);
        }
    }

    /// <summary>
    /// Búsqueda secuencial (lineal). No exige orden previo.
    /// Mejor caso O(1) (clave en la posición 0); promedio y peor caso O(n); espacio extra O(1).
    /// </summary>
    public static class BusquedaSecuencial
    {
        public static ResultadoBusqueda Buscar(int[] datos, int clave)
        {
            long comparaciones = 0;
            for (int i = 0; i < datos.Length; i++)
            {
                comparaciones++;                          // se compara datos[i] contra la clave
                if (datos[i] == clave)
                    return new ResultadoBusqueda(true, i, comparaciones, i + 1);
            }
            return new ResultadoBusqueda(false, -1, comparaciones, datos.Length);
        }
    }

    /// <summary>
    /// Búsqueda binaria iterativa. REQUIERE que el arreglo esté ORDENADO de forma ascendente.
    /// Mejor caso O(1) (clave en el punto medio); promedio y peor caso O(log n); espacio extra O(1).
    /// </summary>
    public static class BusquedaBinaria
    {
        public static ResultadoBusqueda Buscar(int[] ordenado, int clave)
        {
            int izq = 0;
            int der = ordenado.Length - 1;
            long comparaciones = 0;
            long iteraciones = 0;

            while (izq <= der)
            {
                iteraciones++;
                int medio = izq + (der - izq) / 2;        // evita desbordamiento de (izq + der)
                comparaciones++;                          // una comparación de tres vías por iteración
                int c = ordenado[medio].CompareTo(clave);

                if (c == 0) return new ResultadoBusqueda(true, medio, comparaciones, iteraciones);
                if (c < 0) izq = medio + 1;               // la clave está a la derecha
                else der = medio - 1;                     // la clave está a la izquierda
            }
            return new ResultadoBusqueda(false, -1, comparaciones, iteraciones);
        }
    }

    /// <summary>
    /// Ordenamiento por mezcla (merge sort) escrito desde cero. Se emplea para preparar el
    /// arreglo de la búsqueda binaria. Tiempo O(n log n) en todos los casos; espacio O(n).
    /// </summary>
    public static class OrdenamientoMezcla
    {
        /// <summary>Devuelve una COPIA ordenada; el arreglo original no se modifica.</summary>
        public static int[] Ordenar(int[] origen)
        {
            int[] a = (int[])origen.Clone();
            int[] aux = new int[a.Length];
            Mezclar(a, aux, 0, a.Length - 1);
            return a;
        }

        private static void Mezclar(int[] a, int[] aux, int ini, int fin)
        {
            if (ini >= fin) return;
            int medio = ini + (fin - ini) / 2;
            Mezclar(a, aux, ini, medio);
            Mezclar(a, aux, medio + 1, fin);

            int i = ini, j = medio + 1, k = ini;
            while (i <= medio && j <= fin)
                aux[k++] = (a[i] <= a[j]) ? a[i++] : a[j++];
            while (i <= medio) aux[k++] = a[i++];
            while (j <= fin) aux[k++] = a[j++];
            for (k = ini; k <= fin; k++) a[k] = aux[k];
        }

        public static bool EstaOrdenado(int[] a)
        {
            for (int i = 1; i < a.Length; i++)
                if (a[i - 1] > a[i]) return false;
            return true;
        }
    }
}
