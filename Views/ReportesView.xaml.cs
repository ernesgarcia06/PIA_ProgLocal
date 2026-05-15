using GameStoreApp.Models;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.UI;

namespace GameStoreApp.Views;

public sealed partial class ReportesView : UserControl
{
    public event Action<Juego>? AlSeleccionarJuego;
    private List<ResultadoReporte> ultimosResultados = new();
    private int ultimoTipoReporte = 0;
    private string ultimaCategoria = "Todas";
    private string ultimoOperPrecio = "ninguno";
    private decimal? ultimoPrecio = null;
    private int ultimoPeriodoIdx = 0;
    private DateTime? ultimoDesde = null;
    private DateTime? ultimoHasta = null;
    private string ultimoOrdenCalif = "Mejores primero";
    private int ultimoEstrellaMin = 1;
    private int ultimoEstrellaMax = 5;

    public ReportesView() { this.InitializeComponent(); CargarCategorias(); }
    public void Actualizar() => CargarCategorias();

    private void CargarCategorias()
    {
        string sel = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        CmbCategoria.Items.Clear();
        CmbCategoria.Items.Add(new ComboBoxItem { Content = "Todas", IsSelected = true });
        foreach (var cat in ServicioJuegos.ObtenerCategorias().Skip(1))
            CmbCategoria.Items.Add(new ComboBoxItem { Content = cat });
        if (!string.IsNullOrEmpty(sel))
            foreach (ComboBoxItem item in CmbCategoria.Items)
                if (item.Content?.ToString() == sel) { CmbCategoria.SelectedItem = item; break; }
    }

    private void CmbTipoReporte_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTipoReporte is null) return;
        int tipo = CmbTipoReporte.SelectedIndex;
        if (PanelPrecio is not null)
            PanelPrecio.Visibility = (tipo == 0 || tipo == 1) ? Visibility.Visible : Visibility.Collapsed;
        if (PanelCalificaciones is not null)
            PanelCalificaciones.Visibility = tipo == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CmbPeriodo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPeriodo is null) return;
        int p = CmbPeriodo.SelectedIndex;
        if (PanelMesEspecifico is not null) PanelMesEspecifico.Visibility = p == 1 ? Visibility.Visible : Visibility.Collapsed;
        if (PanelRangoFechas is not null)   PanelRangoFechas.Visibility   = p == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CmbOperadorPrecio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TxtPrecio is null || CmbOperadorPrecio is null) return;
        bool mostrar = CmbOperadorPrecio.SelectedIndex > 0;
        TxtPrecio.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
        if (!mostrar) TxtPrecio.Text = string.Empty;
    }

    private void TxtPrecio_TextChanged(object sender, TextChangedEventArgs e)
    {
        string original = TxtPrecio.Text;
        string filtrado  = new string(original.Where(c => c >= '0' && c <= '9').ToArray());
        if (filtrado != original)
        {
            int pos = TxtPrecio.SelectionStart;
            TxtPrecio.Text = filtrado;
            TxtPrecio.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length));
        }
    }

    private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
    {
        TxtErrorFiltro.Visibility = Visibility.Collapsed;
        int tipoReporte = CmbTipoReporte.SelectedIndex;
        int periodoIdx  = CmbPeriodo.SelectedIndex;
        DateTime? desde = null, hasta = null;

        if (periodoIdx == 1)
        {
            if (FechaMesEspecifico.Date is null) { MostrarErrorFiltro("Selecciona una fecha para el mes específico."); return; }
            var f = FechaMesEspecifico.Date!.Value.Date;
            desde = new DateTime(f.Year, f.Month, 1);
            hasta = desde.Value.AddMonths(1).AddDays(-1);
        }
        else if (periodoIdx == 2)
        {
            if (FechaDesde.Date is null || FechaHasta.Date is null) { MostrarErrorFiltro("Selecciona fecha de inicio y fin del rango."); return; }
            desde = FechaDesde.Date!.Value.Date;
            hasta = FechaHasta.Date!.Value.Date.AddDays(1).AddTicks(-1);
            if (desde > hasta) { MostrarErrorFiltro("La fecha de inicio debe ser anterior a la de fin."); return; }
        }

        string categoria  = (CmbCategoria.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";
        string operPrecio = ObtenerOperadorPrecio();
        decimal? precio   = null;

        if (operPrecio != "ninguno")
        {
            if (string.IsNullOrWhiteSpace(TxtPrecio.Text)) { MostrarErrorFiltro("Ingresa un precio para usar el filtro de precio."); return; }
            if (!decimal.TryParse(TxtPrecio.Text, out decimal p) || p < 0 || p > 1000000) { MostrarErrorFiltro("El precio debe ser un numero entre 0 y 1,000,000."); return; }
            precio = p;
        }

        List<ResultadoReporte> resultados = tipoReporte switch
        {
            0 => ServicioReportes.JuegosMasComprados(desde, hasta, categoria, operPrecio, precio),
            1 => ServicioReportes.JuegosMasDeseados(desde, hasta, categoria, operPrecio, precio),
            2 => ServicioReportes.JuegosMasResenados(desde, hasta, categoria),
            3 => ObtenerReporteCalificaciones(desde, hasta, categoria),
            _ => new List<ResultadoReporte>()
        };

        ultimosResultados  = resultados;
        ultimoTipoReporte  = tipoReporte;
        ultimaCategoria    = categoria;
        ultimoOperPrecio   = operPrecio;
        ultimoPrecio       = precio;
        ultimoPeriodoIdx   = periodoIdx;
        ultimoDesde        = desde;
        ultimoHasta        = hasta;
        ultimoOrdenCalif   = (CmbOrdenCalif.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mejores primero";
        ultimoEstrellaMin  = int.TryParse((CmbEstrellaMin.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int emin) ? emin : 1;
        ultimoEstrellaMax  = int.TryParse((CmbEstrellaMax.SelectedItem as ComboBoxItem)?.Content?.ToString(), out int emax) ? emax : 5;
        MostrarResultados(resultados, tipoReporte);
    }

    private List<ResultadoReporte> ObtenerReporteCalificaciones(DateTime? desde, DateTime? hasta, string categoria)
    {
        bool mejores    = CmbOrdenCalif.SelectedIndex == 0;
        int estrellaMin = int.Parse((CmbEstrellaMin.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1");
        int estrellaMax = int.Parse((CmbEstrellaMax.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "5");
        if (estrellaMin > estrellaMax) { MostrarErrorFiltro("Las estrellas mínimas no pueden superar las maximas."); return new(); }
        return ServicioReportes.JuegosOrdenadosPorCalificacion(mejores, estrellaMin, estrellaMax, categoria, desde, hasta);
    }

    private string ObtenerOperadorPrecio() => CmbOperadorPrecio?.SelectedIndex switch
    {
        1 => "igual", 2 => "mayor", 3 => "menor", _ => "ninguno"
    } ?? "ninguno";

    private void MostrarResultados(List<ResultadoReporte> resultados, int tipoReporte)
    {
        EstadoInicial.Visibility       = Visibility.Collapsed;
        EstadoSinResultados.Visibility = Visibility.Collapsed;
        PanelResultados.Visibility     = Visibility.Collapsed;
        BtnExportarPDF.Visibility     = Visibility.Collapsed;

        string[] titulos   = { "Juegos más comprados", "Juegos más deseados", "Juegos más reseñados", "Calificaciones" };
        string[] etiquetas = { "compras", "veces en lista de deseos", "reseñas", "valoraciones" };
        TxtTituloReporte.Text = titulos[tipoReporte];

        if (resultados.Count == 0)
        {
            TxtSubtituloReporte.Text = "Sin datos para los filtros seleccionados";
            TxtConteoResultados.Text = "0 resultados";
            EstadoSinResultados.Visibility = Visibility.Visible;
            return;
        }

        TxtSubtituloReporte.Text = $"Resultados ordenados por {etiquetas[tipoReporte]}";
        TxtConteoResultados.Text = $"{resultados.Count} juego{(resultados.Count != 1 ? "s" : "")}";
        PanelResultados.Children.Clear();
        PanelResultados.Children.Add(CrearEncabezadoTabla(tipoReporte));
        int pos = 1;
        foreach (var r in resultados) PanelResultados.Children.Add(CrearFilaResultado(r, pos++, tipoReporte));
        PanelResultados.Visibility = Visibility.Visible;
        BtnExportarPDF.Visibility = Visibility.Visible;
    }

    private Microsoft.UI.Xaml.Controls.Border CrearEncabezadoTabla(int tipoReporte)
    {
        string lbl = tipoReporte switch { 0 => "Compras", 1 => "En lista de deseos", 2 => "Reseñas", 3 => "Valoraciones", _ => "Cantidad" };
        var grid = new Grid { Padding = new Thickness(14, 10, 14, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        AgregarCeldaEncabezado(grid, "#",            0, Microsoft.UI.Xaml.HorizontalAlignment.Center);
        AgregarCeldaEncabezado(grid, "Juego",        1, Microsoft.UI.Xaml.HorizontalAlignment.Left);
        AgregarCeldaEncabezado(grid, "Categoría",    2, Microsoft.UI.Xaml.HorizontalAlignment.Left);
        AgregarCeldaEncabezado(grid, lbl,            3, Microsoft.UI.Xaml.HorizontalAlignment.Center);
        AgregarCeldaEncabezado(grid, "Calificacion", 4, Microsoft.UI.Xaml.HorizontalAlignment.Center);
        return new Microsoft.UI.Xaml.Controls.Border
        {
            Background   = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)),
            CornerRadius = new CornerRadius(8, 8, 0, 0),
            Child        = grid
        };
    }

    private static void AgregarCeldaEncabezado(Grid grid, string texto, int col, Microsoft.UI.Xaml.HorizontalAlignment ali)
    {
        var tb = new TextBlock { Text = texto, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 97, 97, 97)),
            HorizontalAlignment = ali, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(tb, col);
        grid.Children.Add(tb);
    }

    private Microsoft.UI.Xaml.Controls.Border CrearFilaResultado(ResultadoReporte resultado, int posicion, int tipoReporte)
    {
        bool esPar = posicion % 2 == 0;
        var grid = new Grid { Padding = new Thickness(14, 12, 14, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        var txtPos = new TextBlock
        {
            Text       = posicion switch { 1 => "1°", 2 => "2°", 3 => "3°", _ => $"{posicion}°" },
            FontSize   = posicion <= 3 ? 14 : 12,
            FontWeight = posicion <= 3 ? FontWeights.Bold : FontWeights.Normal,
            Foreground = new SolidColorBrush(posicion switch
            {
                1 => Color.FromArgb(255, 234, 179, 8),
                2 => Color.FromArgb(255, 107, 114, 128),
                3 => Color.FromArgb(255, 180, 83, 9),
                _ => Color.FromArgb(255, 100, 100, 100)
            }),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center
        };
        Grid.SetColumn(txtPos, 0);

        var panelNombre = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var juegoRef    = resultado.Juego;
        var boton = new Button
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            BorderThickness = new Thickness(0), Padding = new Thickness(0),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
            HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left
        };
        boton.Resources["ButtonBackgroundPointerOver"]  = new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));
        boton.Resources["ButtonBackgroundPressed"]      = new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));
        boton.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));
        boton.Resources["ButtonForegroundPointerOver"]  = new SolidColorBrush(Windows.UI.Color.FromArgb(255,13,71,161));
        boton.Content = new TextBlock { Text = resultado.Juego.Nombre, FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush,
            TextDecorations = Windows.UI.Text.TextDecorations.Underline };
        boton.Click += (_, _) => AlSeleccionarJuego?.Invoke(juegoRef);
        panelNombre.Children.Add(boton);
        panelNombre.Children.Add(new TextBlock { Text = $"${resultado.Juego.Precio:N0} MXN", FontSize = 12,
            Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush });
        Grid.SetColumn(panelNombre, 1);

        var badge = new Microsoft.UI.Xaml.Controls.Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 224, 233, 242)),
            CornerRadius = new CornerRadius(12), Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = resultado.Juego.Categoria, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) }
        };
        Grid.SetColumn(badge, 2);

        var panelCant = new StackPanel { HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
        panelCant.Children.Add(new TextBlock { Text = resultado.Cantidad.ToString(), FontSize = 16, FontWeight = FontWeights.Bold,
            Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center });
        Grid.SetColumn(panelCant, 3);

        var panelCalif = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Spacing = 3 };
        if (resultado.CalificacionPromedio > 0)
        {
            panelCalif.Children.Add(new FontIcon { Glyph = "\uE735", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)) });
            panelCalif.Children.Add(new TextBlock { Text = $"{resultado.CalificacionPromedio:F1}", FontSize = 13,
                Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush, VerticalAlignment = VerticalAlignment.Center });
        }
        else
            panelCalif.Children.Add(new TextBlock { Text = "—", FontSize = 13, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush });
        Grid.SetColumn(panelCalif, 4);

        grid.Children.Add(txtPos); grid.Children.Add(panelNombre); grid.Children.Add(badge);
        grid.Children.Add(panelCant); grid.Children.Add(panelCalif);

        return new Microsoft.UI.Xaml.Controls.Border
        {
            Background      = esPar ? new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)) : new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
            BorderBrush     = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child           = grid
        };
    }

    private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
    {
        if (ultimosResultados.Count == 0)
        {
            MostrarErrorFiltro("Genera un reporte primero.");
            return;
        }

        BtnExportarPDF.IsEnabled = false;

        try
        {
            await GuardarReportePDFAsync(
                ultimosResultados,
                ultimoTipoReporte,
                ultimaCategoria);

            var dialogo = new ContentDialog
            {
                Title = "Reporte generado",
                Content = "El reporte PDF fue generado correctamente.",
                PrimaryButtonText = "Aceptar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var azulGaura =
                new SolidColorBrush(Color.FromArgb(255, 26, 44, 61));

            dialogo.Resources["ContentDialogPrimaryButtonBackground"] =
                azulGaura;

            dialogo.Resources["ContentDialogPrimaryButtonBorderBrush"] =
                azulGaura;

            dialogo.Resources["ContentDialogPrimaryButtonForeground"] =
                new SolidColorBrush(Colors.White);

            dialogo.Resources["ContentDialogPrimaryButtonBackgroundPointerOver"] =
                new SolidColorBrush(Color.FromArgb(255, 38, 60, 82));

            dialogo.Resources["ContentDialogPrimaryButtonBackgroundPressed"] =
                new SolidColorBrush(Color.FromArgb(255, 18, 32, 46));

            await dialogo.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialogo = new ContentDialog
            {
                Title = "Error",
                Content = $"No se pudo generar el PDF:\n{ex.Message}",
                PrimaryButtonText = "Aceptar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var azulGaura =
                new SolidColorBrush(Color.FromArgb(255, 26, 44, 61));

            dialogo.Resources["ContentDialogPrimaryButtonBackground"] =
                azulGaura;

            dialogo.Resources["ContentDialogPrimaryButtonBorderBrush"] =
                azulGaura;

            dialogo.Resources["ContentDialogPrimaryButtonForeground"] =
                new SolidColorBrush(Colors.White);

            dialogo.Resources["ContentDialogPrimaryButtonBackgroundPointerOver"] =
                new SolidColorBrush(Color.FromArgb(255, 38, 60, 82));

            dialogo.Resources["ContentDialogPrimaryButtonBackgroundPressed"] =
                new SolidColorBrush(Color.FromArgb(255, 18, 32, 46));

            await dialogo.ShowAsync();
        }
        finally
        {
            BtnExportarPDF.IsEnabled = true;
        }
    }

    private async Task GuardarReportePDFAsync(
      List<ResultadoReporte> resultados,
      int tipoReporte,
      string categoria)
    {
        string[] titulos =
        {
        "Juegos más comprados",
        "Juegos más deseados",
        "Juegos más reseñados",
        "Calificaciones"
    };

        string carpeta =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reportes");

        Directory.CreateDirectory(carpeta);

        string rutaArchivo =
            Path.Combine(
                carpeta,
                $"Reporte_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        PdfDocument documento = new PdfDocument();
        documento.Info.Title = "Reporte Gaura";

        PdfPage pagina = documento.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(pagina);

        var tituloFont = new XFont("Arial", 20);
        var textoFont = new XFont("Arial", 12);
        var boldFont = new XFont("Arial", 12, XFontStyle.Bold);

        int y = 40;

        gfx.DrawString(
            "GAURA REPORTES",
            tituloFont,
            XBrushes.DarkBlue,
            new XRect(0, y, pagina.Width, 30),
            XStringFormats.TopCenter);

        y += 40;

        gfx.DrawString(
            $"Tipo: {titulos[tipoReporte]}",
            boldFont,
            XBrushes.Black,
            40,
            y);

        y += 25;

        gfx.DrawString(
            $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}",
            textoFont,
            XBrushes.Black,
            40,
            y);

        y += 25;

        gfx.DrawString(
            $"Categoría: {categoria}",
            textoFont,
            XBrushes.Black,
            40,
            y);

        y += 35;

        foreach (var r in resultados)
        {
            gfx.DrawString(
                $"{r.Juego.Nombre}",
                boldFont,
                XBrushes.Black,
                40,
                y);

            y += 18;

            gfx.DrawString(
                $"Categoría: {r.Juego.Categoria} | Precio: ${r.Juego.Precio:N0} | Cantidad: {r.Cantidad}",
                textoFont,
                XBrushes.Black,
                50,
                y);

            y += 22;

            if (y > 760)
            {
                pagina = documento.AddPage();
                gfx = XGraphics.FromPdfPage(pagina);
                y = 40;
            }
        }

        documento.Save(rutaArchivo);

        Process.Start(new ProcessStartInfo
        {
            FileName = rutaArchivo,
            UseShellExecute = true
        });

        await Task.CompletedTask;
    }

    private void MostrarErrorFiltro(string msg) { TxtErrorFiltro.Text = msg; TxtErrorFiltro.Visibility = Visibility.Visible; }

    private void BtnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
    {
        CmbTipoReporte.SelectedIndex = 0; CmbPeriodo.SelectedIndex = 0; CmbCategoria.SelectedIndex = 0;
        CmbOperadorPrecio.SelectedIndex = 0; CmbOrdenCalif.SelectedIndex = 0;
        CmbEstrellaMin.SelectedIndex = 0; CmbEstrellaMax.SelectedIndex = 4;
        FechaMesEspecifico.Date = null; FechaDesde.Date = null; FechaHasta.Date = null;
        TxtPrecio.Text = string.Empty; TxtPrecio.Visibility = Visibility.Collapsed;
        TxtErrorFiltro.Visibility = Visibility.Collapsed; PanelResultados.Visibility = Visibility.Collapsed;
        EstadoSinResultados.Visibility = Visibility.Collapsed; EstadoInicial.Visibility = Visibility.Visible;
        BtnExportarPDF.Visibility = Visibility.Collapsed;
        TxtTituloReporte.Text = "Selecciona un tipo de reporte y genera";
        TxtSubtituloReporte.Text = "Configura los filtros y presiona Generar";
        TxtConteoResultados.Text = "—";
        ultimosResultados.Clear();
    }
}
