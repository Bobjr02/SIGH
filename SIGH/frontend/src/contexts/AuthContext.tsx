import React, { createContext, useContext, useState, useEffect } from 'react';
import { UserSession } from '../types';

export const ALL_DISCIPLINARY_PERMISSIONS = [
  'Disciplinary.Cases.Create',
  'Disciplinary.Cases.View',
  'Disciplinary.Cases.Read',
  'Disciplinary.Cases.Open',
  'Disciplinary.Cases.StartInvestigation',
  'Disciplinary.Cases.SubmitForDecision',
  'Disciplinary.Cases.RecordDecision',
  'Disciplinary.Cases.ApproveDecision',
  'Disciplinary.Cases.RejectDecision',
  'Disciplinary.Cases.AddOccurrence',
  'Disciplinary.Cases.AddEmployee',
  'Disciplinary.Cases.AddEvidence',
  'Disciplinary.Cases.ApplyMeasure',
  'Disciplinary.Cases.Cancel',
  'Disciplinary.Cases.Conclude',
  'Disciplinary.InfractionTypes.Create',
  'Disciplinary.InfractionTypes.View',
  'Disciplinary.InfractionTypes.Read',
  'Disciplinary.InfractionTypes.Update',
  'Disciplinary.InfractionTypes.Activate',
  'Disciplinary.InfractionTypes.Deactivate',
  'Disciplinary.Reports.ViewDashboard',
  'Disciplinary.Reports.ViewCases',
  'Disciplinary.Reports.ViewMeasures',
  'Disciplinary.Reports.ViewEmployeeHistory',
  'Disciplinary.Reports.Export',
  'Notifications.View',
  'Notifications.Read',
  'Notifications.Manage',
];

interface AuthContextType {
  user: UserSession | null;
  isAuthenticated: boolean;
  permissions: string[];
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  setPermissions: (permissions: string[]) => void;
  resetAllPermissions: () => void;
  clearAllPermissions: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const STORAGE_KEY = 'sigh_user_permissions';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user] = useState<UserSession>({
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Gestor Disciplinar',
    email: 'gestor@sigh.com.br',
    role: 'AdminDisciplinar',
  });

  const [permissions, setPermissionsState] = useState<string[]>(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // Fallback
      }
    }
    return ALL_DISCIPLINARY_PERMISSIONS;
  });

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(permissions));
  }, [permissions]);

  const hasPermission = (permission: string): boolean => {
    if (permissions.includes(permission)) return true;
    // Map View to Read fallback
    if (permission === 'Disciplinary.Cases.View' && permissions.includes('Disciplinary.Cases.Read')) return true;
    if (permission === 'Disciplinary.Cases.Read' && permissions.includes('Disciplinary.Cases.View')) return true;
    if (permission === 'Disciplinary.InfractionTypes.View' && permissions.includes('Disciplinary.InfractionTypes.Read')) return true;
    if (permission === 'Disciplinary.InfractionTypes.Read' && permissions.includes('Disciplinary.InfractionTypes.View')) return true;
    if (permission === 'Notifications.View' && permissions.includes('Notifications.Read')) return true;
    if (permission === 'Notifications.Read' && permissions.includes('Notifications.View')) return true;
    return false;
  };

  const hasAnyPermission = (perms: string[]): boolean => {
    return perms.some((p) => hasPermission(p));
  };

  const setPermissions = (perms: string[]) => {
    setPermissionsState(perms);
  };

  const resetAllPermissions = () => {
    setPermissionsState(ALL_DISCIPLINARY_PERMISSIONS);
  };

  const clearAllPermissions = () => {
    setPermissionsState([]);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: true,
        permissions,
        hasPermission,
        hasAnyPermission,
        setPermissions,
        resetAllPermissions,
        clearAllPermissions,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuthContext = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuthContext deve ser usado dentro de um AuthProvider');
  }
  return context;
};
