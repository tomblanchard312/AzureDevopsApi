import React from 'react';
import {
  Box,
  Typography,
  Avatar,
  Chip,
  List,
  ListItem,
  Divider,
} from '@mui/material';
import { Person as PersonIcon, SmartToy as BotIcon } from '@mui/icons-material';
import type { ChatMessage } from './RepoChatApi';
import ChatProposalCard from './ChatProposalCard';

interface RepoChatMessageListProps {
  messages: ChatMessage[];
}

const RepoChatMessageList: React.FC<RepoChatMessageListProps> = ({
  messages,
}) => {
  const formatTimestamp = (timestamp: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      hour: '2-digit',
      minute: '2-digit',
    }).format(timestamp);
  };

  return (
    <Box sx={{ height: '100%', overflow: 'auto', p: 2 }}>
      <List>
        {messages.map((message, index) => (
          <React.Fragment key={message.id}>
            <ListItem sx={{ alignItems: 'flex-start', px: 0 }}>
              <Box sx={{ display: 'flex', width: '100%' }}>
                <Avatar
                  sx={{
                    bgcolor: message.role === 'user' ? 'primary.main' : 'secondary.main',
                    mr: 2,
                    mt: 0.5,
                  }}
                >
                  {message.role === 'user' ? <PersonIcon /> : <BotIcon />}
                </Avatar>
                <Box sx={{ flex: 1 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                    <Typography variant="subtitle2" color="text.secondary">
                      {message.role === 'user' ? 'You' : 'Assistant'}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {formatTimestamp(message.timestamp)}
                    </Typography>
                    {message.role === 'assistant' && message.confidence && (
                      <Chip
                        label={`${Math.round(message.confidence * 100)}%`}
                        size="small"
                        color={message.confidence > 0.8 ? 'success' : message.confidence > 0.6 ? 'warning' : 'error'}
                      />
                    )}
                  </Box>
                  <Typography variant="body1" sx={{ mb: 1 }}>
                    {message.content}
                  </Typography>
                  {message.role === 'assistant' && message.sources && message.sources.length > 0 && (
                    <Box sx={{ mb: 1 }}>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                        Sources:
                      </Typography>
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                        {message.sources.map((source, idx) => (
                          <Chip
                            key={idx}
                            label={source}
                            size="small"
                            variant="outlined"
                          />
                        ))}
                      </Box>
                    </Box>
                  )}
                  {message.role === 'assistant' && message.proposals && message.proposals.length > 0 && (
                    <Box>
                      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                        Proposals:
                      </Typography>
                      {message.proposals.map((proposal) => (
                        <ChatProposalCard key={proposal.id} proposal={proposal} />
                      ))}
                    </Box>
                  )}
                </Box>
              </Box>
            </ListItem>
            {index < messages.length - 1 && <Divider />}
          </React.Fragment>
        ))}
      </List>
      {messages.length === 0 && (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <Typography variant="body2" color="text.secondary">
            No messages yet. Start a conversation!
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default RepoChatMessageList;