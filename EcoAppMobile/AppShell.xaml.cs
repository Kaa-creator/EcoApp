namespace EcoAppMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("MapPage", typeof(MapPage));
        Routing.RegisterRoute("TasksPage", typeof(TasksPage));
        Routing.RegisterRoute("EventsPage", typeof(EventsPage));
        Routing.RegisterRoute("ArticlesPage", typeof(ArticlesPage));
        Routing.RegisterRoute("CalculatorPage", typeof(CalculatorPage));
        Routing.RegisterRoute("RatingPage", typeof(RatingPage));
        Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
        Routing.RegisterRoute("AdminPage", typeof(AdminPage));

        // По умолчанию скрываем админку
        AdminMenuItem.IsVisible = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAdminRole();
    }

    private async Task CheckAdminRole()
    {
        try
        {
            var role = await SecureStorage.GetAsync("userRole");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AdminMenuItem.IsVisible = (role == "Admin");
            });
        }
        catch
        {
            AdminMenuItem.IsVisible = false;
        }
    }

    private async void OnProfileTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }

    private async void OnRatingTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new RatingPage());
    }
}