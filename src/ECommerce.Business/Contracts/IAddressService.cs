using ECommerce.Business.DTOs.Address;

namespace ECommerce.Business.Contracts;

public interface IAddressService
{
    Task<ServiceResult<IReadOnlyList<AddressDto>>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<AddressDto>> CreateAsync(
        int userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<AddressDto>> UpdateAsync(
        int userId,
        int addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int userId, int addressId, CancellationToken cancellationToken = default);
}
