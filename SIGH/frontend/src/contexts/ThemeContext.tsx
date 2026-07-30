import React, { createContext, useContext, useState } from 'react';
import { ThemeProvider as MuiThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { sighTheme } from '../styles/theme';

interface ThemeContextType {
  toggleTheme?: () => void;
}

const ThemeContext = createContext<ThemeContextType>({});

export const ThemeContextProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [theme] = useState(sighTheme);

  return (
    <ThemeContext.Provider value={{}}>
      <MuiThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </MuiThemeProvider>
    </ThemeContext.Provider>
  );
};

export const useThemeContext = () => useContext(ThemeContext);
