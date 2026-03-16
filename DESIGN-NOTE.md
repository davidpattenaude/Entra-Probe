# Design Note

`EntraProbe` uses delegated Microsoft Graph access in the signed-in user's security context because the requirement is to read the current user's own profile through `/me`. That makes a public-client desktop flow the correct pattern. A client secret or certificate would move the design toward app-only access, which is the wrong fit for a small endpoint utility that is supposed to answer "who is the current interactive user, and what is their profile value?"

The tool is intentionally silent-only. MSAL silent token acquisition is attempted on every supported platform. On Windows the configuration prefers broker-backed authentication through WAM, and on macOS it uses the macOS broker with the unsigned-app redirect URI. If silent token acquisition is not possible, the tool fails with a distinct authentication exit code instead of opening a browser, showing an account picker, or waiting on user interaction. That behavior is deliberate because the primary consumers are wrappers, remediation scripts, and endpoint management platforms that need deterministic behavior.

Execution context is treated as a first-class input to the application. Running as `SYSTEM` on Windows, over SSH on macOS, in a headless environment, or outside a usable interactive user session is reported as unsupported because delegated user lookup is not reliable or appropriate there. The tool does not attempt impersonation, token theft, or reuse of tokens from other applications.

The remaining architecture choices follow the same operational bias:

- Manual composition in `Program.cs` keeps startup simple and avoids introducing a DI container for a small CLI.
- The Graph call requests only the properties the tool needs.
- Stdout is reserved for successful data output only; all diagnostics go to stderr.
- Exit codes are stable and explicit so wrappers can branch reliably.
