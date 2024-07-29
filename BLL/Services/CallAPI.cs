using BLL.Shared;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace aeroports
{
    public static class CallAPI
    {
        //using!
        public static async Task<GetAeroportResult> GetAeroportAsync(string code)
        {            
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri($"https://places-dev.cteleport.com/{code}"),
            };
            var response = await client.GetAsync($"/airports/{code}");
            if (response.IsSuccessStatusCode == true)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Airport>(content);
                return new GetAeroportResult { Airport = result};
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new GetAeroportResult { Result = TypeResult.BadRequest };
            }
            return new GetAeroportResult { Result = TypeResult.Failed};
        }
    }
}
