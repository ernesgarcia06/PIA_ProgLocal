using System;
using System.Text.Json.Serialization;

namespace GameStoreApp.Models;

public class RegistroCompra
{
    [JsonPropertyName("gameId")]
    public int IdJuego { get; set; }

    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

    [JsonPropertyName("ultimosDigitos")]
    public string UltimosDigitos { get; set; } = string.Empty;
}
