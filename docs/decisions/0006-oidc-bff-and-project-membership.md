# ADR 0006: OIDC BFF and project membership

Status: Accepted

Use Keycloak for local OIDC and Auth.js in Next.js for authorization code with PKCE. Keep OAuth tokens in an encrypted HttpOnly cookie and send them to the API only from a server-side proxy. Validate bearer tokens at the ASP.NET boundary. Use issuer plus subject as the durable local identity key, and a project membership table for Owner, Contributor, and Viewer permissions.

This keeps identity credentials out of browser storage and separates external authentication from project authorization. The legacy Commerce Demo is claimable only while unowned. Imported non-demo V1 projects need an explicit ownership migration.
