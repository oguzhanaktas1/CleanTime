# ADR-014: Employee simulation

- **Status:** Accepted
- **Architecture reference:** §35-36, §143

## Context

Employees work passively; they must not require simulated play.

## Decision

`EmployeeJobSimulator` resolves outcomes mathematically from skill, difficulty and a seed stored on the assignment (`IRandomSource` / `SeededRandomSource`), so results are reproducible. Assignments resolve when a game day ends.

## Consequences

Balance formulas (duration, variance, revenue share) are placeholders and live behind `IEmployeeJobSimulator` / `IEmployeeRevenueCalculator`.
