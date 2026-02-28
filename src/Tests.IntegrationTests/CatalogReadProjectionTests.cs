using System;
using System.Threading;
using System.Threading.Tasks;
using CatalogService.Infrastructure.ReadStore;
using Elastic.Clients.Elasticsearch;
using Moq;
using StackExchange.Redis;
using Shared.Contracts.Events;
using Xunit;

namespace Tests.IntegrationTests;

public class CatalogReadProjectionTests
{
    [Fact]
    public async Task ProductCreatedEvent_CallsEsIndexAndClearsCache()
    {
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Strict, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductCreatedEvent { ProductId = Guid.NewGuid(), Name="X", Description="Y", PriceCents=123, QuantityAvailable=5, Timestamp=DateTime.UtcNow };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            // .ReturnsAsync(new ExistsResponse { Exists = true });
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());
        // project uses the overload accepting an Action<IndexRequestDescriptor<T>>
        esMock.Setup(e => e.IndexAsync(It.IsAny<ProductReadModel>(), It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexResponse());
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await projector.HandleAsync(evt, CancellationToken.None);

        esMock.Verify(e => e.IndexAsync(It.Is<ProductReadModel>(m => m.Id == evt.ProductId && m.Name == "X"), It.IsAny<Action<IndexRequestDescriptor<ProductReadModel>>>(), It.IsAny<CancellationToken>()), Times.Once);
        dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ProductPriceChangedEvent_UpdatesEsAndClearsCache()
    {
        var esMock = new Mock<ElasticsearchClient>(MockBehavior.Strict, new object[] { new ElasticsearchClientSettings(new Uri("http://localhost:9200")) });
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var projector = new ProductReadProjector(esMock.Object, redisMock.Object);
        var evt = new ProductPriceChangedEvent { ProductId = Guid.NewGuid(), OldPriceCents = 100, NewPriceCents = 200, Timestamp = DateTime.UtcNow };

        esMock.Setup(e => e.Indices.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           // .ReturnsAsync(new ExistsResponse { Exists = true });
            .ReturnsAsync(() => new Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse());
        // simplify to match overload using UpdateRequest object
        esMock.Setup(e => e.UpdateAsync<ProductReadModel, object>(It.IsAny<UpdateRequest<ProductReadModel, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResponse<ProductReadModel>());
        dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await projector.HandleAsync(evt, CancellationToken.None);
        esMock.Verify(e => e.UpdateAsync<ProductReadModel, object>(It.Is<UpdateRequest<ProductReadModel, object>>(r => 
            //r.Id 
            (r.Doc as ProductReadModel).Id.ToString()
            == evt.ProductId.ToString()), It.IsAny<CancellationToken>()), Times.Once);
        dbMock.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
