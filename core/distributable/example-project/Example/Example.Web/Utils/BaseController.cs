using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Example.Data;
using Xams.Core.Dtos;

namespace Example.Web.Utils;

using Controller = Microsoft.AspNetCore.Mvc.Controller;

public class BaseController : Controller
{
    public DataContext db;
    public ILogger logger;

    public BaseController(ILogger _logger = null)
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
    public async Task<IActionResult> ExecuteDevOnly(Func<Task<IActionResult>> input)
    {
        if (!(Config.Environment == "Development" || Config.Environment == "Local"))
            return BadRequest();
        try
        {
            return await input();
        }
        catch (Exception e)
        {
            // await LogError(e);
            return BadRequest(e.Message);
        }
    }


    [ApiExplorerSettings(IgnoreApi = true)]
    public Guid GetUserId()
    {
        try
        {
            string userId = Request.Headers["UserId"].ToString();
            return Guid.Parse(userId);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to get UserId: {e.Message}");
        }
    }
}