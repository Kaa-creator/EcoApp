using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;
using EcoAppMobile.Models;

namespace EcoAppMobile;

public partial class MainPage : ContentPage
{
    private bool _isPasswordVisible = false;
    private const string AppVersion = "1.2";

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Шаг 1: Проверяем, первый ли это запуск после установки
        // Preferences удаляются при удалении приложения — идеально для флага
        var hasLaunchedBefore = Preferences.Get("hasLaunchedBefore", false);

        if (!hasLaunchedBefore)
        {
            // ✅ Первый запуск — чистим SecureStorage на всякий случай
            // (могли остаться данные от предыдущей установки)
            SecureStorage.Remove("userId");
            SecureStorage.Remove("userName");
            SecureStorage.Remove("userEmail");
            SecureStorage.Remove("token");
            SecureStorage.Remove("userRole");
            SecureStorage.Remove("appVersion");

            // Помечаем что приложение уже запускалось
            Preferences.Set("hasLaunchedBefore", true);
            await SecureStorage.SetAsync("appVersion", AppVersion);

            // Остаёмся на форме входа — ничего больше не делаем
            return;
        }

        // Шаг 2: Не первый запуск — проверяем версию приложения
        await CheckVersionAndClearStorage();

        // Шаг 3: Проверяем сохранённую сессию
        await CheckLogin();

        // Шаг 4: Если пришли с регистрации — подставляем данные
        await FillRegistrationData();
    }

    private async Task CheckVersionAndClearStorage()
    {
        var savedVersion = await SecureStorage.GetAsync("appVersion");

        // Если версия изменилась — сбрасываем сессию
        // (на случай breaking changes в API или структуре данных)
        if (savedVersion != AppVersion)
        {
            SecureStorage.Remove("userId");
            SecureStorage.Remove("userName");
            SecureStorage.Remove("userEmail");
            SecureStorage.Remove("token");
            SecureStorage.Remove("userRole");

            await SecureStorage.SetAsync("appVersion", AppVersion);
        }
    }

    private async Task CheckLogin()
    {
        var token = await SecureStorage.GetAsync("token");
        var userId = await SecureStorage.GetAsync("userId");

        // Нет сохранённой сессии — остаёмся на форме входа
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId))
        {
            return;
        }

        // ✅ Есть сессия — переходим в приложение
        Application.Current.MainPage = new AppShell();
    }

    private async Task FillRegistrationData()
    {
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