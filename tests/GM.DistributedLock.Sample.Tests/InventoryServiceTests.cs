using GM.DistributedLock;
using GM.DistributedLock.Sample.API.Inventory;
using Xunit;

namespace GM.DistributedLock.Sample.Tests;

public class InventoryServiceTests
{
    private static InventoryService NewService(string sku, int quantity, out InventoryStore store)
    {
        store = new InventoryStore();
        store.Seed(sku, quantity);
        return new InventoryService(new InMemoryDistributedLock(), store);
    }

    [Fact]
    public async Task Reserve_DecrementsStock()
    {
        var service = NewService("widget", 3, out var store);

        Assert.True(await service.ReserveAsync("widget"));
        Assert.Equal(2, store.Remaining("widget"));
    }

    [Fact]
    public async Task Reserve_FailsWhenOutOfStock()
    {
        var service = NewService("widget", 0, out _);

        Assert.False(await service.ReserveAsync("widget"));
    }

    [Fact]
    public async Task ConcurrentReservations_NeverOversell()
    {
        const int stock = 5;
        const int attempts = 50;
        var service = NewService("widget", stock, out var store);

        var results = await Task.WhenAll(
            Enumerable.Range(0, attempts).Select(_ => service.ReserveAsync("widget")));

        Assert.Equal(stock, results.Count(reserved => reserved));  // exactly 5 succeed
        Assert.Equal(0, store.Remaining("widget"));                 // stock never goes negative
    }

    [Fact]
    public async Task ReservationsForDifferentSkus_AreIndependent()
    {
        var store = new InventoryStore();
        store.Seed("a", 1);
        store.Seed("b", 1);
        var service = new InventoryService(new InMemoryDistributedLock(), store);

        Assert.True(await service.ReserveAsync("a"));
        Assert.True(await service.ReserveAsync("b"));
    }
}
