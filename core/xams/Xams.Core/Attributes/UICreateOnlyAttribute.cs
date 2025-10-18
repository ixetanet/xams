// ReSharper disable InconsistentNaming
namespace Xams.Core.Attributes
{
    public class UICreateOnlyAttribute : Attribute
    {
        public string[] Fields { get; private set; }

        public UICreateOnlyAttribute()
        {
            Fields = [];
        }

        public UICreateOnlyAttribute(params string[] fields)
        {
            Fields = fields;
        }
    }
}
