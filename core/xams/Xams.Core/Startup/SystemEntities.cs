namespace Xams.Core.Startup;

public class SystemEntities
{
    public static string JsonValidationSchema = @"[
  {
    ""Name"": ""User"",
    ""Properties"": [
      {
        ""Name"": ""UserId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""Role"",
    ""Properties"": [
      {
        ""Name"": ""RoleId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""Permission"",
    ""Properties"": [
      {
        ""Name"": ""PermissionId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Tag"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""Team"",
    ""Properties"": [
      {
        ""Name"": ""TeamId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""TeamUser"",
    ""Properties"": [
      {
        ""Name"": ""TeamUserId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""TeamId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Team"",
        ""Type"": ""Team""
      },
      {
        ""Name"": ""UserId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""User"",
        ""Type"": ""User""
      }
    ]
  },
  {
    ""Name"": ""TeamRole"",
    ""Properties"": [
      {
        ""Name"": ""TeamRoleId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""TeamId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Team"",
        ""Type"": ""Team""
      },
      {
        ""Name"": ""RoleId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Role"",
        ""Type"": ""Role""
      }
    ]
  },
  {
    ""Name"": ""RolePermission"",
    ""Properties"": [
      {
        ""Name"": ""RolePermissionId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""RoleId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Role"",
        ""Type"": ""Role""
      },
      {
        ""Name"": ""PermissionId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Permission"",
        ""Type"": ""Permission""
      }
    ]
  },
  {
    ""Name"": ""UserRole"",
    ""Properties"": [
      {
        ""Name"": ""UserRoleId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""User"",
        ""Type"": ""User""
      },
      {
        ""Name"": ""UserId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Role"",
        ""Type"": ""Role""
      },
      {
        ""Name"": ""RoleId"",
        ""Type"": ""Guid""
      }
    ]
  },
  {
    ""Name"": ""Option"",
    ""Properties"": [
      {
        ""Name"": ""OptionId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Label"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Value"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Tag"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""Setting"",
    ""Properties"": [
      {
        ""Name"": ""SettingId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Value"",
        ""Type"": ""String""
      }
    ]
  },
  {
    ""Name"": ""Job"",
    ""Properties"": [
      {
        ""Name"": ""JobId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""IsActive"",
        ""Type"": ""Boolean""
      },
      {
        ""Name"": ""Queue"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Status"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""LastExecution"",
        ""Type"": ""DateTime""
      },
      {
        ""Name"": ""Ping"",
        ""Type"": ""DateTime""
      }
    ]
  },
  {
    ""Name"": ""JobHistory"",
    ""Properties"": [
      {
        ""Name"": ""JobHistoryId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Name"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""JobId"",
        ""Type"": ""Guid""
      },
      {
        ""Name"": ""Job"",
        ""Type"": ""Job""
      },
      {
        ""Name"": ""Status"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""Message"",
        ""Type"": ""String""
      },
      {
        ""Name"": ""CreatedDate"",
        ""Type"": ""DateTime""
      },
      {
        ""Name"": ""CompletedDate"",
        ""Type"": ""Nullable`1""
      }
    ]
  }
]";
}