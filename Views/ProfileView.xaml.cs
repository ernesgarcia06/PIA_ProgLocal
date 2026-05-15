using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameStoreApp.Models;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace GameStoreApp.Views;

public sealed partial class ProfileView : UserControl
{
    public event Action?        AlRegresar;
    public event Action<Juego>? AlSeleccionarJuego;

    private enum TabPerfil { Comprados, ListaDeseos, Resenias }
    private TabPerfil pestaniaActiva = TabPerfil.Comprados;

    public ProfileView() => this.InitializeComponent();

    public void Actualizar()
    {
        var usuario = ServicioAutenticacion.UsuarioActual;
        if (usuario is null) return;

        TxtProfileUsername.Text = usuario.NombreUsuario;
        TxtPurchasedCount.Text  = $"{usuario.JuegosComprados.Count} juego{(usuario.JuegosComprados.Count != 1 ? "s" : "")}";
        TxtWishlistCount.Text   = $"{usuario.ListaDeseos.Count} en lista";
        TxtReviewsCount.Text    = $"{usuario.Resenias.Count} reseña{(usuario.Resenias.Count != 1 ? "s" : "")}";

        MostrarPestania(pestaniaActiva);
        ResaltarPestaniaActiva(pestaniaActiva);
    }

    public void MostrarPestanaComprados()
    {
        pestaniaActiva = TabPerfil.Comprados;
        Actualizar();
    }

    private void BotonComprados_Click(object sender, RoutedEventArgs e)
    { pestaniaActiva = TabPerfil.Comprados;   MostrarPestania(pestaniaActiva); ResaltarPestaniaActiva(pestaniaActiva); }

    private void BotonListaDeseos_Click(object sender, RoutedEventArgs e)
    { pestaniaActiva = TabPerfil.ListaDeseos; MostrarPestania(pestaniaActiva); ResaltarPestaniaActiva(pestaniaActiva); }

    private void BotonResenias_Click(object sender, RoutedEventArgs e)
    { pestaniaActiva = TabPerfil.Resenias;    MostrarPestania(pestaniaActiva); ResaltarPestaniaActiva(pestaniaActiva); }

    private void ResaltarPestaniaActiva(TabPerfil tab)
    {
        var pincelDefault = Application.Current.Resources["DarkTextBrush"]    as SolidColorBrush;
        var pincelActivo  = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush;

        BtnTabPurchased.Foreground = pincelDefault;
        BtnTabWishlist.Foreground  = pincelDefault;
        BtnTabReviews.Foreground   = pincelDefault;
        BtnTabPurchased.FontWeight = FontWeights.Normal;
        BtnTabWishlist.FontWeight  = FontWeights.Normal;
        BtnTabReviews.FontWeight   = FontWeights.Normal;
        BtnTabPurchased.Background = new SolidColorBrush(Colors.Transparent);
        BtnTabWishlist.Background  = new SolidColorBrush(Colors.Transparent);
        BtnTabReviews.Background   = new SolidColorBrush(Colors.Transparent);

        var botonActivo = tab switch
        {
            TabPerfil.Comprados   => BtnTabPurchased,
            TabPerfil.ListaDeseos => BtnTabWishlist,
            TabPerfil.Resenias    => BtnTabReviews,
            _                     => BtnTabPurchased
        };

        botonActivo.Foreground = pincelActivo;
        botonActivo.FontWeight = FontWeights.SemiBold;
        botonActivo.Background = new SolidColorBrush(Color.FromArgb(255, 224, 233, 242));
    }

    private static DateTime ParsearFechaRegistro(string fecha)
    {
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy HH:mm:ss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt1))
            return dt1;
        if (DateTime.TryParseExact(fecha, "dd/MM/yyyy HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt2))
            return dt2;
        return DateTime.MinValue;
    }

    private void MostrarPestania(TabPerfil tab)
    {
        ProfileContentPanel.Children.Clear();
        var usuario = ServicioAutenticacion.UsuarioActual;
        if (usuario is null) return;

        switch (tab)
        {
            case TabPerfil.Comprados:
            {
                var idsOrdenados = usuario.RegistrosCompras
                    .GroupBy(r => r.IdJuego)
                    .Select(g => new { IdJuego = g.Key, Fecha = g.Max(r => ParsearFechaRegistro(r.Fecha)) })
                    .OrderByDescending(x => x.Fecha)
                    .Select(x => x.IdJuego)
                    .ToList();
                var sinRegistro = usuario.JuegosComprados
                    .Where(id => !idsOrdenados.Contains(id))
                    .ToList();
                idsOrdenados.AddRange(sinRegistro);
                RenderizarListaJuegos(idsOrdenados, "Juegos comprados", "\uE7FC", "Aún no has comprado ningun juego.");
                break;
            }
            case TabPerfil.ListaDeseos:
            {
                var idsOrdenados = usuario.RegistrosDeseos
                    .GroupBy(r => r.IdJuego)
                    .Select(g => new { IdJuego = g.Key, Fecha = g.Max(r => ParsearFechaRegistro(r.Fecha)) })
                    .OrderByDescending(x => x.Fecha)
                    .Select(x => x.IdJuego)
                    .ToList();
                var sinRegistro = usuario.ListaDeseos
                    .Where(id => !idsOrdenados.Contains(id))
                    .ToList();
                idsOrdenados.AddRange(sinRegistro);
                RenderizarListaJuegos(idsOrdenados, "Lista de deseos", "\uEB52", "Tu lista de deseos está vacía.");
                break;
            }
            case TabPerfil.Resenias:
            {
                RenderizarResenias(ObtenerReseniasSincronizadas(usuario));
                break;
            }
        }
    }

    private void RenderizarListaJuegos(List<int> idsJuegos, string titulo, string glifo, string mensajeVacio)
    {
        var filaTitulo = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 4) };
        filaTitulo.Children.Add(new FontIcon { Glyph = glifo, FontSize = 18, Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush });
        filaTitulo.Children.Add(new TextBlock { Text = titulo, Style = Application.Current.Resources["H2Style"] as Style, FontSize = 18 });
        ProfileContentPanel.Children.Add(filaTitulo);

        var juegos = ServicioJuegos.ObtenerPorIds(idsJuegos);

        if (juegos.Count == 0)
        {
            var panelVacio = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0), Spacing = 12 };
            panelVacio.Children.Add(new FontIcon { Glyph = glifo, FontSize = 40, Foreground = Application.Current.Resources["MediumGrayBrush"] as SolidColorBrush, HorizontalAlignment = HorizontalAlignment.Center });
            panelVacio.Children.Add(new TextBlock { Text = mensajeVacio, Style = Application.Current.Resources["SecondaryTextStyle"] as Style, HorizontalAlignment = HorizontalAlignment.Center });
            ProfileContentPanel.Children.Add(panelVacio);
            return;
        }

        foreach (var juego in juegos)
            ProfileContentPanel.Children.Add(CrearTarjetaJuegoPerfil(juego));
    }

    private Border CrearTarjetaJuegoPerfil(Juego juego)
    {
        var tarjeta = new Border { Style = Application.Current.Resources["CardStyle"] as Style, Padding = new Thickness(14, 12, 14, 12) };
        var grid    = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var bordeIcono = new Border
        {
            Width        = 52,
            Height       = 52,
            CornerRadius = new CornerRadius(8),
            Background   = ObtenerPincelCategoria(juego.Categoria),
            Margin       = new Thickness(0, 0, 14, 0)
        };

        bool tieneImagenLocal = !string.IsNullOrEmpty(juego.UrlImagen)
            && !juego.UrlImagen.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        string rutaCompleta = tieneImagenLocal
            ? Path.Combine(AppContext.BaseDirectory, juego.UrlImagen)
            : string.Empty;

        if (tieneImagenLocal && File.Exists(rutaCompleta))
        {
            try
            {
                bordeIcono.Background = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(rutaCompleta)),
                    Stretch     = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
                };
            }
            catch
            {
                bordeIcono.Child = new FontIcon
                {
                    Glyph               = ObtenerGlifoCategoria(juego.Categoria),
                    FontSize            = 26,
                    Foreground          = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center
                };
            }
        }
        else
        {
            bordeIcono.Child = new FontIcon
            {
                Glyph               = ObtenerGlifoCategoria(juego.Categoria),
                FontSize            = 26,
                Foreground          = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
        }
        Grid.SetColumn(bordeIcono, 0);

        var panelInfo = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        panelInfo.Children.Add(new TextBlock { Text = juego.Nombre, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush });

        var panelMeta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panelMeta.Children.Add(new TextBlock { Text = juego.Categoria,         Style = Application.Current.Resources["SecondaryTextStyle"] as Style, FontSize = 12 });
        panelMeta.Children.Add(new TextBlock { Text = "·",                     Style = Application.Current.Resources["SecondaryTextStyle"] as Style, FontSize = 12 });
        panelMeta.Children.Add(new TextBlock { Text = juego.Anio.ToString(),   Style = Application.Current.Resources["SecondaryTextStyle"] as Style, FontSize = 12 });
        panelInfo.Children.Add(panelMeta);
        panelInfo.Children.Add(new TextBlock { Text = $"${juego.Precio:N0} MXN", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush });
        Grid.SetColumn(panelInfo, 1);

        var juegoCapturado  = juego;
        var botonVer        = new Button { Style = Application.Current.Resources["SecondaryButtonStyle"] as Style, FontSize = 12, Padding = new Thickness(10, 6, 10, 6), VerticalAlignment = VerticalAlignment.Center };
        var contenidoBoton  = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        contenidoBoton.Children.Add(new FontIcon { Glyph = "\uE7B8", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        contenidoBoton.Children.Add(new TextBlock { Text = "Ver detalle", VerticalAlignment = VerticalAlignment.Center });
        botonVer.Content = contenidoBoton;
        botonVer.Click  += async (_, _) => await MostrarDetalleCompraAsync(juegoCapturado);
        Grid.SetColumn(botonVer, 2);

        grid.Children.Add(bordeIcono);
        grid.Children.Add(panelInfo);
        grid.Children.Add(botonVer);
        tarjeta.Child = grid;
        return tarjeta;
    }

    private string ordenResenias = "fecha"; 

    private static List<Resenia> ObtenerReseniasSincronizadas(Usuario usuario) =>
        usuario.Resenias
            .Select(r =>
            {
                var juegoRef = ServicioJuegos.ObtenerPorId(r.IdJuego);
                return juegoRef?.Resenias.FirstOrDefault(rj =>
                    rj.NombreUsuario == r.NombreUsuario) ?? r;
            })
            .ToList();

    private void RenderizarResenias(List<Resenia> resenias)
    {

        var filaTitulo = new Grid();
        filaTitulo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filaTitulo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var panelTitulo = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        panelTitulo.Children.Add(new FontIcon { Glyph = "\uE8F2", FontSize = 18, Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush });
        panelTitulo.Children.Add(new TextBlock { Text = "Mis reseñas", Style = Application.Current.Resources["H2Style"] as Style, FontSize = 18 });
        Grid.SetColumn(panelTitulo, 0);

        var cmbOrden = new ComboBox { FontSize = 12, Padding = new Thickness(8, 4, 8, 4) };
        cmbOrden.Items.Add(new ComboBoxItem { Content = "Ordenar por fecha",    Tag = "fecha" });
        cmbOrden.Items.Add(new ComboBoxItem { Content = "Más likes primero",    Tag = "megusta" });
        cmbOrden.Items.Add(new ComboBoxItem { Content = "Más dislikes primero", Tag = "nomegusta" });
        cmbOrden.SelectedIndex = ordenResenias == "megusta" ? 1 : ordenResenias == "nomegusta" ? 2 : 0;
        cmbOrden.SelectionChanged += (_, _) =>
        {
            var seleccionado = cmbOrden.SelectedItem as ComboBoxItem;
            ordenResenias = seleccionado?.Tag?.ToString() ?? "fecha";
            var usuario2 = ServicioAutenticacion.UsuarioActual;
            if (usuario2 is not null)
            {
                ProfileContentPanel.Children.Clear();
                RenderizarResenias(ObtenerReseniasSincronizadas(usuario2));
            }
        };
        Grid.SetColumn(cmbOrden, 1);

        filaTitulo.Children.Add(panelTitulo);
        filaTitulo.Children.Add(cmbOrden);
        filaTitulo.Margin = new Thickness(0, 0, 0, 4);
        ProfileContentPanel.Children.Add(filaTitulo);

        if (resenias.Count == 0)
        {
            var panelVacio = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0), Spacing = 12 };
            panelVacio.Children.Add(new FontIcon { Glyph = "\uE8F2", FontSize = 40, Foreground = Application.Current.Resources["MediumGrayBrush"] as SolidColorBrush, HorizontalAlignment = HorizontalAlignment.Center });
            panelVacio.Children.Add(new TextBlock { Text = "Aún no has escrito ninguna reseña.", Style = Application.Current.Resources["SecondaryTextStyle"] as Style, HorizontalAlignment = HorizontalAlignment.Center });
            ProfileContentPanel.Children.Add(panelVacio);
            return;
        }


        IEnumerable<Resenia> reseniasSorted = ordenResenias switch
        {
            "megusta"   => resenias.OrderByDescending(r => r.MeGusta),
            "nomegusta" => resenias.OrderByDescending(r => r.NoMeGusta),
            _           => resenias.AsEnumerable()
        };

        foreach (var resenia in reseniasSorted)
        {
            var juego      = ServicioJuegos.ObtenerPorId(resenia.IdJuego);
            string nombre  = juego?.Nombre ?? $"Juego #{resenia.IdJuego}";

            string fechaMostrada = resenia.Fecha;
            if (DateTime.TryParse(resenia.Fecha, out var fechaParsed))
                fechaMostrada = fechaParsed.ToString("dd/MM/yyyy");

            var tarjeta  = new Border { Style = Application.Current.Resources["CardStyle"] as Style, Padding = new Thickness(14, 12, 14, 12) };
            var contenido = new StackPanel { Spacing = 8 };

            var encabezado = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            encabezado.Children.Add(new TextBlock
            {
                Text = nombre, FontSize = 15, FontWeight = FontWeights.SemiBold,
                Foreground = Application.Current.Resources["PrimaryBlueBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center
            });
            var panelEstrellas = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            for (int i = 1; i <= 5; i++)
                panelEstrellas.Children.Add(new FontIcon
                {
                    Glyph    = i <= resenia.Estrellas ? "\uE735" : "\uE734",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(i <= resenia.Estrellas
                        ? Color.FromArgb(255, 245, 158, 11)
                        : Color.FromArgb(255, 209, 213, 219))
                });
            encabezado.Children.Add(panelEstrellas);
            encabezado.Children.Add(new TextBlock
            {
                Text = fechaMostrada, FontSize = 12,
                Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center
            });
            contenido.Children.Add(encabezado);

            contenido.Children.Add(new TextBlock
            {
                Text = resenia.Texto, FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush
            });

            var filaReacciones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            var bordeMeGusta = new Border
            {
                Background    = new SolidColorBrush(Color.FromArgb(255, 224, 233, 242)),
                CornerRadius  = new CornerRadius(12),
                Padding       = new Thickness(10, 4, 10, 4)
            };
            var contenidoMeGusta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            contenidoMeGusta.Children.Add(new FontIcon { Glyph = "\uE8E1", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) });
            contenidoMeGusta.Children.Add(new TextBlock
            {
                Text = $"{resenia.MeGusta} me gusta",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76))
            });
            bordeMeGusta.Child = contenidoMeGusta;

            var bordeNoMeGusta = new Border
            {
                Background   = resenia.NoMeGusta > 0
                    ? new SolidColorBrush(Color.FromArgb(255, 255, 240, 240))
                    : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(10, 4, 10, 4)
            };
            var contenidoNoMeGusta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            contenidoNoMeGusta.Children.Add(new FontIcon
            {
                Glyph = "\uE8E0", FontSize = 12,
                Foreground = new SolidColorBrush(resenia.NoMeGusta > 0
                    ? Color.FromArgb(255, 185, 28, 28)
                    : Color.FromArgb(255, 120, 120, 120))
            });
            contenidoNoMeGusta.Children.Add(new TextBlock
            {
                Text = $"{resenia.NoMeGusta} no me gusta",
                FontSize = 12, FontWeight = resenia.NoMeGusta > 0 ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(resenia.NoMeGusta > 0
                    ? Color.FromArgb(255, 185, 28, 28)
                    : Color.FromArgb(255, 120, 120, 120))
            });
            bordeNoMeGusta.Child = contenidoNoMeGusta;

            filaReacciones.Children.Add(bordeMeGusta);
            filaReacciones.Children.Add(bordeNoMeGusta);
            contenido.Children.Add(filaReacciones);

            tarjeta.Child = contenido;
            ProfileContentPanel.Children.Add(tarjeta);
        }
    }

    private void BotonRegresar_Click(object sender, RoutedEventArgs e) => AlRegresar?.Invoke();

    private static SolidColorBrush ObtenerPincelCategoria(string categoria) => categoria switch
    {
        "RPG"         => new SolidColorBrush(Color.FromArgb(255,  75,   0, 130)),
        "Shooter"     => new SolidColorBrush(Color.FromArgb(255, 183,  28,  28)),
        "MMORPG"      => new SolidColorBrush(Color.FromArgb(255,   0,  96, 100)),
        "Carreras"    => new SolidColorBrush(Color.FromArgb(255, 230,  81,   0)),
        "Aventura"    => new SolidColorBrush(Color.FromArgb(255,  33, 150,  83)),
        "Simulacion"  => new SolidColorBrush(Color.FromArgb(255,  46, 125,  50)),
        "Estrategia"  => new SolidColorBrush(Color.FromArgb(255,  49,  27, 146)),
        "Plataformas" => new SolidColorBrush(Color.FromArgb(255,  21, 101, 192)),
        "Terror"      => new SolidColorBrush(Color.FromArgb(255,  38,  50,  56)),
        "Puzzle"      => new SolidColorBrush(Color.FromArgb(255,   0, 131, 143)),
        _             => new SolidColorBrush(Color.FromArgb(255,  66,  66,  66))
    };

    private static string ObtenerGlifoCategoria(string categoria) => categoria switch
    {
        "RPG"         => "\uE97D",
        "Shooter"     => "\uE946",
        "MMORPG"      => "\uE77B",
        "Carreras"    => "\uE804",
        "Aventura"    => "\uE714",
        "Simulacion"  => "\uE8EF",
        "Estrategia"  => "\uE7C1",
        "Plataformas" => "\uE7FC",
        "Terror"      => "\uE7BA",
        "Puzzle"      => "\uE8E4",
        _             => "\uE7FC"
    };

    private async Task MostrarDetalleCompraAsync(Juego juego)
    {
        var usuario = ServicioAutenticacion.UsuarioActual;
        if (usuario is null) return;

        var registro = usuario.RegistrosCompras
            .Where(r => r.IdJuego == juego.Id)
            .OrderByDescending(r =>
            {
                if (DateTime.TryParseExact(r.Fecha, "dd/MM/yyyy HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt)) return dt;
                return DateTime.MinValue;
            })
            .FirstOrDefault();

        DateTime fechaCompra = DateTime.Now;
        if (registro is not null)
            DateTime.TryParseExact(registro.Fecha, "dd/MM/yyyy HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out fechaCompra);

        string[] meses = { "enero","febrero","marzo","abril","mayo","junio",
                           "julio","agosto","septiembre","octubre","noviembre","diciembre" };
        string fechaLegible = $"{fechaCompra.Day} de {meses[fechaCompra.Month - 1]} de {fechaCompra.Year}";
        string horaLegible  = fechaCompra.ToString("HH:mm");
        string digitos      = string.IsNullOrEmpty(registro?.UltimosDigitos) ? "----" : registro.UltimosDigitos;

        var panelPrincipal = new StackPanel { Spacing = 0, Width = 400 };

        var bordeImagen = new Border
        {
            Width = 88, Height = 88, CornerRadius = new CornerRadius(14),
            Background = ObtenerPincelCategoria(juego.Categoria),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        string rutaCompleta = string.IsNullOrEmpty(juego.UrlImagen) ? string.Empty
            : Path.Combine(AppContext.BaseDirectory, juego.UrlImagen);
        if (File.Exists(rutaCompleta))
        {
            try { bordeImagen.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaCompleta)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
            catch { }
        }
        else
        {
            bordeImagen.Child = new FontIcon { Glyph = ObtenerGlifoCategoria(juego.Categoria), FontSize = 38, Foreground = new SolidColorBrush(Colors.White), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }
        panelPrincipal.Children.Add(bordeImagen);

        panelPrincipal.Children.Add(new TextBlock
        {
            Text = juego.Nombre, FontSize = 20, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 24, 56, 76)),
            Margin = new Thickness(0, 0, 0, 4)
        });
        panelPrincipal.Children.Add(new TextBlock
        {
            Text = $"${juego.Precio:N0} MXN", FontSize = 22, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)),
            Margin = new Thickness(0, 0, 0, 20)
        });

        var panelDetalles = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 245, 247, 250)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 16, 20, 16),
            Margin = new Thickness(0, 0, 0, 0)
        };

        var stackDetalles = new StackPanel { Spacing = 0 };

        stackDetalles.Children.Add(CrearTituloSeccion("Detalles de tu compra"));
        stackDetalles.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 218, 224, 232)), Margin = new Thickness(0, 8, 0, 14) });

        stackDetalles.Children.Add(CrearFilaDetalle("\uE787", "Fecha", fechaLegible));
        stackDetalles.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 230, 234, 240)), Margin = new Thickness(32, 10, 0, 10) });
        stackDetalles.Children.Add(CrearFilaDetalle("\uE823", "Hora", horaLegible));
        stackDetalles.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 230, 234, 240)), Margin = new Thickness(32, 10, 0, 10) });
        stackDetalles.Children.Add(CrearFilaDetalle("\uE8A1", "Total pagado", $"${juego.Precio:N0} MXN"));
        stackDetalles.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 230, 234, 240)), Margin = new Thickness(32, 10, 0, 10) });
        stackDetalles.Children.Add(CrearFilaDetalle("\uE8C7", "Método de pago", $"Tarjeta  ****  {digitos}"));

        panelDetalles.Child = stackDetalles;
        panelPrincipal.Children.Add(panelDetalles);

        var dialogo = new ContentDialog
        {
            Content = panelPrincipal,
            CloseButtonText = "Cerrar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };
        AplicarColoresBotonCerrar(dialogo);

        await dialogo.ShowAsync();
    }

    private static Style EstiloBotonOscuro()
    {
        var estilo = new Style(typeof(Button));
        estilo.Setters.Add(new Setter(Button.BackgroundProperty,              new SolidColorBrush(Color.FromArgb(255, 26, 44, 61))));
        estilo.Setters.Add(new Setter(Button.ForegroundProperty,              new SolidColorBrush(Colors.White)));
        estilo.Setters.Add(new Setter(Button.BorderThicknessProperty,         new Thickness(0)));
        estilo.Setters.Add(new Setter(Button.PaddingProperty,                 new Thickness(24, 10, 24, 10)));
        estilo.Setters.Add(new Setter(Button.CornerRadiusProperty,            new CornerRadius(8)));
        estilo.Setters.Add(new Setter(Button.HorizontalAlignmentProperty,     HorizontalAlignment.Center));
        estilo.Setters.Add(new Setter(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        return estilo;
    }

    private static void AplicarColoresBotonCerrar(ContentDialog dialogo)
    {
        var fondoOscuro  = new SolidColorBrush(Color.FromArgb(255, 26, 44, 61));
        var fondoHover   = new SolidColorBrush(Color.FromArgb(255, 36, 58, 80));
        var fondoPress   = new SolidColorBrush(Color.FromArgb(255, 18, 32, 46));
        var blanco       = new SolidColorBrush(Colors.White);
        var transpar     = new SolidColorBrush(Colors.Transparent);

        dialogo.Resources["ContentDialogCloseButtonBackground"]                 = fondoOscuro;
        dialogo.Resources["ContentDialogCloseButtonBackgroundPointerOver"]      = fondoHover;
        dialogo.Resources["ContentDialogCloseButtonBackgroundPressed"]          = fondoPress;
        dialogo.Resources["ContentDialogCloseButtonForeground"]                 = blanco;
        dialogo.Resources["ContentDialogCloseButtonForegroundPointerOver"]      = blanco;
        dialogo.Resources["ContentDialogCloseButtonForegroundPressed"]          = blanco;
        dialogo.Resources["ContentDialogCloseButtonBorderBrush"]                = transpar;
        dialogo.Resources["ContentDialogCloseButtonBorderBrushPointerOver"]     = transpar;
        dialogo.Resources["ContentDialogCloseButtonBorderBrushPressed"]         = transpar;
        dialogo.Resources["ContentDialogButtonMinWidth"]                        = 200.0;
    }

    private static TextBlock CrearTituloSeccion(string texto) =>
        new TextBlock
        {
            Text = texto, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 90, 106, 126)),
            Margin = new Thickness(0, 0, 0, 0)
        };

    private static StackPanel CrearFilaDetalle(string glifo, string etiqueta, string valor)
    {
        var fila = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };

        var icono = new FontIcon
        {
            Glyph = glifo, FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)),
            VerticalAlignment = VerticalAlignment.Center,
            Width = 32
        };
        fila.Children.Add(icono);

        var panelTexto = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 1 };
        panelTexto.Children.Add(new TextBlock
        {
            Text = etiqueta, FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 130, 145, 165)),
            FontWeight = FontWeights.Normal
        });
        panelTexto.Children.Add(new TextBlock
        {
            Text = valor, FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 24, 40, 60))
        });
        fila.Children.Add(panelTexto);

        return fila;
    }
}
