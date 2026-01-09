import { useMsal } from '@azure/msal-react';
import { useMemo } from 'react';

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
  const { instance, accounts, inProgress } = useMsal();

  const account = accounts[0];
  const isAuthenticated = accounts.length > 0;
  const isLoading = inProgress !== 'none';

  const user = useMemo((): UserInfo | null => {
    if (!account) return null;

    // Extract roles from ID token claims
    const idTokenClaims = account.idTokenClaims as IdTokenClaims;
    const roles = idTokenClaims?.roles || [];

    return {
      name: account.name || account.username || 'Unknown User',
      username: account.username || '',
      roles: Array.isArray(roles) ? roles : [],
    };
  }, [account]);

  const logout = async (): Promise<void> => {
    try {
      await instance.logoutRedirect();
    } catch (error) {
      console.error('Logout failed:', error);
      // Fallback to logout without redirect
      instance.logout();
    }
  };

  const hasRole = (role: string): boolean => {
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