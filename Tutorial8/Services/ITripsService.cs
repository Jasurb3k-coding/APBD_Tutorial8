using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDto>> GetTrips();
    Task<List<ClientTripDto>> GetTripsForClient(int clientId);
    Task<int> CreateNewClient(ClientCreateDto newClientDto);
    Task<bool> PutClientTrip(int clientId, int tripId);
    Task<bool> DeleteClientTrip(int clientId, int tripId);
}