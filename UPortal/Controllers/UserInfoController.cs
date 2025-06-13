using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using UPortal.Services; // For IAppUserService, IMachineService, ILocationService
using UPortal.Dtos; // For AppUserDto, MachineDto, LocationDto
using System.Threading.Tasks; // For Task
using System.Security.Claims; // For ClaimsPrincipal and accessing user claims
using Microsoft.Extensions.Logging; // For ILogger
using System.Linq; // For .Where()
using System.Collections.Generic; // For List<T>
using System; // For Exception

namespace UPortal.Controllers
{
    /// <summary>
    /// Provides API endpoints for retrieving user-related information,
    /// including details about the current user, their associated machines, and available locations.
    /// All endpoints require user authentication.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensures all actions in this controller require authentication
    public class UserInfoController : ControllerBase
    {
        private readonly IAppUserService _appUserService;
        private readonly IMachineService _machineService;
        private readonly ILocationService _locationService;
        private readonly ILogger<UserInfoController> _logger;

        public UserInfoController(
            IAppUserService appUserService,
            IMachineService machineService,
            ILocationService locationService,
            ILogger<UserInfoController> logger)
        {
            _appUserService = appUserService;
            _machineService = machineService;
            _locationService = locationService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves information for the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="AppUserDto"/> containing the user's details.</returns>
        /// <response code="200">Returns the current user's information.</response>
        /// <response code="401">If the user is not authenticated or the Azure AD Object ID is not found in token claims.</response>
        /// <response code="404">If the user's details are not found in the system using the Azure AD Object ID.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var azureAdObjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

            if (string.IsNullOrEmpty(azureAdObjectId))
            {
                _logger.LogWarning("Azure AD Object ID not found in token claims.");
                return Unauthorized("Azure AD Object ID not found in token.");
            }

            _logger.LogInformation("Fetching user by Azure AD Object ID: {AzureAdObjectId}", azureAdObjectId);
            var userDto = await _appUserService.GetByAzureAdObjectIdAsync(azureAdObjectId);

            if (userDto == null)
            {
                _logger.LogWarning("User with Azure AD Object ID: {AzureAdObjectId} not found.", azureAdObjectId);
                return NotFound();
            }

            _logger.LogInformation("Successfully fetched user with Azure AD Object ID: {AzureAdObjectId}", azureAdObjectId);
            return Ok(userDto);
        }

        /// <summary>
        /// A simple endpoint to check if the controller is responsive.
        /// </summary>
        /// <returns>A string "Pong from UserInfoController".</returns>
        /// <response code="200">Indicates the controller is responsive.</response>
        // Placeholder for future methods
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong from UserInfoController");
        }

        /// <summary>
        /// Retrieves a list of machines.
        /// </summary>
        /// <remarks>
        /// If a <paramref name="userId"/> is provided, the list is filtered to machines associated with that user.
        /// Otherwise, all machines accessible to the current user (or all machines, depending on service implementation) are returned.
        /// The current implementation filters by `MachineDto.AppUserId` if `userId` is provided.
        /// </remarks>
        /// <param name="userId">Optional. The ID of the user for whom to filter machines.</param>
        /// <returns>A list of <see cref="MachineDto"/> objects.</returns>
        /// <response code="200">Returns a list of machines.</response>
        /// <response code="500">If an unexpected error occurs while fetching machines.</response>
        [HttpGet("machines")]
        public async Task<IActionResult> GetMachines([FromQuery] int? userId)
        {
            try
            {
                _logger.LogInformation("Attempting to fetch machines. UserId filter: {UserId}", userId.HasValue ? userId.Value.ToString() : "None");

                IEnumerable<MachineDto> machines;
                var allMachines = await _machineService.GetAllAsync(); // Assuming this returns Task<List<MachineDto>> or Task<IEnumerable<MachineDto>>

                if (allMachines == null)
                {
                    _logger.LogWarning("Machine service returned null for GetAllAsync.");
                    // Return an empty list or handle as an error, depending on expected behavior
                    machines = new List<MachineDto>();
                }
                else
                {
                    if (userId.HasValue)
                    {
                        _logger.LogInformation("Filtering machines for AppUserId: {AppUserId}", userId.Value);
                        // Ensure MachineDto has AppUserId. Based on the prompt, it does.
                        machines = allMachines.Where(m => m.AppUserId == userId.Value).ToList();
                        _logger.LogInformation("Found {MachineCount} machines after filtering for AppUserId: {AppUserId}", machines.Count(), userId.Value);
                    }
                    else
                    {
                        machines = allMachines.ToList();
                        _logger.LogInformation("Fetched {MachineCount} total machines.", machines.Count());
                    }
                }
                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching machines. UserId filter: {UserId}", userId.HasValue ? userId.Value.ToString() : "None");
                return StatusCode(500, "An internal server error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Retrieves a list of all available locations.
        /// </summary>
        /// <returns>A list of <see cref="LocationDto"/> objects.</returns>
        /// <response code="200">Returns a list of locations.</response>
        /// <response code="500">If an unexpected error occurs while fetching locations.</response>
        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                _logger.LogInformation("Attempting to fetch all locations.");

                IEnumerable<LocationDto> locations;
                var allLocations = await _locationService.GetAllAsync(); // Assuming this returns Task<List<LocationDto>> or Task<IEnumerable<LocationDto>>

                if (allLocations == null)
                {
                    _logger.LogWarning("Location service returned null for GetAllAsync.");
                    locations = new List<LocationDto>(); // Gracefully handle null by returning an empty list
                }
                else
                {
                    locations = allLocations.ToList();
                    _logger.LogInformation("Fetched {LocationCount} total locations.", locations.Count());
                }

                return Ok(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching locations.");
                return StatusCode(500, "An internal server error occurred while processing your request.");
            }
        }
    }
}
