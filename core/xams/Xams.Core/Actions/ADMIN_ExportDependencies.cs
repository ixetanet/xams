using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions
{
    [ServiceAction("ADMIN_ExportDependencies")]
    public class ADMIN_ExportDependencies : IServiceAction
    {
        public async Task<Response<object?>> Execute(ActionServiceContext context)
        {
            return new Response<object?>()
            {
                Succeeded = true,
                Data = BuildDependencyGraph(context.DataRepository.GetDbContext<IXamsDbContext>())
                    .OrderBy(x => x.Name)
            };
        }
    
        public List<DependencyInfo> BuildDependencyGraph(IXamsDbContext context)
        {
            var graph = new List<DependencyInfo>();

            var dbSetProperties = context.GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToList();

            // var modelBuilder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
            var model = context.Model; // modelBuilder.Model;

            foreach (var prop in dbSetProperties)
            {
                var entityType = prop.PropertyType.GetGenericArguments()[0];
                var navigationProperties = model.FindEntityType(entityType).GetNavigations();

                var dependencies = new List<Dependent>();

                foreach (var navProp in navigationProperties)
                {
                    if (new[] {"CreatedBy", "UpdatedBy"}.Contains(navProp.Name))
                        continue;
                    bool isOptional = false;
                    var idProp = entityType.GetProperty($"{navProp.Name}Id");
                    if (idProp != null)
                    {
                        var underlingType = Nullable.GetUnderlyingType(idProp.PropertyType);
                        if (underlingType != null && underlingType == typeof(Guid))
                        {
                            isOptional = true;
                        }
                    }
                
                    var targetType = navProp.TargetEntityType.ClrType;
                    var existingDependency = dependencies.FirstOrDefault(x => x.Name == targetType.Name);
                    if (existingDependency != null)
                    {
                        if (!isOptional)
                        {
                            existingDependency.Optional = isOptional;    
                        }
                    }
                
                    if (existingDependency == null)
                    {
                        dependencies.Add(new Dependent()
                        {
                            Name = targetType.Metadata().TableName,
                            Optional = isOptional
                        });
                    }
                }

                // graph[entityType.Name] = dependencies;
                graph.Add(new DependencyInfo()
                {
                    Name = entityType.Metadata().TableName,
                    Dependencies = dependencies
                });
            }

            return graph;
        }

        public class DependencyInfo
        {
            public string Name { get; set; }
            public List<Dependent> Dependencies { get; set; }
        }

        public class Dependent
        {
            public string Name { get; set; }
            public bool Optional { get; set; }
        }
    }
}