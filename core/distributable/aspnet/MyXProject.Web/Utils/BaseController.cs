using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Xams.Core.Dtos;

namespace MyXProject.Web.Utils;

public class BaseController : Controller
{
    public ILogger? logger;

    public BaseController(ILogger? _logger = null)
    {
        logger = _logger;
    }
    
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ExecuteAsync(Func<Task<Response<object?>>> input)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var response = await input();
            
            ApiResponse apiResponse;
            if (response.ResponseType == ResponseType.Json)
            {
                apiResponse = new ApiResponse()
                {
                    succeeded = response.Succeeded,
                    friendlyMessage = response.FriendlyMessage,
                    logMessage = response.LogMessage,
                    data = response.Data
                };
                if (apiResponse.succeeded)
                {
                    return Json(apiResponse,  new JsonSerializerOptions()
                    {
                        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                    });
                }

                return BadRequest(apiResponse);
            }

            if (response.ResponseType == ResponseType.File)
            {
                if (response.Data is FileData)
                {
                    var fileData = (FileData)response.Data;
                    if (response.Data != null)
                    {
                        return File(fileData.Stream, fileData.ContentType, fileData.FileName);
                    }
                }
            }

            apiResponse = new ApiResponse()
            {
                succeeded = response.Succeeded,
                friendlyMessage = response.FriendlyMessage,
                logMessage = response.LogMessage,
                data = response.Data
            };
            if (apiResponse.succeeded)
            {
                return Json(apiResponse);
            }

            return BadRequest(apiResponse);
        }
        catch (Exception e)
        {
            return BadRequest(new ApiResponse()
            {
                succeeded = false,
                friendlyMessage = e.Message,
                logMessage = e.InnerException?.Message
            });
        }
    }
    
    [ApiExplorerSettings(IgnoreApi = true)]
    public Guid GetUserId()
    {
        try
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                string userId = Request.Headers["UserId"].ToString();
                return Guid.Parse(userId);
            }
            
            /*
             * This is where you would implement your own logic to retrieve the UserId.
             * You would typically retrieve the UserId from claims or a token and create the User
             * in the database if it doesn't exist.
             */
            throw new Exception($"Retrieving UserId has not been implemented in this environment.");
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get UserId: {e.Message}");
        }
    }
}