using System;

namespace Unidad6.Busquedas
{
    /// <summary>Función de dispersión que se aplicará a la clave.</summary>
    public enum TipoFuncionHash
    {
        /// <summary>h(k) = k mod m  (método de la división; m debe ser primo).</summary>
        Division,
        /// <summary>h(k) = floor(m * frac(k * A)),  A = (raíz de 5 - 1) / 2  (método multiplicativo de Knuth).</summary>
        Multiplicativa
    }

    /// <summary>Funciones de dispersión y utilidades numéricas.</summary>
    public static class FuncionesHash
    {
        private const double ConstanteA = 0.6180339887498949;

        /// <summary>Convierte una clave entera NO negativa en un índice dentro de [0, m).</summary>
        public static int Calcular(int clave, int m, TipoFuncionHash tipo)
        {
            if (tipo == TipoFuncionHash.Division)
                return clave % m;

            double x = clave * ConstanteA;
            x -= Math.Floor(x);                 // parte fraccionaria de k*A
            int h = (int)(m * x);
            return h >= m ? m - 1 : h;
        }

        /// <summary>Menor número primo mayor o igual que n (tamaños primos reparten mejor con el método de la división).</summary>
        public static int SiguientePrimo(int n)
        {
            if (n <= 2) return 2;
            if (n % 2 == 0) n++;
            while (!EsPrimo(n)) n += 2;
            return n;
        }

        private static bool EsPrimo(int n)
        {
            if (n < 2) return false;
            if (n % 2 == 0) return n == 2;
            for (long d = 3; d * d <= n; d += 2)
                if (n % d == 0) return false;
            return true;
        }
    }

    /// <summary>Contrato común de las dos tablas hash, para medirlas con el mismo código.</summary>
    public interface ITablaHash
    {
        string Nombre { get; }
        int Capacidad { get; }
        int Cantidad { get; }
        double FactorCarga { get; }
        /// <summary>Inserciones cuya casilla inicial ya estaba ocupada (colisiones).</summary>
        long Colisiones { get; }
        /// <summary>Cadena más larga (encadenamiento) o mayor secuencia de sondeos de una inserción (abierto).</summary>
        int MaximaLongitud { get; }
        /// <summary>true si se insertó; false si la clave ya existía o la tabla está llena.</summary>
        bool Insertar(int clave);
        ResultadoBusqueda Buscar(int clave);
    }

    /// <summary>
    /// Tabla hash con RESOLUCIÓN POR ENCADENAMIENTO SEPARADO: cada casilla guarda una lista
    /// enlazada con las claves que dispersan a ella. Inserta al inicio de la lista.
    /// Con factor de carga alfa = n/m: búsqueda exitosa ~ 1 + alfa/2 comparaciones,
    /// infructuosa ~ alfa. Peor caso O(n) si todas las claves colisionan. Espacio O(m + n).
    /// </summary>
    public class TablaHashEncadenada : ITablaHash
    {
        private class Nodo
        {
            public int Clave;
            public Nodo Siguiente;
            public Nodo(int clave, Nodo siguiente) { Clave = clave; Siguiente = siguiente; }
        }

        private readonly Nodo[] _cabezas;
        private readonly TipoFuncionHash _funcion;

        public string Nombre { get { return "Hash (encadenamiento)"; } }
        public int Capacidad { get { return _cabezas.Length; } }
        public int Cantidad { get; private set; }
        public long Colisiones { get; private set; }
        public int MaximaLongitud { get; private set; }

        public double FactorCarga { get { return (double)Cantidad / _cabezas.Length; } }

        public TablaHashEncadenada(int capacidad, TipoFuncionHash funcion)
        {
            if (capacidad < 1) throw new ArgumentOutOfRangeException("capacidad");
            _cabezas = new Nodo[capacidad];
            _funcion = funcion;
        }

        public bool Insertar(int clave)
        {
            if (clave < 0) throw new ArgumentException("Las claves deben ser no negativas.");
            int h = FuncionesHash.Calcular(clave, _cabezas.Length, _funcion);

            int longitud = 0;
            for (Nodo n = _cabezas[h]; n != null; n = n.Siguiente)
            {
                if (n.Clave == clave) return false;     // duplicado: se rechaza
                longitud++;
            }

            if (_cabezas[h] != null) Colisiones++;      // la casilla ya tenía elementos
            _cabezas[h] = new Nodo(clave, _cabezas[h]); // inserción al inicio de la lista
            Cantidad++;
            if (longitud + 1 > MaximaLongitud) MaximaLongitud = longitud + 1;
            return true;
        }

        public ResultadoBusqueda Buscar(int clave)
        {
            int h = FuncionesHash.Calcular(clave, _cabezas.Length, _funcion);
            long comparaciones = 0;
            long iteraciones = 0;

            for (Nodo n = _cabezas[h]; n != null; n = n.Siguiente)
            {
                iteraciones++;
                comparaciones++;
                if (n.Clave == clave)
                    return new ResultadoBusqueda(true, h, comparaciones, iteraciones);
            }
            return new ResultadoBusqueda(false, -1, comparaciones, iteraciones);
        }
    }

    /// <summary>
    /// Tabla hash con DIRECCIONAMIENTO ABIERTO y SONDEO LINEAL: todas las claves viven en el
    /// propio arreglo; si la casilla h está ocupada se prueba h+1, h+2, ... (circular).
    /// Requiere factor de carga alfa &lt; 1. Con alfa = n/m: sondeos esperados
    /// (Knuth, 1998): exitosa ~ (1 + 1/(1-alfa))/2, infructuosa ~ (1 + 1/(1-alfa)^2)/2.
    /// Sufre agrupamiento primario cuando alfa crece. Esta versión no implementa borrado.
    /// </summary>
    public class TablaHashAbierta : ITablaHash
    {
        private const int Vacio = -1;                   // las claves válidas son >= 0
        private readonly int[] _tabla;
        private readonly TipoFuncionHash _funcion;

        public string Nombre { get { return "Hash (dir. abierto, sondeo lineal)"; } }
        public int Capacidad { get { return _tabla.Length; } }
        public int Cantidad { get; private set; }
        public long Colisiones { get; private set; }
        public int MaximaLongitud { get; private set; }

        public double FactorCarga { get { return (double)Cantidad / _tabla.Length; } }

        public TablaHashAbierta(int capacidad, TipoFuncionHash funcion)
        {
            if (capacidad < 1) throw new ArgumentOutOfRangeException("capacidad");
            _tabla = new int[capacidad];
            for (int i = 0; i < capacidad; i++) _tabla[i] = Vacio;
            _funcion = funcion;
        }

        public bool Insertar(int clave)
        {
            if (clave < 0) throw new ArgumentException("Las claves deben ser no negativas.");
            if (Cantidad >= _tabla.Length) return false; // tabla llena

            int m = _tabla.Length;
            int idx = FuncionesHash.Calcular(clave, m, _funcion);
            int sondeos = 0;

            while (_tabla[idx] != Vacio)
            {
                if (_tabla[idx] == clave) return false;   // duplicado
                sondeos++;
                idx++;
                if (idx == m) idx = 0;                    // recorrido circular
            }

            if (sondeos > 0) Colisiones++;                // la casilla inicial estaba ocupada
            _tabla[idx] = clave;
            Cantidad++;
            if (sondeos + 1 > MaximaLongitud) MaximaLongitud = sondeos + 1;
            return true;
        }

        public ResultadoBusqueda Buscar(int clave)
        {
            int m = _tabla.Length;
            int idx = FuncionesHash.Calcular(clave, m, _funcion);
            long comparaciones = 0;
            long iteraciones = 0;

            for (int i = 0; i < m; i++)
            {
                iteraciones++;                            // se examina una casilla (sondeo)
                int valor = _tabla[idx];
                if (valor == Vacio)                       // casilla vacía: la clave no está
                    return new ResultadoBusqueda(false, -1, comparaciones, iteraciones);

                comparaciones++;                          // casilla ocupada: se compara la clave
                if (valor == clave)
                    return new ResultadoBusqueda(true, idx, comparaciones, iteraciones);

                idx++;
                if (idx == m) idx = 0;
            }
            return new ResultadoBusqueda(false, -1, comparaciones, iteraciones); // tabla llena y sin la clave
        }
    }
}
