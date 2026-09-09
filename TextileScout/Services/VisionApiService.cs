using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextileScout.Web.Services
{
    public class VisionApiService
    {
        private readonly HttpClient _httpClient;

        public VisionApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Python FastAPI sunucu adresimiz
            _httpClient.BaseAddress = new Uri("http://localhost:8000/");
        }

        public async Task<bool> IsClothingImageAsync(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath)) return false;

                using var form = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(imagePath);
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                form.Add(streamContent, "file", Path.GetFileName(imagePath));

                // Python FastAPI /analyze endpoint'ine isteği atıyoruz
                var response = await _httpClient.PostAsync("analyze", form);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<VisionApiResponse>(responseString);

                    // Python'dan dönen "is_clothing" değerini oku
                    return result?.IsClothing ?? false;
                }
            }
            catch (Exception ex)
            {
                // Python API kapalıysa veya hata verirse resmin kaybolmaması için varsayılan true geçebiliriz
                Console.WriteLine($"Vision API Bağlantı Hatası: {ex.Message}");
                return true;
            }

            return false;
        }
    }

    // Python API'den dönecek JSON yanıt modeli
    public class VisionApiResponse
    {
        [JsonPropertyName("is_clothing")]
        public bool IsClothing { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }
    }
}