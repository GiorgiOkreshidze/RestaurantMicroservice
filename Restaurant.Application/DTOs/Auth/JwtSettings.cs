﻿namespace Restaurant.Application.DTOs.Auth
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; set; }

        public int RefreshTokenExpiryInDays { get; set; }
    }
}
