namespace Xams.Core.Attributes
{
    public class UIHideAttribute : Attribute
    {
        public bool Queryable { get; set; }
        /// <summary>
        /// Field cannot be read from the UI.
        /// </summary>
        /// <param name="queryable">If true, this field can still be used as a filter from the UI.</param>
        public UIHideAttribute(bool queryable = false)
        {
            Queryable = queryable;
        }
    }
}