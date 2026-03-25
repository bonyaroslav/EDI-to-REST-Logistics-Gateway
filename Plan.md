## Technical Implementation Plan: EDI-to-REST-Logistics-Gateway

### Summary

Build and finish a portfolio-grade `.NET 8` API that accepts raw `text/plain` ASC X12 `204` payloads and translates them into a stable JSON contract through one synchronous end-to-end slice.

The implementation is intentionally narrow:

- one transaction type: `ASC X12 204`
- one endpoint: `POST /api/v1/edi/translate-204`
- one success DTO: `LoadTenderResponse`
- one handled error shape: `error`, `message`, `status`
- parser isolation in Infrastructure
- explicit manual mapping in Application

### Delivered Phases

#### Phase 1: Repository And Contract Alignment

- Solution split into `Domain`, `Application`, `Infrastructure`, and `API`
- Public response contract kept intentionally small and believable
- Test projects added for application, infrastructure, and API behavior

#### Phase 2: Domain And Application Contracts

- Parser abstraction defined in `Domain`
- Canonical parsed models isolated from `Indice.Edi`
- Public response DTOs defined in `Application`

#### Phase 3: Infrastructure Parsing

- `Indice.Edi` dependency limited to `Infrastructure`
- Minimal `204` segment support implemented for the demo slice
- Structural parser errors converted into predictable domain validation exceptions

#### Phase 4: Translation And Validation

- Manual mapping from parsed models to public DTOs implemented
- Supported set purpose codes mapped explicitly
- Pickup and delivery stop validation enforced
- Estimated delivery date normalized to UTC-midnight ISO-8601 output

#### Phase 5: API And Error Handling

- Single Minimal API endpoint implemented for `204` translation
- `text/plain` enforced as the only supported request content type
- Stable error contract used for `400`, `415`, and `500`
- Unsupported transaction sets treated as invalid input and returned as `400`
- OpenAPI metadata exposed in development and aligned with the real response set

#### Phase 6: Demo Assets, Tests, And Documentation

- Shared sample payloads added under `samples/204`
- README updated to reflect the actual completed implementation
- Tests expanded to cover:
  - valid translation
  - malformed payloads
  - missing mandatory structure
  - unsupported transaction set
  - missing pickup or delivery stop
  - unsupported media type
  - blank payload
  - unexpected internal failure path

### Current Validation Rules

- reject blank request bodies
- reject non-`text/plain` requests
- reject malformed or non-X12 payloads
- reject non-`204` transaction sets
- reject missing or malformed `GS`, `ST`, `B2`, or `B2A`
- reject unsupported v1 set purpose codes
- require at least one pickup stop and one delivery stop

### Test Plan

- `dotnet test Logistics.EDI.Gateway.sln` is the completion gate
- application tests verify mapping and business validation without parser-specific types
- infrastructure tests verify parser normalization and structural validation
- integration tests verify real HTTP behavior, content negotiation, and structured failure responses

### Assumptions And Non-Goals

- This project is a portfolio demo, not a production integration platform
- The repo intentionally stops at the `204 -> JSON` boundary
- `990`, `214`, partner-specific rules, persistence, brokers, and async ingestion remain out of scope
- If realism conflicts with simplicity, prefer the smallest change that preserves a believable logistics use case
