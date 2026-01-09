import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Collapse,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import { useApiClient } from '../api';
import type { AgentRun, AgentDecision } from '../types/api';

interface AgentRunRowProps {
  run: AgentRun;
  decisions: AgentDecision[];
}

const AgentRunRow: React.FC<AgentRunRowProps> = ({ run, decisions }) => {
  const [expanded, setExpanded] = useState(false);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Completed': return 'success';
      case 'Failed': return 'error';
      case 'Running': return 'warning';
      default: return 'default';
    }
  };

  const duration = useMemo(() => {
    if (!run.completedAt) return null;
    return new Date(run.completedAt).getTime() - new Date(run.startedAt).getTime();
  }, [run.completedAt, run.startedAt]);

  const formatDuration = (ms: number | null) => {
    if (ms === null) return 'Running...';
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  };

  return (
    <>
      <TableRow>
        <TableCell>{run.runType}</TableCell>
        <TableCell>{run.model}</TableCell>
        <TableCell>{run.promptVersion}</TableCell>
        <TableCell>{run.policyVersion}</TableCell>
        <TableCell>
          <Chip
            label={run.status}
            color={getStatusColor(run.status)}
            size="small"
          />
        </TableCell>
        <TableCell>{formatDuration(duration)}</TableCell>
        <TableCell>{new Date(run.startedAt).toLocaleString()}</TableCell>
        <TableCell>
          <IconButton size="small" onClick={() => setExpanded(!expanded)}>
            {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell colSpan={8} sx={{ py: 0 }}>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <Box sx={{ p: 2 }}>
              <Typography variant="subtitle2" gutterBottom>
                Input Summary
              </Typography>
              <Typography variant="body2" sx={{ mb: 2, fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                {run.inputSummary}
              </Typography>

              <Typography variant="subtitle2" gutterBottom>
                Output Summary
              </Typography>
              <Typography variant="body2" sx={{ mb: 2, fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                {run.outputSummary}
              </Typography>

              {run.errorMessage && (
                <>
                  <Typography variant="subtitle2" gutterBottom color="error">
                    Error
                  </Typography>
                  <Typography variant="body2" color="error" sx={{ mb: 2 }}>
                    {run.errorMessage}
                  </Typography>
                </>
              )}

              {decisions.length > 0 && (
                <>
                  <Typography variant="subtitle2" gutterBottom>
                    Decisions Made ({decisions.length})
                  </Typography>
                  {decisions.map((decision) => (
                    <Box key={decision.id} sx={{ mb: 1, pl: 2, borderLeft: '2px solid #ccc' }}>
                      <Typography variant="body2">
                        <strong>{decision.decisionType}</strong> on {decision.targetId}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Confidence: {Math.round(decision.confidence * 100)}% | {decision.reason}
                      </Typography>
                    </Box>
                  ))}
                </>
              )}
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
};

const RepoAgentRunsPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const [runs, setRuns] = useState<AgentRun[]>([]);
  const [decisions, setDecisions] = useState<Record<string, AgentDecision[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadRuns = useCallback(async () => {
    if (!repoKey) return;
    try {
      setLoading(true);
      const data = await api.getAgentRuns(repoKey);
      setRuns(data);

      // Load decisions for each run
      const decisionsMap: Record<string, AgentDecision[]> = {};
      for (const run of data) {
        try {
          const runDecisions = await api.getAgentRunDecisions(run.id);
          decisionsMap[run.id] = runDecisions;
        } catch (err) {
          console.error(`Failed to load decisions for run ${run.id}:`, err);
          decisionsMap[run.id] = [];
        }
      }
      setDecisions(decisionsMap);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load agent runs');
    } finally {
      setLoading(false);
    }
  }, [repoKey, api]);

  useEffect(() => {
    loadRuns();
  }, [loadRuns]);

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
        Agent Runs: {repoKey}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Card>
        <CardContent>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Run Type</TableCell>
                  <TableCell>Model</TableCell>
                  <TableCell>Prompt Version</TableCell>
                  <TableCell>Policy Version</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Duration</TableCell>
                  <TableCell>Started</TableCell>
                  <TableCell>Details</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {runs.map((run) => (
                  <AgentRunRow
                    key={run.id}
                    run={run}
                    decisions={decisions[run.id] || []}
                  />
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Box>
  );
};

export default RepoAgentRunsPage;