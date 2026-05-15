using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GameStoreApp.Models;

namespace GameStoreApp.Services;

public record ResultadoReporte(
    Juego Juego,
    int Cantidad,
    double CalificacionPromedio
);

public static class ServicioReportes
{
    private static List<Usuario> ObtenerUsuarios() =>
        ServicioAutenticacion.ObtenerTodosLosUsuarios();

    private static List<Juego> ObtenerJuegos() =>
        ServicioJuegos.ObtenerTodos();

    private static DateTime? ParsearFecha(string fecha)
    {
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
            return dt2;
        return null;
    }

    private static bool FechaEnRango(string fecha, DateTime? desde, DateTime? hasta)
    {
        if (desde is null && hasta is null) return true;
        var dt = ParsearFecha(fecha);
        if (dt is null) return false;
        if (desde is not null && dt < desde) return false;
        if (hasta is not null && dt > hasta) return false;
        return true;
    }

    private static bool FiltrarJuego(Juego juego, string categoria, string operadorPrecio, decimal? precioExacto)
    {
        if (!string.IsNullOrEmpty(categoria) && categoria != "Todas" &&
            !juego.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            return false;

        if (operadorPrecio == "igual" && precioExacto.HasValue && juego.Precio != precioExacto) return false;
        if (operadorPrecio == "mayor" && precioExacto.HasValue && juego.Precio <= precioExacto) return false;
        if (operadorPrecio == "menor" && precioExacto.HasValue && juego.Precio >= precioExacto) return false;

        return true;
    }

    public static List<ResultadoReporte> JuegosMasComprados(
        DateTime? desde, DateTime? hasta,
        string categoria, string operadorPrecio, decimal? precio)
    {
        var usuarios = ObtenerUsuarios();
        var juegos   = ObtenerJuegos();

        var conteo = juegos.ToDictionary(j => j.Id, _ => 0);

        foreach (var usuario in usuarios)
        {
            foreach (var registro in usuario.RegistrosCompras)
            {
                if (!FechaEnRango(registro.Fecha, desde, hasta)) continue;
                if (conteo.ContainsKey(registro.IdJuego))
                    conteo[registro.IdJuego]++;
            }

            foreach (var idJuego in usuario.JuegosComprados)
            {
                bool yaContado = usuario.RegistrosCompras.Any(r => r.IdJuego == idJuego);
                if (!yaContado && desde is null && hasta is null)
                    if (conteo.ContainsKey(idJuego)) conteo[idJuego]++;
            }
        }

        return juegos
            .Where(j => FiltrarJuego(j, categoria, operadorPrecio, precio) && conteo[j.Id] > 0)
            .OrderByDescending(j => conteo[j.Id])
            .Select(j => new ResultadoReporte(j, conteo[j.Id], j.CalificacionPromedio))
            .ToList();
    }

    public static List<ResultadoReporte> JuegosMasDeseados(
        DateTime? desde, DateTime? hasta,
        string categoria, string operadorPrecio, decimal? precio)
    {
        var usuarios = ObtenerUsuarios();
        var juegos   = ObtenerJuegos();

        var conteo = juegos.ToDictionary(j => j.Id, _ => 0);

        foreach (var usuario in usuarios)
        {
            foreach (var registro in usuario.RegistrosDeseos)
            {
                if (!FechaEnRango(registro.Fecha, desde, hasta)) continue;
                if (conteo.ContainsKey(registro.IdJuego))
                    conteo[registro.IdJuego]++;
            }

            foreach (var idJuego in usuario.ListaDeseos)
            {
                bool yaContado = usuario.RegistrosDeseos.Any(r => r.IdJuego == idJuego);
                if (!yaContado && desde is null && hasta is null)
                    if (conteo.ContainsKey(idJuego)) conteo[idJuego]++;
            }
        }

        return juegos
            .Where(j => FiltrarJuego(j, categoria, operadorPrecio, precio) && conteo[j.Id] > 0)
            .OrderByDescending(j => conteo[j.Id])
            .Select(j => new ResultadoReporte(j, conteo[j.Id], j.CalificacionPromedio))
            .ToList();
    }

    public static List<ResultadoReporte> JuegosMasResenados(
        DateTime? desde, DateTime? hasta, string categoria)
    {
        var usuarios = ObtenerUsuarios();
        var juegos   = ObtenerJuegos();

        var conteo = juegos.ToDictionary(j => j.Id, _ => 0);

        foreach (var usuario in usuarios)
            foreach (var resenia in usuario.Resenias)
            {
                if (!FechaEnRango(resenia.Fecha, desde, hasta)) continue;
                if (conteo.ContainsKey(resenia.IdJuego))
                    conteo[resenia.IdJuego]++;
            }

        return juegos
            .Where(j => (string.IsNullOrEmpty(categoria) || categoria == "Todas"
                         || j.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
                        && conteo[j.Id] > 0)
            .OrderByDescending(j => conteo[j.Id])
            .Select(j => new ResultadoReporte(j, conteo[j.Id], j.CalificacionPromedio))
            .ToList();
    }

    public static List<ResultadoReporte> JuegosOrdenadosPorCalificacion(
        bool mejores, int estrellaMin, int estrellaMax,
        string categoria, DateTime? desde, DateTime? hasta)
    {
        var usuarios = ObtenerUsuarios();
        var juegos   = ObtenerJuegos();

        var calificaciones = juegos.ToDictionary(
            j => j.Id,
            j => new List<int>()
        );

        foreach (var usuario in usuarios)
            foreach (var resenia in usuario.Resenias)
            {
                if (!FechaEnRango(resenia.Fecha, desde, hasta)) continue;
                if (calificaciones.ContainsKey(resenia.IdJuego))
                    calificaciones[resenia.IdJuego].Add(resenia.Estrellas);
            }

        return juegos
            .Where(j =>
            {
                if (!string.IsNullOrEmpty(categoria) && categoria != "Todas" &&
                    !j.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
                    return false;

                var lista = calificaciones[j.Id];
                if (lista.Count == 0) return false;
                double promedio = lista.Average();
                return promedio >= estrellaMin && promedio <= estrellaMax;
            })
            .Select(j =>
            {
                var lista = calificaciones[j.Id];
                double promedio = lista.Count > 0 ? lista.Average() : 0;
                return new ResultadoReporte(j, lista.Count, promedio);
            })
            .OrderByDescending(r => mejores ? r.CalificacionPromedio : -r.CalificacionPromedio)
            .ThenByDescending(r => r.Cantidad)
            .ToList();
    }
}
