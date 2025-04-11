namespace Xams.Core.Attributes
{
    public class UIDisplayNameAttribute : Attribute
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public UIDisplayNameAttribute(string name, string tag = "")
        {
            this.Name = name;
            this.Tag = tag;
        }

        public UIDisplayNameAttribute(Field[] fields)
        {
            
        }
    }

    public struct Field()
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }
}