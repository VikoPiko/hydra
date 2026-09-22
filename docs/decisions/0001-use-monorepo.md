# ADR 0001: Use a monorepo

## Context
A five-person team owns one MVP and release cadence.

## Decision
Keep frontend, backend, infrastructure, tests, and documentation in one Git repository.

## Consequences
Cross-layer changes and local integration are simpler; CI should run checks for both stacks.
