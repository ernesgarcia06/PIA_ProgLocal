using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreApp.Models;

public class Usuario
{
    [JsonPropertyName("username")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Contrasenia { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Rol { get; set; } = "user";

    [JsonPropertyName("activo")]
    public bool Activo { get; set; } = true;

    [JsonIgnore]
    public bool EsAdministrador => Rol == "admin" || Rol == "godadmin";

    [JsonIgnore]
    public bool EsGodAdmin => Rol == "godadmin";

    [JsonIgnore]
    public bool EsAdmin => Rol == "admin";

    [JsonIgnore]
    public bool EsUsuarioNormal => Rol == "user";

    [JsonPropertyName("wishlist")]
    public List<int> ListaDeseos { get; set; } = new();

    [JsonPropertyName("purchasedGames")]
    public List<int> JuegosComprados { get; set; } = new();

    [JsonPropertyName("registrosCompras")]
    public List<RegistroCompra> RegistrosCompras { get; set; } = new();

    [JsonPropertyName("registrosDeseos")]
    public List<RegistroDeseo> RegistrosDeseos { get; set; } = new();

    [JsonPropertyName("reviews")]
    public List<Resenia> Resenias { get; set; } = new();
}
