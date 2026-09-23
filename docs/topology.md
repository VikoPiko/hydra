# Topology and evidence

The topology endpoint still combines component metadata, environment runtime state, and deployed version as in Version 1. Version 3 adds component origin/discovery status and a list of evidence for each dependency. Selecting a graph edge opens its relationship type, declared/discovered/observed source, and evidence location. Compose evidence points to a repository path and YAML location; it is a configuration assertion, not proof of a runtime call.

Imported components are visually identified as discovered, with missing components retained and marked for review. Manual name/type corrections are stored as overrides and survive a rescan. Existing manually declared dependencies receive Manual evidence during migration. Selecting a component still opens runtime metrics, delivery history, related work, and impact controls.

An arrow `A → B` means A depends on B. Impact analysis reverses those arrows and describes direct and indirect components as *potentially affected*. The graph does not assert causation or guaranteed failure. See [discovery.md](discovery.md) for scanning and reconciliation behavior.
