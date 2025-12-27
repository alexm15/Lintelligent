using System.Threading.Tasks;

namespace Lintelligent.CodeFixes
{
    public static class CodeFixProFeatureGate
    {
        public static async Task<bool> IsProEnabledAsync()
        {
            return await LicenseValidator.IsLicenseValidAsync();
        }
    }
}
