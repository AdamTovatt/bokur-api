using InvoiceGenerator.Models.Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Helpers
{
    internal class ApiHelper
    {
        public static ApiHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ApiHelper();
                return _instance;
            }
        }

        private static ApiHelper? _instance;
        private HttpClient client;

        public ApiHelper()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36");
        }

        public async Task<HttpResponseMessage> GetResponseAsync(string endpoint)
        {
            return await client.GetAsync(endpoint);
        }

        public async Task<FontListResponse> GetFontList(string familyName)
        {
            HttpResponseMessage response = await client.GetAsync($"https://fonts.google.com/download/list?family={familyName}");

            if (response.IsSuccessStatusCode)
            {
                string json = (await response.Content.ReadAsStringAsync()).Substring(")]}'".Length);
                FontListResponse? fontListResponse = FontListResponse.FromJson(json);

                if (fontListResponse == null)
                    throw new Exception($"Error when parsing the response from getting the fonts list. This was the json: {json}");

                return fontListResponse;
            }
            else
            {
                throw new Exception($"Error when getting fonts with family name \"{familyName}\": {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task<byte[]> GetSingleFontVariationAsync(string variationUrl)
        {
            HttpResponseMessage response = await client.GetAsync(variationUrl);

            if (response.IsSuccessStatusCode)
            {
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
            else
            {
                throw new Exception($"Error when getting fonts with variationUrl: {variationUrl}");
            }
        }

        public async Task<byte[]> GetImageBytes(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
            else
            {
                throw new Exception($"Error when image with url: {url}");
            }
        }
    }
}
