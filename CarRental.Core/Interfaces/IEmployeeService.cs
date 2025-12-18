using System.Threading.Tasks;
using System.Collections.Generic;
using CarRental.Core.Models;

namespace CarRental.Core.Interfaces
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<Employee?> GetEmployeeByEmailAsync(string email);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee> CreateEmployeeAsync(Employee employee);
        Task<Employee> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> VerifyEmployeeCredentialsAsync(string email, string password);
    }
}