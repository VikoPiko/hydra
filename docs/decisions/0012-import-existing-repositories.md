# ADR 0012: Import existing repositories

Context: Manually entering every component does not scale to an existing software system.

Decision: Start with a bundled, read-only repository source and an explicit scan-preview-apply flow. Project and scan records may exist before topology is accepted.

Consequences: The full discovery path can be demonstrated offline. Remote Git and Azure DevOps sources can be added behind the same scan boundary without changing project ownership.
