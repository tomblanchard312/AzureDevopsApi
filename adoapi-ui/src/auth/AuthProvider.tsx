import React, { useEffect, useState, createContext, useContext } from 'react';
import { PublicClientApplication, EventType } from '@azure/msal-browser';
import type { AuthenticationResult, EventMessage } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { Box, CircularProgress, Typography } from '@mui/material';
import { msalConfig, loginRequest } from './msalConfig';

interface AuthProviderProps {
  children: React.ReactNode;
}

interface AuthContextValue {
  isAuthResolved: boolean;
}

const AuthContext = createContext<AuthContextValue>({
  isAuthResolved: false,
});

export { AuthContext };
export const useAuthContext = () => useContext(AuthContext);

const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null);
  const [isInitialized, setIsInitialized] = useState(false);
  const [isAuthenticating, setIsAuthenticating] = useState(false);
  const [isAuthResolved, setIsAuthResolved] = useState(false);
  const [needsConfiguration, setNeedsConfiguration] = useState(false);

  useEffect(() => {
    const initializeMsal = async () => {
      try {
        // Check if required environment variables are set
        const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID;
        const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID;

        if (!clientId || !tenantId || clientId.includes('[Your') || tenantId.includes('[Your')) {
          console.warn('Entra ID configuration is missing. Please configure VITE_ENTRA_CLIENT_ID and VITE_ENTRA_TENANT_ID environment variables.');
          setNeedsConfiguration(true);
          setIsInitialized(true);
          setIsAuthResolved(true);
          return;
        }

        const msalInstance = new PublicClientApplication(msalConfig);

        // Handle redirect promise for redirect flows
        await msalInstance.handleRedirectPromise();

        // Register event callbacks
        msalInstance.addEventCallback((event: EventMessage) => {
          if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
            const payload = event.payload as AuthenticationResult;
            const account = payload.account;
            msalInstance.setActiveAccount(account);
            setIsAuthResolved(true);
          }

          if (event.eventType === EventType.LOGOUT_SUCCESS) {
            // Clear active account on logout
            msalInstance.setActiveAccount(null);
            setIsAuthResolved(true);
          }
        });

        setMsalInstance(msalInstance);
        setIsInitialized(true);
      } catch (error) {
        console.error('MSAL initialization failed:', error);
        setIsInitialized(true); // Still set to true to avoid infinite loading
      }
    };

    initializeMsal();
  }, []);

  // Attempt automatic authentication once MSAL is initialized
  useEffect(() => {
    if (!isInitialized || !msalInstance || isAuthenticating) return;

    const authenticateUser = async () => {
      setIsAuthenticating(true);

      try {
        // Check if user is already signed in
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
          msalInstance.setActiveAccount(accounts[0]);
          setIsAuthResolved(true);
          setIsAuthenticating(false);
          return;
        }

        // Attempt silent login
        const response = await msalInstance.ssoSilent(loginRequest);
        msalInstance.setActiveAccount(response.account);
        setIsAuthResolved(true);
      } catch (error) {
        console.log('Silent login failed, redirecting to login:', error);
        // If silent login fails, redirect to login page
        try {
          await msalInstance.loginRedirect(loginRequest);
        } catch (loginError) {
          console.error('Login redirect failed:', loginError);
          setIsAuthResolved(true); // Authentication process completed (failed)
        }
      } finally {
        setIsAuthenticating(false);
      }
    };

    authenticateUser();
  }, [isInitialized, msalInstance, isAuthenticating]);

  // Show loading while authentication is not resolved, or configuration message if needed
  if (!isAuthResolved) {
    if (needsConfiguration) {
      return (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            height: '100vh',
            fontFamily: 'Arial, sans-serif',
            gap: 3,
            p: 3,
            textAlign: 'center',
          }}
        >
          <Typography variant="h4" color="warning.main" gutterBottom>
            ⚙️ Configuration Required
          </Typography>
          <Typography variant="h6" color="text.primary" gutterBottom>
            Microsoft Entra ID Authentication Setup Needed
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 600, mb: 2 }}>
            To enable authentication and access Azure DevOps features, you need to configure Microsoft Entra ID (Azure AD) settings.
          </Typography>
          <Box sx={{ textAlign: 'left', maxWidth: 500 }}>
            <Typography variant="subtitle2" gutterBottom>
              Required Environment Variables:
            </Typography>
            <Typography variant="body2" component="div" sx={{ fontFamily: 'monospace', bgcolor: 'grey.100', p: 2, borderRadius: 1 }}>
              VITE_ENTRA_CLIENT_ID=your-client-id<br />
              VITE_ENTRA_TENANT_ID=your-tenant-id
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              Create a .env file in the adoapi-ui directory with these values, or set them in your environment.
            </Typography>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            The application will continue to work for demonstration purposes, but authentication features will be limited.
          </Typography>
        </Box>
      );
    }

    return (
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh',
          fontFamily: 'Arial, sans-serif',
          gap: 2,
        }}
      >
        <CircularProgress size={60} />
        <Typography variant="h6" color="text.secondary">
          {!isInitialized ? 'Initializing authentication...' : 'Signing you in...'}
        </Typography>
      </Box>
    );
  }

  return (
    <AuthContext.Provider value={{ isAuthResolved }}>
      {needsConfiguration ? (
        children
      ) : (
        <MsalProvider instance={msalInstance!}>
          {children}
        </MsalProvider>
      )}
    </AuthContext.Provider>
  );
};

export default AuthProvider;