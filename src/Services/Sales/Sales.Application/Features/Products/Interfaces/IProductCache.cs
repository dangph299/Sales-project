using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Interfaces;

/// <summary>
/// Cache-aside port for <see cref="ProductDto"/>.
/// </summary>
public interface IProductCache : ICacheService<ProductDto>;
