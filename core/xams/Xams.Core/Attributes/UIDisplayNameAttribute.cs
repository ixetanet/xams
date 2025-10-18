namespace Xams.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class UIDisplayNameAttribute : Attribute
    {
        public string Name { get; set; }
        public string Field { get; set; }
        public string Tag { get; set; }

        public UIDisplayNameAttribute(string name, string field = "", string tag = "")
        {
            this.Name = name;
            this.Field = field;
            this.Tag = tag;
        }
    }
}