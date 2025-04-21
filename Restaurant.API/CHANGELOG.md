# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.18.3] - 2025-04-16
### Fixed

- Added `TypeDate` property to feedback model.

## [0.18.2] - 2025-04-16
### Refactored

- Removed test endpoint for health check.

## [0.18.1] - 2025-04-16
### Fixed

- Reduced Cognitive Complexity in `AddFeedbackAsync` method.

## [0.18.0] - 2025-04-16
### Added

- Migrated feedback-related endpoints and `GET /users` to microservice

## [0.17.0] - 2025-04-16
### Added

- Implemented cancel reservation feature.

## [0.16.0] - 2025-04-16
### Added

- Implemented user profile retrieval functionality.

## [0.15.0] - 2025-04-16
### Added

- Added support for fetching reservations (`GET /reservations`) functionality.

## [0.14.0] - 2025-04-16
### Added

- Migrated `GET /locations/{id}/feedbacks` endpoint to microservice

## [0.13.0] - 2025-04-16
### Added

- Migrated `POST /reservations/by-waiter` endpoint to microservice

## [0.12.1] - 2025-04-16
### Fixed

- Fixed Sonar and CORS-related issues.

## [0.12.0] - 2025-04-16
### Added

- Implemented reservations table logic in microservice architecture.

## [0.11.0] - 2025-04-16
### Added

- Migrated `POST /reservations` endpoint to microservice

## [0.10.1] - 2025-04-16
### Fixed

- Fixed CORS issue.

## [0.10.0] -  2025-04-16
### Added

- Migrated `POST /signout` endpoint logic to microservice architecture.

## [0.9.1] - 2025-04-15
### Added

- Migrated `GET /dishes` endpoint logic
- Added support for new environment variables
- Implemented login and refresh token functionality
- Integrated Swagger XML documentation into controllers

## [0.8.1] - 2025-04-14
### Added

- Migrated `GET /dishes/{id}` endpoint to microservice architecture.
- Migrated `POST /sign-up` endpoint to microservice.
- Migrated `GET /dishes/popular` endpoint to microservice.
- Migrated location-related endpoints to microservice.
- Added Swagger documentation and global exception handling to location service.
- Configured AWS STS with automatic credential refresh mechanism.
- Added support for new environment variables configuration.
- Introduced a new environment value.

### Fixed

- Corrected misplaced environment variable values.