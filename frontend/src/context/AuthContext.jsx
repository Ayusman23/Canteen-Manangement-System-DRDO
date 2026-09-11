/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [accessToken, setAccessToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    try {
      const storedToken = localStorage.getItem('drdo_access_token');
      const storedUser = localStorage.getItem('drdo_user');

      if (storedToken && storedUser) {
        setAccessToken(storedToken);
        setUser(JSON.parse(storedUser));
      }
    } catch (e) {
      console.error('Failed to restore session:', e);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const login = async (email, password) => {
    const response = await api.post('/auth/login', { email, password });
    const { accessToken, refreshToken, user } = response.data;

    localStorage.setItem('drdo_access_token', accessToken);
    localStorage.setItem('drdo_refresh_token', refreshToken);
    localStorage.setItem('drdo_user', JSON.stringify(user));

    setAccessToken(accessToken);
    setUser(user);

    return user;
  };

  const logout = async () => {
    try {
      if (accessToken) {
        await api.post('/auth/logout');
      }
    } catch {
      // Ignore network errors on logout
    } finally {
      localStorage.removeItem('drdo_access_token');
      localStorage.removeItem('drdo_refresh_token');
      localStorage.removeItem('drdo_user');
      setAccessToken(null);
      setUser(null);
    }
  };

  const isEmployee = user?.role === 'Employee' || user?.role === 'CanteenManager';
  const isKitchenOperator = user?.role === 'KitchenOperator' || user?.role === 'CanteenManager';
  const isManager = user?.role === 'CanteenManager';

  return (
    <AuthContext.Provider
      value={{
        user,
        accessToken,
        isLoading,
        isAuthenticated: !!user,
        isEmployee,
        isKitchenOperator,
        isManager,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
