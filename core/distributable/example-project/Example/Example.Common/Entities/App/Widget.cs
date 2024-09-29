using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities.App;

[Table("Widget")]
public class Widget
{
    public Guid WidgetId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}