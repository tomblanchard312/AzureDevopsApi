import React, { useEffect, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Chip,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  CircularProgress,
  LinearProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import { useApiClient } from '../api';
import { useAuth } from '../auth/useAuth';
import type { CodeInsight } from '../types/api';

interface InsightCardProps {
  insight: CodeInsight;
  onUpdateStatus: (id: string, status: CodeInsight['status'], reason?: string) => void;
  onRequestWorkItem: (id: string) => void;
  canAdmin: boolean;
  canContributor: boolean;
}

const InsightCard: React.FC<InsightCardProps> = ({ insight, onUpdateStatus, onRequestWorkItem, canAdmin, canContributor }) => {
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Critical': return 'error';
      case 'High': return 'warning';
      case 'Medium': return 'info';
      default: return 'default';
    }
  };

  return (
    <Card sx={{ mb: 2 }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
          <Box>
            <Typography variant="h6" gutterBottom>
              {insight.title}
            </Typography>
            <Box display="flex" gap={1} mb={1}>
              <Chip
                label={insight.severity}
                color={getSeverityColor(insight.severity)}
                size="small"
              />
              <Chip label={'Fingerprint: ' + insight.fingerprint} size="small" variant="outlined" />
            </Box>
          </Box>
          <Box display="flex" flexDirection="column" alignItems="flex-end" gap={1}>
            <LinearProgress
              variant="determinate"
              value={insight.confidence * 100}
              sx={{ width: 100 }}
            />
            <Typography variant="caption">
              {Math.round(insight.confidence * 100)}% confidence
            </Typography>
          </Box>
        </Box>

        <Typography variant="body2" color="text.secondary" gutterBottom>
          {insight.description}
        </Typography>

        {insight.filePath && (
          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
            {insight.filePath}
            {insight.lineStart && `:${insight.lineStart}`}
            {insight.lineEnd && `-${insight.lineEnd}`}
          </Typography>
        )}

        <Box mt={2} p={2} bgcolor="grey.50" borderRadius={1}>
          <Typography variant="body2" sx={{ fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
            {insight.evidence}
          </Typography>
        </Box>

        <Box display="flex" justifyContent="space-between" alignItems="center" mt={2}>
          <Typography variant="caption" color="text.secondary">
            Created: {new Date(insight.createdAt).toLocaleDateString()}
            {insight.updatedAt && ` | Updated: ${new Date(insight.updatedAt).toLocaleDateString()}`}
          </Typography>
          <Box display="flex" gap={1}>
            {canContributor && (
              <Button size="small" onClick={() => onRequestWorkItem(insight.id)}>
                Request Work Item
              </Button>
            )}
            {canAdmin && (
              <>
                <Button size="small" color="warning" onClick={() => onUpdateStatus(insight.id, 'AcceptedRisk')}>
                  Accept Risk
                </Button>
                <Button size="small" color="info" onClick={() => onUpdateStatus(insight.id, 'Suppressed')}>
                  Suppress
                </Button>
                <Button size="small" color="success" onClick={() => onUpdateStatus(insight.id, 'Fixed')}>
                  Mark Fixed
                </Button>
              </>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
};

const RepoInsightsPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const { hasRole } = useAuth();
  const [insights, setInsights] = useState<CodeInsight[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState({
    severity: '',
    status: '',
    type: '',
  });
  const [dialogOpen, setDialogOpen] = useState(false);
  const [dialogData, setDialogData] = useState({
    id: '',
    status: 'AcceptedRisk' as CodeInsight['status'],
    reason: '',
  });

  const loadInsights = useCallback(async () => {
    if (!repoKey) return;
    try {
      setLoading(true);
      const data = await api.getRepoInsights(repoKey, filters);
      setInsights(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load insights');
    } finally {
      setLoading(false);
    }
  }, [repoKey, api, filters]);

  useEffect(() => {
    loadInsights();
  }, [loadInsights]);

  const handleUpdateStatus = (id: string, status: CodeInsight['status']) => {
    setDialogData({ id, status, reason: '' });
    setDialogOpen(true);
  };

  const handleConfirmUpdateStatus = async () => {
    if (!repoKey) return;
    try {
      await api.updateInsightStatus(repoKey, dialogData.id, dialogData.status, dialogData.reason);
      setDialogOpen(false);
      loadInsights();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update insight status');
    }
  };

  const handleRequestWorkItem = async (id: string) => {
    if (!repoKey) return;
    try {
      await api.requestWorkItemProposal(repoKey, id);
      // Refresh insights to show updated status
      loadInsights();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to request work item proposal');
    }
  };

  const groupedInsights = insights.reduce((acc, insight) => {
    const key = insight.filePath || insight.ruleId;
    if (!acc[key]) acc[key] = [];
    acc[key].push(insight);
    return acc;
  }, {} as Record<string, CodeInsight[]>);

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
        Repository Insights: {repoKey}
      </Typography>

      {/* Filters */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Filters
          </Typography>
          <Grid container spacing={2}>
            {/* @ts-expect-error MUI Grid API issue */}
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Severity</InputLabel>
                <Select
                  value={filters.severity}
                  onChange={(e) => setFilters({ ...filters, severity: e.target.value })}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Critical">Critical</MenuItem>
                  <MenuItem value="High">High</MenuItem>
                  <MenuItem value="Medium">Medium</MenuItem>
                  <MenuItem value="Low">Low</MenuItem>
                  <MenuItem value="Info">Info</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            {/* @ts-expect-error MUI Grid API issue */}
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Status</InputLabel>
                <Select
                  value={filters.status}
                  onChange={(e) => setFilters({ ...filters, status: e.target.value })}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Open">Open</MenuItem>
                  <MenuItem value="AcceptedRisk">Accepted Risk</MenuItem>
                  <MenuItem value="Suppressed">Suppressed</MenuItem>
                  <MenuItem value="Fixed">Fixed</MenuItem>
                </Select>
              </FormControl>
            </Grid>
            {/* @ts-expect-error MUI Grid API issue */}
            <Grid item xs={12} md={4}>
              <FormControl fullWidth>
                <InputLabel>Type</InputLabel>
                <Select
                  value={filters.type}
                  onChange={(e) => setFilters({ ...filters, type: e.target.value })}
                >
                  <MenuItem value="">All</MenuItem>
                  <MenuItem value="Security">Security</MenuItem>
                  <MenuItem value="Quality">Quality</MenuItem>
                  <MenuItem value="Performance">Performance</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </CardContent>
      </Card>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Insights */}
      {Object.entries(groupedInsights).map(([groupKey, groupInsights]) => (
        <Box key={groupKey} mb={4}>
          <Typography variant="h6" gutterBottom>
            {groupKey} ({groupInsights.length} insights)
          </Typography>
          {groupInsights.map((insight) => (
            <InsightCard
              key={insight.id}
              insight={insight}
              onUpdateStatus={handleUpdateStatus}
              onRequestWorkItem={handleRequestWorkItem}
              canAdmin={hasRole('Admin')}
              canContributor={hasRole('Contributor')}
            />
          ))}
        </Box>
      ))}

      {/* Status Update Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>Update Insight Status</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Typography gutterBottom>
              Are you sure you want to mark this insight as {dialogData.status}?
            </Typography>
            {(dialogData.status === 'AcceptedRisk' || dialogData.status === 'Suppressed') && (
              <TextField
                label="Reason (required)"
                value={dialogData.reason}
                onChange={(e) => setDialogData({ ...dialogData, reason: e.target.value })}
                fullWidth
                multiline
                rows={3}
                sx={{ mt: 2 }}
              />
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button
            onClick={handleConfirmUpdateStatus}
            variant="contained"
            disabled={(dialogData.status === 'AcceptedRisk' || dialogData.status === 'Suppressed') && !dialogData.reason.trim()}
          >
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default RepoInsightsPage;