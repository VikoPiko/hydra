# ADR 0014: Dependency evidence model

Context: A dependency can be manual, found in configuration, or observed at runtime.

Decision: Add `DependencyEvidence` while retaining V2 `Dependency.Source` for compatibility. Backfill one evidence row per existing dependency and attach Compose source paths to new discoveries.

Consequences: The graph can explain each edge. Consolidating duplicate logical edges across source kinds remains a later migration and must preserve V2 drift queries.
