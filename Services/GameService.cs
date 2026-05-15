using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameStoreApp.Models;

namespace GameStoreApp.Services;

public static class ServicioJuegos
{
    private static List<Juego> todosLosJuegos = new();

    public static async Task CargarJuegosAsync()
    {
        todosLosJuegos = await ServicioDatos.CargarJuegosAsync();
        LimpiarResenias();
    }

    private static void LimpiarResenias()
    {
        foreach (var juego in todosLosJuegos)
            juego.Resenias.Clear();
    }

    public static void ActualizarResenias(List<Usuario> usuarios)
    {
        foreach (var juego in todosLosJuegos)
            juego.Resenias.Clear();

        foreach (var usuario in usuarios)
            foreach (var resenia in usuario.Resenias)
            {
                var juego = todosLosJuegos.Find(j => j.Id == resenia.IdJuego);
                juego?.Resenias.Add(resenia);
            }
    }

    public static List<Juego> ObtenerTodos() => todosLosJuegos;

    public static Juego? ObtenerPorId(int id) =>
        todosLosJuegos.Find(j => j.Id == id);

    public static List<string> ObtenerCategorias() => new()
    {
        "Todas",
        "RPG",
        "Shooter",
        "Aventura",
        "Estrategia",
        "Simulación",
        "Carreras",
        "Plataformas",
        "Terror",
        "Puzzle",
        "MMORPG",
        "Deportes",
        "Sandbox",
        "Rol",
        "Battle Royale",
        "Otro"
    };

    public static List<int> ObtenerAnios() =>
        todosLosJuegos.Select(j => j.Anio).Distinct().OrderByDescending(a => a).ToList();

    public static List<Juego> Filtrar(string textoBusqueda, string categoria, int? anio, string orden)
    {
        var resultado = todosLosJuegos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(textoBusqueda))
            resultado = resultado.Where(j => j.Nombre.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(categoria) && categoria != "Todas")
            resultado = resultado.Where(j => j.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));

        if (anio.HasValue)
            resultado = resultado.Where(j => j.Anio == anio.Value);

        resultado = orden switch
        {
            "A-Z"      => resultado.OrderBy(j => j.Nombre),
            "Z-A"      => resultado.OrderByDescending(j => j.Nombre),
            "Precio ↑" => resultado.OrderBy(j => j.Precio),
            "Precio ↓" => resultado.OrderByDescending(j => j.Precio),
            "Año ↑"    => resultado.OrderBy(j => j.Anio),
            "Año ↓"    => resultado.OrderByDescending(j => j.Anio),
            _          => resultado.OrderBy(j => j.Nombre)
        };

        return resultado.ToList();
    }

    public static List<Juego> ObtenerPorIds(List<int> ids) =>
        ids.Select(id => todosLosJuegos.Find(j => j.Id == id))
           .Where(j => j is not null)
           .Select(j => j!)
           .ToList();

    public static async Task<bool> AgregarJuegoAsync(Juego juego)
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return false;
        try
        {
            juego.Id = todosLosJuegos.Count > 0 ? todosLosJuegos.Max(j => j.Id) + 1 : 1;
            todosLosJuegos.Add(juego);
            await ServicioDatos.GuardarJuegosAsync(todosLosJuegos);
            return true;
        }
        catch { return false; }
    }

    public static async Task<bool> ActualizarJuegoAsync(Juego juegoActualizado)
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return false;
        try
        {
            var existente = todosLosJuegos.Find(j => j.Id == juegoActualizado.Id);
            if (existente is null) return false;

            existente.Nombre      = juegoActualizado.Nombre;
            existente.Descripcion = juegoActualizado.Descripcion;
            existente.Precio      = juegoActualizado.Precio;
            existente.Anio        = juegoActualizado.Anio;
            existente.Categoria   = juegoActualizado.Categoria;
            existente.UrlImagen   = juegoActualizado.UrlImagen;

            await ServicioDatos.GuardarJuegosAsync(todosLosJuegos);
            return true;
        }
        catch { return false; }
    }

    public static async Task<bool> EliminarJuegoAsync(int idJuego)
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return false;
        try
        {
            var juego = todosLosJuegos.Find(j => j.Id == idJuego);
            if (juego is null) return false;
            todosLosJuegos.Remove(juego);
            await ServicioDatos.GuardarJuegosAsync(todosLosJuegos);
            return true;
        }
        catch { return false; }
    }
}
