using ECommerce.API.Extensions;
using ECommerce.Business.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromServiceResult(ServiceResult result) =>
        result.Succeeded
            ? Ok(new { succeeded = true, message = result.Message })
            : StatusCode(400, new { succeeded = false, message = result.Message, errors = result.Errors });

    protected IActionResult FromServiceResult<T>(ServiceResult<T> result) =>
        result.Succeeded
            ? Ok(new { succeeded = true, data = result.Data, message = result.Message })
            : StatusCode(400, new { succeeded = false, message = result.Message, errors = result.Errors });

    protected int CurrentUserId => User.GetUserId();
}
