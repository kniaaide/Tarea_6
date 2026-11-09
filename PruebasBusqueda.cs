using System;
using System.Text;

namespace Unidad6.Busquedas
{
    /// <summary>
    /// Pruebas automáticas con casos pequeños cuyo resultado se calcula a mano (ver reporte 6.2).
    /// Cada verificación compara lo esperado contra lo obtenido e imprime PASA / FALLA.
    /// </summary>
    public static class PruebasBusqueda
    {
        private static int _pasan;
        private static int _fallan;

        private static void Verificar(StringBuilder sb, string nombre, string esperado, string obtenido)
        {
            bool ok = (esperado == obtenido);
            if (ok) _pasan++; else _fallan++;
            sb.AppendLine("  [" + (ok ? "PASA " : "FALLA") + "] " + nombre);
            sb.AppendLine("          esperado: " + esperado);
            if (!ok) sb.AppendLine("          obtenido: " + obtenido);
        }

        /// <summary>Resumen "encontrado/posicion/comparaciones/iteraciones".</summary>
        private static string R(ResultadoBusqueda r)
        {
            return (r.Encontrado ? "si" : "no") + "/" + r.Posicion + "/" + r.Comparaciones + "/" + r.Iteraciones;
        }

        public static string Ejecutar()
        {
            _pasan = 0;
            _fallan = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== REPORTE DE PRUEBAS - METODOS DE BUSQUEDA ===");
            sb.AppendLine("Formato: encontrado / posicion / comparaciones / iteraciones");
            sb.AppendLine();

            // ---------- Secuencial ----------
            sb.AppendLine("PRUEBA 1: busqueda secuencial sobre {5, 3, 9, 1, 7}");
            int[] desord = { 5, 3, 9, 1, 7 };
            Verificar(sb, "Mejor caso: buscar 5 (primer elemento)", "si/0/1/1", R(BusquedaSecuencial.Buscar(desord, 5)));
            Verificar(sb, "Peor caso presente: buscar 7 (ultimo)", "si/4/5/5", R(BusquedaSecuencial.Buscar(desord, 7)));
            Verificar(sb, "Ausente: buscar 4", "no/-1/5/5", R(BusquedaSecuencial.Buscar(desord, 4)));
            Verificar(sb, "Arreglo vacio", "no/-1/0/0", R(BusquedaSecuencial.Buscar(new int[0], 1)));
            sb.AppendLine();

            // ---------- Ordenamiento ----------
            sb.AppendLine("PRUEBA 2: ordenamiento por mezcla");
            int[] ord = OrdenamientoMezcla.Ordenar(desord);
            Verificar(sb, "Orden de {5,3,9,1,7}", "1,3,5,7,9", string.Join(",", Array.ConvertAll(ord, x => x.ToString())));
            Verificar(sb, "El original no se modifica", "5,3,9,1,7", string.Join(",", Array.ConvertAll(desord, x => x.ToString())));
            sb.AppendLine();

            // ---------- Binaria ----------
            sb.AppendLine("PRUEBA 3: busqueda binaria sobre {1, 3, 5, 7, 9}");
            Verificar(sb, "Mejor caso: buscar 5 (punto medio)", "si/2/1/1", R(BusquedaBinaria.Buscar(ord, 5)));
            Verificar(sb, "Buscar 7", "si/3/2/2", R(BusquedaBinaria.Buscar(ord, 7)));
            Verificar(sb, "Buscar 1", "si/0/2/2", R(BusquedaBinaria.Buscar(ord, 1)));
            Verificar(sb, "Ausente: buscar 4", "no/-1/3/3", R(BusquedaBinaria.Buscar(ord, 4)));
            Verificar(sb, "Ausente mayor que todos: buscar 10", "no/-1/3/3", R(BusquedaBinaria.Buscar(ord, 10)));
            Verificar(sb, "Ausente menor que todos: buscar 0", "no/-1/2/2", R(BusquedaBinaria.Buscar(ord, 0)));
            sb.AppendLine();

            // ---------- Funciones y utilidades ----------
            sb.AppendLine("PRUEBA 4: funciones hash y primos");
            Verificar(sb, "SiguientePrimo(14)", "17", FuncionesHash.SiguientePrimo(14).ToString());
            Verificar(sb, "SiguientePrimo(100)", "101", FuncionesHash.SiguientePrimo(100).ToString());
            Verificar(sb, "Division: 24 mod 7", "3", FuncionesHash.Calcular(24, 7, TipoFuncionHash.Division).ToString());
            bool enRango = true;
            for (int k = 0; k < 1000; k++)
            {
                int h = FuncionesHash.Calcular(k, 13, TipoFuncionHash.Multiplicativa);
                if (h < 0 || h >= 13) enRango = false;
            }
            Verificar(sb, "Multiplicativa siempre en [0, m)", "True", enRango.ToString());
            sb.AppendLine();

            // ---------- Hash encadenado ----------
            sb.AppendLine("PRUEBA 5: hash con encadenamiento (m = 7, division); 10, 17 y 24 colisionan en la casilla 3");
            TablaHashEncadenada enc = new TablaHashEncadenada(7, TipoFuncionHash.Division);
            enc.Insertar(10); enc.Insertar(17); enc.Insertar(24);
            Verificar(sb, "Colisiones registradas", "2", enc.Colisiones.ToString());
            Verificar(sb, "Longitud maxima de cadena", "3", enc.MaximaLongitud.ToString());
            Verificar(sb, "Buscar 24 (cabeza de la lista)", "si/3/1/1", R(enc.Buscar(24)));
            Verificar(sb, "Buscar 10 (cola de la lista)", "si/3/3/3", R(enc.Buscar(10)));
            Verificar(sb, "Ausente 31 (misma casilla)", "no/-1/3/3", R(enc.Buscar(31)));
            Verificar(sb, "Ausente 4 (casilla vacia)", "no/-1/0/0", R(enc.Buscar(4)));
            Verificar(sb, "Duplicado 17 es rechazado", "False", enc.Insertar(17).ToString());
            sb.AppendLine();

            // ---------- Hash abierto ----------
            sb.AppendLine("PRUEBA 6: hash con direccionamiento abierto y sondeo lineal (m = 7)");
            TablaHashAbierta abi = new TablaHashAbierta(7, TipoFuncionHash.Division);
            abi.Insertar(10); abi.Insertar(17); abi.Insertar(24);   // ocupan las casillas 3, 4 y 5
            Verificar(sb, "Colisiones registradas", "2", abi.Colisiones.ToString());
            Verificar(sb, "Buscar 10 (casilla 3)", "si/3/1/1", R(abi.Buscar(10)));
            Verificar(sb, "Buscar 24 (casilla 5, tras 2 sondeos extra)", "si/5/3/3", R(abi.Buscar(24)));
            Verificar(sb, "Ausente 31: recorre 3,4,5 y para en la vacia 6", "no/-1/3/4", R(abi.Buscar(31)));
            Verificar(sb, "Ausente 5: casilla 5 ocupada, 6 vacia", "no/-1/1/2", R(abi.Buscar(5)));
            Verificar(sb, "Ausente 2: casilla 2 vacia", "no/-1/0/1", R(abi.Buscar(2)));

            TablaHashAbierta llena = new TablaHashAbierta(3, TipoFuncionHash.Division);
            llena.Insertar(0); llena.Insertar(1); llena.Insertar(2);
            Verificar(sb, "Tabla llena rechaza nueva clave", "False", llena.Insertar(5).ToString());
            Verificar(sb, "Tabla llena: busqueda de ausente termina", "no/-1/3/3", R(llena.Buscar(8)));
            sb.AppendLine();

            // ---------- Integracion ----------
            sb.AppendLine("PRUEBA 7: integracion (N = 2000, semilla 12345)");
            ConfigExperimento cfg = new ConfigExperimento();
            cfg.N = 2000; cfg.Busquedas = 200; cfg.Semilla = 12345;
            ResultadoExperimento res = Experimento.Ejecutar(cfg);
            bool coherente = true;
            for (int i = 0; i < res.Filas.Count; i++)
            {
                FilaResultado f = res.Filas[i];
                if (f.Escenario == "Presentes" && f.Encontradas != f.Busquedas) coherente = false;
                if (f.Escenario == "Ausentes" && f.Encontradas != 0) coherente = false;
            }
            Verificar(sb, "Presentes se encuentran todas; ausentes ninguna (4 metodos)", "True", coherente.ToString());
            Verificar(sb, "Arreglo ordenado correctamente", "True", OrdenamientoMezcla.EstaOrdenado(res.Contexto.Ordenados).ToString());
            Verificar(sb, "Secuencial ausentes: comparaciones = N", "2000", ((long)res.Filas[1].ComparacionesProm).ToString());

            GeneradorAleatorio g1 = new GeneradorAleatorio(7), g2 = new GeneradorAleatorio(7);
            Verificar(sb, "Mismo seed produce la misma secuencia", "True", (g1.Siguiente() == g2.Siguiente() && g1.Siguiente() == g2.Siguiente()).ToString());
            sb.AppendLine();

            sb.AppendLine("RESUMEN: " + _pasan + " verificaciones PASAN, " + _fallan + " FALLAN.");
            return sb.ToString();
        }
    }
}
