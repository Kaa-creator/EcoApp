using System.Text.Json;
using EcoAppMobile.Helpers;

namespace EcoAppMobile;

public partial class EventsPage : ContentPage
{
    private List<EcoEvent> _allEvents = new();
    private string _currentCity = "Все";

    public EventsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCities();
        await LoadEvents();
    }

    private async Task LoadCities()
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{ApiConfig.BaseUrl}/api/Events/cities");
            var cities = JsonSerializer.Deserialize<List<string>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<string>();

            cities.Insert(0, "Все");
            CitiesList.ItemsSource = cities;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки городов: {ex.Message}");
        }
    }

    private async Task LoadEvents(string? city = null)
    {
        try
        {
            var client = new HttpClient();
            var url = $"{ApiConfig.BaseUrl}/api/Events";

            if (!string.IsNullOrEmpty(city) && city != "Все")
            {
                url += $"?city={Uri.EscapeDataString(city)}";
            }

            var response = await client.GetStringAsync(url);
            _allEvents = JsonSerializer.Deserialize<List<EcoEvent>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<EcoEvent>();

            var viewModels = _allEvents.Select(e => new EcoEventViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                City = e.City,
                Address = e.Address,
                EventDate = e.EventDate,
                EndDate = e.EndDate,
                Category = e.Category,
                ImageUrl = e.ImageUrl,
                Organizer = e.Organizer,
                ContactPhone = e.ContactPhone,
                Latitude = e.Latitude,
                Longitude = e.Longitude,
                HasImage = !string.IsNullOrEmpty(e.ImageUrl),
                HasCity = !string.IsNullOrEmpty(e.City),
                HasCoordinates = e.Latitude.HasValue && e.Longitude.HasValue
            }).ToList();

            EventsList.ItemsSource = viewModels;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить мероприятия: {ex.Message}", "OK");
        }
    }

    private async void OnCityTapped(object sender, TappedEventArgs e)
    {
        var city = e.Parameter as string;
        if (city == null) return;

        _currentCity = city;
        await LoadEvents(city);
    }

    private async void OnEventSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is EcoEventViewModel ecoEvent)
        {
            ((CollectionView)sender).SelectedItem = null;

            var details = $"📌 {ecoEvent.Title}\n\n";

            if (!string.IsNullOrEmpty(ecoEvent.City))
                details += $"🏙️ {ecoEvent.City}\n";

            if (!string.IsNullOrEmpty(ecoEvent.Address))
                details += $"📍 {ecoEvent.Address}\n";

            details += $"📅 {ecoEvent.EventDate:dd.MM.yyyy HH:mm}\n";

            if (!string.IsNullOrEmpty(ecoEvent.Category))
                details += $"🏷️ {ecoEvent.Category}\n";

            if (!string.IsNullOrEmpty(ecoEvent.Organizer))
                details += $"👤 {ecoEvent.Organizer}\n";

            if (!string.IsNullOrEmpty(ecoEvent.ContactPhone))
                details += $"📞 {ecoEvent.ContactPhone}\n";

            details += $"\n{ecoEvent.Description}";

            bool openMap = await DisplayAlert(ecoEvent.Title, details, "Маршрут", "Закрыть");

            if (openMap && ecoEvent.Latitude.HasValue && ecoEvent.Longitude.HasValue)
            {
                var url = $"https://www.google.com/maps/dir/?api=1&destination={ecoEvent.Latitude},{ecoEvent.Longitude}";
                await Launcher.Default.OpenAsync(url);
            }
            else if (openMap)
            {
                await DisplayAlert("Информация", "Для этого мероприятия не указан адрес", "OK");
            }
        }
    }

    private async void OnRouteClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var ecoEvent = button?.CommandParameter as EcoEventViewModel;

        if (ecoEvent?.Latitude == null || ecoEvent?.Longitude == null)
        {
            await DisplayAlert("Ошибка", "Координаты не указаны", "OK");
            return;
        }

        var url = $"https://www.google.com/maps/dir/?api=1&destination={ecoEvent.Latitude},{ecoEvent.Longitude}";
        await Launcher.Default.OpenAsync(url);
    }
}

public class EcoEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime EventDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Category { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Organizer { get; set; }
    public string? ContactPhone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class EcoEventViewModel : EcoEvent
{
    public bool HasImage { get; set; }
    public bool HasCity { get; set; }
    public bool HasCoordinates { get; set; }
}