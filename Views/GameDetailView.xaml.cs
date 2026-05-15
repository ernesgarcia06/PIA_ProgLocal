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

public sealed partial class GameDetailView : UserControl
{
    public event Action? AlRegresar;
    public event Action? AlIrAComprados;
    public event Action? AlIrATienda;

    public enum OrigenVista { Tienda, Reporte, Perfil }
    private OrigenVista origenActual = OrigenVista.Tienda;
    public OrigenVista OrigenActual => origenActual;

    public void EstablecerOrigen(OrigenVista origen)
    {
        origenActual = origen;
        ActualizarTextoRegresar();
    }

    private void ActualizarTextoRegresar()
    {
        if (TxtTextoRegresar is null) return;
        TxtTextoRegresar.Text = origenActual switch
        {
            OrigenVista.Reporte => "Volver al reporte",
            OrigenVista.Perfil  => "Volver al perfil",
            _                   => "Volver a la Tienda"
        };
    }

    private Juego? juegoActual;
    private int estrellasSeleccionadas = 0;
    private readonly List<Button> botonesEstrellas = new();

    public GameDetailView() => this.InitializeComponent();

    public void CargarJuego(Juego juego)
    {
        juegoActual = juego;
        ActualizarTextoRegresar();
        MostrarInformacionJuego();
        InicializarEstrellas();
        RenderizarResenias();
        RenderizarGaleriaFotos();
        CargarReseniaExistente();
    }

    private void MostrarInformacionJuego()
    {
        if (juegoActual is null) return;

        TxtGameName.Text         = juegoActual.Nombre;
        TxtGameYear.Text         = juegoActual.Anio.ToString();
        TxtGameCategory.Text     = juegoActual.Categoria;
        TxtGameDescription.Text  = juegoActual.Descripcion;
        TxtGamePrice.Text        = $"${juegoActual.Precio:N0} MXN";
        TxtGameCategoryBadge.Text = juegoActual.Categoria;
        GameBanner.Background    = ObtenerPincelCategoria(juegoActual.Categoria);
        GameCategoryIcon.Glyph  = ObtenerGlifoCategoria(juegoActual.Categoria);

        bool tieneImagenLocal = !string.IsNullOrEmpty(juegoActual.UrlImagen)
            && !juegoActual.UrlImagen.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        if (tieneImagenLocal)
        {
            string rutaCompleta = Path.Combine(AppContext.BaseDirectory, juegoActual.UrlImagen);
            if (File.Exists(rutaCompleta))
            {
                try
                {
                    GameBanner.Child = new Image
                    {
                        Source              = new BitmapImage(new Uri(rutaCompleta)),
                        Stretch             = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment   = VerticalAlignment.Center,
                        MaxWidth            = 240, MaxHeight = 200
                    };
                }
                catch { RestaurarIconoBanner(); }
            }
            else RestaurarIconoBanner();
        }
        else RestaurarIconoBanner();

        if (juegoActual.Resenias.Count > 0)
        {
            TxtGameRating.Text = $"{juegoActual.CalificacionPromedio:F1} ({juegoActual.Resenias.Count} reseñas)";
            RatingDisplay.Visibility = Visibility.Visible;
        }
        else RatingDisplay.Visibility = Visibility.Collapsed;

        bool enListaDeseos = ServicioAutenticacion.EstaEnListaDeseos(juegoActual.Id);
        ActualizarBotonListaDeseos(enListaDeseos);

        bool comprado = ServicioAutenticacion.HaComprado(juegoActual.Id);
        PurchasedBadge.Visibility          = comprado ? Visibility.Visible   : Visibility.Collapsed;
        BtnBuy.Visibility                  = comprado ? Visibility.Collapsed : Visibility.Visible;
        PanelFormularioResenia.Visibility  = comprado ? Visibility.Visible   : Visibility.Collapsed;
        PanelReseniaNoDisponible.Visibility = comprado ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RestaurarIconoBanner()
    {
        if (juegoActual is null) return;
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 10 };
        panel.Children.Add(new FontIcon { Glyph = ObtenerGlifoCategoria(juegoActual.Categoria), FontSize = 52, Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = juegoActual.Categoria, FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.9 });
        GameBanner.Child = panel;
    }

    private void ActualizarBotonListaDeseos(bool enLista)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new FontIcon { Glyph = enLista ? "\uEB52" : "\uEB51", FontSize = 14, Foreground = new SolidColorBrush(enLista ? Color.FromArgb(255, 211, 47, 47) : Color.FromArgb(255, 36, 56, 76)), VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = enLista ? "Quitar de lista" : "Lista de deseos", VerticalAlignment = VerticalAlignment.Center });
        BtnWishlist.Content = panel;
    }

    private void InicializarEstrellas()
    {
        StarPanel.Children.Clear();
        botonesEstrellas.Clear();
        estrellasSeleccionadas = 0;

        for (int i = 1; i <= 5; i++)
        {
            int valorEstrella = i;
            var icono = new FontIcon { Glyph = "\uE734", FontSize = 26, Foreground = new SolidColorBrush(Color.FromArgb(255, 209, 213, 219)) };
            var boton = new Button { Content = icono, Background = new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(0), Padding = new Thickness(4, 0, 4, 0) };
            boton.Click += (_, _) => SeleccionarEstrellas(valorEstrella);
            StarPanel.Children.Add(boton);
            botonesEstrellas.Add(boton);
        }
    }

    private void SeleccionarEstrellas(int cantidad)
    {
        estrellasSeleccionadas = cantidad;
        for (int i = 0; i < botonesEstrellas.Count; i++)
        {
            if (botonesEstrellas[i].Content is FontIcon fi)
            {
                bool activo = i < cantidad;
                fi.Glyph     = activo ? "\uE735" : "\uE734";
                fi.Foreground = new SolidColorBrush(activo ? Color.FromArgb(255, 245, 158, 11) : Color.FromArgb(255, 209, 213, 219));
            }
        }
    }

    private void CargarReseniaExistente()
    {
        if (juegoActual is null) return;
        var reseniaExistente = ServicioAutenticacion.ObtenerReseniaUsuarioParaJuego(juegoActual.Id);
        if (reseniaExistente is not null)
        {
            SeleccionarEstrellas(reseniaExistente.Estrellas);
            TxtReviewText.Text = reseniaExistente.Texto;
            ActualizarBotonPublicar("Actualizar Reseña");
        }
        else
        {
            TxtReviewText.Text = string.Empty;
            ActualizarBotonPublicar("Publicar Reseña");
        }
        ReviewStatusBorder.Visibility = Visibility.Collapsed;
    }

    private void ActualizarBotonPublicar(string etiqueta)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new FontIcon { Glyph = "\uE725", FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(new TextBlock { Text = etiqueta, VerticalAlignment = VerticalAlignment.Center });
        BtnSubmitReview.Content = panel;
    }

    private async void BtnSubmitReview_Click(object sender, RoutedEventArgs e)
    {
        if (juegoActual is null) return;
        if (!ServicioAutenticacion.HaComprado(juegoActual.Id))
        { MostrarEstadoResenia("Debes comprar el juego para poder reseñarlo.", error: true); return; }
        if (estrellasSeleccionadas == 0) { MostrarEstadoResenia("Por favor selecciona una calificación.", error: true); return; }
        if (string.IsNullOrWhiteSpace(TxtReviewText.Text)) { MostrarEstadoResenia("Por favor escribe un comentario.", error: true); return; }

        try
        {
            var resenia = new Resenia
            {
                IdJuego       = juegoActual.Id,
                NombreUsuario = ServicioAutenticacion.UsuarioActual?.NombreUsuario ?? "usuario",
                Estrellas     = estrellasSeleccionadas,
                Texto         = TxtReviewText.Text.Trim(),
                MeGusta       = 0, NoMeGusta = 0,
                Fecha         = DateTime.Now.ToString("dd/MM/yyyy")
            };

            await ServicioAutenticacion.AgregarOActualizarReseniaAsync(resenia);

            var usuarios = await ServicioDatos.CargarUsuariosAsync();
            ServicioJuegos.ActualizarResenias(usuarios);

            var juegoActualizado = ServicioJuegos.ObtenerPorId(juegoActual.Id);
            if (juegoActualizado is not null) { juegoActual = juegoActualizado; MostrarInformacionJuego(); RenderizarResenias(); }

            TxtReviewText.Text = string.Empty;
            InicializarEstrellas();
            ActualizarBotonPublicar("Actualizar Reseña");
            MostrarEstadoResenia("Reseña publicada correctamente.", error: false);
        }
        catch (Exception ex) { MostrarEstadoResenia($"Error al guardar: {ex.Message}", error: true); }
    }

    private void MostrarEstadoResenia(string mensaje, bool error)
    {
        TxtReviewStatus.Text = mensaje;
        ReviewStatusBorder.Background = new SolidColorBrush(error ? Color.FromArgb(255, 255, 245, 245) : Color.FromArgb(255, 232, 245, 233));
        ReviewStatusBorder.BorderBrush = error ? Application.Current.Resources["ErrorBrush"] as SolidColorBrush : Application.Current.Resources["SuccessBrush"] as SolidColorBrush;
        ReviewStatusBorder.BorderThickness = new Thickness(1);
        ReviewStatusIcon.Glyph     = error ? "\uE783" : "\uE930";
        ReviewStatusIcon.Foreground = error ? Application.Current.Resources["ErrorBrush"] as SolidColorBrush : Application.Current.Resources["SuccessBrush"] as SolidColorBrush;
        TxtReviewStatus.Foreground  = ReviewStatusIcon.Foreground;
        ReviewStatusBorder.Visibility = Visibility.Visible;
    }

    private void RenderizarResenias()
    {
        ReviewsPanel.Children.Clear();
        if (juegoActual is null || juegoActual.Resenias.Count == 0)
        { ReviewsEmptyState.Visibility = Visibility.Visible; return; }
        ReviewsEmptyState.Visibility = Visibility.Collapsed;
        foreach (var resenia in juegoActual.Resenias)
            ReviewsPanel.Children.Add(CrearTarjetaResenia(resenia));
    }

    private Border CrearTarjetaResenia(Resenia resenia)
    {
        var tarjeta = new Border { Style = Application.Current.Resources["CardStyle"] as Style, Padding = new Thickness(14, 12, 14, 12) };
        var contenido = new StackPanel { Spacing = 8 };

        var encabezado = new Grid();
        encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var panelUsuario = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panelUsuario.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 15, Foreground = new SolidColorBrush(Color.FromArgb(255, 97, 97, 97)), VerticalAlignment = VerticalAlignment.Center });
        panelUsuario.Children.Add(new TextBlock { Text = resenia.NombreUsuario, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush, VerticalAlignment = VerticalAlignment.Center });
        panelUsuario.Children.Add(new TextBlock { Text = resenia.Fecha, FontSize = 12, Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(panelUsuario, 0);

        var panelEstrellas = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        for (int i = 1; i <= 5; i++)
            panelEstrellas.Children.Add(new FontIcon { Glyph = i <= resenia.Estrellas ? "\uE735" : "\uE734", FontSize = 13, Foreground = new SolidColorBrush(i <= resenia.Estrellas ? Color.FromArgb(255, 245, 158, 11) : Color.FromArgb(255, 209, 213, 219)) });
        Grid.SetColumn(panelEstrellas, 1);

        encabezado.Children.Add(panelUsuario);
        encabezado.Children.Add(panelEstrellas);

        var textoResenia = new TextBlock { Text = resenia.Texto, FontSize = 14, TextWrapping = TextWrapping.Wrap, Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush };

        var panelReacciones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var reseniaCapturada = resenia;
        string usuarioActual = ServicioAutenticacion.UsuarioActual?.NombreUsuario ?? string.Empty;

        Button? botonMeGusta = null, botonNoMeGusta = null;
        botonMeGusta   = new Button { BorderThickness = new Thickness(0), Padding = new Thickness(10, 5, 10, 5), CornerRadius = new CornerRadius(6), FontSize = 12 };
        botonNoMeGusta = new Button { BorderThickness = new Thickness(1), Padding = new Thickness(10, 5, 10, 5), CornerRadius = new CornerRadius(6), FontSize = 12 };

        string reaccionActual = reseniaCapturada.Reacciones.TryGetValue(usuarioActual, out string? r) ? r : string.Empty;
        ActualizarBotonMeGusta(botonMeGusta, reseniaCapturada.MeGusta, reaccionActual == "like");
        ActualizarBotonNoMeGusta(botonNoMeGusta, reseniaCapturada.NoMeGusta, reaccionActual == "dislike");

        botonMeGusta.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(usuarioActual)) return;
            string actual = reseniaCapturada.Reacciones.TryGetValue(usuarioActual, out string? rv) ? rv : string.Empty;
            if (actual == "like")
            {
                reseniaCapturada.MeGusta = Math.Max(0, reseniaCapturada.MeGusta - 1);
                reseniaCapturada.Reacciones.Remove(usuarioActual);
                ActualizarBotonMeGusta(botonMeGusta!, reseniaCapturada.MeGusta, false);
            }
            else
            {
                if (actual == "dislike")
                {
                    reseniaCapturada.NoMeGusta = Math.Max(0, reseniaCapturada.NoMeGusta - 1);
                    ActualizarBotonNoMeGusta(botonNoMeGusta!, reseniaCapturada.NoMeGusta, false);
                }
                reseniaCapturada.MeGusta++;
                reseniaCapturada.Reacciones[usuarioActual] = "like";
                ActualizarBotonMeGusta(botonMeGusta!, reseniaCapturada.MeGusta, true);
            }
            await ServicioAutenticacion.GuardarTodosLosUsuariosAsync();
        };

        botonNoMeGusta.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(usuarioActual)) return;
            string actual = reseniaCapturada.Reacciones.TryGetValue(usuarioActual, out string? rv) ? rv : string.Empty;
            if (actual == "dislike")
            {
                reseniaCapturada.NoMeGusta = Math.Max(0, reseniaCapturada.NoMeGusta - 1);
                reseniaCapturada.Reacciones.Remove(usuarioActual);
                ActualizarBotonNoMeGusta(botonNoMeGusta!, reseniaCapturada.NoMeGusta, false);
            }
            else
            {
                if (actual == "like")
                {
                    reseniaCapturada.MeGusta = Math.Max(0, reseniaCapturada.MeGusta - 1);
                    ActualizarBotonMeGusta(botonMeGusta!, reseniaCapturada.MeGusta, false);
                }
                reseniaCapturada.NoMeGusta++;
                reseniaCapturada.Reacciones[usuarioActual] = "dislike";
                ActualizarBotonNoMeGusta(botonNoMeGusta!, reseniaCapturada.NoMeGusta, true);
            }
            await ServicioAutenticacion.GuardarTodosLosUsuariosAsync();
        };

        panelReacciones.Children.Add(botonMeGusta);
        panelReacciones.Children.Add(botonNoMeGusta);

        contenido.Children.Add(encabezado);
        contenido.Children.Add(textoResenia);
        contenido.Children.Add(panelReacciones);
        tarjeta.Child = contenido;
        return tarjeta;
    }

    private static void ActualizarBotonMeGusta(Button boton, int cantidad, bool activo)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new FontIcon { Glyph = "\uE8E1", FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(activo ? Color.FromArgb(255, 36, 56, 76) : Color.FromArgb(255, 97, 97, 97)) });
        panel.Children.Add(new TextBlock { Text = cantidad.ToString(), VerticalAlignment = VerticalAlignment.Center });
        boton.Content    = panel;
        boton.Background = new SolidColorBrush(activo ? Color.FromArgb(255, 224, 233, 242) : Color.FromArgb(255, 248, 250, 252));
    }

    private static void ActualizarBotonNoMeGusta(Button boton, int cantidad, bool activo)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        panel.Children.Add(new FontIcon { Glyph = "\uE8E0", FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(activo ? Color.FromArgb(255, 185, 28, 28) : Color.FromArgb(255, 97, 97, 97)) });
        panel.Children.Add(new TextBlock { Text = cantidad.ToString(), VerticalAlignment = VerticalAlignment.Center });
        boton.Content    = panel;
        boton.Background = new SolidColorBrush(activo ? Color.FromArgb(255, 255, 240, 240) : Color.FromArgb(255, 248, 250, 252));
    }

    private void RenderizarGaleriaFotos()
    {
        if (juegoActual is null) return;
        var fotos = juegoActual.Fotos;

        if (fotos is null || fotos.Count == 0)
        {
            FotosVaciaPlaceholder.Visibility = Visibility.Visible;
            VisorFotoBorder.Visibility       = Visibility.Collapsed;
            GaleriaMiniaturasPanel.ItemsSource = null;
            TxtPhotosCount.Text = string.Empty;
            return;
        }

        FotosVaciaPlaceholder.Visibility = Visibility.Collapsed;
        TxtPhotosCount.Text = $"({fotos.Count} foto{(fotos.Count != 1 ? "s" : "")})";

        MostrarFotoEnVisor(fotos[0]);

        var miniaturas = new List<Border>();
        for (int i = 0; i < fotos.Count; i++)
        {
            int indiceCapturado   = i;
            string rutaRelativa   = fotos[i];
            string rutaCompleta   = Path.Combine(AppContext.BaseDirectory, rutaRelativa);

            var miniatura = new Border
            {
                Width = 120, Height = 120,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Background  = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200))
            };

            if (File.Exists(rutaCompleta))
            {
                try { miniatura.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaCompleta)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
                catch { }
            }

            miniatura.Tapped += (_, _) =>
            {
                foreach (var item in miniaturas)
                    item.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                miniatura.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76));
                MostrarFotoEnVisor(rutaRelativa);
            };

            miniaturas.Add(miniatura);
        }

        GaleriaMiniaturasPanel.ItemsSource = miniaturas;

        if (miniaturas.Count > 0)
            miniaturas[0].BorderBrush = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76));
    }

    private void MostrarFotoEnVisor(string rutaRelativa)
    {
        string rutaCompleta = Path.Combine(AppContext.BaseDirectory, rutaRelativa);
        if (!File.Exists(rutaCompleta)) return;
        try
        {
            ImgVisorFoto.Source = new BitmapImage(new Uri(rutaCompleta));
            VisorFotoBorder.Visibility = Visibility.Visible;
        }
        catch { }
    }

    private async void BtnWishlist_Click(object sender, RoutedEventArgs e)
    {
        if (juegoActual is null) return;
        await ServicioAutenticacion.AlternarListaDeseosAsync(juegoActual.Id);
        ActualizarBotonListaDeseos(ServicioAutenticacion.EstaEnListaDeseos(juegoActual.Id));
    }

    private async void BtnBuy_Click(object sender, RoutedEventArgs e)
    {
        if (juegoActual is null) return;
        await MostrarConfirmacionCompraAsync();
    }

    private async Task MostrarConfirmacionCompraAsync()
    {
        var panelContenido = new StackPanel { Spacing = 20, Width = 380 };

        var panelJuego = new Grid();
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bannerMini = new Border
        {
            Width = 80, Height = 80, CornerRadius = new CornerRadius(12),
            Background = ObtenerPincelCategoria(juegoActual!.Categoria)
        };
        string rutaImg = string.IsNullOrEmpty(juegoActual.UrlImagen) ? string.Empty
            : System.IO.Path.Combine(AppContext.BaseDirectory, juegoActual.UrlImagen);
        if (System.IO.File.Exists(rutaImg))
        {
            try { bannerMini.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaImg)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
            catch { }
        }
        Grid.SetColumn(bannerMini, 0);

        var panelInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 0, 0), Spacing = 4 };
        panelInfo.Children.Add(new TextBlock { Text = juegoActual.Nombre, FontSize = 16, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)) });
        panelInfo.Children.Add(new TextBlock { Text = $"${juegoActual.Precio:N0} MXN", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) });
        Grid.SetColumn(panelInfo, 1);

        panelJuego.Children.Add(bannerMini);
        panelJuego.Children.Add(panelInfo);
        panelContenido.Children.Add(panelJuego);

        panelContenido.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)) });
        panelContenido.Children.Add(new TextBlock { Text = "¿Deseas continuar con la compra?", FontSize = 14, Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)), TextWrapping = TextWrapping.Wrap });

        var dialogo = new ContentDialog
        {
            Title = "Confirmar compra",
            Content = panelContenido,
            PrimaryButtonText = "Continuar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.None,
            XamlRoot = this.XamlRoot
        };
        AplicarColoresBotonOscuro(dialogo);

        var resultado = await dialogo.ShowAsync();
        if (resultado == ContentDialogResult.Primary)
            await MostrarFormularioPagoAsync();
    }

    private async Task MostrarFormularioPagoAsync()
    {
        var panelPrincipal = new StackPanel { Spacing = 16, Width = 420 };

        var panelJuego = new Grid { Padding = new Thickness(14), Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)) };
        panelJuego.CornerRadius = new CornerRadius(10);
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bannerMini = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(8), Background = ObtenerPincelCategoria(juegoActual!.Categoria), VerticalAlignment = VerticalAlignment.Center };
        string rutaImg = string.IsNullOrEmpty(juegoActual.UrlImagen) ? string.Empty
            : System.IO.Path.Combine(AppContext.BaseDirectory, juegoActual.UrlImagen);
        if (System.IO.File.Exists(rutaImg))
        {
            try { bannerMini.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaImg)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
            catch { }
        }
        Grid.SetColumn(bannerMini, 0);

        var panelInfoJuego = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0), Spacing = 2 };
        panelInfoJuego.Children.Add(new TextBlock { Text = juegoActual.Nombre, FontSize = 14, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panelInfoJuego.Children.Add(new TextBlock { Text = $"${juegoActual.Precio:N0} MXN", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) });
        Grid.SetColumn(panelInfoJuego, 1);
        panelJuego.Children.Add(bannerMini);
        panelJuego.Children.Add(panelInfoJuego);
        panelPrincipal.Children.Add(panelJuego);

        panelPrincipal.Children.Add(new TextBlock { Text = "Datos de pago", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });

        var txtTarjeta = new TextBox { PlaceholderText = "1234 5678 9012 3456", MaxLength = 19, FontSize = 15, Padding = new Thickness(12, 10, 12, 10) };
        CrearEtiquetaYCampo(panelPrincipal, "Número de tarjeta", txtTarjeta);
        txtTarjeta.TextChanged += (_, _) =>
        {
            string soloNums = new string(txtTarjeta.Text.Where(c => char.IsDigit(c)).ToArray());
            if (soloNums.Length > 16) soloNums = soloNums[..16];
            string formateado = string.Join(" ", Enumerable.Range(0, (soloNums.Length + 3) / 4)
                .Select(i => soloNums.Substring(i * 4, Math.Min(4, soloNums.Length - i * 4))));
            if (txtTarjeta.Text != formateado)
            {
                txtTarjeta.Text = formateado;
                txtTarjeta.SelectionStart = formateado.Length;
            }
        };

        var txtTitular = new TextBox { PlaceholderText = "Nombre como aparece en la tarjeta", MaxLength = 40, FontSize = 15, Padding = new Thickness(12, 10, 12, 10) };
        CrearEtiquetaYCampo(panelPrincipal, "Nombre del titular", txtTitular);
        txtTitular.TextChanged += (_, _) =>
        {
            string original = txtTitular.Text;
            string filtrado = new string(original.Where(c => char.IsLetter(c) || c == ' ').ToArray());
            if (filtrado.Length > 0 && filtrado[0] == ' ') filtrado = filtrado.TrimStart();
            while (filtrado.Contains("  ")) filtrado = filtrado.Replace("  ", " ");
            if (filtrado != original) { int pos = txtTitular.SelectionStart; txtTitular.Text = filtrado; txtTitular.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length)); }
        };

        var panelExpCvv = new Grid { ColumnSpacing = 12 };
        panelExpCvv.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panelExpCvv.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var txtExpiracion = new TextBox { PlaceholderText = "MM/AA", MaxLength = 5, FontSize = 15, Padding = new Thickness(12, 10, 12, 10) };
        txtExpiracion.TextChanged += (_, _) =>
        {
            string original = txtExpiracion.Text;
            string soloNums = new string(original.Where(char.IsDigit).ToArray());
            if (soloNums.Length > 4) soloNums = soloNums[..4];
            if (soloNums.Length >= 2)
            {
                int mes = int.Parse(soloNums[..2]);
                if (mes > 12) soloNums = "12" + soloNums[2..];
                if (mes < 1 && soloNums.Length == 2) soloNums = "01";
            }
            string formateado = soloNums.Length > 2 ? $"{soloNums[..2]}/{soloNums[2..]}" : soloNums;
            if (txtExpiracion.Text != formateado) { txtExpiracion.Text = formateado; txtExpiracion.SelectionStart = formateado.Length; }
        };

        var txtCvv = new TextBox { PlaceholderText = "CVV", MaxLength = 3, FontSize = 15, Padding = new Thickness(12, 10, 12, 10) };
        txtCvv.TextChanged += (_, _) =>
        {
            string original = txtCvv.Text;
            string filtrado = new string(original.Where(char.IsDigit).ToArray());
            if (filtrado != original) { txtCvv.Text = filtrado; txtCvv.SelectionStart = filtrado.Length; }
        };

        var colExp = new StackPanel { Spacing = 6 };
        colExp.Children.Add(new TextBlock { Text = "Fecha de expiración", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });
        colExp.Children.Add(txtExpiracion);
        Grid.SetColumn(colExp, 0);

        var colCvv = new StackPanel { Spacing = 6 };
        colCvv.Children.Add(new TextBlock { Text = "CVV", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });
        colCvv.Children.Add(txtCvv);
        Grid.SetColumn(colCvv, 1);

        panelExpCvv.Children.Add(colExp);
        panelExpCvv.Children.Add(colCvv);
        panelPrincipal.Children.Add(panelExpCvv);

        var txtError = new TextBlock { Foreground = new SolidColorBrush(Color.FromArgb(255, 185, 28, 28)), FontSize = 12, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
        panelPrincipal.Children.Add(txtError);

        var dialogoPago = new ContentDialog
        {
            Title = "Método de pago",
            Content = panelPrincipal,
            PrimaryButtonText = "Pagar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.None,
            XamlRoot = this.XamlRoot
        };
        AplicarColoresBotonOscuro(dialogoPago);

        dialogoPago.PrimaryButtonClick += async (dlg, args) =>
        {
            args.Cancel = true;

            string numTarjeta = new string(txtTarjeta.Text.Where(char.IsDigit).ToArray());
            string titular    = txtTitular.Text.Trim();
            string expiracion = txtExpiracion.Text.Trim();
            string cvv        = txtCvv.Text.Trim();

            if (numTarjeta.Length != 16)
            { MostrarErrorPago(txtError, "El número de tarjeta debe tener 16 dígitos."); return; }
            if (string.IsNullOrWhiteSpace(titular) || titular.Length < 3)
            { MostrarErrorPago(txtError, "Ingresa el nombre del titular (mínimo 3 caracteres)."); return; }
            if (!ValidarExpiracion(expiracion))
            { MostrarErrorPago(txtError, "Fecha de expiración inválida. Usa el formato MM/AA con un mes y año vigentes."); return; }
            if (cvv.Length != 3)
            { MostrarErrorPago(txtError, "El CVV debe tener exactamente 3 dígitos."); return; }

            txtError.Visibility = Visibility.Collapsed;
            dialogoPago.IsPrimaryButtonEnabled = false;
            dialogoPago.IsSecondaryButtonEnabled = false;

            var panelProcesando = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 16, Margin = new Thickness(0, 20, 0, 20) };
            panelProcesando.Children.Add(new ProgressRing { IsActive = true, Width = 48, Height = 48, Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) });
            panelProcesando.Children.Add(new TextBlock { Text = "Procesando pago...", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });

            dialogoPago.Content = panelProcesando;

            await Task.Delay(2200);

            dlg.Hide();

            string numTarjetaCompleto = new string(txtTarjeta.Text.Where(char.IsDigit).ToArray());
            string ultimosDigitos = numTarjetaCompleto.Length >= 4
                ? numTarjetaCompleto[^4..] : numTarjetaCompleto;

            await ServicioAutenticacion.AgregarJuegoCompradoAsync(juegoActual!.Id, ultimosDigitos);
            if (ServicioAutenticacion.EstaEnListaDeseos(juegoActual.Id))
            {
                await ServicioAutenticacion.AlternarListaDeseosAsync(juegoActual.Id);
                ActualizarBotonListaDeseos(false);
            }

            PurchasedBadge.Visibility = Visibility.Visible;
            BtnBuy.Visibility         = Visibility.Collapsed;

            await MostrarExitoCompraAsync();
        };

        await dialogoPago.ShowAsync();
    }

    private static void CrearEtiquetaYCampo(StackPanel panel, string etiqueta, TextBox campo)
    {
        var contenedor = new StackPanel { Spacing = 6 };
        contenedor.Children.Add(new TextBlock { Text = etiqueta, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) });
        contenedor.Children.Add(campo);
        panel.Children.Add(contenedor);
    }

    private static void MostrarErrorPago(TextBlock txtError, string mensaje)
    {
        txtError.Text = mensaje;
        txtError.Visibility = Visibility.Visible;
    }

    private static bool ValidarExpiracion(string valor)
    {
        if (valor.Length != 5 || valor[2] != '/') return false;
        if (!int.TryParse(valor[..2], out int mes) || !int.TryParse(valor[3..], out int anio)) return false;
        if (mes < 1 || mes > 12) return false;
        if (anio < 26) return false;
        int anioCompleto = 2000 + anio;
        return new DateTime(anioCompleto, mes, 1).AddMonths(1) > DateTime.Now;
    }

    private async Task MostrarExitoCompraAsync()
    {
        var panelExito = new StackPanel { Spacing = 20, Width = 380, HorizontalAlignment = HorizontalAlignment.Center };

        var circuloExito = new Border
        {
            Width = 72, Height = 72, CornerRadius = new CornerRadius(36),
            Background = new SolidColorBrush(Color.FromArgb(255, 220, 252, 231)),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        circuloExito.Child = new FontIcon { Glyph = "\uE73E", FontSize = 34, Foreground = new SolidColorBrush(Color.FromArgb(255, 21, 128, 61)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panelExito.Children.Add(circuloExito);

        var panelJuego = new Grid { Padding = new Thickness(14), Background = new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)) };
        panelJuego.CornerRadius = new CornerRadius(10);
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
        panelJuego.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bannerMini = new Border { Width = 56, Height = 56, CornerRadius = new CornerRadius(8), Background = ObtenerPincelCategoria(juegoActual!.Categoria), VerticalAlignment = VerticalAlignment.Center };
        string rutaImg = string.IsNullOrEmpty(juegoActual.UrlImagen) ? string.Empty
            : System.IO.Path.Combine(AppContext.BaseDirectory, juegoActual.UrlImagen);
        if (System.IO.File.Exists(rutaImg))
        {
            try { bannerMini.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaImg)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
            catch { }
        }
        Grid.SetColumn(bannerMini, 0);

        var panelInfoJuego = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0), Spacing = 4 };
        panelInfoJuego.Children.Add(new TextBlock { Text = juegoActual.Nombre, FontSize = 15, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
        panelInfoJuego.Children.Add(new TextBlock { Text = $"${juegoActual.Precio:N0} MXN", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)) });
        panelInfoJuego.Children.Add(new TextBlock { Text = $"Comprado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)) });
        Grid.SetColumn(panelInfoJuego, 1);
        panelJuego.Children.Add(bannerMini);
        panelJuego.Children.Add(panelInfoJuego);
        panelExito.Children.Add(panelJuego);

        var tituloCentrado = new TextBlock
        {
            Text = "¡Compra realizada correctamente!",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 21, 128, 61)),
            Margin = new Thickness(0, 0, 0, 4)
        };
        panelExito.Children.Insert(0, tituloCentrado);

        var dialogoExito = new ContentDialog
        {
            Content = panelExito,
            PrimaryButtonText = "Ir a mis juegos",
            SecondaryButtonText = "Seguir comprando",
            DefaultButton = ContentDialogButton.None,
            XamlRoot = this.XamlRoot
        };
        AplicarColoresBotonOscuro(dialogoExito);

        var resultado = await dialogoExito.ShowAsync();
        if (resultado == ContentDialogResult.Primary)
            AlIrAComprados?.Invoke();
        else if (resultado == ContentDialogResult.Secondary)
            AlIrATienda?.Invoke();
    }

    private static Style? EstiloBotonGris()
    {
        var estilo = new Style(typeof(Button));
        estilo.Setters.Add(new Setter(Button.BackgroundProperty,      new SolidColorBrush(Color.FromArgb(255, 26, 44, 61))));
        estilo.Setters.Add(new Setter(Button.ForegroundProperty,      new SolidColorBrush(Colors.White)));
        estilo.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
        estilo.Setters.Add(new Setter(Button.PaddingProperty,         new Thickness(20, 10, 20, 10)));
        estilo.Setters.Add(new Setter(Button.CornerRadiusProperty,    new CornerRadius(8)));
        return estilo;
    }

    private static void AplicarColoresBotonOscuro(ContentDialog dialogo)
    {
        var colorPrincipal = new SolidColorBrush(Color.FromArgb(255, 26, 44, 61));
        var colorHover = new SolidColorBrush(Color.FromArgb(255, 38, 60, 82));
        var colorPressed = new SolidColorBrush(Color.FromArgb(255, 18, 32, 46));

        var blanco = new SolidColorBrush(Colors.White);
        var negro = new SolidColorBrush(Colors.Black);

        var grisClaro = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));
        var grisHover = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230));
        var grisPressed = new SolidColorBrush(Color.FromArgb(255, 210, 210, 210));
        var bordeGris = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));

        dialogo.Resources["ContentDialogBackground"] =
            new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        dialogo.Resources["ContentDialogPrimaryButtonBackground"] = colorPrincipal;
        dialogo.Resources["ContentDialogPrimaryButtonForeground"] = blanco;
        dialogo.Resources["ContentDialogPrimaryButtonBorderBrush"] = colorPrincipal;
        dialogo.Resources["ContentDialogPrimaryButtonBorderThickness"] =
            new Thickness(0);

        dialogo.Resources["ContentDialogPrimaryButtonBackgroundPointerOver"] =
            colorHover;

        dialogo.Resources["ContentDialogPrimaryButtonBackgroundPressed"] =
            colorPressed;

        dialogo.Resources["ContentDialogCloseButtonBackground"] = grisClaro;
        dialogo.Resources["ContentDialogCloseButtonForeground"] = negro;
        dialogo.Resources["ContentDialogCloseButtonBorderBrush"] = bordeGris;
        dialogo.Resources["ContentDialogCloseButtonBorderThickness"] =
            new Thickness(1);

        dialogo.Resources["ContentDialogCloseButtonBackgroundPointerOver"] =
            grisHover;

        dialogo.Resources["ContentDialogCloseButtonBackgroundPressed"] =
            grisPressed;

        dialogo.Loaded += (s, e) =>
        {
            if (VisualTreeHelper.GetChildrenCount(dialogo) > 0)
            {
                var root = VisualTreeHelper.GetChild(dialogo, 0);
                AplicarEstiloBotones(root);
            }
        };
    }

    private static void AplicarEstiloBotones(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is Button btn)
            {
                if (btn.Content?.ToString() == "Pagar" ||
                    btn.Content?.ToString() == "Continuar")
                {
                    btn.Background =
                        new SolidColorBrush(Color.FromArgb(255, 26, 44, 61));

                    btn.Foreground =
                        new SolidColorBrush(Colors.White);

                    btn.BorderThickness = new Thickness(0);
                    btn.Padding = new Thickness(20, 10, 20, 10);
                    btn.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                }

                if (btn.Content?.ToString() == "Cancelar")
                {
                    btn.Background =
                        new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));

                    btn.Foreground =
                        new SolidColorBrush(Colors.Black);

                    btn.BorderBrush =
                        new SolidColorBrush(Color.FromArgb(255, 220, 220, 220));

                    btn.BorderThickness = new Thickness(1);
                    btn.Padding = new Thickness(20, 10, 20, 10);
                    btn.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                }
            }

            AplicarEstiloBotones(child);
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e) => AlRegresar?.Invoke();

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
        "Sandbox"     => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 92, 107, 92)),
        "Rol"         => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 106, 27, 154)),
        "Battle Royale" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 183, 28, 28)),
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
        "Sandbox"     => "\uE81E",
        "Rol"         => "\uE97D",
        "Battle Royale" => "\uE946",
        _             => "\uE7FC"
    };
}
