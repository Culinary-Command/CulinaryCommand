# Senior Design Website

Production site:
- [https://sdmay26-44.sd.ece.iastate.edu](https://sdmay26-44.sd.ece.iastate.edu)

Testing application:
- [http://culinary-command.com/](http://culinary-command.com/)
- `http://3.20.198.36/`

## Playwright Tests

The Playwright UI test project lives in [PlaywrightTests](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests).

Current admin coverage summary:
- Dashboard access, Cognito login redirect, and navigation
- Manage users create, invite verification, edit, cancel, and validation flows
- Recipe and ingredient create/edit/filter/delete flows
- Inventory management lifecycle, search, and delete-cancel flows
- Location management create, edit, configure, and delete flows from Settings
- Task management manual assignment, template quick-assign, and task-list assignment flows

Full coverage summary:
- See [TEST_COVERAGE.md](/Users/wyatthunter/projects/sdmay26-44/TEST_COVERAGE.md)

## Playwright Requirements

To run the Playwright xUnit suite locally, you need:
- .NET 9 SDK
- The app running locally at `http://localhost:5256`
- A valid admin login available through the local app auth flow
- `PLAYWRIGHT_ADMIN_EMAIL` and `PLAYWRIGHT_ADMIN_PASSWORD` environment variables set
- Playwright browser binaries installed for the .NET test project

Admin tests use stored auth state:
- `authState.admin.json`
- This file is created automatically by the admin auth fixture if it does not already exist

Important behavior:
- If you change Razor pages or Blazor components that the tests exercise, restart the local app before rerunning tests so Playwright hits the updated build
- The most reliable place to run test commands from is the `PlaywrightTests` directory
- For screenshots or demos, use `PHOTO_PAUSE=1 PWDEBUG=1 HEADED=1` so Playwright opens a visible browser and pauses at built-in screenshot points

## Playwright Setup

From the repo root:

```bash
cd PlaywrightTests
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install
```

Set credentials before running admin-authenticated tests:

```bash
export PLAYWRIGHT_ADMIN_EMAIL="your-admin-email"
export PLAYWRIGHT_ADMIN_PASSWORD="your-admin-password"
```

## Useful Test Commands

Start the local app first, from the repo root:

```bash
dotnet run --project CulinaryCommandApp/CulinaryCommand.csproj --urls http://localhost:5256
```

Run the full manage-users coverage:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~ManageUsers"
```

Run the full recipe coverage:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~RecipeTests"
```

Run the inventory management coverage:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~InventoryManagementTests"
```

Run the location management coverage:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~LocationManagementTests"
```

Run the task management coverage:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~TaskManagementTests"
```

Run the recipe lifecycle test only:

```bash
cd PlaywrightTests
HEADED=0 dotnet test --filter "FullyQualifiedName~Admin_Recipe_Lifecycle_Create_Edit_ProduceValidation_Delete"
```

Run headed for debugging:

```bash
cd PlaywrightTests
PWDEBUG=1 HEADED=1 dotnet test --filter "FullyQualifiedName~ManageUsers"
```

## Playwright Screenshot / Demo Mode

Some tests include optional photo pauses for demos. These pauses are skipped during normal test runs and only activate when `PHOTO_PAUSE=1` is set.

Run the main user-management lifecycle with screenshot pauses:

```bash
cd PlaywrightTests
PHOTO_PAUSE=1 PWDEBUG=1 HEADED=1 dotnet test --filter "FullyQualifiedName~Admin_User_Lifecycle_Create_VerifyInvite_Delete"
```

This pauses at:
- Filled Add User form before sending the invite
- New invited user visible in the Users list
- Expanded invited user row showing the setup link
- Invited user's Activate Your Account page

Run all user-management tests with screenshot pauses:

```bash
cd PlaywrightTests
PHOTO_PAUSE=1 PWDEBUG=1 HEADED=1 dotnet test --filter "FullyQualifiedName~ManageUsers"
```

Run the recipe lifecycle with screenshot pauses:

```bash
cd PlaywrightTests
PHOTO_PAUSE=1 PWDEBUG=1 HEADED=1 dotnet test --filter "FullyQualifiedName~Admin_Recipe_Lifecycle_Create_Edit_ProduceValidation_Delete"
```

This pauses at:
- Recipe created and visible in the recipe list
- Edited recipe visible in the recipe list
- Recipe detail page before opening the Produce modal
- Produce Recipe modal open
- Produce modal showing zero-serving validation

Run the location management lifecycle with screenshot pauses:

```bash
cd PlaywrightTests
PHOTO_PAUSE=1 PWDEBUG=1 HEADED=1 dotnet test --filter "FullyQualifiedName~Admin_LocationManagement_Lifecycle_Create_Edit_Configure_Delete"
```

When Playwright pauses:
- Take the screenshot or photo you need
- Click `Resume` in the Playwright Inspector to continue

## Current Local Test Status

Recent headed local run against `http://localhost:5256`:
- `ManageUsers` filter: 5 passing, 0 failing
- `RecipeTests` filter: 2 passing, 0 failing
- Full Playwright project: 13 passing, 9 failing

Known current failures:
- Old login/signup tests in `PlaywrightTests/OldTests` time out against `http://3.20.198.36/signin`
- `InventoryManagementTests` currently expect `/inventory-management`, but the app lands on `/inventory-catalog`

For class screenshots, the most reliable passing demos are:
- `Admin_User_Lifecycle_Create_VerifyInvite_Delete`
- `Admin_Edit_User_Name_And_Save`
- `Admin_Recipe_Lifecycle_Create_Edit_ProduceValidation_Delete`

## Current Admin Test Files

- [PlaywrightTests/Tests/ActualTests/AdminTests/DashboardTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/DashboardTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/ManageUsersTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/ManageUsersTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/ManageUsers_EditTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/ManageUsers_EditTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/RecipeTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/RecipeTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/InventoryManagementTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/InventoryManagementTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/LocationManagementTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/LocationManagementTests.cs)
- [PlaywrightTests/Tests/ActualTests/AdminTests/TaskManagementTests.cs](/Users/wyatthunter/projects/sdmay26-44/PlaywrightTests/Tests/ActualTests/AdminTests/TaskManagementTests.cs)
