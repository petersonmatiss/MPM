# MPM.WEB Local Development Setup Guide

This guide provides step-by-step instructions for setting up and running the MPM.WEB application locally, including troubleshooting for common issues.

## Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**
- **Git** for source control

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/petersonmatiss/MPM.git
cd MPM
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the Web Application

```bash
cd src/Mpm.Web
dotnet run
```

The application will be available at:
- **HTTP**: `http://localhost:5277`
- **HTTPS**: `https://localhost:7277`

## Configuration

### Database Configuration

The application uses **in-memory database** by default in development mode, so no additional database setup is required for local testing.

### Environment Variables

No special environment variables are required for local development.

## MudBlazor Dialog Troubleshooting

### Issue: Dialogs Not Opening

**Symptoms:**
- Clicking "New Supplier", "Edit Profile", or other dialog buttons does nothing
- No error messages in the UI
- Button appears to activate but no dialog appears

**Root Cause:**
This is a .NET 8 Blazor Web App render mode issue. MudBlazor dialogs require JavaScript interop, which only works in interactive render modes.

**Solution:**
The issue has been fixed by enabling Interactive Server render mode globally in `App.razor`:

```razor
<HeadOutlet @rendermode="InteractiveServer" />
<Routes @rendermode="InteractiveServer" />
```

**Verification:**
1. Open browser developer tools (F12)
2. Navigate to a page with dialogs (e.g., `/suppliers`)
3. Check the Console tab for these messages:
   - ✅ `WebSocket connected to ws://localhost:5277/_blazor`
   - ❌ `Missing <MudPopoverProvider />` (indicates the issue is not fully resolved)

### Browser Console Debugging

Common console messages and their meanings:

| Message | Status | Meaning |
|---------|--------|---------|
| `WebSocket connected to ws://localhost:5277/_blazor` | ✅ Good | Interactive Server mode is working |
| `Missing <MudPopoverProvider />` | ❌ Error | MudBlazor providers not accessible in interactive context |
| `There was an unhandled exception on the current circuit` | ❌ Error | Server-side exception occurred |

## Common Issues and Solutions

### 1. Build Errors

**Issue**: Compilation errors during `dotnet build`

**Solution**:
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### 2. Port Already in Use

**Issue**: `EADDRINUSE` or similar port binding errors

**Solution**:
```bash
# Kill processes using the port
lsof -ti:5277 | xargs kill -9  # macOS/Linux
netstat -ano | findstr :5277    # Windows
```

### 3. Hot Reload Not Working

**Issue**: Changes to Razor files not reflecting immediately

**Solution**:
1. Stop the application (Ctrl+C)
2. Clear browser cache
3. Restart with: `dotnet run`

### 4. MudBlazor Components Not Styled

**Issue**: Components appear unstyled or with broken layout

**Checklist**:
- ✅ MudBlazor CSS is included in `App.razor`: `_content/MudBlazor/MudBlazor.min.css`
- ✅ MudBlazor JS is included in `App.razor`: `_content/MudBlazor/MudBlazor.min.js`
- ✅ MudBlazor services are registered in `Program.cs`: `AddMudServices()`
- ✅ MudBlazor providers are in `MainLayout.razor`

## Testing Dialog Functionality

### Manual Testing Steps

1. **Start the application**:
   ```bash
   cd src/Mpm.Web
   dotnet run
   ```

2. **Navigate to Suppliers page**: `http://localhost:5277/suppliers`

3. **Test dialog opening**:
   - Click "New Supplier" button
   - Dialog should open with form fields
   - Form should be interactive (typing should work)

4. **Test dialog closing**:
   - Click "Cancel" button
   - Dialog should close and return to suppliers list

### Expected Behavior

✅ **Working correctly:**
- Dialog opens immediately when button is clicked
- Form fields are interactive and accept input
- Validation works (required fields show errors)
- Cancel/Submit buttons function properly

❌ **Needs investigation:**
- Nothing happens when clicking dialog buttons
- Dialog appears but is not interactive
- JavaScript errors in browser console

## Development Tips

### Hot Reload

For the best development experience with automatic reloading:

```bash
dotnet watch run
```

### Debugging

1. **Server-side debugging**: Set breakpoints in C# code
2. **Client-side debugging**: Use browser developer tools
3. **Blazor debugging**: Enable Blazor debugging in browser

### Performance

- In-memory database is used by default (no persistence between runs)
- Sample data is automatically seeded on startup
- No external dependencies required for basic functionality

## Getting Help

If you encounter issues not covered in this guide:

1. Check the [MudBlazor documentation](https://mudblazor.com/getting-started/installation)
2. Review [.NET 8 Blazor Web App documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
3. Check browser console for specific error messages
4. Verify all prerequisites are correctly installed

## Architecture Notes

### Render Modes in .NET 8 Blazor Web Apps

MPM.WEB uses the following render modes:

- **Static SSR** (default): Fast initial load, no interactivity
- **Interactive Server** (for dialogs): Full interactivity via SignalR
- **Interactive WebAssembly**: Client-side execution (not used)
- **Interactive Auto**: Hybrid approach (not used)

MudBlazor components require Interactive modes for full functionality, which is why we've configured the entire application to use Interactive Server mode.