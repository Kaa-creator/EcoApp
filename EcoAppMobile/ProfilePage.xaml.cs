using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;

namespace EcoAppMobile;

public partial class ProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private int _userId;
    private User _currentUser = new();

    private readonly Dictionary<int, string> _levelTags = new()
    {
        { 1, "🌱 Новичок" },
        { 2, "🌿 Эко-старт" },
        { 3, "♻️ Переработчик" },
        { 4, "🌳 Друг природы" },
        { 5, "⚡ Эко-активист" },
        { 6, "🌍 Защитник планеты" },
        { 7, "🏆 Эко-мастер" },
        { 8, "👑 Эко-король" },
        { 9, "🌟 Легенда экологии" },
        { 10, "🚀 Спаситель Земли" }
    };

    public ProfilePage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserId();
        await LoadProfile();
        await LoadStats();
        await LoadTelegramStatus();
        await LoadAvatar();
    }

    private async Task LoadUserId()
    {
        var userIdStr = await SecureStorage.GetAsync("userId");
        if (int.TryParse(userIdStr, out int id))
        {
            _userId = id;
        }
        else
        {
            _userId = 1;
        }
    }

    private async Task LoadProfile()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                _currentUser = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new User();

                NameEntry.Text = _currentUser.Name;
                EmailEntry.Text = _currentUser.Email;
                PhoneEntry.Text = _currentUser.Phone;
                UserNameLabel.Text = _currentUser.Name;

                UpdateLevelDisplay(_currentUser.Points);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить профиль: {ex.Message}", "OK");
        }
    }

    private void UpdateLevelDisplay(int points)
    {
        int level = Math.Min((points / 100) + 1, 10);
        int nextLevelPoints = level * 100;
        int currentLevelBase = (level - 1) * 100;
        int pointsInLevel = points - currentLevelBase;
        int pointsNeeded = 100;

        double progress = (double)pointsInLevel / pointsNeeded;

        UserTagLabel.Text = _levelTags[level];
        LevelProgress.Progress = progress;
        ProgressTextLabel.Text = $"{points} / {nextLevelPoints} баллов (уровень {level})";
    }

    private async Task LoadStats()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}/stats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<UserStats>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (stats != null)
                {
                    TotalPointsLabel.Text = stats.TotalPoints.ToString();
                    CompletedTasksLabel.Text = stats.CompletedTasks.ToString();
                    PendingTasksLabel.Text = stats.PendingTasks.ToString();
                    TotalEarnedLabel.Text = stats.TotalEarned.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
        }
    }

    private async Task LoadAvatar()
    {
        try
        {
            var savedPath = await SecureStorage.GetAsync($"avatarPath_{_userId}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
                {
                    AvatarImage.Source = ImageSource.FromFile(savedPath);
                }
                else
                {
                    AvatarImage.Source = new FontImageSource
                    {
                        Glyph = "🌿",
                        FontFamily = "OpenSansRegular",
                        Size = 50,
                        Color = Colors.White
                    };
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
        }
    }

    private async void OnAvatarTapped(object sender, TappedEventArgs e)
    {
        bool changePhoto = await DisplayAlert(
            "Сменить фото",
            "Вы хотите изменить фото профиля?",
            "Да",
            "Нет");

        if (!changePhoto) return;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.StorageRead>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Нет доступа", "Разрешите доступ к галерее в настройках телефона", "OK");
                return;
            }

            var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Выберите новое фото профиля"
            });

            if (photo == null) return;

            var localPath = Path.Combine(FileSystem.AppDataDirectory, $"avatar_{_userId}.jpg");

            using (var sourceStream = await photo.OpenReadAsync())
            using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(fileStream);
            }

            await SecureStorage.SetAsync($"avatarPath_{_userId}", localPath);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AvatarImage.Source = ImageSource.FromFile(localPath);
            });

            await DisplayAlert("Готово", "Фото профиля обновлено!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось изменить фото: {ex.Message}", "OK");
        }
    }

    private bool _isTelegramSubscribed = false;

    private async Task LoadTelegramStatus()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}/telegram-status");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<TelegramStatusResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (status != null)
                {
                    _isTelegramSubscribed = status.IsSubscribed;
                    UpdateTelegramButtonUI();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки статуса Telegram: {ex.Message}");
        }
    }

    private void UpdateTelegramButtonUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_isTelegramSubscribed)
            {
                TelegramButton.Text = "✅ Подписка активна";
                TelegramButton.BackgroundColor = Colors.Green;
                TelegramButton.TextColor = Colors.White;
            }
            else
            {
                TelegramButton.Text = "🔔 Подписаться на уведомления";
                TelegramButton.BackgroundColor = Color.FromHex("#6A0DAD");
                TelegramButton.TextColor = Colors.White;
            }
        });
    }

    private async void OnTelegramButtonClicked(object sender, EventArgs e)
    {
        if (_isTelegramSubscribed)
        {
            var action = await DisplayActionSheet(
                "Уведомления Telegram",
                "Отмена",
                null,
                "Отписаться");

            if (action == "Отписаться")
            {
                await UnsubscribeTelegram();
            }
        }
        else
        {
            await SubscribeTelegram();
        }
    }

    private async Task SubscribeTelegram()
    {
        try
        {
            var botUsername = "ecoapp_belarus_bot";
            var telegramUrl = $"https://t.me/{botUsername}?start=USERID_{_userId}";

            var canOpen = await Launcher.CanOpenAsync(telegramUrl);
            if (canOpen)
            {
                await Launcher.Default.OpenAsync(telegramUrl);

                await DisplayAlert(
                    "Подписка",
                    "Открылся Telegram. Нажмите «Start» в боте.\nПосле этого вернитесь в приложение.",
                    "Понятно");
            }
            else
            {
                await DisplayAlert(
                    "Telegram не найден",
                    $"Найдите бота @{botUsername} и отправьте:\n/start USERID_{_userId}",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async Task UnsubscribeTelegram()
    {
        bool confirm = await DisplayAlert(
            "Отписка",
            "Отписаться от уведомлений?",
            "Да", "Нет");

        if (!confirm) return;

        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}/telegram-unsubscribe");
            if (response.IsSuccessStatusCode)
            {
                _isTelegramSubscribed = false;
                UpdateTelegramButtonUI();
                await DisplayAlert("Готово", "Вы отписались", "OK");
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

    private async void OnSaveProfileClicked(object sender, EventArgs e)
    {
        try
        {
            var updateData = new
            {
                Name = NameEntry.Text,
                Email = EmailEntry.Text,
                Phone = PhoneEntry.Text
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Профиль обновлен!", "OK");
                UserNameLabel.Text = updateData.Name;
                await SecureStorage.SetAsync("userName", updateData.Name);
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

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        var oldPass = OldPasswordEntry.Text;
        var newPass = NewPasswordEntry.Text;
        var confirmPass = ConfirmPasswordEntry.Text;

        if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
        {
            await DisplayAlert("Ошибка", "Заполните все поля", "OK");
            return;
        }

        if (newPass != confirmPass)
        {
            await DisplayAlert("Ошибка", "Пароли не совпадают", "OK");
            return;
        }

        try
        {
            var data = new { OldPassword = oldPass, NewPassword = newPass };
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/Users/{_userId}/password", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Пароль изменен!", "OK");
                OldPasswordEntry.Text = "";
                NewPasswordEntry.Text = "";
                ConfirmPasswordEntry.Text = "";
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

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Выход", "Выйти из аккаунта?", "Да", "Нет");
        if (confirm)
        {
            SecureStorage.Remove("userId");
            SecureStorage.Remove("userName");
            SecureStorage.Remove("userEmail");
            SecureStorage.Remove("token");
            SecureStorage.Remove("userRole");

            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public int Points { get; set; }
}

public class UserStats
{
    public int TotalPoints { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int TotalEarned { get; set; }
}

public class TelegramStatusResponse
{
    public bool IsSubscribed { get; set; }
}