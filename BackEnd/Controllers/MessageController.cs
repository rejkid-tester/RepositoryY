using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Responses;

namespace Backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : BaseApiController
    {
        public MessageController()
        {
        }

        [HttpGet]
        public IActionResult Get()
        {
            Console.WriteLine(UserID);
            return Ok(new MessageResponse
            {
                Message = "Hello World"
            });
        }
    }
}
