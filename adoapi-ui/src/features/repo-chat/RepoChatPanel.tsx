import React, { useState } from 'react';
import {
  Box,
  Paper,
  IconButton,
  Typography,
  Select,
  MenuItem,
  FormControl,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  Chat as ChatIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import RepoChatMessageList from './RepoChatMessageList';
import RepoChatInput from './RepoChatInput';

// interface RepoChatPanelProps {
//   // repoKey: string; // Will be added when API calls are implemented
// }

type ChatMode = "Explore" | "Review" | "Plan";

const RepoChatPanel: React.FC = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [isOpen, setIsOpen] = useState(false);
  const [selectedMode, setSelectedMode] = useState<ChatMode>("Explore");

  const handleToggleOpen = () => {
    setIsOpen(!isOpen);
  };

  const handleSendMessage = (message: string, mode: string) => {
    // Dummy function since no data fetching
    console.log('Send message:', message, 'mode:', mode);
  };

  if (!isOpen) {
    return (
      <IconButton
        onClick={handleToggleOpen}
        sx={{
          position: 'fixed',
          bottom: 20,
          right: 20,
          bgcolor: 'primary.main',
          color: 'white',
          '&:hover': {
            bgcolor: 'primary.dark',
          },
          zIndex: 1000,
          boxShadow: 3,
        }}
        size="large"
      >
        <ChatIcon />
      </IconButton>
    );
  }

  return (
    <Paper
      sx={{
        position: 'fixed',
        bottom: 20,
        right: 20,
        width: isMobile ? '90vw' : 400,
        height: isMobile ? '70vh' : 600,
        display: 'flex',
        flexDirection: 'column',
        zIndex: 1000,
        boxShadow: 4,
        borderRadius: 2,
        overflow: 'hidden',
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          p: 2,
          bgcolor: 'primary.main',
          color: 'white',
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <ChatIcon />
          <Typography variant="h6" component="div">
            Repository Chat
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FormControl size="small" sx={{ minWidth: 100 }}>
            <Select
              value={selectedMode}
              onChange={(e) => setSelectedMode(e.target.value as ChatMode)}
              sx={{
                color: 'white',
                '& .MuiOutlinedInput-notchedOutline': {
                  borderColor: 'rgba(255, 255, 255, 0.23)',
                },
                '&:hover .MuiOutlinedInput-notchedOutline': {
                  borderColor: 'rgba(255, 255, 255, 0.5)',
                },
                '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
                  borderColor: 'white',
                },
                '& .MuiSelect-icon': {
                  color: 'white',
                },
              }}
            >
              <MenuItem value="Explore">Explore</MenuItem>
              <MenuItem value="Review">Review</MenuItem>
              <MenuItem value="Plan">Plan</MenuItem>
            </Select>
          </FormControl>
          <IconButton
            size="small"
            onClick={handleToggleOpen}
            sx={{ color: 'white' }}
          >
            <CloseIcon />
          </IconButton>
        </Box>
      </Box>

      {/* Content */}
      <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        {/* Messages */}
        <Box sx={{ flex: 1, overflow: 'hidden' }}>
          <RepoChatMessageList
            messages={[]}
          />
        </Box>

        {/* Input */}
        <Box sx={{ flexShrink: 0 }}>
          <RepoChatInput
            onSendMessage={handleSendMessage}
            isLoading={false}
            disabled={false}
            mode={selectedMode}
          />
        </Box>
      </Box>
    </Paper>
  );
};

export default RepoChatPanel;