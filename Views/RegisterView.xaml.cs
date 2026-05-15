using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameStoreApp.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace GameStoreApp.Views;

public sealed partial class RegisterView : UserControl
{
    public event Action? AlRegistrarseExitoso;
    public event Action? AlIrAInicioSesion;

    private bool contraseniaVisible = false;

    public RegisterView() => this.InitializeComponent();

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

        string error = ServicioAutenticacion.ValidarNombreUsuario(TxtUsername.Text);
        TxtUsernameError.Text = error;
        TxtUsernameError.Visibility = string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void TxtPassword_Changed(object sender, RoutedEventArgs e)
        => ValidarContraseniaEnVivo(TxtPassword.Password);

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
        ValidarContraseniaEnVivo(TxtPasswordVisible.Text);
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

    private void ValidarContraseniaEnVivo(string contrasenia)
    {
        bool longitudValida = contrasenia.Length >= 6 && contrasenia.Length <= 20;
        bool tieneMayuscula = Regex.IsMatch(contrasenia, @"[A-Z]");
        bool tieneNumero    = Regex.IsMatch(contrasenia, @"[0-9]");
        bool tieneEspecial  = Regex.IsMatch(contrasenia, @"[@#$%]");

        var pincelOk      = new SolidColorBrush(Colors.Green);
        var pincelError   = new SolidColorBrush(Colors.Red);
        var pincelDefault = Application.Current.Resources["SecondaryTextBrush"] as SolidColorBrush;

        bool vacio = string.IsNullOrEmpty(contrasenia);

        TxtReq1.Foreground = vacio ? pincelDefault : (longitudValida ? pincelOk : pincelError);
        TxtReq1.Text       = (!vacio && longitudValida) ? "  \u2713 6 a 20 caracteres" : "  \u2022 6 a 20 caracteres";

        TxtReq2.Foreground = vacio ? pincelDefault : (tieneMayuscula ? pincelOk : pincelError);
        TxtReq2.Text       = (!vacio && tieneMayuscula) ? "  \u2713 Al menos 1 letra mayúscula" : "  \u2022 Al menos 1 letra mayúscula";

        TxtReq3.Foreground = vacio ? pincelDefault : (tieneNumero ? pincelOk : pincelError);
        TxtReq3.Text       = (!vacio && tieneNumero) ? "  \u2713 Al menos 1 número" : "  \u2022 Al menos 1 número";

        TxtReq4.Foreground = vacio ? pincelDefault : (tieneEspecial ? pincelOk : pincelError);
        TxtReq4.Text       = (!vacio && tieneEspecial) ? "  \u2713 Al menos 1 especial (@,#,$,%)" : "  \u2022 Al menos 1 especial (@,#,$,%)";

        string error = ServicioAutenticacion.ValidarContrasenia(contrasenia);
        TxtPasswordError.Text = error;
        TxtPasswordError.Visibility = (!string.IsNullOrEmpty(error) && !vacio)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
    {
        contraseniaVisible = !contraseniaVisible;
        if (contraseniaVisible)
        {
            TxtPasswordVisible.Text = TxtPassword.Password;
            TxtPasswordVisible.Visibility = Visibility.Visible;
            TxtPassword.Visibility        = Visibility.Collapsed;
            BtnTogglePassword.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uED1A", FontSize = 15 };
        }
        else
        {
            TxtPassword.Password   = TxtPasswordVisible.Text;
            TxtPassword.Visibility = Visibility.Visible;
            TxtPasswordVisible.Visibility = Visibility.Collapsed;
            BtnTogglePassword.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uE7B3", FontSize = 15 };
        }
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        TxtGeneralError.Visibility = Visibility.Collapsed;
        BtnRegister.IsEnabled = false;

        string nombreUsuario = TxtUsername.Text.Trim().ToLower();
        string contrasenia   = contraseniaVisible ? TxtPasswordVisible.Text : TxtPassword.Password;

        try
        {
            string error = await ServicioAutenticacion.RegistrarAsync(nombreUsuario, contrasenia);
            if (!string.IsNullOrEmpty(error))
            {
                TxtGeneralError.Text = error;
                TxtGeneralError.Visibility = Visibility.Visible;
            }
            else { LimpiarFormulario(); AlRegistrarseExitoso?.Invoke(); }
        }
        catch (Exception ex)
        {
            TxtGeneralError.Text = $"Error inesperado: {ex.Message}";
            TxtGeneralError.Visibility = Visibility.Visible;
        }
        finally { BtnRegister.IsEnabled = true; }
    }

    private void BtnGoLogin_Click(object sender, RoutedEventArgs e)
    { LimpiarFormulario(); AlIrAInicioSesion?.Invoke(); }

    private void LimpiarFormulario()
    {
        TxtUsername.Text        = string.Empty;
        TxtPassword.Password    = string.Empty;
        TxtPasswordVisible.Text = string.Empty;
        TxtUsernameError.Visibility = Visibility.Collapsed;
        TxtPasswordError.Visibility = Visibility.Collapsed;
        TxtGeneralError.Visibility  = Visibility.Collapsed;
        contraseniaVisible = false;
        TxtPassword.Visibility        = Visibility.Visible;
        TxtPasswordVisible.Visibility = Visibility.Collapsed;
        BtnTogglePassword.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uE7B3", FontSize = 15 };
    }
}
