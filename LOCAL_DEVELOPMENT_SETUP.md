# Local Development Setup Guide for UPortal

This guide provides instructions on how to set up the UPortal application for local development.

## Prerequisites

*   [.NET SDK](https://dotnet.microsoft.com/download) (version specified in `global.json` or project file)
*   [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)
*   A modern web browser (e.g., Chrome, Firefox, Edge)

## Setup Steps

1.  **Clone the Repository:**
    ```bash
    git clone <repository-url>
    cd <repository-directory>
    ```

2.  **Configure Host File:**
    For the application to work correctly with local services and for the shared authentication cookie to function across different local applications under the same parent domain, you need to map `dev.my-app.local` to your local machine.

    *   **Windows:**
        *   Open Notepad as Administrator.
        *   Open the file `C:\Windows\System32\drivers\etc\hosts`.
        *   Add the following line to the end of the file:
            ```
            127.0.0.1    dev.my-app.local
            ```
        *   Save the file and close Notepad.

    *   **macOS/Linux:**
        *   Open a terminal.
        *   Open the hosts file with a text editor using sudo:
            ```bash
            sudo nano /etc/hosts
            ```
        *   Add the following line to the end of the file:
            ```
            127.0.0.1    dev.my-app.local
            ```
        *   Save the file (Ctrl+O, then Enter in nano) and exit (Ctrl+X in nano).

3.  **Restore Dependencies:**
    Navigate to the `UPortal` project directory and restore the .NET dependencies:
    ```bash
    cd UPortal
    dotnet restore
    ```

4.  **Database Setup:**
    *   Ensure you have a local SQL Server instance or Docker container running.
    *   Update the `ConnectionStrings` in `UPortal/appsettings.Development.json` to point to your local database.
        ```json
        {
          "ConnectionStrings": {
            "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=UPortalDev;Trusted_Connection=True;MultipleActiveResultSets=true"
          }
        }
        ```
    *   Apply database migrations:
        ```bash
        dotnet ef database update
        ```
        *(If you don't have `dotnet-ef` installed, install it globally: `dotnet tool install --global dotnet-ef`)*

5.  **Run the Application:**
    You can run the application using either the .NET CLI or your IDE:
    *   **.NET CLI:**
        ```bash
        dotnet run
        ```
    *   **Visual Studio / VS Code:**
        Open the `UPortal.sln` file and run the `UPortal` project (usually by pressing F5 or the "Run" button).

6.  **Access the Application:**
    Open your web browser and navigate to `https://dev.my-app.local:<port_number>`, where `<port_number>` is the port configured for HTTPS in `UPortal/Properties/launchSettings.json`.

## Troubleshooting

*   **HTTPS Certificate:** On the first run, you might be prompted to trust the ASP.NET Core development certificate. Follow the instructions provided by your system or IDE. If you encounter issues, you can try running `dotnet dev-certs https --trust`.
*   **Port Conflicts:** If the default port is in use, you can change it in `UPortal/Properties/launchSettings.json`. Remember to update your browser URL accordingly.
