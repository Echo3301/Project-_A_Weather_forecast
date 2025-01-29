using System.Collections.Concurrent;
using Newtonsoft.Json;

using Assignment_A1_03.Models;

namespace Assignment_A1_03.Services;

public class OpenWeatherService
{
    readonly HttpClient _httpClient = new HttpClient();
    readonly ConcurrentDictionary<(double, double, string), (Forecast, DateTime)> _cachedGeoForecasts = new ConcurrentDictionary<(double, double, string), (Forecast, DateTime)>();
    readonly ConcurrentDictionary<(string, string), (Forecast, DateTime)> _cachedCityForecasts = new ConcurrentDictionary<(string, string), (Forecast, DateTime)>();

    Object _locker = new Object();

    //Your API Key
    readonly string apiKey = "68aa63b283a8d32f4f346fef8623b2b8";

    //Event declaration
    public event EventHandler<string> WeatherForecastAvailable;
    //Method that triggers the event
    protected virtual void OnWeatherForecastAvailable (string message)
    {
        WeatherForecastAvailable?.Invoke(this, message);
    }
    //Method to get weather forcast for a specific city name
    public async Task<Forecast> GetForecastAsync(string City)
    {
        //Part of cache code here to check if forecast in Cache
        var cacheKey = (City, "City");
        //Preventing concurrency issues with lock
        lock (_locker)
        {
            //Checks if forecast is found i cache and not older then 1 minute              
            if (_cachedCityForecasts.TryGetValue(cacheKey, out var cacheValue) &&
                (DateTime.Now - cacheValue.Item2).TotalMinutes <= 1)
            {
                //Triggers the event and returns forecast from cache
                OnWeatherForecastAvailable($"New weather forecast for {City} available in cache");
                return cacheValue.Item1;
            }
        }       
        //https://openweathermap.org/current
        //If not cached, proceeds to fetch forecast from API
        //Determines the computer's language to describe the weather
        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var uri = $"https://api.openweathermap.org/data/2.5/forecast?q={City}&units=metric&lang={language}&appid={apiKey}";

        //Gets the weather forecast
        Forecast forecast = await ReadWebApiAsync(uri);
        //Checking that forecast is not null and trigger event  
        if (forecast != null)
        {
            OnWeatherForecastAvailable($"New weather forecast for {City} available");
        }
        else
        {
            OnWeatherForecastAvailable($"New weather  forecast for {City} is not available");
        }

        //Cache the newly fetched forecast and time
        _cachedCityForecasts[cacheKey] = (forecast, DateTime.Now);

        return forecast;
    }
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude)
    {
        //Part of cache code here to check if forecast in Cache
        var cacheKey = (latitude, longitude, "Geo coordinates");
        //Preventing concurrency issues with lock
        lock (_locker)
        {
            //Checking that uri is not null and trigger event 
            if (_cachedGeoForecasts.TryGetValue(cacheKey, out var cacheValue) &&
                (DateTime.Now - cacheValue.Item2).TotalMinutes <= 1)
            {
                //Triggers the event and returns forecast from cache
                OnWeatherForecastAvailable($"New weather forecast for ({latitude}, {longitude}) available in cache");
                return cacheValue.Item1;
            }
        }

        //https://openweathermap.org/current
        //If not cached, proceeds to fetch forecast from API
        //Determines the computer's language to describe the weather
        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var uri = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&lang={language}&appid={apiKey}";

        //Gets the weather forecast
        Forecast forecast = await ReadWebApiAsync(uri);
        //Checking that forecast is not null and trigger event  
        if (forecast != null)
        {
            OnWeatherForecastAvailable($"New weather forecast for ({latitude}, {longitude}) available");
        }
        else
        {
            OnWeatherForecastAvailable($"New weather  forecast for ({latitude}, {longitude}) is not available");
        }

        //Cache the newly fetched forecast and time
        _cachedGeoForecasts[cacheKey] = (forecast, DateTime.Now);

        return forecast;
    }
    //Method to send a request to the API and give response
    private async Task<Forecast> ReadWebApiAsync(string uri)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        //Convert Json to NewsResponse
        string content = await response.Content.ReadAsStringAsync();
        WeatherApiData wd = JsonConvert.DeserializeObject<WeatherApiData>(content);

        //Forecast object to hold weather data
        var forecast = new Forecast() 
        {
            City = wd.city.name,
            Items = wd.list.Select(x => new ForecastItem
            {
                DateTime = UnixTimeStampToDateTime(x.dt),
                Temperature = x.main.temp,
                WindSpeed = x.wind.speed,
                Description = x.weather.FirstOrDefault().description,
            }).ToList()
        };
        return forecast;
    }
    //Method to convert Unix timestamp to DateTime
    private DateTime UnixTimeStampToDateTime(double unixTimeStamp) => DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
}

