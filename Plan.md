### 🛠️ Technical Implementation Plan: EDI-to-REST-Logistics-Gateway

#### **Phase 1: Solution Scaffolding & Domain Modeling (Hour 1-2)**
*The goal here is to establish the Clean Architecture skeleton and define the final JSON output (DTOs) that downstream systems will consume.*

**1. Scaffolding (Run these in your terminal):**
```bash
dotnet new sln -n Logistics.EDI.Gateway
dotnet new classlib -n Logistics.EDI.Domain
dotnet new classlib -n Logistics.EDI.Application
dotnet new classlib -n Logistics.EDI.Infrastructure
dotnet new web -n Logistics.EDI.Api # Using 'web' for Minimal APIs

# Add projects to solution
dotnet sln add **/*.csproj

# Set up project references
dotnet add Logistics.EDI.Application/Logistics.EDI.Application.csproj reference Logistics.EDI.Domain/Logistics.EDI.Domain.csproj
dotnet add Logistics.EDI.Infrastructure/Logistics.EDI.Infrastructure.csproj reference Logistics.EDI.Domain/Logistics.EDI.Domain.csproj
dotnet add Logistics.EDI.Api/Logistics.EDI.Api.csproj reference Logistics.EDI.Application/Logistics.EDI.Application.csproj Logistics.EDI.Infrastructure/Logistics.EDI.Infrastructure.csproj
```

**2. Domain Implementation (`Logistics.EDI.Domain`):**
* **Create JSON DTOs:** Create standard C# records/classes (`LoadTenderDto`, `StopDto`, `ReferenceNumberDto`) matching the exact JSON output from Scenario 1 in your README.
* **Create Interfaces:** Define `IEdiParserService` (takes a string/stream, returns an EDI POCO) to decouple the parser.
* **Create Exceptions:** Define `EdiValidationException` (crucial for Scenario 2).

#### **Phase 2: Infrastructure & Parsing Engine (Hour 3-5)**
*The goal is to map the cryptic X12 segments using the Indice.Edi library without letting this dependency leak into the rest of the application.*

**1. Setup Dependency:**
```bash
dotnet add Logistics.EDI.Infrastructure package Indice.Edi
```

**2. EDI POCO Modeling (`Logistics.EDI.Infrastructure/Models`):**
* Create a root class `X12_204_Document`.
* Use `[EdiSegment]` attributes to map the mandatory segments from the README's cURL command:
    * `ISA` (Interchange Control Header)
    * `GS` (Functional Group Header)
    * `ST` (Transaction Set Header)
    * `B2` (Beginning Segment for Highway Carrier)
    * `G62` (Date/Time)
    * `N1` (Name)
* *Crucial Step:* Mark the `GS` segment as `Mandatory = true` in the attribute so the parser inherently catches the missing segment for Scenario 2.

**3. Parser Implementation:**
* Implement `IEdiParserService`. Wrap the `Indice.Edi` deserialization logic in a `try/catch` block. If it fails due to a missing segment, catch the internal error and throw your custom `EdiValidationException` with the clean message shown in the README.

#### **Phase 3: Application Layer & Manual Mappers (Hour 6-7)**
*The goal is to prove you can write explicit, debuggable transformation logic without relying on AutoMapper.*

**1. The Mapper (`Logistics.EDI.Application/Mappers`):**
* Create a static class `LoadTenderMapper`.
* Write an extension method: `public static LoadTenderDto ToDto(this X12_204_Document ediDoc)`
* Manually map the fields. Example:
    ```csharp
    return new LoadTenderDto {
        TransactionId = ediDoc.TransactionHeader?.ControlNumber,
        LoadNumber = ediDoc.BeginningSegment?.ShipmentId,
        EstimatedDeliveryDate = ParseEdiDate(ediDoc.DateSegment?.Date, ediDoc.DateSegment?.Time),
        ShipperName = ediDoc.NameLoop?.FirstOrDefault(n => n.EntityIdentifier == "SH")?.Name
    };
    ```

**2. The Orchestrator (`Logistics.EDI.Application/Services`):**
* Create `LoadTenderIntegrationService`. It takes the raw string, calls `_parserService.Parse()`, and then calls `.ToDto()` on the result, returning the final JSON object.

#### **Phase 4: Gateway Minimal API & Error Handling (Hour 8)**
*The goal is to expose a blazing-fast Minimal API endpoint and configure the safety nets.*

**1. Exception Handler Middleware (`Logistics.EDI.Api/Middleware`):**
* Implement `IExceptionHandler` (new in .NET 8).
* Check if the exception is `EdiValidationException`. If so, format the exact JSON response from Scenario 2 in the README and set the status code to `400 Bad Request`.

**2. Program.cs & Minimal API Setup:**
* Register your services (`AddScoped<IEdiParserService, ...>`).
* Register the exception handler (`app.UseExceptionHandler()`).
* Create the endpoint exactly as defined in the README:
    ```csharp
    app.MapPost("/api/v1/edi/translate-204", async (HttpContext context, ILoadTenderIntegrationService service) => {
        using var reader = new StreamReader(context.Request.Body);
        var rawEdi = await reader.ReadToEndAsync();
        
        var result = service.ProcessLoadTender(rawEdi);
        return Results.Ok(result);
    }).Accepts<string>("text/plain");
    ```

#### **Phase 5: Validation & "Wow Factor" Polish (Hour 9)**
*The goal is to ensure the repository matches the README perfectly so the hiring manager's experience is flawless.*

1.  **Add Sample Files:** Create a `docs/` or `samples/` folder in the repo and add `valid_204.edi` and `broken_204.edi` files.
2.  **Test Scenario 1:** Run the app. Paste the exact "Happy Path" cURL command from your README into the terminal. Ensure it outputs the expected JSON.
3.  **Test Scenario 2:** Paste the exact "Reality Check" cURL command (missing the GS segment) into the terminal. Verify that the Minimal API does *not* throw a 500 error, but cleanly returns your customized 400 Bad Request JSON.