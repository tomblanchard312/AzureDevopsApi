import React, { useEffect, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Tabs,
  Tab,
  Chip,
  Button,
  Alert,
  CircularProgress,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Link,
} from '@mui/material';
import { useApiClient } from '../api';
import { useAuth } from '../auth/useAuth';
import type { WorkItemLink } from '../types/api';

interface WorkItemCardProps {
  workItem: WorkItemLink;
  onApprove: (id: string) => void;
  onReject: (id: string) => void;
  canAdmin: boolean;
}

const WorkItemCard: React.FC<WorkItemCardProps> = ({ workItem, onApprove, onReject, canAdmin }) => {
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Approved': return 'warning';
      case 'Rejected': return 'error';
      case 'Created': return 'success';
      default: return 'default';
    }
  };

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
          <Box>
            <Typography variant="h6" gutterBottom>
              {workItem.title}
            </Typography>
            <Box display="flex" gap={1} mb={1}>
              <Chip label={workItem.workItemType} size="small" />
              <Chip
                label={workItem.status}
                color={getStatusColor(workItem.status)}
                size="small"
              />
            </Box>
          </Box>
          <Box display="flex" flexDirection="column" alignItems="flex-end" gap={1}>
            <LinearProgress
              variant="determinate"
              value={workItem.confidence * 100}
              sx={{ width: 100 }}
            />
            <Typography variant="caption">
              {Math.round(workItem.confidence * 100)}% confidence
            </Typography>
          </Box>
        </Box>

        <Typography variant="body2" color="text.secondary" gutterBottom>
          {workItem.description}
        </Typography>

        <Typography variant="body2" sx={{ fontWeight: 'bold' }} gutterBottom>
          Acceptance Criteria:
        </Typography>
        <Typography variant="body2" sx={{ mb: 2, pl: 2 }}>
          {workItem.acceptanceCriteria}
        </Typography>

        <Typography variant="body2" sx={{ fontStyle: 'italic', mb: 2 }}>
          "Why now": {workItem.whyNow || 'Not specified'}
        </Typography>

        {workItem.azureDevOpsUrl && (
          <Box mb={2}>
            <Link href={workItem.azureDevOpsUrl} target="_blank" rel="noopener">
              View in Azure DevOps
            </Link>
          </Box>
        )}

        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="caption" color="text.secondary">
            Created: {new Date(workItem.createdAt).toLocaleDateString()}
            {workItem.approvedAt && ` | Approved: ${new Date(workItem.approvedAt).toLocaleDateString()}`}
          </Typography>
          {workItem.status === 'Proposed' && canAdmin && (
            <Box display="flex" gap={1}>
              <Button size="small" color="success" variant="contained" onClick={() => onApprove(workItem.id)}>
                Approve & Create
              </Button>
              <Button size="small" color="error" variant="outlined" onClick={() => onReject(workItem.id)}>
                Reject
              </Button>
            </Box>
          )}
          {workItem.status === 'Rejected' && workItem.rejectedReason && (
            <Typography variant="body2" color="error">
              Rejected: {workItem.rejectedReason}
            </Typography>
          )}
        </Box>
      </CardContent>
    </Card>
  );
};

const RepoWorkItemsPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const { hasRole } = useAuth();
  const [workItems, setWorkItems] = useState<WorkItemLink[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [rejectData, setRejectData] = useState({ id: '', reason: '' });

  const loadWorkItems = useCallback(async () => {
    if (!repoKey) return;
    try {
      setLoading(true);
      const data = await api.getWorkItemProposals(repoKey);
      setWorkItems(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load work items');
    } finally {
      setLoading(false);
    }
  }, [repoKey, api]);

  useEffect(() => {
    loadWorkItems();
  }, [loadWorkItems]);

  const handleApprove = async (id: string) => {
    if (!repoKey) return;
    if (!confirm('Are you sure you want to approve this work item proposal and create it in Azure DevOps?')) return;
    try {
      await api.approveWorkItemProposal(repoKey, id);
      loadWorkItems();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to approve work item');
    }
  };

  const handleReject = (id: string) => {
    setRejectData({ id, reason: '' });
    setDialogOpen(true);
  };

  const handleConfirmReject = async () => {
    if (!repoKey) return;
    try {
      await api.rejectWorkItemProposal(repoKey, rejectData.id, rejectData.reason);
      setDialogOpen(false);
      loadWorkItems();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reject work item');
    }
  };

  const proposals = workItems.filter(wi => wi.status === 'Proposed');
  const created = workItems.filter(wi => wi.status === 'Created' || wi.status === 'Approved');

  const currentItems = tabValue === 0 ? proposals : created;

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box p={3}>
      <Typography variant="h4" gutterBottom>
        Work Item Proposals: {repoKey}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)} sx={{ mb: 3 }}>
        <Tab label={`Proposals (${proposals.length})`} />
        <Tab label={`Created/Approved (${created.length})`} />
      </Tabs>

      {currentItems.length === 0 ? (
        <Alert severity="info">
          No {tabValue === 0 ? 'proposals' : 'created work items'} found.
        </Alert>
      ) : (
        currentItems.map((workItem) => (
          <WorkItemCard
            key={workItem.id}
            workItem={workItem}
            onApprove={handleApprove}
            onReject={handleReject}
            canAdmin={hasRole('Admin')}
          />
        ))
      )}

      {/* Reject Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>Reject Work Item Proposal</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Typography gutterBottom>
              Please provide a reason for rejecting this work item proposal.
            </Typography>
            <TextField
              label="Rejection Reason"
              value={rejectData.reason}
              onChange={(e) => setRejectData({ ...rejectData, reason: e.target.value })}
              fullWidth
              multiline
              rows={3}
              sx={{ mt: 2 }}
              required
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleConfirmReject}
            variant="contained"
            color="error"
            disabled={!rejectData.reason.trim()}
          >
            Reject
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default RepoWorkItemsPage;