using System;
using GameStoreApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GameStoreApp.Views;

public sealed partial class LoginView : UserControl
{
    public event Action? AlIniciarSesionExitoso;
    public event Action? AlIrARegistro;

    private bool contraseniaVisible = false;

    public LoginView() => this.InitializeComponent();

    private void BotonIniciarSesion_Click(object sender, RoutedEventArgs e) => IntentarInicioSesion();

    private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
    {
        string original = TxtUsername.Text;
        string filtrado  = FiltrarUsuario(original);
        if (filtrado != original)
        {
            int pos = TxtUsername.SelectionStart;
            TxtUsername.Text = filtrado;
            TxtUsername.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length));
        }
    }

    private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
    {
        string original = TxtPasswordVisible.Text;
        string filtrado  = FiltrarContrasenia(original);
        if (filtrado != original)
        {
            int pos = TxtPasswordVisible.SelectionStart;
            TxtPasswordVisible.Text = filtrado;
            TxtPasswordVisible.SelectionStart = Math.Max(0, pos - (original.Length - filtrado.Length));
        }
    }

    private static string FiltrarUsuario(string texto)
    {
        var resultado = new System.Text.StringBuilder();
        foreach (char c in texto)
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'))
                resultado.Append(char.ToLower(c));
        return resultado.ToString();
    }

    private static string FiltrarContrasenia(string texto)
    {
        var resultado = new System.Text.StringBuilder();
        foreach (char c in texto)
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == '@' || c == '#' || c == '$' || c == '%')
                resultado.Append(c);
        return resultado.ToString();
    }

    private void TxtNombreUsuario_KeyDown(object sender, KeyRoutedEventArgs e)
    { if (e.Key == Windows.System.VirtualKey.Enter) IntentarInicioSesion(); }

    private void TxtContrasenia_KeyDown(object sender, KeyRoutedEventArgs e)
    { if (e.Key == Windows.System.VirtualKey.Enter) IntentarInicioSesion(); }

    private void IntentarInicioSesion()
    {
        string nombreUsuario = TxtUsername.Text.Trim().ToLower();
        string contrasenia   = contraseniaVisible ? TxtPasswordVisible.Text : TxtPassword.Password;

        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasenia))
        { MostrarError("Por favor ingresa usuario y contraseña."); return; }

        if (nombreUsuario.Contains(' ') || contrasenia.Contains(' '))
        { MostrarError("El usuario y la contraseña no pueden contener espacios."); return; }

        if (ServicioAutenticacion.IniciarSesion(nombreUsuario, contrasenia))
        { LimpiarFormulario(); AlIniciarSesionExitoso?.Invoke(); }
        else
            MostrarError("Usuario o contraseña incorrectos.");
    }

    private void BotonMostrarContrasenia_Click(object sender, RoutedEventArgs e)
    {
        contraseniaVisible = !contraseniaVisible;
        if (contraseniaVisible)
        {
            TxtPasswordVisible.Text = TxtPassword.Password;
            TxtPasswordVisible.Visibility = Visibility.Visible;
            TxtPassword.Visibility        = Visibility.Collapsed;
            IconoOjo.Glyph = "\uED1A";
        }
        else
        {
            TxtPassword.Password   = TxtPasswordVisible.Text;
            TxtPassword.Visibility = Visibility.Visible;
            TxtPasswordVisible.Visibility = Visibility.Collapsed;
            IconoOjo.Glyph = "\uE7B3";
        }
    }

    private void BotonIrRegistro_Click(object sender, RoutedEventArgs e)
    { LimpiarFormulario(); AlIrARegistro?.Invoke(); }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        BordeError.Visibility = Visibility.Visible;
    }

    private void LimpiarFormulario()
    {
        TxtUsername.Text        = string.Empty;
        TxtPassword.Password    = string.Empty;
        TxtPasswordVisible.Text = string.Empty;
        BordeError.Visibility   = Visibility.Collapsed;
        contraseniaVisible      = false;
        TxtPassword.Visibility        = Visibility.Visible;
        TxtPasswordVisible.Visibility = Visibility.Collapsed;
        IconoOjo.Glyph = "\uE7B3";
    }
}
