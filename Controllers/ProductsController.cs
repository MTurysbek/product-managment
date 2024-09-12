using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagementAPI.DTOs;
using ProductManagementAPI.Models;
using ProductManagementAPI.Services.Repositories;

namespace ProductManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;
    private readonly IMapper _mapper;

    public ProductsController(IProductRepository repository, ILogger<ProductsController> logger, IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "pageNumber", "pageSize" })]
    public async Task<IActionResult> GetProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber <= 0 || pageSize <= 0)
        {
            _logger.LogWarning("Invalid pagination parameters: pageNumber={PageNumber}, pageSize={PageSize}", pageNumber, pageSize);
            return BadRequest("Page number and page size must be greater than zero.");
        }
        
        var query = _repository.QueryProducts();
        
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            Items = _mapper.Map<IEnumerable<ProductDto>>(products)
        };
        
        _logger.LogInformation("Paged products request: pageNumber={PageNumber}, pageSize={PageSize}, totalItems={TotalItems}, totalPages={TotalPages}, itemsReturned={ItemsReturned}",
            pageNumber, pageSize, totalItems, totalPages, products.Count);
        
        return Ok(response);
    }

    [HttpGet("id")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found.", id);
            return NotFound();
        }

        var productDto = _mapper.Map<ProductDto>(product);
        
        _logger.LogInformation("Product with ID: {ProductId} successfully retrieved.", id);

        return Ok(productDto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Attempt to create product with invalid data: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        var product = _mapper.Map<Product>(dto);
        await _repository.AddProductAsync(product);
        var productDto = _mapper.Map<ProductDto>(product);
        
        _logger.LogInformation("Product created with ID: {ProductId}.", product.Id);
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, productDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Attempt to update product with ID: {ProductId} with invalid data: {@ModelState}", id, ModelState);
            return BadRequest(ModelState);
        }

        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Attempt to update non-existent product with ID: {ProductId}", id);
            return NotFound();
        }

        _mapper.Map(dto, product);
        await _repository.UpdateProductAsync(product);

        _logger.LogInformation("Product with ID: {ProductId} successfully updated.", id);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (!await _repository.ProductExistsAsync(id))
        {
            _logger.LogWarning("Attempt to delete non-existent product with ID: {ProductId}", id);
            return NotFound();
        }

        await _repository.DeleteProductAsync(id);
        
        _logger.LogInformation("Product with ID: {ProductId} successfully deleted.", id);
        
        return NoContent();
    } 
}