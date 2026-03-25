# AGENTS.md

## Intent
This file defines how the agent should work in this repository.
It does not define product requirements.

## Source of truth
Read before coding:
- README.md
- Plan*.md (if present)

Do not invent requirements beyond documented intent. State assumptions in the plan when needed.

## Workflow
For any non-trivial task:
1. Inspect the relevant code and docs.
2. Create or update a concise implementation plan.
3. Imlementation steps.
4. Verify before finishing.

## Coding approach
- Follow `.editorconfig`; it is the style authority.
- Prefer concise, readable, maintainable C#.
- Keep the solution simple: KISS and YAGNI first.
- Apply SOLID pragmatically, not mechanically.
- Prefer the smallest correct change.
- Avoid unnecessary abstractions, entities, layers, and packages.
- Prefer updating existing code over introducing new wrappers by default.
- If you're advicing architectural decision then advice alternatives with tradeoffs.

## Testing
- Prefer TDD where practical.
- Add or update tests for behavior changes and bug fixes.
- Prefer fast unit tests for logic.
- Use focused integration tests only for important infrastructure scenarios.

## Guardrails
- Do not invent product behavior.
- Do not perform broad renames or restructuring unless required.
- Do not leave dead code, commented-out code, or temporary hacks.
- Keep files and APIs as small as practical.

## Done means
- Build passes.
- Relevant tests pass.
- Docs or plan are updated only when necessary.