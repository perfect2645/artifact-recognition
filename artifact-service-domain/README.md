domain-service readme

# artifact-automation-domain-service

## Purpose

Provide business fcuntionality open api interfaces for artifact-automation proejct.

## Tech

- .net 10 webapi service

## HTTP error responses

HTTP API failures use `application/problem+json` with the actual HTTP status code.
Controller exceptions, middleware exceptions, automatic model validation, and empty
HTTP error responses use the same ProblemDetails contract:

```json
{
  "type": "about:blank",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected server error occurred. Please try again later.",
  "instance": "/api/recognition",
  "code": "INTERNAL_SERVER_ERROR",
  "traceId": "request-trace-id"
}
```

Validation failures return HTTP 400 with `code: VALIDATION_ERROR` and an additional
`errors` object containing messages by field. Common codes include `NOT_FOUND` (404),
`METHOD_NOT_ALLOWED` (405), and `UNSUPPORTED_MEDIA_TYPE` (415).

Production responses hide exception messages and stack traces. Development responses
include the root exception message in `detail` and the exception chain in `exception`.
Use `traceId` to correlate the response with server logs. Clients must check the HTTP
status instead of treating an error body as a successful recognition response.
Successful responses keep their existing format.

The implementation is shared through the referenced `CSharp/Laboratory/NetUtils`
project. Include that repository's error-handling changes when moving this fix to
another checkout or machine.

## Regression checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet build artifact-service-domain/artifact.service.domain.csproj
dotnet run --project artifact-service-domain.Tests/artifact.service.domain.Tests.csproj
```

The regression runner starts isolated HTTP servers on local ephemeral ports. It does
not need the Python worker or a running recognition service and adds no test-framework
packages. It checks development and production responses, failures after dispatch,
result-delivery failures, validation and malformed JSON, middleware exceptions,
404/405/415 responses, preserved headers, and successful-response compatibility.