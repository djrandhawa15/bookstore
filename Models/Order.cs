namespace Bookstore.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();

    public decimal GetTotal() => OrderItems.Sum(item => item.GetTotal());
}