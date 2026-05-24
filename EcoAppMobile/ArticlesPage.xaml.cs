
using EcoAppMobile.Helpers;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;

namespace EcoAppMobile;

public partial class ArticlesPage : ContentPage
{
    private List<Article> _articles = new ();
    private string _currentCategory = "Все";
    private ObservableCollection<CategoryFilter> _categories = new ();

    public ArticlesPage()
    {
        InitializeComponent();
        CategoriesList.ItemsSource = _categories;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategories();
        await LoadArticles();
    }

    private async Task LoadCategories()
    {
        try
        {
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{ApiConfig.BaseUrl}/api/Articles/categories");
            var categories = JsonSerializer.Deserialize<List<string>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<string>();

            _categories.Clear();

            // Добавляем "Все" как первую активную категорию
            _categories.Add(new CategoryFilter
            {
                Name = "Все",
                IsSelected = true,
                BackgroundColor = Color.FromArgb("#6A0DAD"),
                TextColor = Colors.White,
                BorderColor = Color.FromArgb("#6A0DAD")
            });

            foreach (var cat in categories)
            {
                _categories.Add(new CategoryFilter
                {
                    Name = cat,
                    IsSelected = false,
                    BackgroundColor = Color.FromArgb("#252540"),
                    TextColor = Colors.White,
                    BorderColor = Color.FromArgb("#27273A")
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
        }
    }

    private async Task LoadArticles(string? category = null)
    {
        try
        {
            var client = new HttpClient();
            var url = $"{ApiConfig.BaseUrl}/api/Articles";
            if (!string.IsNullOrEmpty(category) && category != "Все")
            {
                url += $"?category={Uri.EscapeDataString(category)}";
            }

            var response = await client.GetStringAsync(url);

            _articles = JsonSerializer.Deserialize < List < Article >> (response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List< Article > ();

            var viewModels = _articles.Select(a => new ArticleViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Category = a.Category,
                Preview = a.Content.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content
            }).ToList();

            ArticlesList.ItemsSource = viewModels;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить статьи: {ex.Message}", "OK");
        }
    }

    private void OnCategoryTapped(object sender, TappedEventArgs e)
    {
        var selected = e.Parameter as CategoryFilter;
        if (selected == null) return;

        // Сброс всех категорий на неактивный стиль
        foreach (var cat in _categories)
        {
            cat.IsSelected = false;
            cat.BackgroundColor = Color.FromArgb("#252540");
            cat.TextColor = Colors.White;
            cat.BorderColor = Color.FromArgb("#27273A");
        }

        // Активная категория — фиолетовая
        selected.IsSelected = true;
        selected.BackgroundColor = Color.FromArgb("#6A0DAD");
        selected.TextColor = Colors.White;
        selected.BorderColor = Color.FromArgb("#6A0DAD");

        _currentCategory = selected.Name;
        _ = LoadArticles(selected.Name);
    }

    private async void OnArticleSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ArticleViewModel article)
        {
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new ArticleDetailPage(article));
        }
    }

    private async void OnSdgsTapped(object sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("https://sdgs.by");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось открыть сайт: {ex.Message}", "OK");
        }
    }
}

// ✅ НОВАЯ МОДЕЛЬ для фильтров с цветами
public class CategoryFilter : INotifyPropertyChanged
{
    public string Name { get; set; } = "";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    private Color _backgroundColor = Color.FromArgb("#252540");
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }

    private Color _textColor = Colors.White;
    public Color TextColor
    {
        get => _textColor;
        set
        {
            _textColor = value;
            OnPropertyChanged(nameof(TextColor));
        }
    }

    private Color _borderColor = Color.FromArgb("#27273A");
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "";
}

public class ArticleViewModel : Article
{
    public string Preview { get; set; } = "";
}