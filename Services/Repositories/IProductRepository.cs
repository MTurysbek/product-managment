using ProductManagementAPI.Models;

namespace ProductManagementAPI.Services.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<bool> ProductExistsAsync(int id);
    IQueryable<Product> QueryProducts();
}