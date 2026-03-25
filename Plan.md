## Technical Implementation Plan: EDI-to-REST-Logistics-Gateway

### Summary

Build a focused portfolio demo of an ASP.NET Core `.NET 8` API that accepts raw `text/plain` ASC X12 204 payloads and returns a clean JSON contract. Keep the Clean Architecture story visible, but implement only one strong vertical slice with realistic logistics concerns.

Locked goals for v1:
- Support one transaction type only: `ASC X12 204`
- Expose one endpoint: `POST /api/v1/edi/translate-204`
- Keep synchronous request/response behavior
- Use `Indice.Edi` only in `Infrastructure`
- Use explicit manual mapping only
- Position the project as a portfolio demo with production-minded practices, not production-ready software

### Phase 1: Repository And Contract Alignment

- Scaffold `src/Logistics.EDI.Domain`, `src/Logistics.EDI.Application`, `src/Logistics.EDI.Infrastructure`, `src/Logistics.EDI.API`
- Add `tests/` for unit and integration coverage
- Align naming, startup path, endpoint path, and sample usage across docs
- Keep the public response contract small but believable:
  - `transactionId`
  - `loadNumber`
  - `carrierAlphaCode`
  - `setPurpose`
  - `estimatedDeliveryDate`
  - `shipperName`
  - `stops`
  - `status`

### Phase 2: Domain And Application Contracts

- Put cross-layer abstractions and exceptions in `Domain`
- Define parser abstraction in `Domain` so `Application` depends only on contracts
- Define application-facing parsed models that are independent of `Indice.Edi`
- Define the public response DTO in `Application`
- Keep parser-specific types out of `Application`

### Phase 3: Infrastructure Parsing

- Add `Indice.Edi` only to `Infrastructure`
- Model the minimum viable 204 segments for the demo:
  - `ISA`
  - `GS`
  - `ST`
  - `B2`
  - `B2A`
  - `G62`
  - `N1`
  - `S5`
  - required trailer segments for parse completeness
- Parse raw EDI into infrastructure POCOs, then translate those into application-safe parsed models
- Do not expose infrastructure parser POCOs outside `Infrastructure`

### Phase 4: Translation And Validation

- Implement explicit manual mapping from parsed models to the public DTO
- Map `B2A` to `setPurpose`
- Map stop loop data into a minimal `stops` collection
- Derive `estimatedDeliveryDate` from the chosen demo scheduling field and document the rule clearly
- Perform explicit validation in `Application` so demo behavior is predictable and not coupled to parser exception wording

Validation rules for v1:
- reject missing or malformed `GS`, `ST`, `B2`, or `B2A`
- reject payloads that do not parse as 204
- reject invalid date/time values used for the API contract
- require at least one pickup stop and one delivery stop

### Phase 5: API And Error Handling

- Implement the single translation endpoint in `API`
- Accept `text/plain` only for v1
- Return `200` with the DTO on success
- Convert validation exceptions into a structured `400` response with consistent fields
- Keep the entrypoint simple and review-friendly

### Phase 6: Demo Assets, Tests, And Documentation

- Add realistic sample payloads under `samples/` or `docs/`
- Include one valid original tender and one invalid payload missing required structure
- Add unit tests for mapping and business validation
- Add one integration test for the HTTP endpoint with raw `text/plain` input
- Add a short README note showing awareness of the broader lifecycle: `204 -> 990 -> 214`, while keeping those transactions out of scope

### Test Plan

- Valid 204 with original tender returns `200` and populates all public fields including `setPurpose` and `stops`
- Missing `GS` returns `400`
- Missing `B2A` returns `400`
- Payload without both pickup and delivery stops returns `400`
- Invalid schedule date/time returns `400`
- Non-204 or malformed payload returns `400`
- Application tests prove mapping works without referencing infrastructure parser types

### Assumptions And Non-Goals

- v1 favors a believable demo over broad 204 coverage
- `stops` are the main real-world addition because they improve authenticity without expanding the scope too far
- Optional deep 204 details such as lading lines, equipment requirements, reference-number variants, and partner-specific qualifiers stay out of v1
- If timezone is absent in EDI input, date/time normalization will be documented and applied consistently
- Non-goals for v1:
  - queues and worker services
  - multiple EDI standards
  - `990` / `214` processing
  - partner-specific customization
  - full implementation-guide compliance
