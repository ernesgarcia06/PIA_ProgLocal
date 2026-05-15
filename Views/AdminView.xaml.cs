using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameStoreApp.Models;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

namespace GameStoreApp.Views;

public sealed partial class AdminView : UserControl
{
    public event Action?         AlRegresar;
    public event Action<Juego>?  AlSeleccionarJuego;

    private List<Juego>   todosLosJuegos        = new();
    private Juego?        juegoEnEdicion        = null;
    private string        rutaImagenSeleccionada = string.Empty;
    private List<string>  fotosJuego             = new();
    private List<Usuario> todosLosUsuarios       = new();

    private int anioSeleccionado = 0;
    private int decadaBase       = 0;

    public AdminView()
    {
        this.InitializeComponent();
        InicializarSelectorAnio();
    }

    private void InicializarSelectorAnio()
    {
        decadaBase = (DateTime.Now.Year / 10) * 10;
        CargarDecada(decadaBase);
    }

    private void CargarDecada(int base10)
    {
        if (GridAnios is null) return;
        GridAnios.Items.Clear();
        int anioMax = DateTime.Now.Year;
        for (int a = base10; a < base10 + 10; a++)
        {
            if (a >= 1900 && a <= anioMax)
                GridAnios.Items.Add(a);
        }
        if (TxtAnioSeleccionado is not null && anioSeleccionado == 0)
            TxtAnioSeleccionado.Text = $"{base10} - {Math.Min(base10 + 9, anioMax)}";

        if (anioSeleccionado != 0 && GridAnios.Items.Contains(anioSeleccionado))
            GridAnios.SelectedItem = anioSeleccionado;
    }

    private void BtnAnioAnterior_Click(object sender, RoutedEventArgs e)
    {
        if (decadaBase - 10 < 1900) return;
        decadaBase -= 10;
        CargarDecada(decadaBase);
    }

    private void BtnAnioSiguiente_Click(object sender, RoutedEventArgs e)
    {
        if (decadaBase + 10 > DateTime.Now.Year) return;
        decadaBase += 10;
        CargarDecada(decadaBase);
    }

    private void GridAnios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridAnios.SelectedItem is int anio)
        {
            anioSeleccionado = anio;
            TxtAnioSeleccionado.Text = anio.ToString();
            TxtAnioSeleccionado.Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush;
            TxtYearError.Visibility = Visibility.Collapsed;
        }
    }

    private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        string texto = TxtName.Text;
        if (texto.Length > 0 && texto[0] == ' ')
        {
            TxtName.Text = texto.TrimStart();
            TxtName.SelectionStart = TxtName.Text.Length;
            return;
        }
        string original = TxtName.Text;
        string filtrado  = FiltrarNombreJuego(original);
        if (filtrado != original)
        {
            int pos = TxtName.SelectionStart;
            TxtName.Text = filtrado;
            TxtName.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length));
        }
        string error = ValidarNombreJuego(TxtName.Text);
        TxtNameError.Text = error;
        TxtNameError.Visibility = string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void TxtPrice_TextChanged(object sender, TextChangedEventArgs e)
    {
        string original = TxtPrice.Text;
        string filtrado  = FiltrarPrecio(original);
        if (filtrado != original)
        {
            int pos = TxtPrice.SelectionStart;
            TxtPrice.Text = filtrado;
            TxtPrice.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length));
        }
        string error = ValidarPrecio(TxtPrice.Text);
        TxtPriceError.Text = error;
        TxtPriceError.Visibility = string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static string FiltrarNombreJuego(string texto)
    {
        var resultado = new System.Text.StringBuilder();
        foreach (char c in texto)
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == ' ' || c == ':' || c == '-' || c == '\'')
                resultado.Append(c);
        return resultado.ToString();
    }

    private static string FiltrarPrecio(string texto)
    {
        var resultado = new System.Text.StringBuilder();
        foreach (char c in texto)
            if (c >= '0' && c <= '9')
                resultado.Append(c);
        return resultado.ToString();
    }

    private static string ValidarNombreJuego(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return string.Empty;
        if (nombre != nombre.Trim())
            return "El nombre no puede tener espacios al inicio o al final.";
        if (nombre.Length < 2)
            return "El nombre debe tener al menos 2 caracteres.";
        if (nombre.Length > 60)
            return "El nombre no puede tener más de 60 caracteres.";
        if (!Regex.IsMatch(nombre, @"^[a-zA-Z0-9 :\-']+$"))
            return "Solo se permiten letras, números, espacios, ':', '-' y \"'\".";
        return string.Empty;
    }

    private static string ValidarPrecio(string precioStr)
    {
        if (string.IsNullOrWhiteSpace(precioStr))
            return string.Empty;
        if (!int.TryParse(precioStr, out int precio))
            return "El precio debe ser un numero entero.";
        if (precio < 0 || precio > 1000000)
            return "El precio debe estar entre 0 y 1,000,000.";
        return string.Empty;
    }

    public void Actualizar()
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return;
        todosLosJuegos   = ServicioJuegos.ObtenerTodos();
        todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios();
        ActualizarSubtitulo();
        RenderizarListaJuegos();
        LimpiarFormularioJuego();
        RenderizarListaUsuarios();

        if (VistaReportes is not null)
        {
            VistaReportes.AlSeleccionarJuego -= ReportesView_AlSeleccionarJuego;
            VistaReportes.AlSeleccionarJuego += ReportesView_AlSeleccionarJuego;
        }
    }

    private void ReportesView_AlSeleccionarJuego(Juego juego) =>
        AlSeleccionarJuego?.Invoke(juego);

    private void ActualizarSubtitulo()
    {
        if (AdminPivot is null || TxtSubtitle is null) return;
        if (AdminPivot.SelectedIndex == 0)
        {
            int total = todosLosJuegos.Count;
            TxtSubtitle.Text = $"{total} juego{(total != 1 ? "s" : "")} en el catálogo";
        }
        else
        {
            int totalUsuarios = todosLosUsuarios.Count;
            int totalAdmins   = todosLosUsuarios.Count(u => u.EsAdministrador);
            TxtSubtitle.Text  = $"{totalUsuarios} usuario{(totalUsuarios != 1 ? "s" : "")} · {totalAdmins} administrador{(totalAdmins != 1 ? "es" : "")}";
        }
    }

    private void AdminPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ActualizarSubtitulo();
        OcultarMensajeAdmin();
        OcultarMensajeFormulario();
        if (AdminPivot.SelectedIndex == 2)
            VistaReportes?.Actualizar();
    }

    private void RenderizarListaJuegos()
    {
        if (GamesListPanel is null) return;
        GamesListPanel.Children.Clear();

        string busqueda = TxtSearchGame?.Text?.Trim() ?? string.Empty;
        var juegos = string.IsNullOrEmpty(busqueda)
            ? new List<Juego>(todosLosJuegos)
            : todosLosJuegos.Where(j => j.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();

        juegos = juegos.OrderBy(j => j.Nombre).ToList();

        foreach (var juego in juegos)
            GamesListPanel.Children.Add(CrearFilaJuego(juego));

        if (juegos.Count == 0)
            GamesListPanel.Children.Add(CrearEtiquetaVacia("No se encontraron juegos."));
    }

    private Border CrearFilaJuego(Juego juego)
    {
        bool enEdicion = juegoEnEdicion?.Id == juego.Id;

        var txtNombre = new TextBlock
        {
            Text = juego.Nombre, FontSize = 13,
            FontWeight = enEdicion ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var txtPrecio = new TextBlock
        {
            Text = $"${juego.Precio:N0} MXN · {juego.Categoria} · {juego.Anio}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var botonEditar = new Button
        {
            Content = "Editar", FontSize = 11,
            Padding = new Thickness(8, 4, 8, 4),
            Background = enEdicion
                ? new SolidColorBrush(Color.FromArgb(255, 36, 56, 76))
                : new SolidColorBrush(Color.FromArgb(255, 224, 233, 242)),
            Foreground = enEdicion
                ? new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(255, 36, 56, 76)),
            BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(6)
        };
        var juegoCapturado = juego;
        botonEditar.Click += (_, _) => CargarJuegoParaEditar(juegoCapturado);

        var botonEliminar = new Button
        {
            Content = "Eliminar", FontSize = 11,
            Padding = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromArgb(255, 255, 240, 240)),
            Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 40, 40)),
            BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(6)
        };
        botonEliminar.Click += async (_, _) => await ConfirmarEliminarJuegoAsync(juegoCapturado);

        var panelBotones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        panelBotones.Children.Add(botonEditar);
        panelBotones.Children.Add(botonEliminar);

        var panelInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        panelInfo.Children.Add(txtNombre);
        panelInfo.Children.Add(txtPrecio);

        var gridFila = new Grid { Padding = new Thickness(12, 8, 12, 8) };
        gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(panelInfo, 0);
        Grid.SetColumn(panelBotones, 1);
        gridFila.Children.Add(panelInfo);
        gridFila.Children.Add(panelBotones);

        return new Border
        {
            Background = enEdicion
                ? new SolidColorBrush(Color.FromArgb(255, 224, 233, 242))
                : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
            BorderBrush = new SolidColorBrush(enEdicion
                ? Color.FromArgb(255, 36, 56, 76)
                : Color.FromArgb(255, 226, 232, 240)),
            BorderThickness = new Thickness(enEdicion ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Child = gridFila
        };
    }

    private void CargarJuegoParaEditar(Juego juego)
    {
        juegoEnEdicion = juego;
        TxtFormTitle.Text = "Editar juego"; FormIcon.Glyph = "\uE70F";
        TxtName.Text  = juego.Nombre;
        TxtPrice.Text = ((int)juego.Precio).ToString();

        anioSeleccionado = juego.Anio;
        decadaBase       = (juego.Anio / 10) * 10;
        CargarDecada(decadaBase);
        TxtAnioSeleccionado.Text       = juego.Anio.ToString();
        TxtAnioSeleccionado.Foreground = Application.Current.Resources["DarkTextBrush"] as SolidColorBrush;

        rutaImagenSeleccionada = string.Empty;
        if (!string.IsNullOrEmpty(juego.UrlImagen))
        {
            string rutaCompleta = Path.Combine(AppContext.BaseDirectory, juego.UrlImagen);
            if (File.Exists(rutaCompleta))
            {
                try
                {
                    ImgPreview.Source           = new BitmapImage(new Uri(rutaCompleta));
                    ImgPreview.Visibility       = Visibility.Visible;
                    ImagePlaceholder.Visibility = Visibility.Collapsed;
                    TxtImageFileName.Text       = Path.GetFileName(rutaCompleta);
                }
                catch { }
            }
            else TxtImageFileName.Text = Path.GetFileName(juego.UrlImagen);
        }

        TxtDescription.Text = juego.Descripcion;
        fotosJuego = new List<string>(juego.Fotos ?? new List<string>());
        RenderizarMiniaturasFotos();

        foreach (ComboBoxItem item in CmbCategory.Items)
            if (item.Content?.ToString() == juego.Categoria) { CmbCategory.SelectedItem = item; break; }

        BtnSaveText.Text = "Guardar cambios"; BtnSaveIcon.Glyph = "\uE74E";
        BtnCancel.Visibility = Visibility.Visible;
        TxtNameError.Visibility  = Visibility.Collapsed;
        TxtPriceError.Visibility = Visibility.Collapsed;
        TxtYearError.Visibility  = Visibility.Collapsed;
        OcultarMensajeFormulario();
        RenderizarListaJuegos();
    }

    private void LimpiarFormularioJuego()
    {
        juegoEnEdicion    = null;
        TxtFormTitle.Text = "Agregar nuevo juego"; FormIcon.Glyph = "\uE710";
        TxtName.Text      = string.Empty;
        TxtPrice.Text     = string.Empty;
        TxtDescription.Text = string.Empty;
        CmbCategory.SelectedItem = null;
        BtnSaveText.Text     = "Agregar juego"; BtnSaveIcon.Glyph = "\uE710";
        BtnCancel.Visibility = Visibility.Collapsed;

        anioSeleccionado = 0;
        decadaBase       = (DateTime.Now.Year / 10) * 10;
        CargarDecada(decadaBase);
        TxtAnioSeleccionado.Text       = "Sin seleccionar";
        TxtAnioSeleccionado.Foreground = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush;

        rutaImagenSeleccionada      = string.Empty;
        ImgPreview.Source           = null;
        ImgPreview.Visibility       = Visibility.Collapsed;
        ImagePlaceholder.Visibility = Visibility.Visible;
        TxtImageFileName.Text       = "Ninguna imagen seleccionada";
        fotosJuego.Clear();
        RenderizarMiniaturasFotos();

        TxtNameError.Visibility  = Visibility.Collapsed;
        TxtPriceError.Visibility = Visibility.Collapsed;
        TxtYearError.Visibility  = Visibility.Collapsed;
        OcultarMensajeFormulario();
    }

    private (bool valido, string error, Juego? juego) ValidarFormularioJuego()
    {
        string nombre      = TxtName.Text.Trim();
        string precioStr   = TxtPrice.Text.Trim();
        string categoria   = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        string descripcion = TxtDescription.Text.Trim();

        string errorNombre = ValidarNombreJuego(nombre);
        if (!string.IsNullOrEmpty(nombre) && nombre.StartsWith(' ') || nombre.EndsWith(' '))
            return (false, "El nombre no puede tener espacios al inicio o al final.", null);
        if (nombre.Length < 2)
            return (false, "El nombre debe tener al menos 2 caracteres.", null);
        if (!string.IsNullOrEmpty(errorNombre))
            return (false, errorNombre, null);

        if (string.IsNullOrEmpty(precioStr))
            return (false, "El precio es obligatorio.", null);
        if (!int.TryParse(precioStr, out int precio) || precio < 0 || precio > 1000000)
            return (false, "El precio debe ser un entero entre 0 y 1,000,000.", null);

        if (anioSeleccionado == 0)
            return (false, "Selecciona el año de lanzamiento.", null);

        if (string.IsNullOrEmpty(categoria))
            return (false, "Selecciona una categoría.", null);
        if (string.IsNullOrEmpty(descripcion))
            return (false, "La descripción es obligatoria.", null);

        return (true, string.Empty, new Juego
        {
            Nombre = nombre, Precio = precio, Anio = anioSeleccionado,
            Categoria = categoria, UrlImagen = string.Empty, Descripcion = descripcion
        });
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true)
        { MostrarMensajeFormulario("Acceso denegado.", isError: true); return; }

        var (valido, error, juego) = ValidarFormularioJuego();
        if (!valido) { MostrarMensajeFormulario(error, isError: true); return; }

        BtnSave.IsEnabled = false;
        try
        {
            if (!string.IsNullOrEmpty(rutaImagenSeleccionada) && File.Exists(rutaImagenSeleccionada))
                juego!.UrlImagen = await ServicioDatos.CopiarImagenJuegoAsync(rutaImagenSeleccionada);
            else if (juegoEnEdicion is not null && !string.IsNullOrEmpty(juegoEnEdicion.UrlImagen))
                juego!.UrlImagen = juegoEnEdicion.UrlImagen;
            else
                juego!.UrlImagen = string.Empty;

            bool exito;
            if (juegoEnEdicion is null)
            {
                exito = await ServicioJuegos.AgregarJuegoAsync(juego!);
                if (exito) MostrarMensajeFormulario($"\"{juego!.Nombre}\" agregado correctamente.", isError: false);
                else       MostrarMensajeFormulario("Error al agregar el juego.", isError: true);
            }
            else
            {
                juego!.Id = juegoEnEdicion.Id;
                exito = await ServicioJuegos.ActualizarJuegoAsync(juego);
                if (exito) MostrarMensajeFormulario($"\"{juego.Nombre}\" actualizado correctamente.", isError: false);
                else       MostrarMensajeFormulario("Error al guardar cambios.", isError: true);
            }

            if (exito)
            {
                var juegoGuardado = ServicioJuegos.ObtenerPorId(
                    juegoEnEdicion is null
                        ? ServicioJuegos.ObtenerTodos().Max(j => j.Id)
                        : juego!.Id);
                if (juegoGuardado is not null)
                {
                    juegoGuardado.Fotos = new List<string>(fotosJuego);
                    await ServicioDatos.GuardarJuegosAsync(ServicioJuegos.ObtenerTodos());
                }
                todosLosJuegos = ServicioJuegos.ObtenerTodos();
                ActualizarSubtitulo();
                LimpiarFormularioJuego();
                RenderizarListaJuegos();
            }
        }
        catch (Exception ex) { MostrarMensajeFormulario($"Error: {ex.Message}", isError: true); }
        finally { BtnSave.IsEnabled = true; }
    }

    private async Task ConfirmarEliminarJuegoAsync(Juego juego)
    {
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return;

        var dialogo = new ContentDialog
        {
            Title = "Confirmar eliminación",
            Content = $"¿Eliminar \"{juego.Nombre}\"?\nEsta accion no se puede deshacer.",
            PrimaryButtonText = "Sí, eliminar",
            CloseButtonText   = "Cancelar",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = this.XamlRoot
        };

        if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

        bool exito = await ServicioJuegos.EliminarJuegoAsync(juego.Id);
        if (exito)
        {
            if (juegoEnEdicion?.Id == juego.Id) LimpiarFormularioJuego();
            todosLosJuegos = ServicioJuegos.ObtenerTodos();
            ActualizarSubtitulo();
            RenderizarListaJuegos();
            MostrarMensajeFormulario($"\"{juego.Nombre}\" eliminado.", isError: false);
        }
        else MostrarMensajeFormulario("Error al eliminar el juego.", isError: true);
    }

    private void TxtSearchGame_TextChanged(object sender, TextChangedEventArgs e)
    {
        string texto = TxtSearchGame.Text;
        if (texto.Length > 0 && texto[0] == ' ')
        {
            TxtSearchGame.Text = texto.TrimStart();
            TxtSearchGame.SelectionStart = TxtSearchGame.Text.Length;
            return;
        }
        RenderizarListaJuegos();
    }
    private void BtnSearchGame_Click(object sender, RoutedEventArgs e)             => RenderizarListaJuegos();
    private void BtnCancel_Click(object sender, RoutedEventArgs e) { LimpiarFormularioJuego(); RenderizarListaJuegos(); }

    private void MostrarMensajeFormulario(string mensaje, bool isError)
    {
        TxtFormMessage.Text = mensaje;
        TxtFormMessage.Foreground = isError
            ? new SolidColorBrush(Color.FromArgb(255, 185, 28, 28))
            : new SolidColorBrush(Color.FromArgb(255, 21, 128, 61));
        TxtFormMessage.Visibility = Visibility.Visible;
    }
    private void OcultarMensajeFormulario() => TxtFormMessage.Visibility = Visibility.Collapsed;

    private void RenderizarListaUsuarios()
    {
        if (UsersListPanel is null) return;
        UsersListPanel.Children.Clear();
        if (ServicioAutenticacion.UsuarioActual?.EsAdministrador != true) return;

        todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios();

        var ordenados = todosLosUsuarios
            .OrderBy(u => u.EsGodAdmin ? 0 : u.EsAdmin ? 1 : 2)
            .ThenBy(u => u.NombreUsuario)
            .ToList();

        foreach (var usuario in ordenados)
            UsersListPanel.Children.Add(CrearFilaUsuario(usuario));

        if (ordenados.Count == 0)
            UsersListPanel.Children.Add(CrearEtiquetaVacia("No hay usuarios registrados."));

        if (TxtUserCount is not null)
        {
            int godAdmins   = todosLosUsuarios.Count(u => u.EsGodAdmin);
            int admins      = todosLosUsuarios.Count(u => u.EsAdmin);
            int normales    = todosLosUsuarios.Count(u => u.EsUsuarioNormal);
            int inactivos   = todosLosUsuarios.Count(u => !u.Activo);
            TxtUserCount.Text = $"Total: {todosLosUsuarios.Count}  GodAdmin: {godAdmins}  Admin: {admins}  User: {normales}  Inactivos: {inactivos}";
        }
    }

    private Border CrearFilaUsuario(Usuario usuario)
    {
        var ejecutor        = ServicioAutenticacion.UsuarioActual;
        bool esMismo        = usuario.NombreUsuario == ejecutor?.NombreUsuario;
        bool esGodAdmin     = usuario.EsGodAdmin;
        bool esAdmin        = usuario.EsAdmin;
        bool esActivo       = usuario.Activo;
        bool soyGodAdmin    = ejecutor?.EsGodAdmin == true;

        string glyphIcon = esGodAdmin ? "\uE734" : esAdmin ? "\uE713" : "\uE77B";
        var iconoAvatar = new FontIcon
        {
            Glyph = glyphIcon, FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Foreground = esGodAdmin
                ? new SolidColorBrush(Color.FromArgb(255, 180, 135, 10))
                : (SolidColorBrush)Application.Current.Resources["DarkTextBrush"]!
        };

        string etiquetaMismo = esMismo ? "  (tu)" : "";
        string etiquetaInactivo = !esActivo ? "  [inactivo]" : "";
        var txtNombre = new TextBlock
        {
            Text = usuario.NombreUsuario + etiquetaMismo + etiquetaInactivo,
            FontSize = 13,
            FontWeight = esMismo ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = !esActivo
                ? new SolidColorBrush(Color.FromArgb(255, 160, 160, 160))
                : (SolidColorBrush)Application.Current.Resources["DarkTextBrush"]!
        };

        var columnaUsuario = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        columnaUsuario.Children.Add(iconoAvatar);
        columnaUsuario.Children.Add(txtNombre);

        string textoRol = esGodAdmin ? "GodAdmin" : esAdmin ? "Admin" : "User";
        var colorRolFondo = esGodAdmin
            ? Color.FromArgb(255, 255, 248, 220)
            : esAdmin
                ? Color.FromArgb(255, 224, 233, 242)
                : Color.FromArgb(255, 240, 253, 244);
        var colorRolTexto = esGodAdmin
            ? Color.FromArgb(255, 146, 64, 14)
            : esAdmin
                ? Color.FromArgb(255, 36, 56, 76)
                : Color.FromArgb(255, 21, 128, 61);

        var badgeRol = new Border
        {
            Background   = new SolidColorBrush(colorRolFondo),
            CornerRadius = new CornerRadius(12),
            Padding      = new Thickness(10, 3, 10, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = textoRol, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(colorRolTexto)
            }
        };

        var panelAcciones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

        if (!esMismo && !esGodAdmin)
        {
            if (esActivo)
            {
                bool puedeDesactivar = (esUsuarioNormal: usuario.EsUsuarioNormal, soyGodAdmin) switch
                {
                    (true, _)      => true,
                    (_, true)      => true,
                    _              => false
                };

                if (puedeDesactivar)
                {
                    var usuarioRef = usuario;
                    var btnDesactivar = new Button
                    {
                        Content = "Desactivar", FontSize = 11,
                        Padding = new Thickness(8, 4, 8, 4),
                        Background = new SolidColorBrush(Color.FromArgb(255, 255, 240, 240)),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 185, 28, 28)),
                        BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(6)
                    };
                    btnDesactivar.Click += async (_, _) => await ConfirmarDesactivarUsuarioAsync(usuarioRef);
                    panelAcciones.Children.Add(btnDesactivar);
                }
            }
            else
            {
                bool puedeActivar = usuario.EsUsuarioNormal || soyGodAdmin;
                if (puedeActivar)
                {
                    var usuarioRef = usuario;
                    var btnActivar = new Button
                    {
                        Content = "Activar", FontSize = 11,
                        Padding = new Thickness(8, 4, 8, 4),
                        Background = new SolidColorBrush(Color.FromArgb(255, 240, 253, 244)),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 21, 128, 61)),
                        BorderThickness = new Thickness(0), CornerRadius = new CornerRadius(6)
                    };
                    btnActivar.Click += async (_, _) => await ConfirmarActivarUsuarioAsync(usuarioRef);
                    panelAcciones.Children.Add(btnActivar);
                }
            }

            if (soyGodAdmin)
            {
                var usuarioRef = usuario;
                if (usuario.EsUsuarioNormal)
                {
                    var btnPromover = new Button
                    {
                        Content = "Hacer admin", FontSize = 11,
                        Padding = new Thickness(8, 4, 8, 4),
                        Background = new SolidColorBrush(Color.FromArgb(255, 255, 248, 220)),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 146, 64, 14)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 253, 230, 138)),
                        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6)
                    };
                    btnPromover.Click += async (_, _) => await ConfirmarPromoverAdminAsync(usuarioRef);
                    panelAcciones.Children.Add(btnPromover);
                }
                else if (usuario.EsAdmin)
                {
                    var btnDegadar = new Button
                    {
                        Content = "Quitar admin", FontSize = 11,
                        Padding = new Thickness(8, 4, 8, 4),
                        Background = new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 90, 106, 126)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
                        BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6)
                    };
                    btnDegadar.Click += async (_, _) => await ConfirmarDegradararAdminAsync(usuarioRef);
                    panelAcciones.Children.Add(btnDegadar);
                }
            }
        }

        if (panelAcciones.Children.Count == 0)
        {
            panelAcciones.Children.Add(new TextBlock
            {
                Text = "—", FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160)),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        var gridFila = new Grid { Padding = new Thickness(16, 10, 16, 10) };
        gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        gridFila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(columnaUsuario, 0);
        Grid.SetColumn(badgeRol,       1);
        Grid.SetColumn(panelAcciones,  2);
        badgeRol.HorizontalAlignment      = Microsoft.UI.Xaml.HorizontalAlignment.Center;
        panelAcciones.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
        panelAcciones.MinWidth            = 200;
        gridFila.Children.Add(columnaUsuario);
        gridFila.Children.Add(badgeRol);
        gridFila.Children.Add(panelAcciones);

        return new Border
        {
            Background = esMismo
                ? new SolidColorBrush(Color.FromArgb(255, 255, 253, 235))
                : !esActivo
                    ? new SolidColorBrush(Color.FromArgb(255, 250, 250, 250))
                    : new SolidColorBrush(Color.FromArgb(255, 248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = gridFila
        };
    }

    private async Task ConfirmarDesactivarUsuarioAsync(Usuario usuario)
    {
        var dialogo = new ContentDialog
        {
            Title = "Desactivar usuario",
            Content = $"¿Desactivar a \"{usuario.NombreUsuario}\"?\nNo podrá iniciar sesión.",
            PrimaryButtonText = "Sí, desactivar",
            CloseButtonText   = "Cancelar",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = this.XamlRoot
        };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

        var (exito, mensaje) = await ServicioAutenticacion.DesactivarUsuarioAsync(usuario.NombreUsuario);
        MostrarMensajeAdmin(mensaje, isError: !exito);
        if (exito) { todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios(); ActualizarSubtitulo(); RenderizarListaUsuarios(); }
    }

    private async Task ConfirmarActivarUsuarioAsync(Usuario usuario)
    {
        var dialogo = new ContentDialog
        {
            Title = "Activar usuario",
            Content = $"¿Reactivar a \"{usuario.NombreUsuario}\"?",
            PrimaryButtonText = "Sí, activar",
            CloseButtonText   = "Cancelar",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = this.XamlRoot
        };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

        var (exito, mensaje) = await ServicioAutenticacion.ActivarUsuarioAsync(usuario.NombreUsuario);
        MostrarMensajeAdmin(mensaje, isError: !exito);
        if (exito) { todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios(); ActualizarSubtitulo(); RenderizarListaUsuarios(); }
    }

    private async Task ConfirmarPromoverAdminAsync(Usuario usuario)
    {
        var dialogo = new ContentDialog
        {
            Title = "Promover a administrador",
            Content = $"¿Convertir a \"{usuario.NombreUsuario}\" en administrador?",
            PrimaryButtonText = "Sí, hacer admin",
            CloseButtonText   = "Cancelar",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = this.XamlRoot
        };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

        var (exito, mensaje) = await ServicioAutenticacion.PromoverAdministradorAsync(usuario.NombreUsuario);
        MostrarMensajeAdmin(mensaje, isError: !exito);
        if (exito) { todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios(); ActualizarSubtitulo(); RenderizarListaUsuarios(); }
    }

    private async Task ConfirmarDegradararAdminAsync(Usuario usuario)
    {
        var dialogo = new ContentDialog
        {
            Title = "Quitar rol de administrador",
            Content = $"¿Quitar el rol de admin a \"{usuario.NombreUsuario}\"?",
            PrimaryButtonText = "Sí, quitar",
            CloseButtonText   = "Cancelar",
            DefaultButton     = ContentDialogButton.Close,
            XamlRoot          = this.XamlRoot
        };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary) return;

        var (exito, mensaje) = await ServicioAutenticacion.DegradrarAdministradorAsync(usuario.NombreUsuario);
        MostrarMensajeAdmin(mensaje, isError: !exito);
        if (exito) { todosLosUsuarios = ServicioAutenticacion.ObtenerTodosLosUsuarios(); ActualizarSubtitulo(); RenderizarListaUsuarios(); }
    }

    private void MostrarMensajeAdmin(string mensaje, bool isError)
    {
        TxtAdminMessage.Text = mensaje;
        TxtAdminMessage.Foreground = isError
            ? new SolidColorBrush(Color.FromArgb(255, 185, 28, 28))
            : new SolidColorBrush(Color.FromArgb(255, 21, 128, 61));
        TxtAdminMessage.Visibility = Visibility.Visible;
    }
    private void OcultarMensajeAdmin() => TxtAdminMessage.Visibility = Visibility.Collapsed;

    private static TextBlock CrearEtiquetaVacia(string texto) => new()
    {
        Text = texto, FontSize = 13,
        Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)),
        Margin = new Thickness(16, 20, 16, 0),
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private async void BtnSelectImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            StorageFile? archivo = await picker.PickSingleFileAsync();
            if (archivo is null) return;

            string ext = archivo.FileType.ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            { MostrarMensajeFormulario("Solo se permiten imagenes JPG o PNG.", isError: true); return; }

            rutaImagenSeleccionada = archivo.Path;

            var bitmapImagen = new BitmapImage();
            using var stream = await archivo.OpenReadAsync();
            await bitmapImagen.SetSourceAsync(stream);

            ImgPreview.Source           = bitmapImagen;
            ImgPreview.Visibility       = Visibility.Visible;
            ImagePlaceholder.Visibility = Visibility.Collapsed;
            TxtImageFileName.Text       = archivo.Name;
            OcultarMensajeFormulario();
        }
        catch (Exception ex) { MostrarMensajeFormulario($"Error al seleccionar imagen: {ex.Message}", isError: true); }
    }

    private async void BtnAddPhoto_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var archivos = await picker.PickMultipleFilesAsync();
            if (archivos is null || archivos.Count == 0) return;

            foreach (var archivo in archivos)
            {
                string ext = archivo.FileType.ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") continue;
                string rutaRelativa = await ServicioDatos.CopiarFotoJuegoAsync(archivo.Path);
                fotosJuego.Add(rutaRelativa);
            }

            RenderizarMiniaturasFotos();
            OcultarMensajeFormulario();
        }
        catch (Exception ex) { MostrarMensajeFormulario($"Error al agregar foto: {ex.Message}", isError: true); }
    }

    private void RenderizarMiniaturasFotos()
    {
        if (PhotosPreviewPanel is null) return;
        PhotosPreviewPanel.Children.Clear();

        for (int i = 0; i < fotosJuego.Count; i++)
        {
            int indiceCapturado = i;
            string rutaRelativa = fotosJuego[i];
            string rutaCompleta = Path.Combine(AppContext.BaseDirectory, rutaRelativa);

            var miniatura = new Border
            {
                Width = 70, Height = 70, CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230))
            };

            if (File.Exists(rutaCompleta))
            {
                try { miniatura.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(rutaCompleta)), Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill }; }
                catch { }
            }

            var botonEliminar = new Button
            {
                Content = new FontIcon { Glyph = "\uE711", FontSize = 10, Foreground = new SolidColorBrush(Colors.White) },
                Width = 20, Height = 20, Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(200, 200, 40, 40)),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0)
            };
            botonEliminar.Click += (_, _) => { fotosJuego.RemoveAt(indiceCapturado); RenderizarMiniaturasFotos(); };

            var grid = new Grid();
            grid.Children.Add(miniatura);
            grid.Children.Add(botonEliminar);
            PhotosPreviewPanel.Children.Add(grid);
        }

        if (TxtPhotosCount is not null)
            TxtPhotosCount.Text = fotosJuego.Count == 0
                ? "0 fotos agregadas"
                : $"{fotosJuego.Count} foto{(fotosJuego.Count != 1 ? "s" : "")} agregada{(fotosJuego.Count != 1 ? "s" : "")}";
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e) => AlRegresar?.Invoke();
}
