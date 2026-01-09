import React, { useState } from 'react';
import {
  Box,
  TextField,
  Button,
  Typography,
  Paper,
} from '@mui/material';
import { Send as SendIcon } from '@mui/icons-material';

interface RepoChatInputProps {
  onSendMessage: (message: string, mode: string) => void;
  isLoading: boolean;
  disabled?: boolean;
  mode: string;
}

const RepoChatInput: React.FC<RepoChatInputProps> = ({
  onSendMessage,
  isLoading,
  disabled = false,
  mode,
}) => {
  const [message, setMessage] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (message.trim() && !isLoading && !disabled) {
      onSendMessage(message.trim(), mode);
      setMessage('');
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  return (
    <Paper
      component="form"
      onSubmit={handleSubmit}
      sx={{
        p: 2,
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
        borderTop: 1,
        borderColor: 'divider',
      }}
    >
      <Box sx={{ display: 'flex', gap: 1 }}>
        <TextField
          fullWidth
          multiline
          maxRows={4}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Ask about this repository..."
          disabled={isLoading || disabled}
          variant="outlined"
          size="small"
        />
        <Button
          type="submit"
          variant="contained"
          disabled={!message.trim() || isLoading || disabled}
          sx={{ minWidth: 'auto', px: 2 }}
        >
          <SendIcon />
        </Button>
      </Box>

      <Typography variant="caption" color="text.secondary">
        Press Enter to send, Shift+Enter for new line
      </Typography>
    </Paper>
  );
};

export default RepoChatInput;