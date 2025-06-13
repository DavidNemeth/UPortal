# UPortal Application Architecture Overview

This document provides an overview of the UPortal application's architecture, focusing on its role as a central authentication portal and how it facilitates shared authentication for other applications within the same domain.

## Core Concept: Central Authentication Portal

UPortal is designed to act as a central hub for user authentication and access to various integrated applications. Instead of each application managing its own user database and authentication mechanism, UPortal handles this, providing a single point of login and user management.

## Shared Authentication via Cookie

The key mechanism for enabling shared authentication is a **secure, HTTP-only cookie** set at a common parent domain (e.g., `.my-app.local`).

1.  **User Login:**
    *   When a user navigates to UPortal (e.g., `dev.my-app.local`) and successfully authenticates, UPortal issues an authentication cookie.
    *   This cookie is scoped to the parent domain (e.g., `.my-app.local`), making it accessible to any subdomain of `my-app.local` (e.g., `app1.my-app.local`, `app2.my-app.local`).

2.  **Cookie Properties:**
    *   **HTTPOnly:** To prevent access via client-side scripts, mitigating XSS risks.
    *   **Secure:** Ensures the cookie is only transmitted over HTTPS.
    *   **SameSite:** Typically set to `Lax` or `Strict` to protect against CSRF attacks. The exact configuration might depend on the specific cross-site interaction needs.
    *   **Domain:** Scoped to the parent domain (e.g., `.my-app.local`).

## Consuming Shared Authentication in Other Applications

Other applications (e.g., `service1.my-app.local`, `dashboard.my-app.local`) hosted on subdomains of the main portal domain can leverage this shared authentication cookie.

1.  **Automatic Login (SSO-like experience):**
    *   When a user, already authenticated with UPortal, visits another application (e.g., `app1.my-app.local`), their browser automatically sends the shared authentication cookie with the request.
    *   The target application (`app1.my-app.local`) must be configured to recognize and validate this cookie. This typically involves:
        *   Shared cookie decryption keys or a common identity provider configuration if using protocols like OpenID Connect or SAML (though the current setup implies a custom cookie mechanism).
        *   An endpoint or middleware that can interpret the cookie and establish a local session for the user.

2.  **Accessing User Information:**
    *   Once the user's identity is verified via the shared cookie, the application can fetch more detailed user information.
    *   UPortal exposes secure API endpoints (e.g., `/api/UserInfo`) that other applications can call.
    *   These API calls must be authenticated, typically using the same shared cookie or a token derived from it. The API endpoint on UPortal validates the cookie/token and returns user details (like username, roles, permissions, etc.).

    ```
    [Browser]                                  [UPortal: dev.my-app.local]
    User logs in ----------------------------> Authenticates user
                                               Sets auth cookie for .my-app.local
                                               <---------------------------- Returns cookie

    [Browser]                                  [Other App: app1.my-app.local]
    User visits app1.my-app.local
    (sends .my-app.local cookie) ------------> Validates cookie
                                               (Optional: Calls UPortal API for user details)
                                               <---------------------------- Serves authenticated content
    ```

## Benefits

*   **Single Sign-On (SSO):** Users log in once to UPortal and gain access to all integrated applications without needing to re-authenticate.
*   **Centralized User Management:** User accounts, roles, and permissions are managed in one place (UPortal).
*   **Simplified Application Development:** Individual applications don't need to implement complex authentication logic. They can rely on UPortal.
*   **Improved Security:** Authentication logic is centralized, making it easier to maintain and secure. Consistent security policies can be applied.

## Considerations for Development

*   **Domain Structure:** All applications intended to share authentication must reside under the same parent domain for the cookie to be shared (e.g., `dev.my-app.local`, `app1.my-app.local`, `app2.my-app.local`).
*   **HTTPS:** Essential for protecting the authentication cookie in transit. All applications should enforce HTTPS.
*   **Cookie Configuration:** Careful configuration of cookie properties (Secure, HTTPOnly, SameSite, Domain, Path) is critical for security and functionality.
*   **API Security:** APIs exposed by UPortal for fetching user information must be secured and only accessible to authenticated and authorized applications/users.
*   **Logout:** Centralized logout is important. When a user logs out from UPortal, all sessions in other integrated applications should ideally be invalidated. This might involve UPortal notifying other applications or a mechanism for other apps to re-validate their sessions against UPortal.

This shared authentication model provides a robust and user-friendly way to manage access across a suite of related web applications.
