using AppNomesBr.Domain.Entities;

namespace AppNomesBr.Domain.Interfaces.Repositories
{
    public interface INomesBrRepository
    {
        Task<List<NomesBr>> GetAll();
        Task<NomesBr> GetById(int id);
        Task Create(NomesBr data);
        Task Update(NomesBr data);
        Task Delete(int id);
    }
}
