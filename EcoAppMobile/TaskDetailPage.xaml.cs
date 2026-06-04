using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;

namespace EcoAppMobile;

public partial class TaskDetailPage : ContentPage
{
    private readonly EcoTaskViewModel _task;
    private readonly HttpClient _httpClient;
    private int _currentUserId;

    public TaskDetailPage(EcoTaskViewModel task)
    {
        InitializeComponent();
        _task = task;
        BindingContext = task;
        _httpClient = new HttpClient();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserId();
    }

    private async Task LoadUserId()
    {
        var userIdStr = await SecureStorage.GetAsync("userId");
        if (int.TryParse(userIdStr, out int id))
        {
            _currentUserId = id;
        }
    }

    private async void OnCompleteClicked(object sender, EventArgs e)
    {
        if (_task.IsBlocked) return;

        if (_task.RequiresPhoto)
        {
            _task.Status = TaskStatus.WaitingForPhoto;
            OnPropertyChanged(nameof(_task.ShowExecuteButton));
            OnPropertyChanged(nameof(_task.ShowPhotoButtons));
        }
        else
        {
            await SubmitTaskAsync(null);
        }
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync < Permissions.Camera > ();
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
                await ProcessPhotoAsync(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Камера: {ex.Message}", "OK");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Выберите фото"
            });

            if (photo != null)
            {
                await ProcessPhotoAsync(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Галерея: {ex.Message}", "OK");
        }
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        _task.Status = TaskStatus.Available;
        _task.IsBlocked = false;
        _task.RetrySecondsRemaining = 0;

        // Обновляем UI
        OnPropertyChanged(nameof(_task.ShowRejectedIcon));
        OnPropertyChanged(nameof(_task.ShowRetryButton));
        OnPropertyChanged(nameof(_task.ShowExecuteButton));
    }

    private async Task ProcessPhotoAsync(FileResult photo)
    {
        _task.PreviewImageSource = ImageSource.FromFile(photo.FullPath);
        _task.HasPreviewImage = true;
        await SubmitTaskAsync(photo);
    }

    private async Task SubmitTaskAsync(FileResult? photo)
    {
        try
        {
            _task.Status = TaskStatus.Submitting;
            OnPropertyChanged(nameof(_task.ShowStatus));
            OnPropertyChanged(nameof(_task.ShowPhotoButtons));

            HttpResponseMessage response;

            if (photo != null)
            {
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(_currentUserId.ToString()), "UserId");
                content.Add(new StringContent(_task.Id.ToString()), "TaskId");
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
                    TaskId = _task.Id,
                    Comment = "Выполнено без фото",
                    PhotoUrl = ""
                };

                var json = JsonSerializer.Serialize(report);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/TaskReports", content);
            }

            if (response.IsSuccessStatusCode)
            {
                _task.Status = TaskStatus.PendingApproval;
                _task.IsBlocked = true;

                OnPropertyChanged(nameof(_task.ShowStatus));
                OnPropertyChanged(nameof(_task.ShowExecuteButton));
                OnPropertyChanged(nameof(_task.ShowPhotoButtons));

                await DisplayAlert("Успех", "Задание отправлено на проверку!", "OK");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _task.Status = TaskStatus.Error;
                OnPropertyChanged(nameof(_task.ShowStatus));
                await DisplayAlert("Ошибка", $"Сервер: {response.StatusCode}\n{error}", "OK");
            }
        }
        catch (Exception ex)
        {
            _task.Status = TaskStatus.Error;
            OnPropertyChanged(nameof(_task.ShowStatus));
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}