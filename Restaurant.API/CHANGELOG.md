# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.7.1] - 2025-04-14
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