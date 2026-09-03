using ECommerce.Business.Abstractions;
using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Data.Persistence;
using ECommerce.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Data.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IInventoryRepository? _inventories;
    private ICartRepository? _carts;
    private IOrderRepository? _orders;
    private IPaymentRepository? _payments;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IAddressRepository? _addresses;

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IInventoryRepository Inventories => _inventories ??= new InventoryRepository(_context);
    public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IAddressRepository Addresses => _addresses ??= new AddressRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        await _context.DisposeAsync();
    }
}
