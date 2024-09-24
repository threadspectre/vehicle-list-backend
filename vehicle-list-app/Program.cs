using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Configuration;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/vehicles", async () =>
{
    dynamic vehiclesRaw = await HttpHelper.GetRawVehicles();
    try{
        vehiclesRaw = vehiclesRaw.GetProperty("Results");
    }
    catch(Exception e){
        Console.WriteLine("Errored while getting results");
        return "";
    }
    int length = vehiclesRaw.GetArrayLength();
    if(length == 0){
        return "";
    }
    // Console.Write(vehiclesRaw);
    VehicleList vehicles = new VehicleList();
    for(int i = 0; i < length; i++){
        // Console.WriteLine(vehiclesRaw[i]);
        var vehicleTypes = vehiclesRaw[i].GetProperty("VehicleTypes");
        int vehicleTypesLength = vehicleTypes.GetArrayLength();
        if(vehicleTypesLength > 0){
            Console.WriteLine(vehiclesRaw[i]);
            var id = -1;
            var shortName = "";
            var fullName = "";
            var country = "";
            try{
                id = vehiclesRaw[i].GetProperty("Mfr_ID").GetInt32(); 
            }
            catch{
                id = -2;
            };
            try{
                shortName = vehiclesRaw[i].GetProperty("Mfr_CommonName").GetString();
            }
            catch{
                shortName = "unknown";
            }
            try{
                fullName = vehiclesRaw[i].GetProperty("Mfr_Name").GetString();
            }
            catch{
                fullName = "unknown";
            }
            try{
                country = vehiclesRaw[i].GetProperty("Country").GetString();
            }
            catch{
                country = "Unknown";
            }

            Manufacturer manufacturer = new Manufacturer { 
                Id = id, 
                ShortName = shortName, 
                FullName = fullName, 
                Country = country 
            };
            for(int j = 0; j < vehicleTypesLength; j++){
                var vehicleTypeName = vehicleTypes[j].GetProperty("Name").GetString();
                vehicles.AddManufacturerToVehicleType(vehicleTypeName, manufacturer);
            }
        }

    }
    // dynamic vehicles = JObject.Parse(vehiclesRaw);
    // Console.Write(vehicles);
    string jsonString = JsonSerializer.Serialize(vehicles);
    return jsonString;
})
.WithName("GetVehicles")
.WithOpenApi();

app.Run();

public class Manufacturer
{
    public int Id { get; set; }
    public string ShortName { get; set; }
    public string FullName { get; set; }
    public string Country { get; set; }
}

public class Vehicle
{
    public string VehicleType { get; set; }
    public List<Manufacturer> Manufacturers { get; set; }

    public Vehicle()
    {
        Manufacturers = new List<Manufacturer>();
    }

    // Method to add a Manufacturer dynamically
    public void AddManufacturer(Manufacturer manufacturer)
    {
        Manufacturers.Add(manufacturer);
    }
}

public class VehicleList
{
    public List<Vehicle> Vehicles { get; set; }

    public VehicleList()
    {
        Vehicles = new List<Vehicle>();
    }

    // Method to add a Vehicle dynamically
    public void AddVehicle(string vehicleType, List<Manufacturer> manufacturers)
    {
        var vehicle = new Vehicle
        {
            VehicleType = vehicleType,
            Manufacturers = manufacturers
        };

        Vehicles.Add(vehicle);
    }

    // Method to find a vehicle by its type
    public Vehicle GetVehicleByType(string vehicleType)
    {
        return Vehicles.FirstOrDefault(v => v.VehicleType.Equals(vehicleType, StringComparison.OrdinalIgnoreCase));
    }

    // Method to add a Manufacturer to a specific VehicleType
    public void AddManufacturerToVehicleType(string vehicleType, Manufacturer manufacturer)
    {
        var vehicle = GetVehicleByType(vehicleType);
        if (vehicle != null)
        {
            vehicle.AddManufacturer(manufacturer);
        }
        else
        {
            var manufacturerList = new List<Manufacturer>();
            manufacturerList.Add(manufacturer);
            AddVehicle(vehicleType, manufacturerList);
        }
    }
}

public static class HttpHelper
{    private static readonly string uri = "https://navixrecruitingcasestudy.blob.core.windows.net/manufacturers/vehicle-manufacturers.json";

    public static async Task<dynamic> GetRawVehicles()
    {
        using (var client = new HttpClient())
        {
            try
            {
                return await client.GetFromJsonAsync<dynamic>(uri);
            }
            catch (HttpRequestException) // Non success
            {
                Console.WriteLine("An error occurred.");
                return default;
            }
            catch (NotSupportedException) // When content type is not valid
            {
                Console.WriteLine("The content type is not supported.");
                return default;
            }
            catch ( System.Text.Json.JsonException) // Invalid JSON
            {
                Console.WriteLine("Invalid JSON.");
                return default;
            }


        }
    }
}