using Microsoft.AspNetCore.Mvc;
using MyXProject.Services;
using MyXProject.Web.Utils;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;

namespace MyXProject.Web.Controllers;

// [Authorize]
[ApiController]
[Route("[controller]/[action]")]
public class DataController : BaseController
{
    private readonly IDataService _dataService;
    public DataController(IDataService dataService)
    {
        _dataService = dataService;
    }

    [HttpPost]
    public async Task<IActionResult> Permissions(PermissionsInput permissionsInput)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Permissions(permissionsInput, userId);
        });
    }
    

    [HttpPost]
    public async Task<IActionResult> Metadata(MetadataInput metadataInput)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Metadata(metadataInput, userId);
        });
    }

    [HttpPost]
    public async Task<IActionResult> Read([FromBody] ReadInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            var response = await _dataService.Read(userId, input);
            return new Response<object?>()
            {
                Succeeded = response.Succeeded,
                FriendlyMessage = response.FriendlyMessage,
                LogMessage = response.LogMessage,
                Data = response.Data
            };
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BatchInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Create(userId, input);
        });
    }

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] BatchInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Update(userId, input);
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] BatchInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Delete(userId, input);
        });
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] BatchInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Upsert(userId, input);
        });
    }
    
    [HttpPost]
    public async Task<IActionResult> Bulk([FromBody] BulkInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Bulk(userId, input);
        });
    }

    [HttpPost]
    public async Task<IActionResult> Action([FromBody] ActionInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Action(userId, input, HttpContext);
        });
    }

    [HttpPost]
    public async Task<IActionResult> File([FromForm] FileInput input)
    {
        return await ExecuteAsync(async () =>
        {
            Guid userId = GetUserId();
            return await _dataService.Action(userId, input, HttpContext);
        });
    }
}