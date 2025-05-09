using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public ClientsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    /// <summary>
    /// GET /api/clients/{id}/trips
    /// Returns all trips that a given client has registered for, including registration/payment dates.
    /// </summary>
    [HttpGet("{id:int}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        try
        {
            var trips = await _tripsService.GetTripsForClient(id);
            return Ok(trips);
        }
        catch (ClientNotFoundException)
        {
            return NotFound("The user with ID was not found");
        }
    }

    /// <summary>
    /// POST /api/clients
    /// Creates a brand‐new client.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDto newClientDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newClientId = await _tripsService.CreateNewClient(newClientDto);
        
        return Created($"/api/clients/{newClientId}", new { IdClient = newClientId });

    }

    /// <summary>
    /// PUT /api/clients/{id}/trips/{tripId}
    /// Registers the client for the given trip, if there is space.
    /// </summary>
    [HttpPut("{clientId:int}/trips/{tripId:int}")]
    public IActionResult RegisterClientToTrip(int clientId, int tripId)
    {
            _tripsService.PutClientTrip(clientId, tripId);
        try
        {
            return NoContent();
        } catch (Exception e)
        {
            Console.WriteLine("BAD");
            return BadRequest(e.ToString());
        }
    }

    /// <summary>
    /// DELETE /api/clients/{id}/trips/{tripId}
    /// Removes an existing registration.
    /// </summary>
    [HttpDelete("{clientId:int}/trips/{tripId:int}")]
    public IActionResult UnregisterClientFromTrip(int clientId, int tripId)
    {
        try
        {
            _tripsService.DeleteClientTrip(clientId, tripId);
            return NoContent();
        }
        catch (Exception e)
        {
            return BadRequest(e.ToString());
        }

    }
}