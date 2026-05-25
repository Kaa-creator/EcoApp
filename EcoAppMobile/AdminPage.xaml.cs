using EcoAppMobile.Helpers;
using Microsoft.Maui.Controls.Shapes;
using System.Text;
using System.Text.Json;

namespace EcoAppMobile;

public partial class AdminPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _currentTab = "reports";

    public AdminPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCurrentTabAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadCurrentTabAsync();
        AdminRefreshView.IsRefreshing = false;
    }

    private void OnTabClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        TabReports.BackgroundColor = Color.FromArgb("#2a2a3e");
        TabPoints.BackgroundColor = Color.FromArgb("#2a2a3e");
        TabTasks.BackgroundColor = Color.FromArgb("#2a2a3e");
        TabArticles.BackgroundColor = Color.FromArgb("#2a2a3e");
        TabEvents.BackgroundColor = Color.FromArgb("#2a2a3e");

        button.BackgroundColor = Color.FromArgb("#6A0DAD");

        _currentTab = button switch
        {
            _ when button == TabReports => "reports",
            _ when button == TabPoints => "points",
            _ when button == TabTasks => "tasks",
            _ when button == TabArticles => "articles",
            _ when button == TabEvents => "events",
            _ => "reports"
        };

        _ = LoadCurrentTabAsync();
    }

    private async Task LoadCurrentTabAsync()
    {
        ContentLayout.Children.Clear();

        switch (_currentTab)
        {
            case "reports": await LoadReports(); break;
            case "points": await LoadPoints(); break;
            case "tasks": await LoadTasks(); break;
            case "articles": await LoadArticles(); break;
            case "events": await LoadEvents(); break;
        }
    }

    // ============================================
    // ПРОВЕРКА ЗАДАНИЙ
    // ============================================

    private async Task LoadReports()
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/TaskReports/pending");

            if (!response.IsSuccessStatusCode)
            {
                ContentLayout.Children.Add(new Label { Text = "Ошибка загрузки", TextColor = Colors.Red });
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var reports = JsonSerializer.Deserialize<List<PendingReport>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (reports == null || reports.Count == 0)
            {
                ContentLayout.Children.Add(new Label
                {
                    Text = "Нет заданий на проверке",
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 50)
                });
                return;
            }

            foreach (var report in reports)
            {
                ContentLayout.Children.Add(CreateReportCard(report));
            }
        }
        catch (Exception ex)
        {
            ContentLayout.Children.Add(new Label { Text = $"Ошибка: {ex.Message}", TextColor = Colors.Red });
        }
    }

    private Border CreateReportCard(PendingReport report)
    {
        var border = CreateCardBorder();
        var layout = new VerticalStackLayout { Spacing = 10 };

        layout.Children.Add(new Label
        {
            Text = $"👤 {report.UserName} (ID: {report.UserId})",
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16
        });

        layout.Children.Add(new Label
        {
            Text = $"📋 {report.TaskTitle}",
            TextColor = Color.FromArgb("#e0e0e0"),
            FontSize = 14
        });

        layout.Children.Add(new Label
        {
            Text = $"+{report.TaskPoints} баллов",
            TextColor = Color.FromArgb("#4CAF50"),
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        });

        if (!string.IsNullOrEmpty(report.PhotoUrl))
        {
            var fullUrl = $"{ApiConfig.BaseUrl}{report.PhotoUrl}";
            layout.Children.Add(new Image
            {
                Source = fullUrl,
                HeightRequest = 200,
                Aspect = Aspect.AspectFit
            });
        }

        if (!string.IsNullOrEmpty(report.Comment))
        {
            layout.Children.Add(new Label
            {
                Text = $"💬 {report.Comment}",
                TextColor = Color.FromArgb("#888"),
                FontAttributes = FontAttributes.Italic,
                FontSize = 12
            });
        }

        var buttonsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        var approveBtn = new Button
        {
            Text = "✅ Одобрить",
            BackgroundColor = Color.FromArgb("#4CAF50"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 45,
            FontSize = 14
        };
        approveBtn.Clicked += async (s, e) => await ApproveReport(report.Id);

        var rejectBtn = new Button
        {
            Text = "❌ Отклонить",
            BackgroundColor = Color.FromArgb("#ff4444"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 45,
            FontSize = 14
        };
        rejectBtn.Clicked += async (s, e) => await RejectReport(report.Id);

        buttonsGrid.Children.Add(approveBtn);
        buttonsGrid.SetColumn(approveBtn, 0);
        buttonsGrid.Children.Add(rejectBtn);
        buttonsGrid.SetColumn(rejectBtn, 1);

        layout.Children.Add(buttonsGrid);
        border.Content = layout;
        return border;
    }

    private async Task ApproveReport(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/TaskReports/{id}/approve", null);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Задание одобрено!", "OK");
                await LoadReports();
            }
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    private async Task RejectReport(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/TaskReports/{id}/reject", null);
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Задание отклонено!", "OK");
                await LoadReports();
            }
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    // ============================================
    // ЭКО-ТОЧКИ (CRUD)
    // ============================================

    private async Task LoadPoints()
    {
        AddTitle("Эко-точки");
        AddButton("➕ Добавить точку", async () => await ShowPointForm());

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/EcoPoints");
            var json = await response.Content.ReadAsStringAsync();
            var points = JsonSerializer.Deserialize<List<EcoPointAdmin>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (points == null) return;

            foreach (var point in points)
            {
                ContentLayout.Children.Add(CreatePointCard(point));
            }
        }
        catch (Exception ex) { ContentLayout.Children.Add(new Label { Text = ex.Message, TextColor = Colors.Red }); }
    }

    private Border CreatePointCard(EcoPointAdmin point)
    {
        var border = CreateCardBorder();
        var layout = new VerticalStackLayout { Spacing = 5 };

        layout.Children.Add(new Label { Text = point.Name, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        layout.Children.Add(new Label { Text = point.Address, TextColor = Color.FromArgb("#888"), FontSize = 12 });
        layout.Children.Add(new Label { Text = $"Категория: {point.Category}", TextColor = Color.FromArgb("#6A0DAD"), FontSize = 12 });

        var buttons = new HorizontalStackLayout { Spacing = 10 };
        buttons.Children.Add(CreateActionButton("✏️", "#2196F3", async () => await EditPoint(point)));
        buttons.Children.Add(CreateActionButton("🗑️", "#ff4444", async () => await DeletePoint(point.Id)));
        layout.Children.Add(buttons);

        border.Content = layout;
        return border;
    }

    private async Task ShowPointForm(EcoPointAdmin? point = null)
    {
        var nameEntry = new Entry { Placeholder = "Название", Text = point?.Name ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var descEntry = new Entry { Placeholder = "Описание", Text = point?.Description ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var catEntry = new Entry { Placeholder = "Категория", Text = point?.Category ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var addrEntry = new Entry { Placeholder = "Адрес", Text = point?.Address ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };

        // ✅ НОВЫЕ ПОЛЯ (необязательные)
        var phoneEntry = new Entry { Placeholder = "Телефон (необязательно)", Text = point?.Phone ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Telephone };
        var websiteEntry = new Entry { Placeholder = "Веб-сайт (необязательно)", Text = point?.Website ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Url };

        var latEntry = new Entry { Placeholder = "Широта", Text = point?.Latitude.ToString() ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Numeric };
        var lonEntry = new Entry { Placeholder = "Долгота", Text = point?.Longitude.ToString() ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Numeric };

        var scroll = new ScrollView();
        var formLayout = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(20) };
        formLayout.Children.Add(new Label { Text = point == null ? "Новая точка" : "Редактирование", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        formLayout.Children.Add(nameEntry);
        formLayout.Children.Add(descEntry);
        formLayout.Children.Add(catEntry);
        formLayout.Children.Add(addrEntry);
        formLayout.Children.Add(phoneEntry);      // ✅ ДОБАВЛЕНО
        formLayout.Children.Add(websiteEntry);    // ✅ ДОБАВЛЕНО
        formLayout.Children.Add(latEntry);
        formLayout.Children.Add(lonEntry);

        var saveBtn = new Button { Text = "💾 Сохранить", BackgroundColor = Color.FromArgb("#4CAF50"), TextColor = Colors.White };
        saveBtn.Clicked += async (s, e) =>
        {
            // ✅ Обрабатываем и точку, и запятую
            var latText = latEntry.Text.Replace('.', ',');
            var lonText = lonEntry.Text.Replace('.', ',');

            if (!double.TryParse(latText, out double lat))
            {
                await DisplayAlert("Ошибка", "Неверная широта. Используйте точку или запятую", "OK");
                return;
            }
            if (!double.TryParse(lonText, out double lon))
            {
                await DisplayAlert("Ошибка", "Неверная долгота. Используйте точку или запятую", "OK");
                return;
            }

            var newPoint = new EcoPointAdmin
            {
                Id = point?.Id ?? 0,
                Name = nameEntry.Text,
                Description = descEntry.Text,
                Category = catEntry.Text,
                Address = addrEntry.Text,
                Phone = string.IsNullOrWhiteSpace(phoneEntry.Text) ? null : phoneEntry.Text,        // ✅
                Website = string.IsNullOrWhiteSpace(websiteEntry.Text) ? null : websiteEntry.Text,  // ✅
                Latitude = lat,
                Longitude = lon
            };

            await SavePoint(newPoint);
            await Navigation.PopAsync();
            await LoadPoints();
        };

        formLayout.Children.Add(saveBtn);
        scroll.Content = formLayout;

        await Navigation.PushAsync(new ContentPage { Content = scroll, BackgroundColor = Color.FromArgb("#1a1a2e") });
    }

    private async Task SavePoint(EcoPointAdmin point)
    {
        try
        {
            await SetAuthHeaderAsync();
            var json = JsonSerializer.Serialize(point);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (point.Id == 0)
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/EcoPoints", content);
            else
                response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/EcoPoints/{point.Id}", content);

            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Ошибка", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    private async Task EditPoint(EcoPointAdmin point) => await ShowPointForm(point);

    private async Task DeletePoint(int id)
    {
        bool confirm = await DisplayAlert("Удаление", "Удалить точку?", "Да", "Нет");
        if (!confirm) return;

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"{ApiConfig.BaseUrl}/api/EcoPoints/{id}");
            if (response.IsSuccessStatusCode) await LoadPoints();
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    // ============================================
    // ЗАДАНИЯ (CRUD) — С ФОТО SWITCH
    // ============================================

    private async Task LoadTasks()
    {
        AddTitle("Эко-задания");
        AddButton("➕ Добавить задание", async () => await ShowTaskForm());

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/EcoTasks");
            var json = await response.Content.ReadAsStringAsync();
            var tasks = JsonSerializer.Deserialize<List<EcoTaskAdmin>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tasks == null) return;

            foreach (var task in tasks)
            {
                ContentLayout.Children.Add(CreateTaskCard(task));
            }
        }
        catch (Exception ex) { ContentLayout.Children.Add(new Label { Text = ex.Message, TextColor = Colors.Red }); }
    }

    private Border CreateTaskCard(EcoTaskAdmin task)
    {
        var border = CreateCardBorder();
        var layout = new VerticalStackLayout { Spacing = 5 };

        layout.Children.Add(new Label { Text = task.Title, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        layout.Children.Add(new Label { Text = task.Description, TextColor = Color.FromArgb("#888"), FontSize = 12, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation });
        layout.Children.Add(new Label { Text = $"{task.Points} баллов | {task.Category} | {(task.RequiresPhoto ? "📷 Требуется фото" : "Без фото")}", TextColor = Color.FromArgb("#4CAF50"), FontSize = 12 });

        var buttons = new HorizontalStackLayout { Spacing = 10 };
        buttons.Children.Add(CreateActionButton("✏️", "#2196F3", async () => await EditTask(task)));
        buttons.Children.Add(CreateActionButton("🗑️", "#ff4444", async () => await DeleteTask(task.Id)));
        layout.Children.Add(buttons);

        border.Content = layout;
        return border;
    }

    private async Task ShowTaskForm(EcoTaskAdmin? task = null)
    {
        var titleEntry = new Entry { Placeholder = "Название", Text = task?.Title ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var descEntry = new Entry { Placeholder = "Описание", Text = task?.Description ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var pointsEntry = new Entry { Placeholder = "Баллы", Text = task?.Points.ToString() ?? "10", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Numeric };
        var catEntry = new Entry { Placeholder = "Категория", Text = task?.Category ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };

        // ✅ SWITCH ДЛЯ ФОТО
        var photoSwitch = new Switch { IsToggled = task?.RequiresPhoto ?? true };
        var photoLabel = new Label { Text = "Требуется фото", TextColor = Colors.White, VerticalOptions = LayoutOptions.Center };

        var switchLayout = new HorizontalStackLayout { Spacing = 10 };
        switchLayout.Children.Add(photoSwitch);
        switchLayout.Children.Add(photoLabel);

        var scroll = new ScrollView();
        var formLayout = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(20) };
        formLayout.Children.Add(new Label { Text = task == null ? "Новое задание" : "Редактирование", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        formLayout.Children.Add(titleEntry);
        formLayout.Children.Add(descEntry);
        formLayout.Children.Add(pointsEntry);
        formLayout.Children.Add(catEntry);
        formLayout.Children.Add(switchLayout); // ✅ ДОБАВЛЕН SWITCH

        var saveBtn = new Button { Text = "💾 Сохранить", BackgroundColor = Color.FromArgb("#4CAF50"), TextColor = Colors.White };
        saveBtn.Clicked += async (s, e) =>
        {
            if (!int.TryParse(pointsEntry.Text, out int points))
            {
                await DisplayAlert("Ошибка", "Неверное количество баллов", "OK");
                return;
            }

            var newTask = new EcoTaskAdmin
            {
                Id = task?.Id ?? 0,
                Title = titleEntry.Text,
                Description = descEntry.Text,
                Points = points,
                Category = catEntry.Text,
                RequiresPhoto = photoSwitch.IsToggled // ✅ ИСПОЛЬЗУЕМ SWITCH
            };

            await SaveTask(newTask);
            await Navigation.PopAsync();
            await LoadTasks();
        };

        formLayout.Children.Add(saveBtn);
        scroll.Content = formLayout;

        await Navigation.PushAsync(new ContentPage { Content = scroll, BackgroundColor = Color.FromArgb("#1a1a2e") });
    }

    private async Task SaveTask(EcoTaskAdmin task)
    {
        try
        {
            await SetAuthHeaderAsync();
            var json = JsonSerializer.Serialize(task);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (task.Id == 0)
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/EcoTasks", content);
            else
                response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/EcoTasks/{task.Id}", content);

            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Ошибка", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    private async Task EditTask(EcoTaskAdmin task) => await ShowTaskForm(task);

    private async Task DeleteTask(int id)
    {
        bool confirm = await DisplayAlert("Удаление", "Удалить задание?", "Да", "Нет");
        if (!confirm) return;

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"{ApiConfig.BaseUrl}/api/EcoTasks/{id}");
            if (response.IsSuccessStatusCode) await LoadTasks();
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    // ============================================
    // СТАТЬИ (CRUD)
    // ============================================

    private async Task LoadArticles()
    {
        AddTitle("Статьи");
        AddButton("➕ Добавить статью", async () => await ShowArticleForm());

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Articles");
            var json = await response.Content.ReadAsStringAsync();
            var articles = JsonSerializer.Deserialize<List<ArticleAdmin>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (articles == null) return;

            foreach (var article in articles)
            {
                ContentLayout.Children.Add(CreateArticleCard(article));
            }
        }
        catch (Exception ex) { ContentLayout.Children.Add(new Label { Text = ex.Message, TextColor = Colors.Red }); }
    }

    private Border CreateArticleCard(ArticleAdmin article)
    {
        var border = CreateCardBorder();
        var layout = new VerticalStackLayout { Spacing = 5 };

        layout.Children.Add(new Label { Text = article.Title, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        layout.Children.Add(new Label { Text = article.Category, TextColor = Color.FromArgb("#4CAF50"), FontSize = 12 });
        layout.Children.Add(new Label { Text = article.Content, TextColor = Color.FromArgb("#888"), FontSize = 12, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation });

        var buttons = new HorizontalStackLayout { Spacing = 10 };
        buttons.Children.Add(CreateActionButton("✏️", "#2196F3", async () => await EditArticle(article)));
        buttons.Children.Add(CreateActionButton("🗑️", "#ff4444", async () => await DeleteArticle(article.Id)));
        layout.Children.Add(buttons);

        border.Content = layout;
        return border;
    }

    private async Task ShowArticleForm(ArticleAdmin? article = null)
    {
        var titleEntry = new Entry { Placeholder = "Заголовок", Text = article?.Title ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var catEntry = new Entry { Placeholder = "Категория", Text = article?.Category ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };

        var contentEditor = new Editor
        {
            Placeholder = "Текст статьи",
            Text = article?.Content ?? "",
            TextColor = Colors.White,
            PlaceholderColor = Color.FromArgb("#666"),
            HeightRequest = 200
        };

        var scroll = new ScrollView();
        var formLayout = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(20) };
        formLayout.Children.Add(new Label { Text = article == null ? "Новая статья" : "Редактирование", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        formLayout.Children.Add(titleEntry);
        formLayout.Children.Add(catEntry);
        formLayout.Children.Add(contentEditor);

        var saveBtn = new Button { Text = "💾 Сохранить", BackgroundColor = Color.FromArgb("#4CAF50"), TextColor = Colors.White };
        saveBtn.Clicked += async (s, e) =>
        {
            var newArticle = new ArticleAdmin
            {
                Id = article?.Id ?? 0,
                Title = titleEntry.Text,
                Category = catEntry.Text,
                Content = contentEditor.Text
            };

            await SaveArticle(newArticle);
            await Navigation.PopAsync();
            await LoadArticles();
        };

        formLayout.Children.Add(saveBtn);
        scroll.Content = formLayout;

        await Navigation.PushAsync(new ContentPage { Content = scroll, BackgroundColor = Color.FromArgb("#1a1a2e") });
    }

    private async Task SaveArticle(ArticleAdmin article)
    {
        try
        {
            await SetAuthHeaderAsync();
            var json = JsonSerializer.Serialize(article);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (article.Id == 0)
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/Articles", content);
            else
                response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/Articles/{article.Id}", content);

            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Ошибка", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    private async Task EditArticle(ArticleAdmin article) => await ShowArticleForm(article);

    private async Task DeleteArticle(int id)
    {
        bool confirm = await DisplayAlert("Удаление", "Удалить статью?", "Да", "Нет");
        if (!confirm) return;

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"{ApiConfig.BaseUrl}/api/Articles/{id}");
            if (response.IsSuccessStatusCode) await LoadArticles();
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    // ============================================
    // АКЦИИ/МЕРОПРИЯТИЯ (CRUD)
    // ============================================

    private async Task LoadEvents()
    {
        AddTitle("Эко-мероприятия");
        AddButton("➕ Добавить акцию", async () => await ShowEventForm());

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Admin/events");
            var json = await response.Content.ReadAsStringAsync();
            var events = JsonSerializer.Deserialize<List<EcoEventAdmin>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (events == null) return;

            foreach (var ecoEvent in events)
            {
                ContentLayout.Children.Add(CreateEventCard(ecoEvent));
            }
        }
        catch (Exception ex) { ContentLayout.Children.Add(new Label { Text = ex.Message, TextColor = Colors.Red }); }
    }

    private Border CreateEventCard(EcoEventAdmin ecoEvent)
    {
        var border = CreateCardBorder();
        var layout = new VerticalStackLayout { Spacing = 5 };

        layout.Children.Add(new Label { Text = ecoEvent.Title, TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        layout.Children.Add(new Label { Text = $"{ecoEvent.City} | {ecoEvent.EventDate:dd.MM.yyyy}", TextColor = Color.FromArgb("#4CAF50"), FontSize = 12 });
        layout.Children.Add(new Label { Text = ecoEvent.Address, TextColor = Color.FromArgb("#888"), FontSize = 12 });

        var buttons = new HorizontalStackLayout { Spacing = 10 };
        buttons.Children.Add(CreateActionButton("✏️", "#2196F3", async () => await EditEvent(ecoEvent)));
        buttons.Children.Add(CreateActionButton("🗑️", "#ff4444", async () => await DeleteEvent(ecoEvent.Id)));
        layout.Children.Add(buttons);

        border.Content = layout;
        return border;
    }

    private async Task ShowEventForm(EcoEventAdmin? ecoEvent = null)
    {
        var titleEntry = new Entry { Placeholder = "Название *", Text = ecoEvent?.Title ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var descEntry = new Entry { Placeholder = "Описание *", Text = ecoEvent?.Description ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var cityEntry = new Entry { Placeholder = "Город (необязательно)", Text = ecoEvent?.City ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var addrEntry = new Entry { Placeholder = "Адрес (необязательно)", Text = ecoEvent?.Address ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var catEntry = new Entry { Placeholder = "Категория *", Text = ecoEvent?.Category ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var dateEntry = new Entry { Placeholder = "Дата (гггг-мм-дд) *", Text = ecoEvent?.EventDate.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd"), TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var orgEntry = new Entry { Placeholder = "Организатор (необязательно)", Text = ecoEvent?.Organizer ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666") };
        var phoneEntry = new Entry { Placeholder = "Телефон (необязательно)", Text = ecoEvent?.ContactPhone ?? "", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#666"), Keyboard = Keyboard.Telephone };

        var scroll = new ScrollView();
        var formLayout = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(20) };
        formLayout.Children.Add(new Label { Text = ecoEvent == null ? "Новое мероприятие" : "Редактирование", TextColor = Colors.White, FontSize = 20, FontAttributes = FontAttributes.Bold });
        formLayout.Children.Add(titleEntry);
        formLayout.Children.Add(descEntry);
        formLayout.Children.Add(cityEntry);
        formLayout.Children.Add(addrEntry);
        formLayout.Children.Add(catEntry);
        formLayout.Children.Add(dateEntry);
        formLayout.Children.Add(orgEntry);
        formLayout.Children.Add(phoneEntry);

        var saveBtn = new Button { Text = "💾 Сохранить", BackgroundColor = Color.FromArgb("#4CAF50"), TextColor = Colors.White };
        saveBtn.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(titleEntry.Text) || string.IsNullOrWhiteSpace(descEntry.Text))
            {
                await DisplayAlert("Ошибка", "Название и описание обязательны", "OK");
                return;
            }

            if (!DateTime.TryParse(dateEntry.Text, out var parsedDate))
            {
                await DisplayAlert("Ошибка", "Неверный формат даты. Используйте гггг-мм-дд", "OK");
                return;
            }

            var newEvent = new EcoEventAdmin
            {
                Id = ecoEvent?.Id ?? 0,
                Title = titleEntry.Text,
                Description = descEntry.Text,
                City = string.IsNullOrWhiteSpace(cityEntry.Text) ? null : cityEntry.Text,
                Address = string.IsNullOrWhiteSpace(addrEntry.Text) ? null : addrEntry.Text,
                Category = string.IsNullOrWhiteSpace(catEntry.Text) ? null : catEntry.Text,
                EventDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc),
                Organizer = string.IsNullOrWhiteSpace(orgEntry.Text) ? null : orgEntry.Text,
                ContactPhone = string.IsNullOrWhiteSpace(phoneEntry.Text) ? null : phoneEntry.Text,
                IsActive = true
            };

            await SaveEvent(newEvent);
            await Navigation.PopAsync();
            await LoadEvents();
        };

        formLayout.Children.Add(saveBtn);
        scroll.Content = formLayout;

        await Navigation.PushAsync(new ContentPage { Content = scroll, BackgroundColor = Color.FromArgb("#1a1a2e") });
    }

    private async Task SaveEvent(EcoEventAdmin ecoEvent)
    {
        try
        {
            await SetAuthHeaderAsync();
            var json = JsonSerializer.Serialize(ecoEvent);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (ecoEvent.Id == 0)
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/Admin/events", content);
            else
                response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/Admin/events/{ecoEvent.Id}", content);

            if (!response.IsSuccessStatusCode)
                await DisplayAlert("Ошибка", await response.Content.ReadAsStringAsync(), "OK");
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    private async Task EditEvent(EcoEventAdmin ecoEvent) => await ShowEventForm(ecoEvent);

    private async Task DeleteEvent(int id)
    {
        bool confirm = await DisplayAlert("Удаление", "Удалить мероприятие?", "Да", "Нет");
        if (!confirm) return;

        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"{ApiConfig.BaseUrl}/api/Admin/events/{id}");
            if (response.IsSuccessStatusCode) await LoadEvents();
        }
        catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
    }

    // ============================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================

    private async Task SetAuthHeaderAsync()
    {
        var token = await SecureStorage.GetAsync("token");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private Border CreateCardBorder()
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#2a2a3e"),
            Stroke = Color.FromArgb("#6A0DAD"),
            StrokeThickness = 1,
            Padding = new Thickness(15),
            Margin = new Thickness(0, 6),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) }
        };
    }

    private Button CreateActionButton(string text, string color, Func<Task> action)
    {
        var btn = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(color),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 40,
            WidthRequest = 50
        };
        btn.Clicked += async (s, e) => await action();
        return btn;
    }

    private void AddTitle(string text)
    {
        ContentLayout.Children.Add(new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10)
        });
    }

    private void AddButton(string text, Func<Task> action)
    {
        var btn = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#4CAF50"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 45,
            Margin = new Thickness(0, 10)
        };
        btn.Clicked += async (s, e) => await action();
        ContentLayout.Children.Add(btn);
    }
}

// ============================================
// МОДЕЛИ ДЛЯ АДМИНКИ
// ============================================

public class PendingReport
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = "";
    public int TaskPoints { get; set; }
    public string Comment { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public string FullPhotoUrl { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class EcoPointAdmin
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Phone { get; set; }      // ✅ nullable
    public string? Website { get; set; }     // ✅ nullable
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class EcoTaskAdmin
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Points { get; set; }
    public string Category { get; set; } = "";
    public bool RequiresPhoto { get; set; } = true;
}

public class ArticleAdmin
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "";
}

public class EcoEventAdmin
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? City { get; set; }
    public string? Address { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Category { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Organizer { get; set; }
    public string? ContactPhone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}