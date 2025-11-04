using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Services;

public interface IDriverService
{
    Task<IEnumerable<Driver>> GetDriversAsync();
    Task<Driver?> GetDriverByIdAsync(int id);
    Task<Driver> CreateDriverAsync(Driver driver);
    Task<Driver> UpdateDriverAsync(Driver driver);
    Task<bool> DeleteDriverAsync(int id);
}
