using AutoMapper;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Domain.DTOs;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles;

public class ReservationProfile : Profile
{
    public ReservationProfile()
    {
        CreateMap<Reservation, ReservationDto>().ReverseMap();
        CreateMap<Reservation, ClientReservationResponse>().ReverseMap();

        CreateMap<Reservation, ReservationResponseDto>()
                        .ForMember(dest => dest.ClientType, opt => opt.MapFrom(src => src.ClientTypeString));
        CreateMap<ReservationsQueryParameters, ReservationsQueryParametersDto>();
    }
}