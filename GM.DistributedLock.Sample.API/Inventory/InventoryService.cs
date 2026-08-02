using GM.DistributedLock;

namespace GM.DistributedLock.Sample.API.Inventory;

/// <summary>
/// Reserves stock under a per-SKU distributed lock, so the non-atomic
/// <see cref="InventoryStore.TryConsume"/> is safe even when many requests (or many service
/// instances) hit the same SKU at once — no overselling.
/// </summary>
public sealed class InventoryService(IDistributedLock locks, InventoryStore store)
{
    public async Task<bool> ReserveAsync(string sku, CancellationToken cancellationToken = default)
    {
        // Serialize all reservations for this SKU. Each holder does the check-and-decrement alone.
        await using var handle = await locks.AcquireAsync(
            resource: $"stock:{sku}",
            expiry: TimeSpan.FromSeconds(30),
            wait: TimeSpan.FromSeconds(10),
            retryInterval: TimeSpan.FromMilliseconds(25),
            cancellationToken);

        return store.TryConsume(sku);
    }

    public int Remaining(string sku) => store.Remaining(sku);
}
