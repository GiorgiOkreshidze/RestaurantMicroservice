# generations

## Diagrams
![Sprint 3 Diagram](https://github.com/user-attachments/assets/9fc39610-2297-47d4-be6d-5dcfc5fde228)

![Sprint 4 Diagram](https://github.com/user-attachments/assets/67724a67-75bb-45fa-b221-c4011510b059)

![Sprint 5 Diagram](https://github.com/user-attachments/assets/ab053a10-6d68-4b98-9069-9fbfbf11fdca)


## Getting started

To make it easy for you to get started with GitLab, here's a list of recommended next steps.

Already a pro? Just edit this README.md and make it your own. Want to make it easy? [Use the template at the bottom](#editing-this-readme)!

## Add your files

- [ ] [Create](https://docs.gitlab.com/ee/user/project/repository/web_editor.html#create-a-file) or [upload](https://docs.gitlab.com/ee/user/project/repository/web_editor.html#upload-a-file) files
- [ ] [Add files using the command line](https://docs.gitlab.com/ee/gitlab-basics/add-file.html#add-a-file-using-the-command-line) or push an existing Git repository with the following command:

```
cd existing_repo
git remote add origin https://git.epam.com/epm-edai/project-runs/run-7/team-2/generations.git
git branch -M main
git push -uf origin main
```

## Integrate with your tools

- [ ] [Set up project integrations](https://git.epam.com/epm-edai/project-runs/run-7/team-2/generations/-/settings/integrations)

## Collaborate with your team

- [ ] [Invite team members and collaborators](https://docs.gitlab.com/ee/user/project/members/)
- [ ] [Create a new merge request](https://docs.gitlab.com/ee/user/project/merge_requests/creating_merge_requests.html)
- [ ] [Automatically close issues from merge requests](https://docs.gitlab.com/ee/user/project/issues/managing_issues.html#closing-issues-automatically)
- [ ] [Enable merge request approvals](https://docs.gitlab.com/ee/user/project/merge_requests/approvals/)
- [ ] [Set auto-merge](https://docs.gitlab.com/ee/user/project/merge_requests/merge_when_pipeline_succeeds.html)

## Test and Deploy

Use the built-in continuous integration in GitLab.

- [ ] [Get started with GitLab CI/CD](https://docs.gitlab.com/ee/ci/quick_start/index.html)
- [ ] [Analyze your code for known vulnerabilities with Static Application Security Testing (SAST)](https://docs.gitlab.com/ee/user/application_security/sast/)
- [ ] [Deploy to Kubernetes, Amazon EC2, or Amazon ECS using Auto Deploy](https://docs.gitlab.com/ee/topics/autodevops/requirements.html)
- [ ] [Use pull-based deployments for improved Kubernetes management](https://docs.gitlab.com/ee/user/clusters/agent/)
- [ ] [Set up protected environments](https://docs.gitlab.com/ee/ci/environments/protected_environments.html)

***

# Editing this README

When you're ready to make this README your own, just edit this file and use the handy template below (or feel free to structure it however you want - this is just a starting point!). Thanks to [makeareadme.com](https://www.makeareadme.com/) for this template.

## Suggestions for a good README

Every project is different, so consider which of these sections apply to yours. The sections used in the template are suggestions for most open source projects. Also keep in mind that while a README can be too long and detailed, too long is better than too short. If you think your README is too long, consider utilizing another form of documentation rather than cutting out information.

## Name
Choose a self-explaining name for your project.

## Description
Let people know what your project can do specifically. Provide context and add a link to any reference visitors might be unfamiliar with. A list of Features or a Background subsection can also be added here. If there are alternatives to your project, this is a good place to list differentiating factors.

## Badges
On some READMEs, you may see small images that convey metadata, such as whether or not all the tests are passing for the project. You can use Shields to add some to your README. Many services also have instructions for adding a badge.

## Visuals
Depending on what you are making, it can be a good idea to include screenshots or even a video (you'll frequently see GIFs rather than actual videos). Tools like ttygif can help, but check out Asciinema for a more sophisticated method.

## Installation
Within a particular ecosystem, there may be a common way of installing things, such as using Yarn, NuGet, or Homebrew. However, consider the possibility that whoever is reading your README is a novice and would like more guidance. Listing specific steps helps remove ambiguity and gets people to using your project as quickly as possible. If it only runs in a specific context like a particular programming language version or operating system or has dependencies that have to be installed manually, also add a Requirements subsection.

## Usage
Use examples liberally, and show the expected output if you can. It's helpful to have inline the smallest example of usage that you can demonstrate, while providing links to more sophisticated examples if they are too long to reasonably include in the README.

## Support
Tell people where they can go to for help. It can be any combination of an issue tracker, a chat room, an email address, etc.

## Roadmap
If you have ideas for releases in the future, it is a good idea to list them in the README.

## Contributing
State if you are open to contributions and what your requirements are for accepting them.

For people who want to make changes to your project, it's helpful to have some documentation on how to get started. Perhaps there is a script that they should run or some environment variables that they need to set. Make these steps explicit. These instructions could also be useful to your future self.

You can also document commands to lint the code or run tests. These steps help to ensure high code quality and reduce the likelihood that the changes inadvertently break something. Having instructions for running tests is especially helpful if it requires external setup, such as starting a Selenium server for testing in a browser.

## Authors and acknowledgment
Show your appreciation to those who have contributed to the project.

## License
For open source projects, say how it is licensed.

## Project status
If you have run out of energy or time for your project, put a note at the top of the README saying that development has slowed down or stopped completely. Someone may choose to fork your project or volunteer to step in as a maintainer or owner, allowing your project to keep going. You can also make an explicit request for maintainers.

# Restaurant API

## Overview

This is a production-ready ASP.NET Core 8 WebAPI application based on the provided OpenAPI specification. The application follows Clean Architecture principles and includes JWT authentication, role-based authorization, and integration with AWS DynamoDB.

## Solution Structure

The solution is divided into the following projects:

- **Restaurant.API**: The API layer containing controllers, filters, and middleware.
- **Restaurant.Application**: The application layer containing services, DTOs, and Automapper profiles.
- **Restaurant.Domain**: The domain layer containing entities, value objects, and domain events.
- **Restaurant.Infrastructure**: The infrastructure layer containing repositories and external services.

## Getting Started

### Prerequisites

- .NET 8 SDK
- AWS DynamoDB Local (for development)

### Setup

1. Clone the repository:

```sh
git clone https://your-repo-url.git
cd Restaurant
```

2. Build the solution:

```sh
dotnet build
```

3. Update the `appsettings.json` file in the `Restaurant.API` project with your configuration values.

4. Run the application:

```sh
dotnet run --project Restaurant.API
```

### Testing

Run the unit tests using the following command:

```sh
dotnet test
```

## Architecture

The application is designed following the principles of Clean Architecture, ensuring separation of concerns and maintainability. The main layers are:

- **API Layer**: Handles HTTP requests and responses.
- **Application Layer**: Contains business logic and application services.
- **Domain Layer**: Contains the core domain entities and logic.
- **Infrastructure Layer**: Handles data access and external service integrations.

## Security

- JWT-based authentication with refresh tokens.
- Role-based authorization with policy-based permissions.
- Secure password hashing with ASP.NET Core Identity.

## Data Persistence

- AWS DynamoDB integration using the latest SDK.
- Repository pattern for data access.

## API Documentation

Swagger is used for API documentation. You can access the Swagger UI at `/swagger` once the application is running.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the MIT License.
