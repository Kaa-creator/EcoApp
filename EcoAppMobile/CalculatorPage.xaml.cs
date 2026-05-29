using System.Text;
using System.Text.Json;
using EcoAppMobile.Helpers;

namespace EcoAppMobile;

public partial class CalculatorPage : ContentPage
{
    public CalculatorPage()
    {
        InitializeComponent();
    }

    // ✅ ДОБАВЛЕНО: свайп вниз — сброс всего
    private void OnRefreshing(object sender, EventArgs e)
    {
        AluminumEntry.Text = "";
        PaperEntry.Text = "";
        PlasticEntry.Text = "";
        GlassEntry.Text = "";
        ResultsBorder.IsVisible = false;

        CalcRefreshView.IsRefreshing = false;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            double aluminum = ParseEntry(AluminumEntry.Text);
            double paper = ParseEntry(PaperEntry.Text);
            double plastic = ParseEntry(PlasticEntry.Text);
            double glass = ParseEntry(GlassEntry.Text);

            if (aluminum == 0 && paper == 0 && plastic == 0 && glass == 0)
            {
                await DisplayAlert("Внимание", "Введите хотя бы одно значение", "OK");
                return;
            }

            var client = new HttpClient();
            var data = new
            {
                AluminumKg = aluminum,
                PaperKg = paper,
                PlasticKg = plastic,
                GlassKg = glass
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{ApiConfig.BaseUrl}/api/Recycling/calculate", content);

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecyclingResult>(resultJson);

                EnergyLabel.Text = $"Энергия сэкономлена: {result.energySaved} кВт·ч";
                TreesLabel.Text = $"Деревьев спасено: {result.treesSaved}";
                WaterLabel.Text = $"Воды сэкономлено: {result.waterSaved} л";
                OilLabel.Text = $"Нефти сэкономлено: {result.oilSaved} л";
                RawMaterialLabel.Text = $"Сырья сэкономлено: {result.rawMaterialSaved} кг";
                CO2Label.Text = $"CO₂ предотвращено: {result.totalCO2} кг";

                ResultsBorder.IsVisible = true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Ошибка сервера", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private double ParseEntry(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return double.TryParse(text.Replace(".", ","), out double result) ? result : 0;
    }
}

public class RecyclingResult
{
    public double energySaved { get; set; }
    public double treesSaved { get; set; }
    public double waterSaved { get; set; }
    public double oilSaved { get; set; }
    public double rawMaterialSaved { get; set; }
    public double totalCO2 { get; set; }
}