using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Address;
using ECommerce.Models.Entities;
using AutoMapper;

namespace ECommerce.Business.Services.Addresses;

public class AddressService : IAddressService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public AddressService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IReadOnlyList<AddressDto>>> GetByUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var addresses = await _uow.Addresses.GetByUserAsync(userId, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<AddressDto>>(addresses);

        return ServiceResult<IReadOnlyList<AddressDto>>.Success(dtos);
    }

    public async Task<ServiceResult<AddressDto>> CreateAsync(
        int userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var userExists = await _uow.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
        {
            return ServiceResult<AddressDto>.Failure("User not found.");
        }

        var address = new Address
        {
            UserId = userId,
            FullName = request.FullName,
            Street = request.Street,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Phone = request.Phone,
            IsDefault = request.IsDefault,
            CreatedBy = userId.ToString()
        };

        if (address.IsDefault)
        {
            await UnsetDefaultAsync(userId, cancellationToken);
        }

        await _uow.Addresses.AddAsync(address, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<AddressDto>(address);
        return ServiceResult<AddressDto>.Success(dto, "Address created.");
    }

    public async Task<ServiceResult<AddressDto>> UpdateAsync(
        int userId,
        int addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var address = await FindOwnedAsync(userId, addressId, cancellationToken);

        if (address is null)
        {
            return ServiceResult<AddressDto>.Failure("Address not found.");
        }

        if (request.IsDefault && !address.IsDefault)
        {
            await UnsetDefaultAsync(userId, cancellationToken);
        }

        address.FullName = request.FullName;
        address.Street = request.Street;
        address.City = request.City;
        address.State = request.State;
        address.PostalCode = request.PostalCode;
        address.Country = request.Country;
        address.Phone = request.Phone;
        address.IsDefault = request.IsDefault;
        address.UpdatedBy = userId.ToString();

        _uow.Addresses.Update(address);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<AddressDto>(address);
        return ServiceResult<AddressDto>.Success(dto, "Address updated.");
    }

    public async Task<ServiceResult> DeleteAsync(
        int userId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var address = await FindOwnedAsync(userId, addressId, cancellationToken);

        if (address is null)
        {
            return ServiceResult.Failure("Address not found.");
        }

        _uow.Addresses.Remove(address);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success("Address deleted.");
    }

    private async Task<Address?> FindOwnedAsync(
        int userId,
        int addressId,
        CancellationToken cancellationToken)
    {
        var address = await _uow.Addresses.GetByIdAsync(addressId, cancellationToken);
        return address?.UserId == userId ? address : null;
    }

    private async Task UnsetDefaultAsync(int userId, CancellationToken cancellationToken)
    {
        var all = await _uow.Addresses.GetByUserAsync(userId, cancellationToken);
        foreach (var a in all.Where(a => a.IsDefault))
        {
            a.IsDefault = false;
        }
    }
}
