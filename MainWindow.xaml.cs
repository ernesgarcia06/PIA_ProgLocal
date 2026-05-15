using GameStoreApp.Models;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics;

namespace GameStoreApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        EstablecerIconoVentana();
        EstablecerTamanoMinimo(900, 620);
        _ = InicializarAplicacionAsync();
        SuscribirEventosVistas();
    }

    private void EstablecerIconoVentana()
    {
        try
        {
            string rutaIco = Path.Combine(AppContext.BaseDirectory, "Assets", "gaura.ico");
            if (File.Exists(rutaIco))
                this.AppWindow.SetIcon(rutaIco);
        }
        catch { }
    }

    private void EstablecerTamanoMinimo(int anchoMinimo, int altoMinimo)
    {
        var ventanaApp = this.AppWindow;
        if (ventanaApp is not null)
        {
            ventanaApp.Changed += (s, e) =>
            {
                var tamano = ventanaApp.Size;
                int ancho = tamano.Width  < anchoMinimo ? anchoMinimo : tamano.Width;
                int alto  = tamano.Height < altoMinimo  ? altoMinimo  : tamano.Height;
                if (ancho != tamano.Width || alto != tamano.Height)
                    ventanaApp.Resize(new SizeInt32(ancho, alto));
            };
            var actual = ventanaApp.Size;
            int ia = actual.Width  < anchoMinimo ? anchoMinimo : actual.Width;
            int ih = actual.Height < altoMinimo  ? altoMinimo  : actual.Height;
            if (ia != actual.Width || ih != actual.Height)
                ventanaApp.Resize(new SizeInt32(ia, ih));
        }
    }

    private async Task InicializarAplicacionAsync()
    {
        try
        {
            await ServicioDatos.InicializarAsync();
            await ServicioAutenticacion.CargarUsuariosAsync();
            await ServicioJuegos.CargarJuegosAsync();
            var usuarios = await ServicioDatos.CargarUsuariosAsync();
            ServicioJuegos.ActualizarResenias(usuarios);
        }
        catch { }
    }

    private void SuscribirEventosVistas()
    {
        VistaInicioSesion.AlIniciarSesionExitoso += () => NavegaA("tienda");
        VistaInicioSesion.AlIrARegistro          += () => NavegaA("registro");

        VistaRegistro.AlRegistrarseExitoso += () => NavegaA("tienda");
        VistaRegistro.AlIrAInicioSesion    += () => NavegaA("inicio-sesion");

        VistaTienda.AlSeleccionarJuego += (juego) =>
        {
            VistaDetalleJuego.EstablecerOrigen(Views.GameDetailView.OrigenVista.Tienda);
            VistaDetalleJuego.CargarJuego(juego);
            NavegaA("detalle");
        };

        VistaDetalleJuego.AlRegresar += () =>
        {
            var dest = VistaDetalleJuego.OrigenActual switch
            {
                Views.GameDetailView.OrigenVista.Reporte => "admin",
                Views.GameDetailView.OrigenVista.Perfil  => "perfil",
                _                                        => "tienda"
            };
            NavegaA(dest);
        };

        VistaDetalleJuego.AlIrAComprados += () =>
        {
            NavegaA("perfil");
            VistaPerfil.MostrarPestanaComprados();
        };

        VistaDetalleJuego.AlIrATienda += () => NavegaA("tienda");

        VistaPerfil.AlRegresar         += () => NavegaA("tienda");
        VistaPerfil.AlSeleccionarJuego += (juego) =>
        {
            VistaDetalleJuego.EstablecerOrigen(Views.GameDetailView.OrigenVista.Perfil);
            VistaDetalleJuego.CargarJuego(juego);
            NavegaA("detalle");
        };

        VistaAdmin.AlRegresar += () => NavegaA("tienda");
        VistaAdmin.AlSeleccionarJuego += (juego) =>
        {
            VistaDetalleJuego.EstablecerOrigen(Views.GameDetailView.OrigenVista.Reporte);
            VistaDetalleJuego.CargarJuego(juego);
            NavegaA("detalle");
        };
    }

    public void NavegaA(string nombreVista)
    {
        VistaInicioSesion.Visibility  = Visibility.Collapsed;
        VistaRegistro.Visibility      = Visibility.Collapsed;
        VistaTienda.Visibility        = Visibility.Collapsed;
        VistaDetalleJuego.Visibility  = Visibility.Collapsed;
        VistaPerfil.Visibility        = Visibility.Collapsed;
        VistaAdmin.Visibility         = Visibility.Collapsed;

        bool autenticado     = ServicioAutenticacion.UsuarioActual is not null;
        bool esAdministrador = ServicioAutenticacion.UsuarioActual?.EsAdministrador == true;

        PanelBotonesNavegacion.Visibility = autenticado     ? Visibility.Visible   : Visibility.Collapsed;
        PanelUsuario.Visibility           = autenticado     ? Visibility.Visible   : Visibility.Collapsed;
        BotonNavAdmin.Visibility          = esAdministrador ? Visibility.Visible   : Visibility.Collapsed;

        if (autenticado)
            TxtNombreUsuarioActual.Text = ServicioAutenticacion.UsuarioActual!.NombreUsuario;

        var pincelActivo   = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(60, 255, 255, 255));
        var pincelInactivo = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(0, 255, 255, 255));

        BotonNavTienda.Background = pincelInactivo;
        BotonNavPerfil.Background = pincelInactivo;
        BotonNavAdmin.Background  = pincelInactivo;

        switch (nombreVista)
        {
            case "inicio-sesion":
                VistaInicioSesion.Visibility = Visibility.Visible;
                break;
            case "registro":
                VistaRegistro.Visibility = Visibility.Visible;
                break;
            case "tienda":
                VistaTienda.Visibility = Visibility.Visible;
                VistaTienda.Actualizar();
                BotonNavTienda.Background = pincelActivo;
                break;
            case "detalle":
                VistaDetalleJuego.Visibility = Visibility.Visible;
                BotonNavTienda.Background = pincelActivo;
                break;
            case "perfil":
                VistaPerfil.Visibility = Visibility.Visible;
                VistaPerfil.Actualizar();
                BotonNavPerfil.Background = pincelActivo;
                break;
            case "admin":
                if (!esAdministrador) { NavegaA("tienda"); return; }
                VistaAdmin.Visibility = Visibility.Visible;
                VistaAdmin.Actualizar();
                BotonNavAdmin.Background = pincelActivo;
                break;
        }
    }

    private void BotonNavTienda_Click(object sender, RoutedEventArgs e)     => NavegaA("tienda");
    private void BotonNavPerfil_Click(object sender, RoutedEventArgs e)     => NavegaA("perfil");
    private void BotonNavAdmin_Click(object sender, RoutedEventArgs e)      => NavegaA("admin");
    private void BotonUsuarioActual_Click(object sender, RoutedEventArgs e) => NavegaA("perfil");

    private void BotonCerrarSesion_Click(object sender, RoutedEventArgs e)
    {
        ServicioAutenticacion.CerrarSesion();
        NavegaA("inicio-sesion");
    }
}
