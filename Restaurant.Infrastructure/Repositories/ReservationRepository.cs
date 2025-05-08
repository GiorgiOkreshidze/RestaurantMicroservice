using MongoDB.Driver;
using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IMongoCollection<Reservation> _collection;

    public ReservationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Reservation>("Reservations");
        
        var dateLocationIndexKeys = Builders<Reservation>.IndexKeys
            .Ascending(r => r.Date)
            .Ascending(r => r.LocationId);
        var dateLocationIndexOptions = new CreateIndexOptions
        {
            Name = "Date_LocationId_Index"
        };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Reservation>(dateLocationIndexKeys, dateLocationIndexOptions));

        var waiterIdIndexKeys = Builders<Reservation>.IndexKeys
            .Ascending(r => r.WaiterId);
        var waiterIdIndexOptions = new CreateIndexOptions
        {
            Name = "WaiterId_Index"
        };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Reservation>(waiterIdIndexKeys, waiterIdIndexOptions));
        
        var userEmailIndexKeys = Builders<Reservation>.IndexKeys
            .Ascending(r => r.UserEmail);
        var userEmailIndexOptions = new CreateIndexOptions
        {
            Name = "UserEmail_Index"
        };
        _collection.Indexes.CreateOneAsync(new CreateIndexModel<Reservation>(userEmailIndexKeys, userEmailIndexOptions));
    }


    public async Task<Reservation> UpsertReservationAsync(Reservation reservation)
    {
        var filter = Builders<Reservation>.Filter.Eq(r => r.Id, reservation.Id);
        var options = new ReplaceOptions { IsUpsert = true };

        await _collection.ReplaceOneAsync(filter, reservation, options);

        return reservation;
    }

    public async Task<bool> ReservationExistsAsync(string reservationId)
    {
        if (string.IsNullOrWhiteSpace(reservationId))
            return false;

        return await _collection.Find(r => r.Id == reservationId).AnyAsync();
    }

    public async Task<int> GetWaiterReservationCountAsync(string waiterId, string date)
    {
        var builder = Builders<Reservation>.Filter;

        var waiterIdFilter = builder.Eq(r => r.WaiterId, waiterId);
        var dateFilter = builder.Eq(r => r.Date, date);
        var combinedFilter = builder.And(dateFilter, waiterIdFilter);

        var count = await _collection.CountDocumentsAsync(combinedFilter);
        return (int)count;
    }

    public async Task<Reservation?> GetReservationByIdAsync(string reservationId)
    {
        return await _collection.Find(r => r.Id == reservationId).FirstOrDefaultAsync();
    }

    public async Task<List<Reservation>> GetReservationsByDateLocationTable(string date, string locationAddress,
        string tableId)
    {
        var builder = Builders<Reservation>.Filter;

        var dateFilter = builder.Eq(r => r.Date, date);
        var locationAddressFilter  = builder.Eq(r => r.LocationAddress, locationAddress);
        var tableIdFilter  = builder.Eq(r => r.TableId, tableId);
        var combinedFilter = builder.And(dateFilter, locationAddressFilter, tableIdFilter);

        var reservations = await _collection.Find(combinedFilter).ToListAsync();
        return reservations;
    }

    public async Task<IEnumerable<Reservation>> GetReservationsForDateAndLocation(string date, string locationId)
    {
        var builder = Builders<Reservation>.Filter;
        
        var dateFilter  = builder.Eq(r => r.Date, date);
        var locationFilter  = builder.Eq(r => r.LocationId, locationId);
        var notCanceledFilter  = builder.Ne(r => r.Status, ReservationStatus.Canceled.ToString());
        var combinedFilter = builder.And(dateFilter, locationFilter, notCanceledFilter);

        var reservations = await _collection.Find(combinedFilter).ToListAsync();
        return reservations;
    }

    public async Task<IEnumerable<Reservation>> GetCustomerReservationsAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Enumerable.Empty<Reservation>();
        }

        var filter = Builders<Reservation>.Filter.Eq(r => r.UserEmail, email);
        var reservations = await _collection.Find(filter).ToListAsync();

        return reservations;
    }

    public async Task<IEnumerable<Reservation>> GetWaiterReservationsAsync(ReservationsQueryParametersDto queryParams,
        string waiterId)
    {
        if (string.IsNullOrEmpty(waiterId))
        {
            return new List<Reservation>();
        }

        var builder = Builders<Reservation>.Filter;
        var filters = new List<FilterDefinition<Reservation>>
        {
            builder.Eq(r => r.WaiterId, waiterId)
        };

        if (!string.IsNullOrEmpty(queryParams.Date))
        {
            filters.Add(builder.Eq(r => r.Date, queryParams.Date));
        }

        if (!string.IsNullOrEmpty(queryParams.TimeFrom))
        {
            filters.Add(builder.Eq(r => r.TimeFrom, queryParams.TimeFrom));
        }

        if (!string.IsNullOrEmpty(queryParams.TableId))
        {
            filters.Add(builder.Eq(r => r.TableId, queryParams.TableId));
        }

        var combinedFilter = builder.And(filters);

        return await _collection.Find(combinedFilter).ToListAsync();
    }

    public async Task<Reservation> CancelReservationAsync(string reservationId)
    {
        var filter = Builders<Reservation>.Filter
            .Eq(r => r.Id, reservationId);
        var update = Builders<Reservation>.Update.Set(r => r.Status, ReservationStatus.Canceled.ToString());
        var options = new FindOneAndUpdateOptions<Reservation>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _collection.FindOneAndUpdateAsync(filter, update, options);
    }
}