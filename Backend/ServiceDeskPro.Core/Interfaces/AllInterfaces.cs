namespace ServiceDeskPro.Core.Interfaces;

public interface IOrderRepository
{
    Task<Entities.Order?> GetByIdAsync(int id);
    Task<IEnumerable<Entities.Order>> GetAllAsync();
    Task<IEnumerable<Entities.Order>> GetByStatusAsync(string status);
    Task<IEnumerable<Entities.Order>> GetByClientAsync(int clientId);
    Task<Entities.Order> CreateAsync(Entities.Order order);
    Task UpdateAsync(Entities.Order order);
    Task DeleteAsync(int id);
}

public interface IClientRepository
{
    Task<Entities.Client?> GetByIdAsync(int id);
    Task<IEnumerable<Entities.Client>> GetAllAsync();
    Task<Entities.Client> CreateAsync(Entities.Client client);
    Task UpdateAsync(Entities.Client client);
}

public interface IMasterRepository
{
    Task<Entities.Master?> GetByIdAsync(int id);
    Task<IEnumerable<Entities.Master>> GetAllAsync();
}

public interface ISparePartRepository
{
    Task<Entities.SparePart?> GetByIdAsync(int id);
    Task<IEnumerable<Entities.SparePart>> GetAllAsync();
}