using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameStoreApp.Models;

public class ReporteGuardado
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("fechaGenerado")]
    public string FechaGenerado { get; set; } = string.Empty;

    [JsonPropertyName("generadoPor")]
    public string GeneradoPor { get; set; } = string.Empty;

    [JsonPropertyName("filtros")]
    public FiltrosReporte Filtros { get; set; } = new();

    [JsonPropertyName("resultados")]
    public List<ResultadoReporteGuardado> Resultados { get; set; } = new();

    [JsonPropertyName("totalResultados")]
    public int TotalResultados { get; set; }
}

public class FiltrosReporte
{
    [JsonPropertyName("periodo")]
    public string Periodo { get; set; } = string.Empty;

    [JsonPropertyName("desde")]
    public string? Desde { get; set; }

    [JsonPropertyName("hasta")]
    public string? Hasta { get; set; }

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("operadorPrecio")]
    public string? OperadorPrecio { get; set; }

    [JsonPropertyName("precio")]
    public decimal? Precio { get; set; }

    [JsonPropertyName("ordenCalificacion")]
    public string? OrdenCalificacion { get; set; }

    [JsonPropertyName("estrellaMinima")]
    public int? EstrellaMinima { get; set; }

    [JsonPropertyName("estrellaMaxima")]
    public int? EstrellaMaxima { get; set; }
}

public class ResultadoReporteGuardado
{
    [JsonPropertyName("posicion")]
    public int Posicion { get; set; }

    [JsonPropertyName("idJuego")]
    public int IdJuego { get; set; }

    [JsonPropertyName("nombreJuego")]
    public string NombreJuego { get; set; } = string.Empty;

    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }

    [JsonPropertyName("anio")]
    public int Anio { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("calificacionPromedio")]
    public double CalificacionPromedio { get; set; }
}
