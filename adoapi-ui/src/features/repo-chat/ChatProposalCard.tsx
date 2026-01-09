import React from 'react';
import {
  Card,
  CardContent,
  Typography,
  Chip,
  Box,
} from '@mui/material';
import type { ChatProposal } from './RepoChatApi';

interface ChatProposalCardProps {
  proposal: ChatProposal;
}

const ChatProposalCard: React.FC<ChatProposalCardProps> = ({ proposal }) => {
  return (
    <Card sx={{ mb: 1 }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <Typography variant="h6" component="div">
            {proposal.title}
          </Typography>
          <Chip
            label={`${Math.round(proposal.confidence * 100)}%`}
            color={proposal.confidence > 0.8 ? 'success' : proposal.confidence > 0.6 ? 'warning' : 'error'}
            size="small"
          />
        </Box>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {proposal.description}
        </Typography>
        <Chip label={proposal.type} size="small" variant="outlined" />
      </CardContent>
    </Card>
  );
};

export default ChatProposalCard;