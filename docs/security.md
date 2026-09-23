# Security boundaries

OIDC, BFF session handling, bearer-token validation, and project membership follow [authentication.md](authentication.md). Repository connection creation, scans, application, and disconnect require the project Owner role. Read-only scan history and previews require project membership. Imported source text is untrusted and is never executed.

The current `LocalDevelopment` provider accepts only `demo://commerce-system`, mapped to a bundled read-only directory. The scanner rejects symlinks, ignores generated directories, validates paths against the configured root, and caps file size, count, YAML events, nesting, and service count. YAML aliases are rejected before constructing the document tree. The preview contains normalized names, types, paths, dependencies, and warnings, never complete file contents. Parser errors avoid echoing YAML values that could contain secrets.

Repository connection and scan responses contain no credentials. No Azure DevOps PAT or remote repository URL is accepted in this slice. Future provider adapters must validate URL schemes and destination policy, use bounded timeouts, and keep credentials behind a secret store before remote fetches are enabled. The current worker logs IDs, counts, and error types without source content or tokens.
