using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace Example.Common.Entities;

[Table("Option")]
public class Option
{
    public Guid OptionId { get; set; }
    
    [UIName]
    public string Label { get; set; }
    
    [UIDisplayName("Option Name")]
    public string Name { get; set; }
    
    public string Value { get; set; }
    [UIHide]
    public string Tag { get; set; }
}