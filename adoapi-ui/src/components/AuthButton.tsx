import React, { useCallback } from 'react';
import { useMsal } from '@azure/msal-react';
import { Button, Box, Typography, Avatar } from '@mui/material';
import { AccountCircle as AccountIcon, Logout as LogoutIcon } from '@mui/icons-material';

const AuthButton: React.FC = () => {
  const { instance, accounts } = useMsal();
  const account = accounts[0];

  const handleLogout = useCallback(() => {
    instance.logoutRedirect().catch((error) => {
      console.error('Logout failed:', error);
    });
  }, [instance]);

  // Only show logout button when authenticated
  if (!account) {
    return null; // Don't render anything when not authenticated
  }

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <Avatar sx={{ width: 32, height: 32 }}>
        {account.name?.charAt(0) || <AccountIcon />}
      </Avatar>
      <Box sx={{ display: { xs: 'none', sm: 'block' } }}>
        <Typography variant="body2" sx={{ fontWeight: 500 }}>
          {account.name}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {account.username}
        </Typography>
      </Box>
      <Button
        variant="outlined"
        size="small"
        startIcon={<LogoutIcon />}
        onClick={handleLogout}
        sx={{ ml: 1 }}
      >
        Logout
      </Button>
    </Box>
  );
};

export default AuthButton;