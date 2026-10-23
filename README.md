# E-Mobility Charging Session Assessment

## Introduction
This assessment is set in the context of E-Mobility, focusing on the management of EV (Electric Vehicle) charging sessions.
You will build a simple API to handle charging sessions, simulating interactions between Charge Point Operators (CPOs) and end users.

Try to spend no more than **1 hour** on this assessment. The goal is to demonstrate and show off your coding skills, not to complete every feature.

## Objective
Take the provided ASP.NET Core project and develop a minimal RESTful API that allows:
- Submitting new charging sessions.
- Users to retrieve their own charging sessions.
- **Note:** The provided code contains some intentionally incomplete validation logic that is not well-written. Your task is to improve upon it.

## Requirements
1. **Receive Charging Sessions**
    - Implement an endpoint to accept new charging session data.
        - Sequential calls for the same session should be viewed and stored as updates.
    - Use in-memory storage (e.g., a static list) for simplicity. 
    - Each session should include: session ID, user ID, charge point ID, start time, and energy consumed (kWh).
    - Example JSON:
```json
 {
   "sessionId": "string",
   "userId": "string",
   "chargePointId": "string",
   "startTime": "2025-05-01T08:00:00Z",
   "endTime": "2025-05-02T08:00:00Z",
   "energyKwh": 00.0
 }
```

2. **Retrieve Charging Sessions for a User**
    - Implement an endpoint for users to retrieve all their charging sessions.
    - Charging sessions should be returned newest first.

## Bonus (Optional, if time allows)
If you have time, consider implementing one or more of the following features, or suggest your own improvements:
- Add error handling.
- Support filtering by date or other properties.
- Add caching.
- Implement unit tests.
- Use a database (e.g., SQLite or EF-core) instead of in-memory storage.
- Add authentication (e.g., JWT or static key) to secure the endpoints.
- Implement rate limiting to prevent abuse of the API.
- Add logging to track API usage and errors.

## Getting Started
- The project is already set up with ASP.NET Core and Swagger for API documentation.
- Add your models, controllers, and logic in the provided structure.
- To run the project:
  1. Restore dependencies: `dotnet restore`
  2. Build the project: `dotnet build`
  3. Run the project: `dotnet run`
  4. Access Swagger UI at `https://localhost:5001/swagger` (or the port shown in the console)

## Submission
- Ensure your code builds and runs.
- Submit your solution as a Git repository or a zip file.

---
Good luck! If you have any questions, feel free to ask.

## TLDR
A .NET 10 minimal API for charging sessions, built as Endpoints → Services → Repositories, with EF Core on the in-memory provider.
- `POST /api/users/{userId}/sessions` creates a session.
- `PUT /api/sessions` updates an existing session.
- `GET /api/users/{userId}/sessions?page=1&pageSize=20` returns a user's sessions newest first, paginated.
- Validation happens in two places: request models check that the input is well-formed, and the `ChargingSession` domain model enforces the business rules. Blacklisted users are rejected.
- A global exception handler returns ProblemDetails responses: 400 for bad input, 401 for blacklisted users, 422 for domain rule violations.
- Unit tests cover the request models, the domain model, and the service, and a smoke integration test covers the endpoints.
- I left out JWT auth and rate limiting. The decision log below explains why.

## Decision Log
- Updated to .NET 10 and updated the NuGet packages.
- Considered vertical feature slices, but with only one feature (charging sessions) a slice adds no value, so kept a layer-based structure (Controllers, Services, Repositories, Models).
- Replaced the controller with minimal API endpoints: personal preference and slightly faster. For more complex APIs I usually switch back to classic controllers for the extra control they offer through attributes.
- Split creating (POST) and updating (PUT) a session into separate endpoints instead of one endpoint that treats repeat submissions as updates. This is cleaner, separates concerns, and keeps each request model lighter.
- Added separate request and response models for the API, so only the necessary data goes in and out. This also makes validation easier, and the domain model (`ChargingSession`) stays internal.
- Decided not to build on the existing `SessionInspector`, and rewrote its logic as proper services that can be injected through DI.
- Split validation between the request models and the domain model. Request models only check that the input is complete and well-formed (for example, required fields are filled in), while the domain model enforces the business rules. I prefer to keep the business rules on the domain model itself so they can't be bypassed. For these rules I assumed sessions can be scheduled in the future, so a future start time is allowed. I could be wrong about that.
- Gave the domain model private setters (personal preference). State can only change through its own methods, so its validation is always enforced and can't be bypassed.
- Built the validation red/green (test-first): AI wrote the failing (red) tests, and I wrote the implementation to make them pass.
- Added the service and repository boilerplate, again with AI tests first and the implementation after. The service layer handles validation and auth, and the repository is only responsible for database access. This is a preference of mine: mixing EF logic directly into service logic can get messy, and over time the same EF code ends up duplicated in many places.
- Quickly set up EF Core with the in-memory provider. It's easy to test with and mimics a real database, to a degree.
- Added offset pagination (`page` and `pageSize`) to the GET endpoint, because a user's session history only grows, and returning all of it in one response doesn't scale. Defaults are page 1 with 20 items, and the page size is capped at 100 so a client can't request everything at once. Invalid values return a 400. The repository applies `Skip`/`Take` after ordering newest first, so the paging happens in the database instead of in memory.
- Added a user repository toan implement the blacklist, but didn't go as far as creating real users. For now it checks a static list of blacklisted user IDs.
- Went with a global exception handler plus a custom `DomainValidationException`, and declared on each endpoint which responses it can return. I like the custom exception because it gives me control over how the global exception handler treats it. In this case it maps to a 422 Unprocessable Entity, a status code I hadn't used before and found while going through `StatusCodes`.
- Updated the `ChargingSession` entity configuration: every property is required except `EndTime`, and the IDs have a max length of 36. I didn't base that length on anything specific. The database constraints act as a final check before storing, a last line of defense against corrupt data, because it's better to have multiple safeguards. I haven't decided yet how to handle it when EF throws on one of these constraints, and I'm not sure EF can tell us directly which constraint was triggered, but at least no corrupt data gets stored.
- Wanted to add two more things: JWT auth and integration tests. From this point on I used AI more, because my hands were getting tired.
- Writing the smoke integration test uncovered a bug: calling GET without pagination params threw an exception. With `[AsParameters]`, minimal APIs treat non-nullable `int` properties as required and ignore their property initializer defaults. Fixed by giving `PaginationParams` a primary constructor with default parameter values, which minimal APIs do respect.
- Decided to stop here and not implement JWT auth or rate limiting. Both are fairly simple and built into .NET, so they're mostly boilerplate. Along with JWT, I would have added a small middleware that runs after authentication and sets an injectable `ISession` holding the logged-in user's `UserId`. The service layer could then use it to validate requests against the logged-in user.
