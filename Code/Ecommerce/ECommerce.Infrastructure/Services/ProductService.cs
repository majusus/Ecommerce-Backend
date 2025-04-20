using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITextSummarizationService _summarizationService;

        public ProductService(
            IProductRepository productRepository, 
            ICategoryRepository categoryRepository,
            ITextSummarizationService summarizationService)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _summarizationService = summarizationService;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 10)
        {
            var products = await _productRepository.GetAllAsync(page, pageSize);
            return products.Select(MapToProductDto);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product != null ? MapToProductDto(product) : null;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            // Validate that the category exists
            var category = await _categoryRepository.GetByIdAsync(createProductDto.CategoryId);
            if (category == null)
            {
                throw new InvalidOperationException($"Category with ID {createProductDto.CategoryId} not found");
            }

            var product = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                CategoryId = createProductDto.CategoryId,
                ImageUrl = createProductDto.ImageUrl,
                StockQuantity = createProductDto.StockQuantity,
                CreatedDate = DateTime.UtcNow,
                Attributes = createProductDto.Attributes ?? new Dictionary<string, object>()
            };

            var productId = await _productRepository.CreateAsync(product);
            product.Id = productId;
            product.Category = category; // Set the category for the DTO mapping

            return MapToProductDto(product);
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;

            // If category is being changed, validate it exists
            if (updateProductDto.CategoryId.HasValue && updateProductDto.CategoryId.Value != product.CategoryId)
            {
                var category = await _categoryRepository.GetByIdAsync(updateProductDto.CategoryId.Value);
                if (category == null)
                {
                    throw new InvalidOperationException($"Category with ID {updateProductDto.CategoryId} not found");
                }
                product.CategoryId = updateProductDto.CategoryId.Value;
            }

            // Update only non-null values
            if (!string.IsNullOrEmpty(updateProductDto.Name))
                product.Name = updateProductDto.Name;
            if (!string.IsNullOrEmpty(updateProductDto.Description))
                product.Description = updateProductDto.Description;
            if (!string.IsNullOrEmpty(updateProductDto.ImageUrl))
                product.ImageUrl = updateProductDto.ImageUrl;
            if (updateProductDto.StockQuantity.HasValue)
                product.StockQuantity = updateProductDto.StockQuantity.Value;

            // Update price if it's provided and not zero
            if (updateProductDto.Price > 0)
                product.Price = updateProductDto.Price;

            if (updateProductDto.Attributes != null)
            {
                foreach (var attr in updateProductDto.Attributes)
                {
                    product.Attributes[attr.Key] = attr.Value;
                }
            }

            return await _productRepository.UpdateAsync(product);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _productRepository.DeleteAsync(id);
        }

        public async Task<ProductSummaryDto> GetProductSummaryAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return null;

            var summary = await _summarizationService.SummarizeTextAsync(product.Description);

            return new ProductSummaryDto
            {
                ProductId = product.Id,
                Name = product.Name,
                OriginalDescription = product.Description,
                Summary = summary
            };
        }

        private ProductDto MapToProductDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
                CreatedDate = product.CreatedDate,
                Attributes = product.Attributes
            };
        }
    }
}