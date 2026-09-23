# ADR 0019: Do not execute repository content

Context: Imported repositories are untrusted, and build scripts can execute arbitrary commands.

Decision: Scan only bounded configuration files as data. Never run build tools, package installation, hooks, scripts, or Docker builds during discovery.

Consequences: Static discovery is safer and repeatable. It cannot infer every runtime dependency; runtime observations remain a separate evidence source.
