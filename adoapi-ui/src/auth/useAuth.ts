import { useMsal } from '@azure/msal-react';
import { useMemo, useContext } from 'react';
import { AuthContext } from './AuthProvider';

interface IdTokenClaims {
  roles?: string[];
  [key: string]: unknown;
}

export interface UserInfo {
  name: string;
  username: string;
  roles: string[];
}

export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: UserInfo | null;
  error: string | null;
}

export const useAuth = (): AuthState & {
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
} => {
  const { isAuthResolved } = useContext(AuthContext);

  // Check if MSAL is available (not in configuration-needed state)
  const msalAvailable = isAuthResolved;

  let instance: any = null;
  let accounts: any[] = [];
  let inProgress: string = 'none';

  if (msalAvailable) {
    try {
      const msalResult = useMsal();
      instance = msalResult.instance;
      accounts = msalResult.accounts;
      inProgress = msalResult.inProgress;
    } catch (error) {
      // MSAL not available, continue with fallback
    }
  }

  const account = accounts[0];
  const isAuthenticated = msalAvailable ? accounts.length > 0 : false;
  const isLoading = msalAvailable ? inProgress !== 'none' : false;

  const user = useMemo((): UserInfo | null => {
    if (!msalAvailable || !account) return null;

    // Extract roles from ID token claims
    const idTokenClaims = account.idTokenClaims as IdTokenClaims;
    const roles = idTokenClaims?.roles || [];

    return {
      name: account.name || account.username || 'Unknown User',
      username: account.username || '',
      roles: Array.isArray(roles) ? roles : [],
    };
  }, [msalAvailable, account]);

  const logout = async (): Promise<void> => {
    if (!msalAvailable || !instance) {
      console.warn('Logout not available - authentication not configured');
      return;
    }

    try {
      await instance.logoutRedirect();
    } catch (error) {
      console.error('Logout failed:', error);
      // Fallback to logout without redirect
      instance.logout();
    }
  };

  const hasRole = (role: string): boolean => {
    if (!msalAvailable) return false; // No roles when auth is not configured
    return user?.roles.includes(role) ?? false;
  };

  return {
    isAuthenticated,
    isLoading,
    user,
    error: null, // MSAL handles errors internally
    logout,
    hasRole,
  };
};