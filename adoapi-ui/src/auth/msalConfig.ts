import type { Configuration } from '@azure/msal-browser';
import { LogLevel } from '@azure/msal-browser';

// MSAL configuration for Microsoft Entra ID
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID || '',
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID || 'common'}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    // PKCE is enabled by default
  },
  cache: {
    // Use in-memory cache (default) - do not store tokens in localStorage or sessionStorage
    cacheLocation: 'memory',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
        }
      },
      logLevel: import.meta.env.DEV ? LogLevel.Info : LogLevel.Error,
    },
  },
};

// Login request configuration
export const loginRequest = {
  scopes: ['User.Read', 'openid', 'profile'],
};

// Token request for API calls
export const apiTokenRequest = {
  scopes: [`api://${import.meta.env.VITE_API_CLIENT_ID || 'default'}/access_as_user`],
};

// Token request for general use (backward compatibility)
export const tokenRequest = {
  scopes: ['User.Read', 'openid', 'profile'],
};