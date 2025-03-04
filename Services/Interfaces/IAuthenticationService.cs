using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(string pin);
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        UserSettings CurrentUser { get; }
    }
}