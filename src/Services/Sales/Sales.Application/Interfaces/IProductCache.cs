namespace Sales.Application;

/// <summary>
/// Cache-aside port for <see cref="ProductDto"/>.
/// </summary>
public interface IProductCache : ICacheService<ProductDto>;
