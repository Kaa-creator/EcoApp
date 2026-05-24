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

        AdminMenuItem.IsVisible = false;
        CheckAdminRole();
    }

    private void CheckAdminRole()
    {
        try
        {
            var role = SecureStorage.GetAsync("userRole").Result;
            AdminMenuItem.IsVisible = (role == "Admin");
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