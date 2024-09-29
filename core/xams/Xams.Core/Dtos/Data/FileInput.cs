using Microsoft.AspNetCore.Http;

namespace Xams.Core.Dtos.Data
{
    public class FileInput : ActionInput
    {
        public IFormFile file { get; set; }
        
    }
}