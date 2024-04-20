using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.Infrastructure.Repositories.Common;
public static class CrudOperations
{
    public static async Task<T> CreateAsync<T>(T entity, DbContext context) where T : class
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return entity;
    }

    public static async Task UpdateAsync<T>(T entity, DbContext context) where T : class
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public static async Task DeleteAsync<T>(T entity, DbContext context) where T : class
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Set<T>().Remove(entity);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    
    public static async Task<List<T>> FindAllAsync<T>(DbContext context) where T : class
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result=await context.Set<T>().ToListAsync();
        await transaction.CommitAsync();
        return result;
    }
    
    public static async Task<T?> FindByIdAsync<T>(DbContext context, params object[] primaryKey) where T : class
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await context.Set<T>().FindAsync(primaryKey);
        await transaction.CommitAsync();
        return result;
    }
}
