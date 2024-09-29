using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table(nameof(Option))]
public class Option
{
    public Guid OptionId { get; set; }
    [UIName]
    [MaxLength(250)]
    public string? Label { get; set; } 

    [UIDisplayName("Option Name")]
    [MaxLength(250)]
    public string? Name { get; set; } 

    [MaxLength(250)]
    public string? Value { get; set; } 
    
    public int? Order { get; set; }

    [UIHide]
    [MaxLength(250)]
    public string? Tag { get; set; }
}