using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreApp.Models;

public class Resenia
{
    [JsonPropertyName("gameId")]
    public int IdJuego { get; set; }

    [JsonPropertyName("username")]
    public string NombreUsuario { get; set; } = string.Empty;

    [JsonPropertyName("stars")]
    public int Estrellas { get; set; }

    [JsonPropertyName("text")]
    public string Texto { get; set; } = string.Empty;

    [JsonPropertyName("likes")]
    public int MeGusta { get; set; }

    [JsonPropertyName("dislikes")]
    public int NoMeGusta { get; set; }

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");

    [JsonPropertyName("reactions")]
    public Dictionary<string, string> Reacciones { get; set; } = new();
}
