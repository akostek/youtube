using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YtApi.Services;

namespace YtApi.Controllers;

[ApiController, Route("api/dashboard"), Authorize]
public class DashboardController(DashboardService svc) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(await svc.GetStats());

    [HttpGet("active-jobs")]
    public async Task<IActionResult> ActiveJobs() => Ok(await svc.GetActiveJobs());

    [HttpGet("logs")]
    public async Task<IActionResult> Logs() => Ok(await svc.GetLogs());
}
