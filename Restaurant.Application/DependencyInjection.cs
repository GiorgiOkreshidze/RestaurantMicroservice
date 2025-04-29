using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application.Factories;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;

namespace Restaurant.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IDishService, DishService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFeedbackFactory, FeedbackFactory>();
            services.AddScoped<IPreOrderService, PreOrderService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IReportingService, ReportingService>();

            return services;
        }
    }
}
