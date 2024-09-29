namespace Xams.Core.Attributes
{
    /// <summary>
    /// Sets a string field on the entity to the lookupname value of a related entity
    /// </summary>
    public class UISetFieldFromLookupAttribute : Attribute
    {
        public string LookupIdProperty { get; set; }
    
        public UISetFieldFromLookupAttribute(string lookupIdProperty)
        {
            LookupIdProperty = lookupIdProperty;
        }
    }
}