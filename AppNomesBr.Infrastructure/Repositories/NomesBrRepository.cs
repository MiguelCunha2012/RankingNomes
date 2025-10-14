using AppNomesBr.Domain.Entities;
using AppNomesBr.Domain.Interfaces.Repositories;
using AppNomesBr.Infrastructure.Context;
using Microsoft.Extensions.Configuration;

namespace AppNomesBr.Infrastructure.Repositories
{
    public class NomesBrRepository : LocalDbContext, INomesBrRepository
    {
        public NomesBrRepository(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<List<NomesBr>> GetAll()
        {
            return await connection.Table<NomesBr>().ToListAsync();
        }
        public async Task<NomesBr> GetById(int id)
        {
            return await connection.Table<NomesBr>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task Create(NomesBr data)
        {
            await connection.InsertAsync(data);
        }

        public async Task Update(NomesBr data)
        {
            var exists = await GetById(data.Id);
            if(exists != null)
                await connection.UpdateAsync(data);
        }

        public async Task Delete(int id)
        {
            var result = await GetById(id);
            await connection.DeleteAsync(result);
        }
    }
}
