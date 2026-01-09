# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(["dist"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ["./tsconfig.node.json", "./tsconfig.app.json"],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
]);
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from "eslint-plugin-react-x";
import reactDom from "eslint-plugin-react-dom";

export default defineConfig([
  globalIgnores(["dist"]),
  {
    files: ["**/*.{ts,tsx}"],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs["recommended-typescript"],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ["./tsconfig.node.json", "./tsconfig.app.json"],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
]);
```

## API Configuration

This application uses environment variables to configure API endpoints:

### Development

- The Vite dev server proxies all `/api/*` requests to the backend server
- Default backend URL: `http://localhost:5000`
- Configure the backend URL using the `VITE_API_BASE_URL` environment variable

### Production

- Set `VITE_API_BASE_URL` to your production backend URL
- The application makes direct API calls to this URL

### Environment Variables

Create a `.env.local` file (gitignored) for local development:

```bash
VITE_API_BASE_URL=http://localhost:5000
```

For production deployment, set the environment variable in your hosting platform.

### Proxy Configuration

The Vite dev server automatically proxies API requests during development:

- Frontend requests to `/api/*` → Backend at `http://localhost:5000/api/*`
- CORS issues are handled automatically in development
- No additional configuration needed for local development

## Microsoft Entra ID Authentication

This application uses Microsoft Entra ID (Azure AD) for authentication via MSAL (Microsoft Authentication Library).

### Setup

1. **Register an application in Azure AD:**

   - Go to Azure Portal → Microsoft Entra ID → App registrations
   - Create a new registration with:
     - Name: Your app name
     - Supported account types: Accounts in this organizational directory only
     - Redirect URI: `http://localhost:5173` (for development)

2. **Configure permissions:**

   - Add Microsoft Graph permissions: `User.Read`, `openid`, `profile`

3. **Get configuration values:**

   - Application (client) ID
   - Directory (tenant) ID

4. **Environment variables:**
   ```bash
   VITE_ENTRA_CLIENT_ID=your-client-id-here
   VITE_ENTRA_TENANT_ID=your-tenant-id-here
   ```

### Authentication Flow

- **Automatic Authentication**: On app load, the application automatically attempts silent SSO
- **Silent SSO**: Attempts to sign in user automatically if already logged into Microsoft account
- **Automatic Redirect**: If silent authentication fails, automatically redirects to Microsoft login page
- **Loading Screen**: Shows a loading spinner while authentication is resolving
- **Token Storage**: Uses sessionStorage (default MSAL behavior)
- **Logout**: Redirects to Microsoft logout and back to application

### Usage

The `AuthButton` component is automatically included in the app header and shows:

- Loading spinner during authentication resolution
- User avatar, name, and Logout button when authenticated
- No manual login button (authentication is fully automatic)

### Security Notes

- Tokens are stored in sessionStorage (cleared when browser tab closes)
- No refresh token storage in browser
- CORS is handled automatically in development via Vite proxy
- Production deployments should configure CORS on the backend
## API Client Authentication

The application automatically attaches Microsoft Entra ID access tokens to all API requests using axios interceptors.

### Token Acquisition

- **Scope**: `api://<API_CLIENT_ID>/access_as_user`
- **Silent Acquisition**: Attempts to acquire tokens silently first
- **Interactive Fallback**: Falls back to popup authentication if silent acquisition fails
- **Automatic Retry**: Automatically retries token acquisition on 401 responses

### Configuration

Add the backend API Client ID to your environment variables:

```bash
VITE_API_CLIENT_ID=your-api-client-id-here
```

This should be the Client ID of your backend API app registration in Azure AD.

### Error Handling

The API client provides friendly error messages for authentication issues:

- **401 Unauthorized**: "Authentication failed. Please sign in again."
- **403 Forbidden**: "Access denied. You do not have permission to access this resource."
- **Network Errors**: "Network error. Please check your internet connection and try again."

### Usage

API calls are made using the `useApiClient` hook, which automatically handles authentication:

```typescript
import { useApiClient } from 
\./api\';

const MyComponent = () => {
  const api = useApiClient();

  const fetchData = async () => {
    try {
      const projects = await api.getProjects();
      // API call automatically includes Bearer token
    } catch (error) {
      // Error handling with friendly messages
      console.error(error.message);
    }
  };

  // ...
};
```

All API requests automatically include the `Authorization: Bearer <token>` header when authenticated.
## Documentation Assistant

The Documentation Assistant page provides an automated workflow for generating and applying documentation to Azure DevOps repositories.

### Automatic Data Loading

- **Authentication Trigger**: Projects and repositories are automatically loaded after successful authentication
- **Project Loading**: Calls `GET /api/project/projects` to populate the project dropdown
- **Repository Loading**: When a project is selected, calls `GET /api/repository/repositories/{project}` to populate the repository dropdown
- **Loading Indicators**: Shows loading spinners in dropdowns during data fetching
- **Error Handling**: Displays error messages if API calls fail, with retry capability

### User Interface States

- **Not Authenticated**: Shows informational message prompting user to sign in
- **Loading Projects**: Project dropdown shows loading spinner and is disabled
- **Loading Repositories**: Repository dropdown shows loading spinner when fetching repositories
- **Error States**: Dropdowns display error messages below the controls when API calls fail
- **Success States**: Dropdowns are populated with data and fully functional

### Workflow Steps

1. **Authentication**: User signs in automatically or via redirect
2. **Project Selection**: Projects are loaded automatically and displayed in dropdown
3. **Repository Selection**: Repositories load automatically when project is selected
4. **Branch Selection**: User can specify target branch (defaults to 
\main\')
5. **Preview Generation**: Generate documentation preview for selected repository
6. **Review & Apply**: Review changes and apply to repository

### Features

- **Persistent Selections**: Project, repository, and branch selections are saved to localStorage
- **Validation**: Prevents actions when required selections are missing
- **Progress Indicators**: Shows loading states for long-running operations
- **Error Recovery**: Failed operations can be retried without losing selections
