namespace Common.Shared.DTOs;

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Count { get; set; }
    public decimal UnitPrice { get; set; }
}