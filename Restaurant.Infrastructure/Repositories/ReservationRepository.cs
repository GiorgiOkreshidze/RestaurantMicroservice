using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class ReservationRepository(IDynamoDBContext context) : IReservationRepository
{
    public async Task<Reservation> UpsertReservationAsync(Reservation reservation)
    {
        var reservationId = reservation.Id;
        var existingReservation = await context.LoadAsync<Reservation>(reservationId);
        if (existingReservation != null)
        {
            existingReservation.CreatedAt = reservation.CreatedAt;
            existingReservation.Date = reservation.Date;
            existingReservation.GuestsNumber = reservation.GuestsNumber;
            existingReservation.LocationAddress = reservation.LocationAddress;
            existingReservation.LocationId = reservation.LocationId;
            existingReservation.PreOrder = reservation.PreOrder;
            existingReservation.Status = reservation.Status;
            existingReservation.TableNumber = reservation.TableNumber;
            existingReservation.TableId = reservation.TableId;
            existingReservation.TimeFrom = reservation.TimeFrom;
            existingReservation.TimeTo = reservation.TimeTo;
            existingReservation.TimeSlot = reservation.TimeSlot;
            existingReservation.UserInfo = reservation.UserInfo;
            existingReservation.WaiterId = reservation.WaiterId;
            existingReservation.UserEmail = reservation.UserEmail;
            existingReservation.ClientType = reservation.ClientType;
            existingReservation.TableCapacity = reservation.TableCapacity;

            await context.SaveAsync(existingReservation);
        }
        else
        {
            await context.SaveAsync(reservation);
        }
        
        return reservation;
    }

    public async Task<bool> ReservationExistsAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var reservation = await context.LoadAsync<Reservation>(id);
        return reservation is not null;
    }

    public async Task<int> GetWaiterReservationCountAsync(string waiterId, string date)
    {
        var scanCondition = new List<ScanCondition>()
        {
            new("WaiterId", ScanOperator.Equal, waiterId),
            new("Date", ScanOperator.Equal, date)
        };

        var reservations = await context.ScanAsync<Reservation>(scanCondition, new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();
        
        return reservations.Count;
    }

    public async Task<Reservation?> GetReservationByIdAsync(string id)
    {
        var reservation = await context.LoadAsync<Reservation>(id);
        return reservation ?? null;
    }

    public async Task<List<Reservation>> GetReservationsByDateLocationTable(string date, string locationAddress, string tableId)
    {
        var scanCondition = new List<ScanCondition>()
        {
            new("LocationAddress", ScanOperator.Equal, locationAddress),
            new("Date", ScanOperator.Equal, date),
            new("TableId", ScanOperator.Equal, tableId)
        };

        var reservations = await context.ScanAsync<Reservation>(scanCondition, new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();

        return reservations;
    }

    public async Task<IEnumerable<Reservation>> GetReservationsForDateAndLocation(string date, string locationId)
    {
        // Create scan conditions for date and locationId
        var scanConditions = new List<ScanCondition>
    {
        new ScanCondition("Date", ScanOperator.Equal, date),
        new ScanCondition("LocationId", ScanOperator.Equal, locationId),
        new ScanCondition("Status", ScanOperator.NotEqual, "Cancelled")
    };

        // Execute the scan operation
        var reservations = await context.ScanAsync<Reservation>(scanConditions, new DynamoDBOperationConfig
        {
            Conversion = DynamoDBEntryConversion.V2
        }).GetRemainingAsync();

        return reservations;
    }
}