using System.Threading.Tasks;
using CarRental.Core.Models;

namespace CarRental.Core.Interfaces
{
    public interface IAuthService
    {
        Task<Employee?> ValidateLoginAsync(string email, string password);
        Task<Employee> RegisterEmployeeAsync(Employee employee, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}