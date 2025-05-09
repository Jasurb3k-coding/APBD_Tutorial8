using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public TripsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    /// <summary>
    /// GET /api/trips
    /// Retrieves all trips along with their associated countries.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllTrips()
    {
        var trips = await _tripsService.GetTrips();
        return Ok(trips);
    }
}