using CatalogReadService.Application.Models;
using CatalogReadService.Infrastructure.ReadStore;
using Elastic.Clients.Elasticsearch;
using Moq;
using StackExchange.Redis;
using Shared.Contracts.Events;

namespace Tests.IntegrationTests;

public class CatalogReadProjectionTests
{
    [Fact]
    public async Task ProductCreatedEvent_CallsEsIndexAndClearsCache()
    {
        // Arrange
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Loose, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductCreatedEvent 
        { 
            ProductId = Guid.NewGuid(), 
            Name = "X", 
            Description = "Y", 
            PriceCents = 123, 
            QuantityAvailable = 5, 
            Timestamp = DateTime.UtcNow 
        };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<Indices>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());
        
        esMock.Setup(e => e.IndexAsync(It.IsAny<ProductReadModel>(), It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexResponse());
        
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await projector.HandleAsync(evt, CancellationToken.None);

        // Assert - Verify ES was called with correct product data
        esMock.Verify(
            e => e.IndexAsync(
                It.Is<ProductReadModel>(m => 
                    m.Id == evt.ProductId && 
                    m.Name == evt.Name && 
                    m.Description == evt.Description &&
                    m.PriceCents == evt.PriceCents &&
                    m.QuantityAvailable == evt.QuantityAvailable),
                It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), 
                It.IsAny<CancellationToken>()), 
            Times.Once,
            "Should index the product in Elasticsearch with correct data");

        // Assert - Verify cache was cleared
        dbMock.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), 
            Times.Once,
            "Should clear the cache after indexing");
    }

    [Fact]
    public async Task ProductCreatedEvent_WithInvalidData_ShouldThrow()
    {
        // Arrange
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Loose, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductCreatedEvent 
        { 
            ProductId = Guid.Empty,  // Invalid - empty GUID
            Name = "", // Invalid - empty name
            Description = "Y", 
            PriceCents = -100,  // Invalid - negative price
            QuantityAvailable = 5, 
            Timestamp = DateTime.UtcNow 
        };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<Indices>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => projector.HandleAsync(evt, CancellationToken.None));
        
        // Verify ES was NOT called with invalid data
        esMock.Verify(
            e => e.IndexAsync(It.IsAny<ProductReadModel>(), It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), It.IsAny<CancellationToken>()), 
            Times.Never,
            "Should not index invalid product data");
    }

    [Fact]
    public async Task ProductPriceChangedEvent_UpdatesEsAndClearsCache()
    {
        // Arrange
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Loose, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductPriceChangedEvent 
        { 
            ProductId = Guid.NewGuid(), 
            OldPriceCents = 100, 
            NewPriceCents = 200, 
            Timestamp = DateTime.UtcNow 
        };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<Indices>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());
        
        esMock.Setup(e => e.UpdateAsync<ProductReadModel, object>(It.IsAny<UpdateRequest<ProductReadModel, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResponse<ProductReadModel>());
        
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await projector.HandleAsync(evt, CancellationToken.None);

        // Assert - Verify ES UpdateAsync was called
        esMock.Verify(
            e => e.UpdateAsync<ProductReadModel, object>(It.IsAny<UpdateRequest<ProductReadModel, object>>(), It.IsAny<CancellationToken>()), 
            Times.Once,
            "Should update the product in Elasticsearch");

        // Assert - Verify cache was cleared
        dbMock.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), 
            Times.Once,
            "Should clear the cache after updating price");
    }

    [Fact]
    public async Task ProductPriceChangedEvent_WithNegativePrice_ShouldThrow()
    {
        // Arrange
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Loose, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductPriceChangedEvent 
        { 
            ProductId = Guid.NewGuid(), 
            OldPriceCents = 100, 
            NewPriceCents = -50,  // Invalid - negative price
            Timestamp = DateTime.UtcNow 
        };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<Indices>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => projector.HandleAsync(evt, CancellationToken.None));

        // Verify ES was NOT called with invalid data
        esMock.Verify(
            e => e.UpdateAsync<ProductReadModel, object>(It.IsAny<UpdateRequest<ProductReadModel, object>>(), It.IsAny<CancellationToken>()), 
            Times.Never,
            "Should not update with invalid price");
    }

    [Fact]
    public async Task ProductCreatedEvent_IfEsFailsButRedisClearsFirst_ShouldHandleGracefully()
    {
        // Arrange
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Loose, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductCreatedEvent 
        { 
            ProductId = Guid.NewGuid(), 
            Name = "X", 
            Description = "Y", 
            PriceCents = 123, 
            QuantityAvailable = 5, 
            Timestamp = DateTime.UtcNow 
        };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<Indices>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());
        
        esMock.Setup(e => e.IndexAsync(It.IsAny<ProductReadModel>(), It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Elasticsearch connection failed"));
        
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => projector.HandleAsync(evt, CancellationToken.None));
        Assert.Contains("Elasticsearch connection failed", ex.Message);

        // Verify cache was still cleared even though ES failed
        dbMock.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), 
            Times.Once,
            "Should still clear cache even if Elasticsearch fails");
    }
}