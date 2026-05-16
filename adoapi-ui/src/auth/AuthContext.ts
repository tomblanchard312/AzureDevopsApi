import { createContext } from 'react';

interface AuthContextValue {
  isAuthResolved: boolean;
  isAuthConfigured: boolean;
}

export const AuthContext = createContext<AuthContextValue>({
  isAuthResolved: false,
  isAuthConfigured: false,
});
