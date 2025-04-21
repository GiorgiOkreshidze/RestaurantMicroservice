using AutoMapper;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities;

namespace Restaurant.Application.Profiles;

public class FeedbackProfile : Profile
{
    public FeedbackProfile()
    {
        CreateMap<Feedback, FeedbackDto>().ReverseMap();
    }
}