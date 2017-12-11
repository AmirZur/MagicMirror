using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MagicMirror.Models
{
    public class WeatherApi
    {
        private const string _apiKey = "<APIKEY>";

        public async Task<string> GetCurrentWeatherByZipAsync(string zip)
        {
            var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(string.Format($"http://api.openweathermap.org/data/2.5/weather?zip={zip},us&cnt=1&appid={_apiKey}"));
        }

        public async Task<string> GetCurrentWeatherByCityAsync(string city, string state)
        {
            var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(string.Format($"http://api.openweathermap.org/data/2.5/weather?q={city},{state}&cnt=1&appid={_apiKey}"));
        }
    }
}
