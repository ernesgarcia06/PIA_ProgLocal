using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreApp.Models;

public class Juego
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Precio { get; set; }

    [JsonPropertyName("year")]
    public int Anio { get; set; }

    [JsonPropertyName("category")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string UrlImagen { get; set; } = string.Empty;

    [JsonPropertyName("photos")]
    public List<string> Fotos { get; set; } = new();

    [JsonIgnore]
    public List<Resenia> Resenias { get; set; } = new();

    [JsonIgnore]
    public double CalificacionPromedio =>
        Resenias.Count > 0 ? Resenias.Average(r => r.Estrellas) : 0.0;
}
