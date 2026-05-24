using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Timers;
using EcoAppMobile.Helpers;

namespace EcoAppMobile;

public partial class TasksPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private ObservableCollection<EcoTaskViewModel> _tasks = new();
    private List<TaskReport> _userReports = new();
    private int _currentUserId;
    private System.Timers.Timer? _retryTimer;

    public TasksPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        TasksList.ItemsSource = _tasks;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserId();
        await LoadData();
        StartRetryTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _retryTimer?.Stop();
        _retryTimer?.Dispose();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadData();
        TasksRefreshView.IsRefreshing = false;
    }

    private void StartRetryTimer()
    {
        _retryTimer?.Stop();
        _retryTimer = new System.Timers.Timer(1000);
        _retryTimer.Elapsed += async (s, e) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await UpdateRetryTimers();
            });
        };
        _retryTimer.Start();
    }

    private async Task UpdateRetryTimers()
    {
        foreach (var task in _tasks.Where(t => t.Status == TaskStatus.Rejected))
        {
            var report = _userReports.FirstOrDefault(r => r.TaskId == task.Id);
            if (report == null) continue;

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{ApiConfig.BaseUrl}/api/TaskReports/can-retry/{report.Id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RetryCheckResult>(json);

                    if (result?.canRetry == true)
                    {
                        task.Status = TaskStatus.Available;
                        task.IsBlocked = false;
                        task.RetrySecondsRemaining = 0;
                        _userReports.Remove(report);
                    }
                    else
                    {
                        task.RetrySecondsRemaining = result?.secondsRemaining ?? 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки retry: {ex.Message}");
            }
        }
    }

    private async Task LoadUserId()
    {
        var userIdStr = await SecureStorage.GetAsync("userId");
        if (int.TryParse(userIdStr, out int id))
        {
            _currentUserId = id;
        }
        else
        {
            _currentUserId = 1;
        }
    }

    private async Task LoadData()
    {
        await LoadUserReports();
        await LoadTasks();
        await LoadUserPoints();
    }

    private async Task LoadUserReports()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/TaskReports");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var allReports = JsonSerializer.Deserialize<List<TaskReport>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _userReports = allReports?.Where(r => r.UserId == _currentUserId).ToList() ?? new List<TaskReport>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки отчетов: {ex.Message}");
        }
    }

    private async Task LoadTasks()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/EcoTasks");
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Ошибка", $"Сервер вернул: {response.StatusCode}", "OK");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tasks = JsonSerializer.Deserialize<List<EcoTask>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _tasks.Clear();
            if (tasks == null) return;

            foreach (var task in tasks)
            {
                var existingReport = _userReports.FirstOrDefault(r => r.TaskId == task.Id);

                var viewModel = new EcoTaskViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Points = task.Points,
                    Category = task.Category,
                    RequiresPhoto = task.RequiresPhoto
                };

                if (existingReport != null)
                {
                    viewModel.Status = existingReport.Status switch
                    {
                        "Pending" => TaskStatus.PendingApproval,
                        "Approved" => TaskStatus.Approved,
                        "Rejected" => TaskStatus.Rejected,
                        _ => TaskStatus.Available
                    };

                    if (existingReport.Status == "Approved" || existingReport.Status == "Pending")
                    {
                        viewModel.IsBlocked = true;
                    }

                    if (existingReport.Status == "Rejected")
                    {
                        try
                        {
                            var retryResponse = await _httpClient.GetAsync(
                                $"{ApiConfig.BaseUrl}/api/TaskReports/can-retry/{existingReport.Id}");
                            if (retryResponse.IsSuccessStatusCode)
                            {
                                var retryJson = await retryResponse.Content.ReadAsStringAsync();
                                var retryResult = JsonSerializer.Deserialize<RetryCheckResult>(retryJson);
                                viewModel.RetrySecondsRemaining = retryResult?.secondsRemaining ?? 0;
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    viewModel.Status = TaskStatus.Available;
                }

                _tasks.Add(viewModel);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить задания: {ex.Message}", "OK");
        }
    }

    private async Task LoadUserPoints()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiConfig.BaseUrl}/api/Users/{_currentUserId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<TaskPageUser>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (user != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UserPointsLabel.Text = user.Points.ToString();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки баллов: {ex.Message}");
        }
    }

    private async void OnCompleteClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var task = button?.CommandParameter as EcoTaskViewModel;

        if (task == null || task.IsBlocked) return;

        if (task.RequiresPhoto)
        {
            task.Status = TaskStatus.WaitingForPhoto;
        }
        else
        {
            await SubmitTaskAsync(task, null);
        }
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var task = button?.CommandParameter as EcoTaskViewModel;

        if (task == null) return;

        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Нет доступа", "Разрешите использование камеры", "OK");
                return;
            }

            var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Сфотографируйте выполнение задания"
            });

            if (photo != null)
            {
                await ProcessPhotoAsync(task, photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Камера: {ex.Message}", "OK");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var task = button?.CommandParameter as EcoTaskViewModel;

        if (task == null) return;

        try
        {
            var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Выберите фото"
            });

            if (photo != null)
            {
                await ProcessPhotoAsync(task, photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Галерея: {ex.Message}", "OK");
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var task = button?.CommandParameter as EcoTaskViewModel;

        if (task == null) return;

        task.Status = TaskStatus.Available;
        task.IsBlocked = false;
        task.RetrySecondsRemaining = 0;

        var oldReport = _userReports.FirstOrDefault(r => r.TaskId == task.Id);
        if (oldReport != null) _userReports.Remove(oldReport);
    }

    private async Task ProcessPhotoAsync(EcoTaskViewModel task, FileResult photo)
    {
        task.PreviewImageSource = ImageSource.FromFile(photo.FullPath);
        task.HasPreviewImage = true;
        await SubmitTaskAsync(task, photo);
    }

    private async Task SubmitTaskAsync(EcoTaskViewModel task, FileResult? photo)
    {
        try
        {
            task.Status = TaskStatus.Submitting;

            HttpResponseMessage response;

            if (photo != null)
            {
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(_currentUserId.ToString()), "UserId");
                content.Add(new StringContent(task.Id.ToString()), "TaskId");
                content.Add(new StringContent("Фото приложено"), "Comment");

                var stream = await photo.OpenReadAsync();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "photo", photo.FileName);

                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/TaskReports/upload", content);
            }
            else
            {
                var report = new
                {
                    UserId = _currentUserId,
                    TaskId = task.Id,
                    Comment = "Выполнено без фото",
                    PhotoUrl = ""
                };

                var json = JsonSerializer.Serialize(report);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/TaskReports", content);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdReport = JsonSerializer.Deserialize<TaskReport>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                task.Status = TaskStatus.PendingApproval;
                task.IsBlocked = true;

                await DisplayAlert("Успех", "Задание отправлено на проверку!", "OK");
                await LoadUserReports();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                task.Status = TaskStatus.Error;
                await DisplayAlert("Ошибка", $"Сервер: {response.StatusCode}\n{error}", "OK");
            }
        }
        catch (Exception ex)
        {
            task.Status = TaskStatus.Error;
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}

public class RetryCheckResult
{
    public bool canRetry { get; set; }
    public int secondsRemaining { get; set; }
}

public class EcoTaskViewModel : BindableObject
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Points { get; set; }
    public string Category { get; set; } = "";
    public bool RequiresPhoto { get; set; }

    private TaskStatus _status = TaskStatus.Available;
    public TaskStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(ShowExecuteButton));
            OnPropertyChanged(nameof(ShowPhotoButtons));
            OnPropertyChanged(nameof(ShowApprovedIcon));
            OnPropertyChanged(nameof(ShowRejectedIcon));
            OnPropertyChanged(nameof(ShowRetryButton));
            OnPropertyChanged(nameof(ShowStatus));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsBlocked));
        }
    }

    private bool _isBlocked;
    public bool IsBlocked
    {
        get => _isBlocked;
        set
        {
            _isBlocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowExecuteButton));
        }
    }

    private int _retrySecondsRemaining;
    public int RetrySecondsRemaining
    {
        get => _retrySecondsRemaining;
        set
        {
            _retrySecondsRemaining = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RetryButtonText));
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(ShowRejectedIcon));
            OnPropertyChanged(nameof(ShowRetryButton));
        }
    }

    public bool ShowExecuteButton => Status == TaskStatus.Available && !IsBlocked;
    public bool ShowPhotoButtons => Status == TaskStatus.WaitingForPhoto;
    public bool ShowApprovedIcon => Status == TaskStatus.Approved;
    public bool ShowRejectedIcon => Status == TaskStatus.Rejected && !CanRetry;
    public bool ShowRetryButton => Status == TaskStatus.Rejected && CanRetry;
    public bool ShowStatus => Status == TaskStatus.Submitting || Status == TaskStatus.PendingApproval || Status == TaskStatus.Error;

    public string RetryButtonText => CanRetry
        ? "Повторить"
        : $"Отклонено ({RetrySecondsRemaining / 60:D1}:{RetrySecondsRemaining % 60:D2})";

    public bool CanRetry => Status == TaskStatus.Rejected && RetrySecondsRemaining <= 0;

    public string StatusText => Status switch
    {
        TaskStatus.Submitting => "Отправка...",
        TaskStatus.PendingApproval => "На проверке у администратора",
        TaskStatus.Error => "Ошибка отправки",
        _ => ""
    };

    public Color StatusColor => Status switch
    {
        TaskStatus.PendingApproval => Colors.Orange,
        TaskStatus.Approved => Colors.Green,
        TaskStatus.Rejected => Colors.Red,
        TaskStatus.Error => Colors.Red,
        _ => Colors.Gray
    };

    private ImageSource? _previewImageSource;
    public ImageSource? PreviewImageSource
    {
        get => _previewImageSource;
        set
        {
            _previewImageSource = value;
            OnPropertyChanged();
        }
    }

    private bool _hasPreviewImage;
    public bool HasPreviewImage
    {
        get => _hasPreviewImage;
        set
        {
            _hasPreviewImage = value;
            OnPropertyChanged();
        }
    }
}

public enum TaskStatus
{
    Available,
    WaitingForPhoto,
    Submitting,
    PendingApproval,
    Approved,
    Rejected,
    Error
}

public class EcoTask
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Points { get; set; }
    public string Category { get; set; } = "";
    public bool RequiresPhoto { get; set; }
}

public class TaskReport
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TaskId { get; set; }
    public string PhotoUrl { get; set; } = "";
    public string Comment { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class TaskPageUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public int Points { get; set; }
}