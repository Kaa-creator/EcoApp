using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;
using System.Text.RegularExpressions;

namespace EcoAppMobile;

public partial class RegisterPage : ContentPage
{
    private bool _isRegPasswordVisible = false;

    public RegisterPage()
    {
        InitializeComponent();
    }

    private void OnRegEyeTapped(object sender, TappedEventArgs e)
    {
        _isRegPasswordVisible = !_isRegPasswordVisible;
        PasswordEntry.IsPassword = !_isRegPasswordVisible;
        RegEyeIcon.Text = _isRegPasswordVisible ? "🙈" : "👁️";
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        try
        {
            var name = NameEntry.Text?.Trim();
            var email = EmailEntry.Text?.Trim();
            var phone = PhoneEntry.Text?.Trim();
            var password = PasswordEntry.Text;
            var confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Ошибка", "Заполните все обязательные поля", "OK");
                return;
            }

            // ✅ ВАЛИДАЦИЯ EMAIL
            if (!IsValidEmail(email))
            {
                await DisplayAlert("Ошибка", "Введите корректный email (должен содержать @ и домен)", "OK");
                return;
            }

            // ✅ ВАЛИДАЦИЯ ТЕЛЕФОНА — ТОЛЬКО + И ЦИФРЫ
            if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
            {
                await DisplayAlert("Ошибка", "Телефон должен начинаться с + и содержать только цифры", "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
                return;
            }

            if (password.Length < 6)
            {
                await DisplayAlert("Ошибка", "Пароль должен быть не менее 6 символов", "OK");
                return;
            }

            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var data = new
            {
                name = name,
                email = email,
                phone = phone ?? "",
                passwordHash = password,
                role = "User",
                points = 0
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{ApiConfig.BaseUrl}/api/Auth/register", content);

            if (response.IsSuccessStatusCode)
            {
                // ✅ Подтверждение email отключено — сразу сохраняем данные и входим
                await SecureStorage.SetAsync("tempEmail", email);
                await SecureStorage.SetAsync("tempPassword", password);

                await DisplayAlert(
                    "Регистрация успешна!",
                    "Теперь вы можете войти в приложение.",
                    "OK");

                await Navigation.PopAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка", $"Не удалось зарегистрироваться\n{error}", "OK");
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

    // ✅ ВАЛИДАЦИЯ ТЕЛЕФОНА — ТОЛЬКО + И ЦИФРЫ
    private bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return true; // Необязательное поле
        return Regex.IsMatch(phone, @"^\+\d+$");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}