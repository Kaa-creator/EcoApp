using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;
using EcoAppMobile.Models;
using System.Text.RegularExpressions;

namespace EcoAppMobile;

public partial class MainPage : ContentPage
{
    private bool _isPasswordVisible = false;

    public MainPage()
    {
        InitializeComponent();
        CheckLogin();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var savedEmail = await SecureStorage.GetAsync("tempEmail");
        var savedPassword = await SecureStorage.GetAsync("tempPassword");

        if (!string.IsNullOrEmpty(savedEmail))
        {
            EmailEntry.Text = savedEmail;
            PasswordEntry.Text = savedPassword;

            SecureStorage.Remove("tempEmail");
            SecureStorage.Remove("tempPassword");
        }
    }

    private async void CheckLogin()
    {
        var userId = await SecureStorage.GetAsync("userId");
        var role = await SecureStorage.GetAsync("userRole");

        if (!string.IsNullOrEmpty(userId))
        {
            Application.Current.MainPage = new AppShell();
        }
    }

    private void OnEyeTapped(object sender, TappedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PasswordEntry.IsPassword = !_isPasswordVisible;
        EyeIcon.Text = _isPasswordVisible ? "🙈" : "👁️";
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Ошибка", "Введите email и пароль", "OK");
                return;
            }

            // ✅ ВАЛИДАЦИЯ EMAIL
            if (!IsValidEmail(email))
            {
                await DisplayAlert("Ошибка", "Введите корректный email (должен содержать @)", "OK");
                return;
            }

            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var data = new { Email = email, Password = password };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{ApiConfig.BaseUrl}/api/Auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResult>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResult != null)
                {
                    await SecureStorage.SetAsync("userId", loginResult.UserId.ToString());
                    await SecureStorage.SetAsync("userName", loginResult.UserName);
                    await SecureStorage.SetAsync("userEmail", loginResult.Email);
                    await SecureStorage.SetAsync("token", loginResult.Token);
                    await SecureStorage.SetAsync("userRole", loginResult.Role);

                    Application.Current.MainPage = new AppShell();
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    // ✅ ВАЛИДАЦИЯ EMAIL
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return email.Contains("@") && email.Contains(".");
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}