namespace payment_gateway_API.src.Models;

public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Key { get; set; }
    public required string Operation { get; set; }
    public Guid UserId { get; set; }
    public required string ResponseJson { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
