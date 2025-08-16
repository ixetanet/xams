

Console.WriteLine("END");
// // See https://aka.ms/new-console-template for more information
//
// // Generate System metadata
//
// using System.ComponentModel.DataAnnotations.Schema;
// using System.Reflection;
// using System.Text.Json;
// using MyXProject.Data;
// using Xams.Core;
// using Xams.Core.Utils;
//
//
// File.WriteAllText("system-metadata.json", GenerateSystemMetadata());
//
// string GenerateSystemMetadata()
// {
//     List<Cache.Entity> entities = new();
//     foreach (var dbContextProps in typeof(DataContext).GetProperties())
//     {
//         var propType = dbContextProps.PropertyType;
//         if (propType.GenericTypeArguments.Length > 0)
//         {
//             Type tableType = propType.GenericTypeArguments[0];
//             
//             TableAttribute tableAttribute = tableType.GetCustomAttribute<TableAttribute>();
//             
//             if (EntityUtil.IsSystemEntity(tableAttribute.Name))
//             {
//                 var entity = new Cache.Entity()
//                 {
//                     Name = tableAttribute.Name,
//                     Properties = new List<Cache.EntityProperty>()
//                 };
//                 foreach (var property in tableType.GetProperties())
//                 {
//                     entity.Properties.Add(new Cache.EntityProperty()
//                     {
//                         Name = property.Name,
//                         Type = property.PropertyType.Name
//                     });
//                 }
//                 entities.Add(entity);
//             }
//         }
//     }
//
//     return JsonSerializer.Serialize(entities);
// }
