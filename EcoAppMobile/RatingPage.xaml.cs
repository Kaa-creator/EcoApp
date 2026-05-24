using EcoAppMobile.Helpers;
using System.Text.Json;

namespace EcoAppMobile;

public partial class RatingPage : ContentPage
{
    public RatingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLeaderboard();
    }

    private async Task LoadLeaderboard()
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{ApiConfig.BaseUrl}/api/Users/leaderboard");

            var users = JsonSerializer.Deserialize<List<RatingUser>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (users == null) return;

            var filteredUsers = users
                .Where(u => u.Role != "Admin")
                .ToList();

            // Заполняем подиум
            if (filteredUsers.Count > 0)
            {
                FirstName.Text = filteredUsers[0].Name;
                FirstPoints.Text = filteredUsers[0].Points.ToString();
            }
            if (filteredUsers.Count > 1)
            {
                SecondName.Text = filteredUsers[1].Name;
                SecondPoints.Text = filteredUsers[1].Points.ToString();
            }
            if (filteredUsers.Count > 2)
            {
                ThirdName.Text = filteredUsers[2].Name;
                ThirdPoints.Text = filteredUsers[2].Points.ToString();
            }

            // Остальные участники
            var viewModels = new List< LeaderboardItem > ();
            for (int i = 3; i < filteredUsers.Count; i++)
            {
                var levelNum = Math.Min((filteredUsers[i].Points / 100) + 1, 10);
                var levelTag = levelNum switch
                {
                    1 => "🌱 Новичок",
                    2 => "🌿 Эко-старт",
                    3 => "♻️ Переработчик",
                    4 => "🌳 Друг природы",
                    5 => "⚡ Эко-активист",
                    6 => "🌍 Защитник планеты",
                    7 => "🏆 Эко-мастер",
                    8 => "👑 Эко-король",
                    9 => "🌟 Легенда экологии",
                    10 => "🚀 Спаситель Земли",
                    _ => "🌱 Новичок"
                };

                viewModels.Add(new LeaderboardItem
                {
                    Name = filteredUsers[i].Name,
                    Points = filteredUsers[i].Points,
                    PlaceNumber = (i + 1).ToString(),
                    LevelTag = levelTag
                });
            }

            LeaderboardList.ItemsSource = viewModels;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить рейтинг: {ex.Message}", "OK");
        }
    }
}

public class RatingUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public int Points { get; set; }
    public string Role { get; set; } = "User";
}

public class LeaderboardItem
{
    public string Name { get; set; } = "";
    public int Points { get; set; }
    public string PlaceNumber { get; set; } = "";
    public string LevelTag { get; set; } = "";
}