using Microsoft.AspNetCore.Mvc;
using EcoApp.API.Models;

namespace EcoApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecyclingController : ControllerBase
    {
        [HttpPost("calculate")]
        public IActionResult Calculate(RecyclingRequest request)
        {
            // алюминий
            double energySaved = request.AluminumKg * 14;
            double co2Aluminum = request.AluminumKg * 9;

            // бумага
            double treesSaved = request.PaperKg * 0.017;
            double waterSaved = request.PaperKg * 26;

            // пластик
            double co2Plastic = request.PlasticKg * 2;
            double oilSaved = request.PlasticKg * 3;

            // стекло
            double co2Glass = request.GlassKg * 0.3;
            double rawMaterialSaved = request.GlassKg * 1.2;

            double totalCO2 = co2Aluminum + co2Plastic + co2Glass;

            return Ok(new
            {
                energySaved = Math.Round(energySaved, 2),
                treesSaved = Math.Round(treesSaved, 2),
                waterSaved = Math.Round(waterSaved, 2),
                oilSaved = Math.Round(oilSaved, 2),
                rawMaterialSaved = Math.Round(rawMaterialSaved, 2),
                totalCO2 = Math.Round(totalCO2, 2)
            });
        }
    }
}