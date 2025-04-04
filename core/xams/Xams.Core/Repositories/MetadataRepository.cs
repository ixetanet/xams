using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Repositories
{
    public class MetadataRepository
    {
        private Type _dataContextType = typeof(IXamsDbContext);
        private IXamsDbContext? _dataContext;

        public MetadataRepository(Type dataContext)
        {
            _dataContextType = dataContext;
            _dataContext = (IXamsDbContext?)Activator.CreateInstance(_dataContextType);
        }

        public IXamsDbContext? GetDataContext()
        {
            return (IXamsDbContext?)Activator.CreateInstance(_dataContextType);
        }

        public async Task<Response<object?>> Metadata(MetadataInput metadataInput, Guid userId)
        {
            if (metadataInput.method == "table_metadata")
            {
                return GetTableMetadata(metadataInput);
            }

            if (metadataInput.method == "table_list")
            {
                return await Tables(metadataInput, userId);
            }

            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "Method not found."
            };
        }

        public Response<object?> GetTableMetadata(MetadataInput metadataInput)
        {
            if (metadataInput.parameters == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Parameters are required."
                };
            }
            
            if (!metadataInput.parameters.ContainsKey("tableName"))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "tableName is required."
                };
            }
            
            string tableName = metadataInput.parameters["tableName"].GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(tableName))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "tableName is required."
                };
            }

            if (!Cache.Instance.TableMetadata.ContainsKey(tableName))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Table {tableName} not found."
                };
            }
            
            var tableMetadata = Cache.Instance.GetTableMetadata(tableName);
            
            return new Response<object?>()
            {
                Succeeded = true,
                Data = tableMetadata.MetadataOutput
            };
        }

        public async Task<Response<object?>> Tables(MetadataInput metadataInput, Guid userId)
        {
            if (metadataInput.parameters == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Parameters are required."
                };
            }
            
            string? tag = metadataInput.parameters.ContainsKey("tag") ? metadataInput.parameters["tag"].GetString() : null;
            List<TablesOutput> tables = new List<TablesOutput>();
            if (tag != null)
            {
                tables = Cache.Instance.GetTableMetadata()
                    .Where(x => x.DisplayNameAttribute.Tag == tag)
                    .Select(x => new TablesOutput()
                    {
                        tableName = x.TableName,
                        displayName = x.DisplayNameAttribute?.Name ?? x.TableName,
                        tag = x.DisplayNameAttribute?.Tag ?? ""
                    }).ToList();
            }
            else
            {
                tables = Cache.Instance.GetTableMetadata()
                    .Select(x => new TablesOutput()
                    {
                        tableName = x.TableName,
                        displayName = x.DisplayNameAttribute?.Name ?? x.TableName,
                        tag = x.DisplayNameAttribute?.Tag ?? ""
                    }).ToList();
            }

            // Only return tables the user has read access to
            string[] permissions = await PermissionCache.GetUserPermissions(userId);
            List<TablesOutput> filteredTables = new List<TablesOutput>();
            foreach (var table in tables)
            {
                if (permissions.Any(x => x.StartsWith($"TABLE_{table.tableName}_READ_")))
                {
                    filteredTables.Add(table);
                }
            }

            Response<object?> results = new Response<object?>();
            results.Data = filteredTables.OrderBy(x => x.tableName).ToArray();
            results.Succeeded = true;
            return results;
        }
        
    
    }
}