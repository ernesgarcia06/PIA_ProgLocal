using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameStoreApp.Models;

namespace GameStoreApp.Services;

public static class ServicioAutenticacion
{
    public static Usuario? UsuarioActual { get; private set; }
    private static List<Usuario> listaUsuarios = new();

    public static async Task CargarUsuariosAsync()
    {
        listaUsuarios = await ServicioDatos.CargarUsuariosAsync();
    }

    public static bool IniciarSesion(string nombreUsuario, string contrasenia)
    {
        string usuarioLimpio = nombreUsuario.Trim().ToLower();

        if (usuarioLimpio.Contains(' ') || contrasenia.Contains(' '))
            return false;

        var usuario = listaUsuarios.Find(u =>
            u.NombreUsuario.Equals(usuarioLimpio, StringComparison.Ordinal) &&
            u.Contrasenia == contrasenia);

        if (usuario is null) return false;
        if (!usuario.Activo) return false;

        UsuarioActual = usuario;
        return true;
    }

    public static async Task<string> RegistrarAsync(string nombreUsuario, string contrasenia)
    {
        string usuarioLimpio = nombreUsuario.Trim().ToLower();

        string errorUsuario = ValidarNombreUsuario(usuarioLimpio);
        if (!string.IsNullOrEmpty(errorUsuario)) return errorUsuario;

        string errorContrasenia = ValidarContrasenia(contrasenia);
        if (!string.IsNullOrEmpty(errorContrasenia)) return errorContrasenia;

        bool existeUsuario = listaUsuarios.Exists(u =>
            u.NombreUsuario.Equals(usuarioLimpio, StringComparison.Ordinal));
        if (existeUsuario) return "El nombre de usuario ya está en uso.";

        var nuevoUsuario = new Usuario { NombreUsuario = usuarioLimpio, Contrasenia = contrasenia, Activo = true };
        listaUsuarios.Add(nuevoUsuario);
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
        UsuarioActual = nuevoUsuario;
        return string.Empty;
    }

    public static void CerrarSesion() => UsuarioActual = null;

    public static async Task GuardarUsuarioActualAsync()
    {
        if (UsuarioActual is null) return;
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
    }

    public static async Task GuardarTodosLosUsuariosAsync()
    {
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
    }

    public static string ValidarNombreUsuario(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            return "El usuario no puede estar vacío.";
        if (nombreUsuario.Contains(' '))
            return "El usuario no puede contener espacios.";
        if (nombreUsuario.Length < 3)
            return "El usuario debe tener al menos 3 caracteres.";
        if (nombreUsuario.Length > 15)
            return "El usuario no puede tener más de 15 caracteres.";
        if (!Regex.IsMatch(nombreUsuario, @"^[a-z0-9]+$"))
            return "El usuario solo puede contener letras minúsculas y números.";
        return string.Empty;
    }

    public static string ValidarContrasenia(string contrasenia)
    {
        if (string.IsNullOrWhiteSpace(contrasenia))
            return "La contraseña no puede estar vacía.";
        if (contrasenia.Contains(' '))
            return "La contraseña no puede contener espacios.";
        if (contrasenia.Length < 6)
            return "La contraseña debe tener al menos 6 caracteres.";
        if (contrasenia.Length > 20)
            return "La contraseña no puede tener más de 20 caracteres.";
        if (!Regex.IsMatch(contrasenia, @"[A-Z]"))
            return "La contraseña debe tener al menos una letra mayúscula.";
        if (!Regex.IsMatch(contrasenia, @"[0-9]"))
            return "La contraseña debe tener al menos un número.";
        if (!Regex.IsMatch(contrasenia, @"[@#$%]"))
            return "La contraseña debe tener al menos un carácter especial (@, #, $, %).";
        if (!Regex.IsMatch(contrasenia, @"^[a-zA-Z0-9@#$%]+$"))
            return "La contraseña solo puede contener letras, números y los caracteres: @, #, $, %.";
        return string.Empty;
    }

    public static bool EstaEnListaDeseos(int idJuego) =>
        UsuarioActual?.ListaDeseos.Contains(idJuego) ?? false;

    public static bool HaComprado(int idJuego) =>
        UsuarioActual?.JuegosComprados.Contains(idJuego) ?? false;

    public static async Task AlternarListaDeseosAsync(int idJuego)
    {
        if (UsuarioActual is null) return;
        if (UsuarioActual.ListaDeseos.Contains(idJuego))
        {
            UsuarioActual.ListaDeseos.Remove(idJuego);
        }
        else
        {
            UsuarioActual.ListaDeseos.Add(idJuego);
            UsuarioActual.RegistrosDeseos.Add(new RegistroDeseo
            {
                IdJuego = idJuego,
                Fecha   = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }
        await GuardarUsuarioActualAsync();
    }

    public static async Task AgregarJuegoCompradoAsync(int idJuego, string ultimosDigitosTarjeta = "")
    {
        if (UsuarioActual is null) return;
        if (!UsuarioActual.JuegosComprados.Contains(idJuego))
        {
            UsuarioActual.JuegosComprados.Add(idJuego);
            UsuarioActual.RegistrosCompras.Add(new RegistroCompra
            {
                IdJuego          = idJuego,
                Fecha            = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                UltimosDigitos   = ultimosDigitosTarjeta
            });
            await GuardarUsuarioActualAsync();
        }
    }

    public static async Task AgregarOActualizarReseniaAsync(Resenia resenia)
    {
        if (UsuarioActual is null) return;
        UsuarioActual.Resenias.RemoveAll(r => r.IdJuego == resenia.IdJuego);
        UsuarioActual.Resenias.Add(resenia);
        await GuardarUsuarioActualAsync();
    }

    public static Resenia? ObtenerReseniaUsuarioParaJuego(int idJuego) =>
        UsuarioActual?.Resenias.Find(r => r.IdJuego == idJuego);

    public static List<Usuario> ObtenerTodosLosUsuarios() => new(listaUsuarios);

    public static async Task<(bool exito, string mensaje)> DesactivarUsuarioAsync(string nombreUsuario)
    {
        var ejecutor = UsuarioActual;
        if (ejecutor is null || !ejecutor.EsAdministrador)
            return (false, "Acceso denegado.");

        var objetivo = listaUsuarios.Find(u =>
            u.NombreUsuario.Equals(nombreUsuario, StringComparison.Ordinal));

        if (objetivo is null)
            return (false, $"El usuario \"{nombreUsuario}\" no existe.");

        if (objetivo.EsGodAdmin)
            return (false, "El godadmin no puede ser desactivado.");

        if (objetivo.NombreUsuario == ejecutor.NombreUsuario)
            return (false, "No puedes desactivarte a ti mismo.");

        if (objetivo.EsAdministrador && !ejecutor.EsGodAdmin)
            return (false, "Solo el godadmin puede desactivar administradores.");

        objetivo.Activo = false;
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
        return (true, $"\"{nombreUsuario}\" ha sido desactivado.");
    }

    public static async Task<(bool exito, string mensaje)> ActivarUsuarioAsync(string nombreUsuario)
    {
        var ejecutor = UsuarioActual;
        if (ejecutor is null || !ejecutor.EsAdministrador)
            return (false, "Acceso denegado.");

        var objetivo = listaUsuarios.Find(u =>
            u.NombreUsuario.Equals(nombreUsuario, StringComparison.Ordinal));

        if (objetivo is null)
            return (false, $"El usuario \"{nombreUsuario}\" no existe.");

        if (objetivo.EsAdministrador && !ejecutor.EsGodAdmin)
            return (false, "Solo el godadmin puede reactivar administradores.");

        objetivo.Activo = true;
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
        return (true, $"\"{nombreUsuario}\" ha sido reactivado.");
    }

    public static async Task<(bool exito, string mensaje)> PromoverAdministradorAsync(string nombreUsuario)
    {
        var ejecutor = UsuarioActual;
        if (ejecutor is null || !ejecutor.EsGodAdmin)
            return (false, "Solo el godadmin puede promover administradores.");

        var objetivo = listaUsuarios.Find(u =>
            u.NombreUsuario.Equals(nombreUsuario, StringComparison.Ordinal));

        if (objetivo is null)
            return (false, $"El usuario \"{nombreUsuario}\" no existe.");
        if (objetivo.EsAdministrador)
            return (false, $"\"{nombreUsuario}\" ya es administrador.");

        objetivo.Rol = "admin";
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
        return (true, $"\"{nombreUsuario}\" ahora es administrador.");
    }

    public static async Task<(bool exito, string mensaje)> DegradrarAdministradorAsync(string nombreUsuario)
    {
        var ejecutor = UsuarioActual;
        if (ejecutor is null || !ejecutor.EsGodAdmin)
            return (false, "Solo el godadmin puede quitar administradores.");

        var objetivo = listaUsuarios.Find(u =>
            u.NombreUsuario.Equals(nombreUsuario, StringComparison.Ordinal));

        if (objetivo is null)
            return (false, $"El usuario \"{nombreUsuario}\" no existe.");
        if (objetivo.EsGodAdmin)
            return (false, "El godadmin no puede ser degradado.");
        if (!objetivo.EsAdministrador)
            return (false, $"\"{nombreUsuario}\" no es administrador.");

        objetivo.Rol = "user";
        await ServicioDatos.GuardarUsuariosAsync(listaUsuarios);
        return (true, $"\"{nombreUsuario}\" ahora es usuario normal.");
    }
}
