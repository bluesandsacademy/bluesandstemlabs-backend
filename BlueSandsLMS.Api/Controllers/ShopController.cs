using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{

    public sealed record PlaceOrderRequest(
        [Required] Guid ProductId,
        [Range(1, 1000)] int Quantity
    );

    [ApiController]
    public sealed class ShopController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public ShopController(BlueSandsLMSDbContext db) => _db = db;

        private Guid Me()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        [HttpGet("api/shop/products")]
        [AllowAnonymous]
        public async Task<IActionResult> Products([FromQuery] string? category, CancellationToken ct)
        {
            var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var items = await query
                .OrderBy(p => p.Category).ThenBy(p => p.Name)
                .Select(p => new
                {
                    p.Id, p.Name, p.Description, p.Price, p.Currency,
                    p.Category, p.ImageUrl, p.StockCount, p.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpPost("api/shop/orders")]
        [Authorize]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req, CancellationToken ct)
        {
            if (req.Quantity < 1) return BadRequest(new { message = "Quantity must be at least 1." });

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                var product = await _db.Products
                    .FirstOrDefaultAsync(p => p.Id == req.ProductId && p.IsActive, ct);

                if (product == null)
                    return (IActionResult)NotFound(new { message = "Product not found or inactive." });

                if (product.StockCount < req.Quantity)
                    return BadRequest(new { message = $"Insufficient stock. Only {product.StockCount} left." });

                product.StockCount -= req.Quantity;

                var order = new Order
                {
                    UserId      = Me(),
                    ProductId   = product.Id,
                    Quantity    = req.Quantity,
                    TotalAmount = product.Price * req.Quantity,
                    Currency    = product.Currency,
                    Status      = OrderStatus.Pending,
                    CreatedAt   = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Ok(new { id = order.Id, status = order.Status.ToString(), total = order.TotalAmount, currency = order.Currency });
            });
        }

        [HttpGet("api/shop/orders/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
        {
            var me = Me();
            var isAdmin = User.IsInRole("GlobalAdmin");

            var order = await _db.Orders.AsNoTracking()
                .Where(o => o.Id == id && (isAdmin || o.UserId == me))
                .Select(o => new
                {
                    o.Id, o.ProductId, o.Quantity, o.TotalAmount, o.Currency,
                    Status = o.Status.ToString(), o.CreatedAt,
                    ProductName = o.Product != null ? o.Product.Name : null
                })
                .FirstOrDefaultAsync(ct);

            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet("api/admin/shop/orders")]
        [Authorize(Roles = "GlobalAdmin")]
        public async Task<IActionResult> AdminOrders(
            [FromQuery] OrderStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.Orders.AsNoTracking().AsQueryable();
            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id, o.UserId, o.ProductId, o.Quantity, o.TotalAmount, o.Currency,
                    Status = o.Status.ToString(), o.CreatedAt,
                    ProductName = o.Product != null ? o.Product.Name : null
                })
                .ToListAsync(ct);

            return Ok(new { total, page, pageSize, items });
        }
    }
}
