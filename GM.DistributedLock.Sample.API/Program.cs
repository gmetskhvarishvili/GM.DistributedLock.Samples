using GM.DistributedLock;
using GM.DistributedLock.Redis;
using GM.DistributedLock.Sample.API.Inventory;

var builder = WebApplication.CreateBuilder(args);

// Choose the lock backend from configuration: "Lock:Provider" = "Redis" or "Memory" (default).
// Both expose the same IDistributedLock, so nothing else in the app changes. Use Redis to make the
// lock work across multiple instances/machines; in-memory only coordinates within one process.
if (string.Equals(builder.Configuration["Lock:Provider"], "Redis", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddGMRedisDistributedLock(o =>
    {
        o.ConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        o.KeyPrefix = "gm-lock-sample:";
    });
}
else
{
    builder.Services.AddGMDistributedLock();
}

// A demo stock ledger seeded with a few SKUs.
var store = new InventoryStore();
store.Seed("widget", 5);
store.Seed("gadget", 100);
builder.Services.AddSingleton(store);
builder.Services.AddScoped<InventoryService>();

var app = builder.Build();

// Fire many concurrent reservations at "widget" (stock 5): exactly 5 succeed, the rest fail —
// the per-SKU lock serializes the check-and-decrement so it can never oversell.
app.MapPost("/inventory/{sku}/reserve", async (string sku, InventoryService inventory) =>
{
    var reserved = await inventory.ReserveAsync(sku);
    return Results.Ok(new { sku, reserved, remaining = inventory.Remaining(sku) });
});

app.MapGet("/inventory/{sku}", (string sku, InventoryService inventory) =>
    Results.Ok(new { sku, remaining = inventory.Remaining(sku) }));

app.Run();
