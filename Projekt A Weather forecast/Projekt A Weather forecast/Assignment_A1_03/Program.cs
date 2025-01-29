using Assignment_A1_03.Models;
using Assignment_A1_03.Services;

namespace Assignment_A1_03;

class Program
{
    static void Main(string[] args)
    {
        Object _locker = new Object();
        OpenWeatherService service = new OpenWeatherService();

        //Register the event      
        service.WeatherForecastAvailable += OnWeatherForecastAvailable;

        Task<Forecast>[] tasks = { null, null, null, null };
        Exception exception = null;
        try
        {
            double latitude = 59.5086798659495;
            double longitude = 18.2654625932976;

            //Create the two tasks and wait for completion
            tasks[0] = service.GetForecastAsync(latitude, longitude);
            tasks[1] = service.GetForecastAsync("Miami");

            Task.WaitAll(tasks[0], tasks[1]);

            tasks[2] = service.GetForecastAsync(latitude, longitude);
            tasks[3] = service.GetForecastAsync("Miami");

            //Wait and confirm we get an event showing cahced data avaialable
            Task.WaitAll(tasks[2], tasks[3]);
        }
        catch (Exception ex)
        {
            exception = ex;
            //Handles an exception
            Console.WriteLine($"An error has occurred: {ex.Message}");
        }
        //Loop through all tasks
        foreach (var task in tasks)
        {
            //How to deal with successful and fault tasks
            //If task is successfully completed
            if (task.Status == TaskStatus.RanToCompletion)
            {
                if (task != null)
                {
                    //Print weather forecast city
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(new string('-', 70));
                    Console.WriteLine($"Weather forecast for {task.Result.City}\n");
                    Console.ResetColor();

                    //Call for method to print grouped forecast
                    PrintForcastGrouped(task.Result);

                    Console.WriteLine("Forecast recived to completion");
                }
                else
                {
                    Console.WriteLine("Forecast not avalible");
                }
            }
            //If the task encounters an error or faulted
            else if (task.Status == TaskStatus.Faulted)
            {
                Console.WriteLine($"Task failed: {task.Exception.Message}");
            }
        }
        //Event handler method to handle the WeatherForecastAvailable when triggerd
        static void OnWeatherForecastAvailable(object sender, string message)
        {
            Console.WriteLine(message);

        }
        //Method to print the weather grouped by date
        static void PrintForcastGrouped(Forecast forecast)
        {
            //Check if forecast or forecast items is null
            if (forecast == null || forecast.Items == null)
            { 
                Console.WriteLine("Forecast not avalible");
                return;
            }
            //Group the forecast items by date
            var forecastByDt = forecast.Items
                .GroupBy(x => x.DateTime.Date)
                .ToList();

            //Loops through each group
            foreach (var item in forecastByDt)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"Date:{item.Key:yyyy-MM-dd}");
                Console.ResetColor();

                foreach (var weather in item)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"    \t{weather.DateTime:HH:mm}: {weather.Description}, Temperature {weather.Temperature}°C, Wind {weather.WindSpeed} ");
                    Console.ResetColor();
                }
            }
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(new string('-', 70));
                Console.ResetColor();
        }
    }
    //Method to convert Unix timestamp to DateTime
    private DateTime UnixTimeStampToDateTime(double unixTimeStamp) => DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
}

