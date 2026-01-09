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

export const useAuthContext = () => useContext(AuthContext);

const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [msalInstance, setMsalInstance] = useState<PublicClientApplication | null>(null);
  const [isInitialized, setIsInitialized] = useState(false);
  const [isAuthenticating, setIsAuthenticating] = useState(false);
  const [isAuthResolved, setIsAuthResolved] = useState(false);

  useEffect(() => {
    const initializeMsal = async () => {
      try {
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

  // Show loading while authentication is not resolved
  if (!isAuthResolved) {
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
      <MsalProvider instance={msalInstance!}>
        {children}
      </MsalProvider>
    </AuthContext.Provider>
  );
};

export default AuthProvider;