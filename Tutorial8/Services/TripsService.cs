using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString;

    public TripsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<List<TripDto>> GetTrips()
    {
        var trips = new Dictionary<int, TripDto>();

        const string sql = @"
SELECT
    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
    c.IdCountry, c.Name AS CountryName
FROM Trip AS t
LEFT JOIN Country_Trip AS ct ON t.IdTrip = ct.IdTrip
LEFT JOIN Country      AS c  ON ct.IdCountry = c.IdCountry;";
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);

        {
            await conn.OpenAsync();

            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                var id = rdr.GetInt32(rdr.GetOrdinal("IdTrip"));
                if (!trips.TryGetValue(id, out var dto))
                {
                    dto = new TripDto
                    {
                        IdTrip = id,
                        Name = rdr.GetString(rdr.GetOrdinal("Name")),
                        Description = rdr.GetString(rdr.GetOrdinal("Description")),
                        DateFrom = rdr.GetDateTime(rdr.GetOrdinal("DateFrom")),
                        DateTo = rdr.GetDateTime(rdr.GetOrdinal("DateTo")),
                        MaxPeople = rdr.GetInt32(rdr.GetOrdinal("MaxPeople"))
                    };
                    trips[id] = dto;
                }

                if (!rdr.IsDBNull(rdr.GetOrdinal("IdCountry")))
                {
                    dto.Countries.Add(new CountryDto
                    {
                        IdCountry = rdr.GetInt32(rdr.GetOrdinal("IdCountry")),
                        Name = rdr.GetString(rdr.GetOrdinal("CountryName"))
                    });
                }
            }
        }
        return trips.Values.ToList();
    }

    public async Task<List<ClientTripDto>> GetTripsForClient(int clientId)
    {
        using var conn = new SqlConnection(_connectionString);

        // Check if client exists
        const string checkClientSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @id";
        using (var cmd = new SqlCommand(checkClientSql, conn))
        {
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
            await conn.OpenAsync();
            var exists = (int)cmd.ExecuteScalar()! > 0;
            if (!exists)
                throw new ClientNotFoundException();
        }

        // 2) Retrieve their trips
        const string sql = @"
SELECT
    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
    ct.RegisteredAt, ct.PaymentDate
FROM Client_Trip AS ct
JOIN Trip         AS t  ON t.IdTrip = ct.IdTrip
WHERE ct.IdClient = @id
ORDER BY ct.RegisteredAt;";

        using var cmd2 = new SqlCommand(sql, conn);
        cmd2.Parameters.Add("@id", SqlDbType.Int).Value = clientId;

        var trips = new List<ClientTripDto>();
        using var rdr = await cmd2.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            trips.Add(new ClientTripDto
            {
                IdTrip = rdr.GetInt32(rdr.GetOrdinal("IdTrip")),
                Name = rdr.GetString(rdr.GetOrdinal("Name")),
                Description = rdr.GetString(rdr.GetOrdinal("Description")),
                DateFrom = rdr.GetDateTime(rdr.GetOrdinal("DateFrom")),
                DateTo = rdr.GetDateTime(rdr.GetOrdinal("DateTo")),
                MaxPeople = rdr.GetInt32(rdr.GetOrdinal("MaxPeople")),
                RegisteredAt = rdr.GetInt32(rdr.GetOrdinal("RegisteredAt")),
                PaymentDate = rdr.IsDBNull(rdr.GetOrdinal("PaymentDate"))
                    ? (int?)null
                    : rdr.GetInt32(rdr.GetOrdinal("PaymentDate"))
            });
        }

        return trips;
    }

    public async Task<int> CreateNewClient(ClientCreateDto newClientDto)
    {
        const string sql = @"
INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
VALUES (@fn, @ln, @email, @tel, @pesel);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@fn", SqlDbType.NVarChar, 120).Value = newClientDto.FirstName;
        cmd.Parameters.Add("@ln", SqlDbType.NVarChar, 120).Value = newClientDto.LastName;
        cmd.Parameters.Add("@email", SqlDbType.NVarChar, 120).Value = newClientDto.Email;
        cmd.Parameters.Add("@tel", SqlDbType.NVarChar, 120).Value = newClientDto.Telephone;
        cmd.Parameters.Add("@pesel", SqlDbType.NVarChar, 120).Value = newClientDto.Pesel;

        await conn.OpenAsync();
        var newId = (int)cmd.ExecuteScalar()!;
        return newId;
    }

    public async Task<bool> PutClientTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1) Does client exist?
        using (var c1 = new SqlCommand("SELECT COUNT(1) FROM Client WHERE IdClient=@id", conn))
        {
            c1.Parameters.Add("@id", SqlDbType.Int).Value = clientId;

            if ((int)(await c1.ExecuteScalarAsync()! ?? 0) == 0)
            {
                throw new ClientNotFoundException();
            }
        }


        // 2) Does trip exist & what is its MaxPeople?
        int maxPeople;
        using (var c2 = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip=@t", conn))
        {
            c2.Parameters.Add("@t", SqlDbType.Int).Value = tripId;
            var obj = await c2.ExecuteScalarAsync();
            if (obj == null)
                throw new TripNotFoundException();
            maxPeople = (int)obj;
        }


        // 3) Is it already full?
        using (var c3 = new SqlCommand(
                   "SELECT COUNT(1) FROM Client_Trip WHERE IdTrip=@t", conn))
        {
            c3.Parameters.Add("@t", SqlDbType.Int).Value = tripId;
            var count = (int)c3.ExecuteScalar()!;
            if (count >= maxPeople)
                throw new TripAlreadyFullException();
        }

        int today = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
        // 4) Finally insert registration (RegisteredAt = GETDATE())
        string ins = $"""
                      INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                      VALUES (@id, @t, {today});
                      """;


        using var c4 = new SqlCommand(ins, conn);
        c4.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
        c4.Parameters.Add("@t", SqlDbType.Int).Value = tripId;

        await c4.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteClientTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // 1) Does the registration exist?
        const string chk = @"
SELECT COUNT(1)
FROM Client_Trip
WHERE IdClient = @id AND IdTrip = @t;";

        using (var c1 = new SqlCommand(chk, conn))
        {
            c1.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
            c1.Parameters.Add("@t", SqlDbType.Int).Value = tripId;
            if ((int)c1.ExecuteScalar()! == 0)
            {
                Console.WriteLine("THROWINGGG");
                throw new ClientTripNotFoundException();
            }
        }

        // 2) Delete it
        using var c2 = new SqlCommand(
            "DELETE FROM Client_Trip WHERE IdClient=@id AND IdTrip=@t", conn);
        c2.Parameters.Add("@id", SqlDbType.Int).Value = clientId;
        c2.Parameters.Add("@t", SqlDbType.Int).Value = tripId;
        await c2.ExecuteNonQueryAsync();
        return true;
    }
}