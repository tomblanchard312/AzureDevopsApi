import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Chip,
  Alert,
  CircularProgress,
} from '@mui/material';
import { useApiClient } from '../api';
import type { RepoOverview } from '../types/api';

const RepoOverviewPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const [overview, setOverview] = useState<RepoOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!repoKey) return;

    const loadOverview = async () => {
      try {
        setLoading(true);
        const data = await api.getRepoOverview(repoKey);
        setOverview(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load overview');
      } finally {
        setLoading(false);
      }
    };

    loadOverview();
  }, [repoKey, api]);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        {error}
      </Alert>
    );
  }

  if (!overview) {
    return (
      <Alert severity="info" sx={{ m: 2 }}>
        No overview data available
      </Alert>
    );
  }

  const { repoSummary, insightCounts, workItemCounts, policyState } = overview;

  return (
    <Box p={3}>
      <Typography variant="h4" gutterBottom>
        Repository Overview: {repoKey}
      </Typography>

      <Grid container spacing={3}>
        {/* Repo Summary */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Latest Snapshot
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Branch: {repoSummary.branch}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Commit: {repoSummary.commitId.substring(0, 8)}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Files: {repoSummary.fileCount.toLocaleString()}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Lines: {repoSummary.totalLines.toLocaleString()}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Updated: {new Date(repoSummary.snapshotDate).toLocaleDateString()}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Policy State */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Automation Policy
              </Typography>
              <Chip
                label={policyState}
                color={
                  policyState === 'Enabled' ? 'success' :
                  policyState === 'Restricted' ? 'warning' : 'default'
                }
                sx={{ mb: 1 }}
              />
              <Typography variant="body2" color="text.secondary">
                {policyState === 'Disabled' && 'Auto-creation is disabled for this repository.'}
                {policyState === 'Restricted' && 'Auto-creation is restricted to high-confidence items.'}
                {policyState === 'Enabled' && 'Auto-creation is enabled for this repository.'}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Insights Summary */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Open Insights by Severity
              </Typography>
              {Object.entries(insightCounts).map(([severity, count]) => (
                <Box key={severity} display="flex" justifyContent="space-between" mb={1}>
                  <Typography variant="body2">{severity}</Typography>
                  <Chip
                    label={count}
                    size="small"
                    color={
                      severity === 'Critical' ? 'error' :
                      severity === 'High' ? 'warning' :
                      severity === 'Medium' ? 'info' : 'default'
                    }
                  />
                </Box>
              ))}
            </CardContent>
          </Card>
        </Grid>

        {/* Work Items Summary */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Work Item Proposals
              </Typography>
              <Box display="flex" justifyContent="space-between" mb={1}>
                <Typography variant="body2">Proposed</Typography>
                <Chip label={workItemCounts.proposed} size="small" color="warning" />
              </Box>
              <Box display="flex" justifyContent="space-between">
                <Typography variant="body2">Created</Typography>
                <Chip label={workItemCounts.created} size="small" color="success" />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default RepoOverviewPage;