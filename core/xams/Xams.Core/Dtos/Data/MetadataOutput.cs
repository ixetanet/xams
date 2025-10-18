namespace Xams.Core.Dtos.Data
{
    public class MetadataOutput
    {
        public string tableName { get; set; }
        public string displayName { get; set; }
        public string primaryKey { get; set; }
        public List<MetadataField> fields { get; set; }
    }


    public class MetadataField
    {
        public required string name { get; set; }
        public string? displayName { get; set; }
        public string? type { get; set; }
        public int? characterLimit { get; set; }
        public int order { get; set; }
        public string? lookupName { get; set; }
        public string? lookupTable { get; set; }
        public string? lookupTableNameField { get; set; }
        public string? lookupTableDescriptionField { get; set; }
        public string? lookupTablePrimaryKeyField { get; set; }
        public bool lookupTableHasActiveField { get; set; }
        public string? dateFormat { get; set; }
        public bool isNullable { get; set; }
        public bool isRequired { get; set; }
        public bool isRecommended { get; set; }
        public bool isReadOnly { get; set; }
        public bool isCreateOnly { get; set; }
        public string option { get; set; }
        public string? numberRange { get; set; }
        public MetadataMultiSelect? multiSelect { get; set; }
    }

    public class MetadataMultiSelect
    {
        public required string junctionTable { get; set; }
        public required string junctionOwnerIdField { get; set; }
        public required string junctionTargetIdField { get; set; }
        public required string targetTable { get; set; }
        public required string targetNameField { get; set; }
        public string? targetDescriptionField { get; set; }
        public required string targetPrimaryKeyField { get; set; }
        public bool targetHasActiveField { get; set; }
    }
}