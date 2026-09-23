# ADR 0013: Static discovery engine

Context: Repository configuration can describe services without executing untrusted code.

Decision: Parsers implement `IDiscoveryParser` and return normalized candidates and warnings. Docker Compose uses YamlDotNet with limits and no aliases.

Consequences: Parsing is independently testable and has a clear trust boundary. Discoveries describe configuration and cannot prove runtime behavior.
