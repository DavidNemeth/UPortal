# UPortal API Documentation

This document provides details about the available API endpoints for the UPortal application.

## Authentication

All API endpoints require authentication. Clients should use Azure AD authentication, and the necessary cookies established by the main application will be used for authorizing API requests. If a request is made without proper authentication, the API will return an HTTP `401 Unauthorized` status.

## Endpoints

### UserInfo API (`/api/userinfo`)

This controller provides information about users, machines, and locations.

#### 1. Get Current User Information

*   **Endpoint:** `GET /api/userinfo/me`
*   **Purpose:** Retrieves detailed information about the currently authenticated user.
*   **Authentication:** Required.
*   **Parameters:** None.
*   **Responses:**
    *   `200 OK`: Successfully retrieved user information.
        ```json
        {
          "id": 1,
          "name": "Test User",
          "isAdmin": false,
          "isActive": true,
          "azureAdObjectId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
          "locationId": 10,
          "locationName": "Main Office"
        }
        ```
        *(Note: The structure matches `AppUserDto.cs`)*
    *   `401 Unauthorized`: If the user is not authenticated.
    *   `404 Not Found`: If the authenticated user's details cannot be found in the system.

#### 2. Get Machine Information

*   **Endpoint:** `GET /api/userinfo/machines`
*   **Purpose:** Retrieves a list of machines. Can be filtered by user.
*   **Authentication:** Required.
*   **Parameters:**
    *   `userId` (integer, optional): If provided, filters the machines to include only those assigned to the specified user ID.
*   **Responses:**
    *   `200 OK`: Successfully retrieved machine information.
        ```json
        [
          {
            "id": 101,
            "name": "Desktop-ABC",
            "locationName": "Main Office",
            "assignedUserName": "Test User",
            "locationId": 10,
            "appUserId": 1
          },
          {
            "id": 102,
            "name": "Laptop-XYZ",
            "locationName": "Remote Office",
            "assignedUserName": "Another User",
            "locationId": 20,
            "appUserId": 2
          }
        ]
        ```
        *(Note: The structure matches `MachineDto.cs`. The list can be empty if no machines meet the criteria.)*
    *   `401 Unauthorized`: If the user is not authenticated.

#### 3. Get Location Information

*   **Endpoint:** `GET /api/userinfo/locations`
*   **Purpose:** Retrieves a list of all configured locations.
*   **Authentication:** Required.
*   **Parameters:** None.
*   **Responses:**
    *   `200 OK`: Successfully retrieved location information.
        ```json
        [
          {
            "id": 10,
            "name": "Main Office",
            "userCount": 15,
            "machineCount": 25
          },
          {
            "id": 20,
            "name": "Remote Office",
            "userCount": 5,
            "machineCount": 8
          }
        ]
        ```
        *(Note: The structure matches `LocationDto.cs`. The list can be empty if no locations are configured.)*
    *   `401 Unauthorized`: If the user is not authenticated.

#### 4. Ping Test

*   **Endpoint:** `GET /api/userinfo/ping`
*   **Purpose:** A simple endpoint to check if the UserInfo API controller is responsive.
*   **Authentication:** Required.
*   **Parameters:** None.
*   **Responses:**
    *   `200 OK`: Controller is responsive. The response body will be a plain text string:
        ```text
        Pong from UserInfoController
        ```
    *   `401 Unauthorized`: If the user is not authenticated.
