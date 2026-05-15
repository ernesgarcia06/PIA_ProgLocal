using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameStoreApp.Models;

namespace GameStoreApp.Services;

public static class ServicioDatos
{
    private static readonly string carpetaDatos =
        Path.Combine(AppContext.BaseDirectory, "Data");

    private static readonly string rutaUsuarios =
        Path.Combine(carpetaDatos, "users.json");

    private static readonly string rutaJuegos =
        Path.Combine(carpetaDatos, "games.json");

    private static readonly string rutaReportes =
        Path.Combine(carpetaDatos, "reports.json");

    private static readonly string carpetaImagenes =
        Path.Combine(AppContext.BaseDirectory, "Data", "Images");

    public static string RutaCarpetaDatos   => carpetaDatos;
    public static string RutaCarpetaImagenes => carpetaImagenes;

    private static readonly JsonSerializerOptions opcionesJson = new() { WriteIndented = true };

    public static async Task InicializarAsync()
    {
        Directory.CreateDirectory(carpetaDatos);
        Directory.CreateDirectory(carpetaImagenes);

        if (!File.Exists(rutaJuegos))
            await GuardarJuegosAsync(ObtenerJuegosIniciales());

        if (!File.Exists(rutaUsuarios))
            await GuardarUsuariosAsync(ObtenerUsuariosIniciales());
    }

    public static async Task<List<Juego>> CargarJuegosAsync()
    {
        try
        {
            string json = await File.ReadAllTextAsync(rutaJuegos);
            return JsonSerializer.Deserialize<List<Juego>>(json, opcionesJson) ?? new();
        }
        catch { return ObtenerJuegosIniciales(); }
    }

    public static async Task GuardarJuegosAsync(List<Juego> juegos)
    {
        string json = JsonSerializer.Serialize(juegos, opcionesJson);
        await File.WriteAllTextAsync(rutaJuegos, json);
    }

    public static async Task<List<Usuario>> CargarUsuariosAsync()
    {
        try
        {
            string json = await File.ReadAllTextAsync(rutaUsuarios);
            return JsonSerializer.Deserialize<List<Usuario>>(json, opcionesJson) ?? new();
        }
        catch { return ObtenerUsuariosIniciales(); }
    }

    public static async Task GuardarUsuariosAsync(List<Usuario> usuarios)
    {
        string json = JsonSerializer.Serialize(usuarios, opcionesJson);
        await File.WriteAllTextAsync(rutaUsuarios, json);
    }

    public static async Task<List<ReporteGuardado>> CargarReportesAsync()
    {
        try
        {
            if (!File.Exists(rutaReportes)) return new();
            string json = await File.ReadAllTextAsync(rutaReportes);
            return JsonSerializer.Deserialize<List<ReporteGuardado>>(json, opcionesJson) ?? new();
        }
        catch { return new(); }
    }

    public static async Task GuardarReporteAsync(ReporteGuardado reporte)
    {
        var reportes = await CargarReportesAsync();
        reportes.Insert(0, reporte);
        string json = JsonSerializer.Serialize(reportes, opcionesJson);
        await File.WriteAllTextAsync(rutaReportes, json);
    }

    public static async Task<string> CopiarImagenJuegoAsync(string rutaOrigen)
    {
        Directory.CreateDirectory(carpetaImagenes);
        string extension = Path.GetExtension(rutaOrigen).ToLowerInvariant();
        string nombreArchivo = $"game_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";
        string destino = Path.Combine(carpetaImagenes, nombreArchivo);
        await Task.Run(() => File.Copy(rutaOrigen, destino, overwrite: true));
        return Path.Combine("Data", "Images", nombreArchivo);
    }

    public static async Task<string> CopiarFotoJuegoAsync(string rutaOrigen)
    {
        string carpetaFotos = Path.Combine(carpetaImagenes, "Photos");
        Directory.CreateDirectory(carpetaFotos);
        string extension = Path.GetExtension(rutaOrigen).ToLowerInvariant();
        string nombreArchivo = $"photo_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}";
        string destino = Path.Combine(carpetaFotos, nombreArchivo);
        await Task.Run(() => File.Copy(rutaOrigen, destino, overwrite: true));
        return Path.Combine("Data", "Images", "Photos", nombreArchivo);
    }

    private static List<Juego> ObtenerJuegosIniciales() => new()
    {
        new Juego { Id=1,  Nombre="Minecraft", Descripcion="Introducción a un juego sandbox cúbico. Minecraft es un juego formado por bloques, criaturas y comunidades. La elección es tuya: sobrevivir a la noche o crear una obra de arte.", Precio=749.00m, Anio=2009, Categoria="SANDBOX", UrlImagen="" },

        new Juego { Id=2,  Nombre="GTA V", Descripcion="Grand Theft Auto V (GTAV) es un juego de acción y aventura en línea desarrollado por Rockstar Games. Es uno de los dieciséis títulos de la franquicia Grand Theft Auto. Conocido como un juego de mundo abierto, GTAV permite a los jugadores explorar libremente el estado ficticio de San Andreas. GTAV cuenta con tres personajes jugables, todos ellos criminales endurecidos que comparten una historia en común.", Precio=599.00m, Anio=2013, Categoria="ACCION", UrlImagen="" },

        new Juego { Id=3,  Nombre="Fortnite", Descripcion="Juego de supervivencia donde 100 jugadores se enfrentan en modo jugador contra jugador, el último en quedar en pie se lleva la victoria magistral.", Precio=0.00m, Anio=2017, Categoria="BATTLE ROYALE", UrlImagen="" },

        new Juego { Id=4,  Nombre="The Legend of Zelda: Ocarina of Time", Descripcion="Historia que sigue a Link en su misión por evitar que el malvado Ganondorf obtenga la trifuerza para dominar el reino de Hyrule.", Precio=700.00m, Anio=1998, Categoria="AVENTURA", UrlImagen="" },

        new Juego { Id=5,  Nombre="Elden Ring", Descripcion="Recorre este impresionante mundo a pie o a caballo, en solitario u online con otros jugadores. Sumérgete en las verdes llanuras, en los pantanos agobiantes, en las montañas tortuosas, en unos castillos que no auguran nada bueno y en otros parajes majestuosos. Todo ello, a una escala nunca antes vista en un juego de FromSoftware.", Precio=1000.00m, Anio=2022, Categoria="RPG", UrlImagen="" },

        new Juego { Id=6,  Nombre="God of War", Descripcion="Habiendo consumado su venganza contra los dioses el Olimpo años atrás, Kratos ahora vive como un hombre en el reino de los dioses y los monstruos nórdicos. En este hostil e inhóspito mundo, debe pelear por sobrevivir y enseñarle a su hijo a hacer lo mismo.", Precio=899.00m, Anio=2022, Categoria="ACCION", UrlImagen="" },

        new Juego { Id=7,  Nombre="Assassin's Creed Unity", Descripcion="La Revolución francesa ha convertido una ciudad antaño magnífica en un lugar de terror y caos. Las calles adoquinadas están teñidas de rojo con la sangre de los comuneros que se atrevieron a alzarse contra la aristocracia opresora. Revive la emblemática entrega de la franquicia de Assassin's Creed.", Precio=599.00m, Anio=2014, Categoria="SIGILO", UrlImagen="" },

        new Juego { Id=8,  Nombre="FIFA 26", Descripcion="Videojuego de fútbol desarrollado por EA Vancouver y EA Romania y publicado por Electronic Arts.", Precio=419.00m, Anio=2025, Categoria="DEPORTES", UrlImagen="" },

        new Juego { Id=9,  Nombre="Red Dead Redemption 2", Descripcion="Videojuego de acción-aventura de mundo abierto desarrollado y publicado por Rockstar Games.", Precio=1199.00m, Anio=2018, Categoria="ACCION", UrlImagen="" },

        new Juego { Id=10, Nombre="Fallout 3", Descripcion="Videojuego de rol de acción de disparos en primera persona de 2008 desarrollado por Bethesda Game Studios y publicado por Bethesda Softworks.", Precio=199.00m, Anio=2008, Categoria="ROL", UrlImagen="" },

        new Juego { Id=11, Nombre="Call of Duty 4: Modern Warfare", Descripcion="Videojuego de disparos en primera persona bélica desarrollado por Infinity Ward y distribuido por Activision.", Precio=499.00m, Anio=2007, Categoria="ACCION", UrlImagen="" },

        new Juego { Id=12, Nombre="Fall Guys", Descripcion="Videojuego de plataformas y battle royale gratuito desarrollado por Mediatonic.", Precio=0.00m, Anio=2020, Categoria="OTROS", UrlImagen="" },

        new Juego { Id=13, Nombre="Half-Life", Descripcion="Videojuego de disparos en primera persona de 1998 desarrollado por Valve Corporation y publicado por Sierra Studios para Windows.", Precio=113.99m, Anio=1998, Categoria="SHOOTER", UrlImagen="" },

        new Juego { Id=14, Nombre="Chrono Trigger", Descripcion="Videojuego de rol desarrollado y publicado por Square para la videoconsola Super Nintendo Entertainment System. Tendrás que viajar desde el albor de la civilización hasta el fin del mundo en el legendario Chrono Trigger.", Precio=139.00m, Anio=1995, Categoria="ROL", UrlImagen="" },
    };

    private static List<Usuario> ObtenerUsuariosIniciales() => new()
    {
        new Usuario { NombreUsuario="demo",     Contrasenia="Demo123@",    Rol="user",     Activo=true, JuegosComprados=new(), ListaDeseos=new(), RegistrosCompras=new(), RegistrosDeseos=new(), Resenias=new() },
        new Usuario { NombreUsuario="admin",    Contrasenia="Admin123@",   Rol="admin",    Activo=true, JuegosComprados=new(), ListaDeseos=new(), RegistrosCompras=new(), RegistrosDeseos=new(), Resenias=new() },
        new Usuario { NombreUsuario="godadmin", Contrasenia="GodAdmin1@",  Rol="godadmin", Activo=true, JuegosComprados=new(), ListaDeseos=new(), RegistrosCompras=new(), RegistrosDeseos=new(), Resenias=new() }
    };
}
