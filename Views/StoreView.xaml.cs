using System;
using System.IO;
using System.Collections.Generic;
using GameStoreApp.Models;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameStoreApp.Views;

public sealed partial class StoreView : UserControl
{
    public event Action<Juego>? AlSeleccionarJuego;

    private List<Juego> juegosFiltrados = new();

    public StoreView() => this.InitializeComponent();

    public void Actualizar()
    {
        CargarOpcionesFiltros();
        AplicarFiltros();
    }

    private void CargarOpcionesFiltros()
    {
        if (CmbCategory is null || CmbYear is null) return;

        var categoriaSeleccionada = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString();
        CmbCategory.Items.Clear();
        foreach (var categoria in ServicioJuegos.ObtenerCategorias())
        {
            var item = new ComboBoxItem { Content = categoria };
            CmbCategory.Items.Add(item);
            if (categoria == categoriaSeleccionada) CmbCategory.SelectedItem = item;
        }

        var anioSeleccionado = (CmbYear.SelectedItem as ComboBoxItem)?.Content?.ToString();
        CmbYear.Items.Clear();
        CmbYear.Items.Add(new ComboBoxItem { Content = "Todos" });
        foreach (var anio in ServicioJuegos.ObtenerAnios())
        {
            var item = new ComboBoxItem { Content = anio.ToString() };
            CmbYear.Items.Add(item);
            if (anio.ToString() == anioSeleccionado) CmbYear.SelectedItem = item;
        }
    }

    private void AplicarFiltros()
    {
        if (TxtSearch is null || CmbCategory is null || CmbYear is null ||
            CmbSort is null || TxtGameCount is null || EmptyState is null ||
            GamesRepeater is null) return;

        string textoBusqueda = TxtSearch.Text;
        string categoria     = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todas";
        string textoAnio     = (CmbYear.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
        string orden         = (CmbSort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "A-Z";

        int? anio = (textoAnio != "Todos" && int.TryParse(textoAnio, out int a)) ? a : null;

        juegosFiltrados = ServicioJuegos.Filtrar(textoBusqueda, categoria, anio, orden);

        TxtGameCount.Text = $"{juegosFiltrados.Count} juego{(juegosFiltrados.Count != 1 ? "s" : "")} disponible{(juegosFiltrados.Count != 1 ? "s" : "")}";
        EmptyState.Visibility = juegosFiltrados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        RenderizarTarjetas();
    }

    private void RenderizarTarjetas()
    {
        if (GamesRepeater is null) return;
        GamesRepeater.ItemsSource = null;

        var tarjetas = new List<Border>();
        foreach (var juego in juegosFiltrados)
            tarjetas.Add(CrearTarjetaJuego(juego));

        GamesRepeater.ItemsSource = tarjetas;
    }

    private Border CrearTarjetaJuego(Juego juego)
    {
        var contenedorImagen = new Border
        {
            Height       = 150,
            CornerRadius = new CornerRadius(14, 14, 0, 0),
            Background   = ObtenerPincelCategoria(juego.Categoria)
        };

        bool tieneImagenLocal = !string.IsNullOrEmpty(juego.UrlImagen)
            && !juego.UrlImagen.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        string rutaCompleta = tieneImagenLocal
            ? Path.Combine(AppContext.BaseDirectory, juego.UrlImagen)
            : string.Empty;
        bool archivoExiste = tieneImagenLocal && File.Exists(rutaCompleta);

        if (archivoExiste)
        {
            try
            {
                contenedorImagen.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(rutaCompleta)),
                    Stretch     = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                };
            }
            catch { contenedorImagen.Child = CrearPlaceholderCategoria(juego.Categoria); }
        }
        else
        {
            contenedorImagen.Child = CrearPlaceholderCategoria(juego.Categoria);
        }

        var cuerpoTarjeta = new StackPanel { Padding = new Thickness(12, 10, 12, 12), Spacing = 6 };

        cuerpoTarjeta.Children.Add(new TextBlock
        {
            Text        = juego.Nombre,
            FontSize    = 15,
            FontWeight  = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground  = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines    = 2
        });

        var metaDatos = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        metaDatos.Children.Add(new TextBlock { Text = juego.Anio.ToString(), FontSize = 12, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush });
        metaDatos.Children.Add(new TextBlock { Text = "•", FontSize = 12, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush });
        metaDatos.Children.Add(new TextBlock { Text = juego.Categoria, FontSize = 12, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush });
        cuerpoTarjeta.Children.Add(metaDatos);

        if (juego.Resenias.Count > 0)
        {
            var panelCalificacion = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            panelCalificacion.Children.Add(new FontIcon { Glyph = "\uE735", FontSize = 13, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 158, 11)) });
            panelCalificacion.Children.Add(new TextBlock { Text = $"{juego.CalificacionPromedio:F1}", FontSize = 13, Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush });
            panelCalificacion.Children.Add(new TextBlock { Text = $"({juego.Resenias.Count})", FontSize = 12, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush });
            cuerpoTarjeta.Children.Add(panelCalificacion);
        }

        var filaPrecio = new Grid();
        filaPrecio.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filaPrecio.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textoPrecio = new TextBlock
        {
            Text       = $"${juego.Precio:N0} MXN",
            FontSize   = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(textoPrecio, 0);

        var botonVer = new Button
        {
            Content = "Ver",
            Style   = Application.Current.Resources["PrimaryButtonStyle"] as Style,
            Padding = new Thickness(12, 6, 12, 6),
            FontSize = 13
        };
        Grid.SetColumn(botonVer, 1);

        var juegoCapturado = juego;
        botonVer.Click += (s, e) => AlSeleccionarJuego?.Invoke(juegoCapturado);

        filaPrecio.Children.Add(textoPrecio);
        filaPrecio.Children.Add(botonVer);
        cuerpoTarjeta.Children.Add(filaPrecio);

        var contenidoTarjeta = new StackPanel();
        contenidoTarjeta.Children.Add(contenedorImagen);
        contenidoTarjeta.Children.Add(cuerpoTarjeta);

        var tarjeta = new Border
        {
            Style    = Application.Current.Resources["GameCardStyle"] as Style,
            Padding  = new Thickness(0),
            Margin   = new Thickness(0),
            Child    = contenidoTarjeta,
            MinWidth = 230,
            MaxWidth = 320
        };

        tarjeta.Tapped += (s, e) => AlSeleccionarJuego?.Invoke(juegoCapturado);
        return tarjeta;
    }

    private static StackPanel CrearPlaceholderCategoria(string categoria)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6 };
        panel.Children.Add(new FontIcon { Glyph = ObtenerGlifoCategoria(categoria), FontSize = 40, Foreground = new SolidColorBrush(Colors.White), HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = categoria, FontSize = 12, Foreground = new SolidColorBrush(Colors.White), HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.85 });
        return panel;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        string texto = TxtSearch.Text;
        if (texto.Length > 0 && texto[0] == ' ')
        {
            TxtSearch.Text = texto.TrimStart();
            TxtSearch.SelectionStart = TxtSearch.Text.Length;
            return;
        }
        AplicarFiltros();
    }
    private void BtnSearch_Click(object sender, RoutedEventArgs e)            => AplicarFiltros();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e)   => AplicarFiltros();

    private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Text = string.Empty;
        if (CmbCategory.Items.Count > 0) CmbCategory.SelectedIndex = 0;
        if (CmbYear.Items.Count > 0)     CmbYear.SelectedIndex     = 0;
        if (CmbSort.Items.Count > 0)     CmbSort.SelectedIndex     = 0;
        AplicarFiltros();
    }

    private static SolidColorBrush ObtenerPincelCategoria(string categoria)
    {
        return (categoria ?? string.Empty).ToUpperInvariant() switch
        {
            "RPG"          => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  75,   0, 130)),
            "SHOOTER"      => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 183,  28,  28)),
            "MMORPG"       => new SolidColorBrush(Windows.UI.Color.FromArgb(255,   0,  96, 100)),
            "CARRERAS"     => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230,  81,   0)),
            "AVENTURA"     => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  33, 150,  83)),
            "SIMULACIÓN"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  46, 125,  50)),
            "SIMULACION"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  46, 125,  50)),
            "ESTRATEGIA"   => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  49,  27, 146)),
            "PLATAFORMAS"  => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  21, 101, 192)),
            "TERROR"       => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  38,  50,  56)),
            "PUZZLE"       => new SolidColorBrush(Windows.UI.Color.FromArgb(255,   0, 131, 143)),
            "SANDBOX"      => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  92, 107,  92)),
            "ROL"          => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 106,  27, 154)),
            "BATTLE ROYALE"=> new SolidColorBrush(Windows.UI.Color.FromArgb(255, 183,  28,  28)),
            "DEPORTES"     => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  27, 100,  50)),
            _              => new SolidColorBrush(Windows.UI.Color.FromArgb(255,  66,  66,  66))
        };
    }

    private static string ObtenerGlifoCategoria(string categoria)
    {
        return (categoria ?? string.Empty).ToUpperInvariant() switch
        {
            "RPG"          => "\uE97D",
            "SHOOTER"      => "\uE946",
            "MMORPG"       => "\uE77B",
            "CARRERAS"     => "\uE804",
            "AVENTURA"     => "\uE714",
            "SIMULACIÓN"   => "\uE8EF",
            "SIMULACION"   => "\uE8EF",
            "ESTRATEGIA"   => "\uE7C1",
            "PLATAFORMAS"  => "\uE7FC",
            "TERROR"       => "\uE7BA",
            "PUZZLE"       => "\uE8E4",
            "SANDBOX"      => "\uE81E",
            "ROL"          => "\uE97D",
            "BATTLE ROYALE"=> "\uE946",
            "DEPORTES"     => "\uEB43",
            _              => "\uE7FC"
        };
    }
}
