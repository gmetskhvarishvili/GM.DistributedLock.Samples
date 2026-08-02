using System.Collections.Concurrent;

namespace GM.DistributedLock.Sample.API.Inventory;

/// <summary>
/// A deliberately naive in-memory stock ledger: <see cref="TryConsume"/> does a non-atomic
/// read-then-write, so without a lock around it concurrent callers would oversell. The
/// <see cref="InventoryService"/> guards it with a distributed lock to keep it correct.
/// </summary>
public sealed class InventoryStore
{
    private readonly ConcurrentDictionary<string, int> _stock = new();

    public void Seed(string sku, int quantity) => _stock[sku] = quantity;

    public int Remaining(string sku) => _stock.TryGetValue(sku, out var q) ? q : 0;

    /// <summary>Consumes one unit if available. NOT atomic on its own — call it under a lock.</summary>
    public bool TryConsume(string sku)
    {
        var current = Remaining(sku);
        if (current <= 0)
            return false;

        // A real race window: another thread could read the same 'current' before we write back.
        _stock[sku] = current - 1;
        return true;
    }
}
