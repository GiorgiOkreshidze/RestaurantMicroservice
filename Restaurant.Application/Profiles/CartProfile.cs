using AutoMapper;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles
{
    public class CartProfile : Profile
    {
        public CartProfile()
        {
            CreateMap<PreOrderItem, DishItemDto>()
                .ForMember(dest => dest.DishId, opt => opt.MapFrom(src => src.DishId))
                .ForMember(dest => dest.DishName, opt => opt.MapFrom(src => src.DishName))
                .ForMember(dest => dest.DishPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.DishQuantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.DishStatus, opt => opt.MapFrom(src => src.DishStatus))
                .ForMember(dest => dest.DishImageUrl, opt => opt.MapFrom(src => src.DishImageUrl));

            CreateMap<PreOrder, PreOrderDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ReservationId, opt => opt.MapFrom(src => src.ReservationId))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.TimeSlot, opt => opt.MapFrom(src => src.TimeSlot))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.ReservationDate, opt => opt.MapFrom(src => src.ReservationDate))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
                .ForMember(dest => dest.DishItems, opt => opt.MapFrom(src => src.Items));

            CreateMap<List<PreOrder>, CartDto>()
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.IsEmpty, opt =>
                    opt.MapFrom(src => src == null || src.Count == 0));
            
            CreateMap<List<PreOrderItem>, PreOrderDishesDto>()
                .ForMember(dest => dest.Dishes, opt => opt.MapFrom(src => src));
        }
    }
}
