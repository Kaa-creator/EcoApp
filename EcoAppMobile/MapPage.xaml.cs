using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using EcoAppMobile.Models;
using EcoAppMobile.Helpers;
using System.Text.Json;

namespace EcoAppMobile;

public partial class MapPage : ContentPage
{
    private List<EcoPoint> _allPoints = new();
    private string _currentCategory = "Все";

    public MapPage()
    {
        InitializeComponent();

        var location = new Location(53.9, 27.5667);
        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(5)));

        UpdateButtonStyles();
    }

    // ✅ ПЕРЕЗАГРУЗКА ТОЧЕК ПРИ КАЖДОМ ПОЯВЛЕНИИ СТРАНИЦЫ
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPoints();
    }

    private async Task LoadPoints()
    {
        try
        {
            var client = new HttpClient();
            var url = $"{ApiConfig.BaseUrl}/api/EcoPoints";
            var response = await client.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _allPoints = JsonSerializer.Deserialize<List<EcoPoint>>(response, options)
                        ?? new List<EcoPoint>();

            DisplayPoints();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
        }
    }

    private void DisplayPoints()
    {
        MyMap.Pins.Clear();

        var filteredPoints = _currentCategory == "Все"
            ? _allPoints
            : _allPoints.Where(p =>
                !string.IsNullOrWhiteSpace(p.Category) &&
                p.Category.Trim().ToLower() == _currentCategory.Trim().ToLower()
            ).ToList();

        foreach (var point in filteredPoints)
        {
            if (point.Latitude == 0 && point.Longitude == 0)
                continue;

            var pin = new Pin
            {
                Label = $"{GetIcon(point.Category)} {point.Name}",
                Address = point.Address,
                Location = new Location(point.Latitude, point.Longitude)
            };

            pin.MarkerClicked += async (s, e) =>
            {
                e.HideInfoWindow = true;
                bool go = await DisplayAlert(
                    $"{GetIcon(point.Category)} {point.Name}",
                    $"{point.Description}\n\nКатегория: {point.Category}\nАдрес: {point.Address}",
                    "Маршрут",
                    "Закрыть");

                if (go)
                {
                    var url = $"https://www.google.com/maps/dir/?api=1&destination={point.Latitude},{point.Longitude}";
                    await Launcher.Default.OpenAsync(url);
                }
            };

            MyMap.Pins.Add(pin);
        }

        if (filteredPoints.Count > 0)
        {
            var first = filteredPoints.First();
            MyMap.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude),
                    Distance.FromKilometers(3)));
        }
    }

    private string GetIcon(string category)
    {
        return category?.Trim().ToLower() switch
        {
            "переработка" => "♻️",
            "зарядки" => "⚡",
            "приюты" => "🐶",
            _ => "📍"
        };
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        _currentCategory = button.Text;

        UpdateButtonStyles();
        DisplayPoints();
    }

    private void UpdateButtonStyles()
    {
        BtnAll.BackgroundColor = Color.FromArgb("#252540");
        BtnAll.TextColor = Color.FromArgb("#A1A1AA");
        BtnRecycling.BackgroundColor = Color.FromArgb("#252540");
        BtnRecycling.TextColor = Color.FromArgb("#A1A1AA");
        BtnCharging.BackgroundColor = Color.FromArgb("#252540");
        BtnCharging.TextColor = Color.FromArgb("#A1A1AA");
        BtnShelter.BackgroundColor = Color.FromArgb("#252540");
        BtnShelter.TextColor = Color.FromArgb("#A1A1AA");

        var activeButton = _currentCategory switch
        {
            "Все" => BtnAll,
            "Переработка" => BtnRecycling,
            "Зарядки" => BtnCharging,
            "Приюты" => BtnShelter,
            _ => BtnAll
        };

        activeButton.BackgroundColor = Color.FromArgb("#6A0DAD");
        activeButton.TextColor = Colors.White;
    }
}