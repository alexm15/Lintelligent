using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lintelligent.CodeFixes
{
    public static class LicenseValidator
    {
        public static async Task<bool> IsLicenseValidAsync()
        {
            var licenseKey = Environment.GetEnvironmentVariable("LINTELLIGENT_LICENSE_KEY");
            if (string.IsNullOrWhiteSpace(licenseKey))
                return false;

            // Call your license server (replace with your real endpoint)
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync($"https://lintelligent.dev/api/validate?key={licenseKey}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Fail closed if server unreachable
                return false;
            }
        }
    }
}
