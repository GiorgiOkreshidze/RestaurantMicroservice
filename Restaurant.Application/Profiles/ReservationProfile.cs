using AutoMapper;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles;

public class ReservationProfile : Profile
{
    public ReservationProfile()
    {
        CreateMap<Reservation, ReservationDto>().ReverseMap();
        CreateMap<Reservation, ClientReservationResponse>().ReverseMap();
    }
}