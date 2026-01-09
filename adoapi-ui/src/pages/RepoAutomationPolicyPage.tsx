import React, { useEffect, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Switch,
  FormControlLabel,
  Slider,
  TextField,
  Button,
  Alert,
  CircularProgress,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
} from '@mui/material';
import { useApiClient } from '../api';
import { useAuth } from '../auth/useAuth';
import type { AutomationPolicy } from '../types/api';

const RepoAutomationPolicyPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const { hasRole } = useAuth();
  const [policy, setPolicy] = useState<AutomationPolicy | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPolicy = useCallback(async () => {
    if (!repoKey) return;
    try {
      setLoading(true);
      const data = await api.getAutomationPolicy(repoKey);
      setPolicy(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load policy');
    } finally {
      setLoading(false);
    }
  }, [repoKey, api]);

  useEffect(() => {
    loadPolicy();
  }, [loadPolicy]);

  const handleSave = async () => {
    if (!repoKey || !policy) return;
    try {
      setSaving(true);
      await api.updateAutomationPolicy(repoKey, policy);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save policy');
    } finally {
      setSaving(false);
    }
  };

  if (!hasRole('Admin')) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        Access denied. Admin role required.
      </Alert>
    );
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (!policy) {
    return (
      <Alert severity="info" sx={{ m: 2 }}>
        No policy data available
      </Alert>
    );
  }

  return (
    <Box p={3} maxWidth={800}>
      <Typography variant="h4" gutterBottom>
        Automation Policy: {repoKey}
      </Typography>

      <Alert severity="warning" sx={{ mb: 3 }}>
        <Typography variant="body2">
          <strong>Warning:</strong> Auto-creation of work items can have significant impact on your Azure DevOps projects.
          Only enable this for repositories where automated actions are appropriate and monitored.
          Incorrect configuration may lead to unwanted work items or security issues.
        </Typography>
      </Alert>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            General Settings
          </Typography>
          <Box display="flex" flexDirection="column" gap={2}>
            <FormControlLabel
              control={
                <Switch
                  checked={policy.autoCreateEnabled}
                  onChange={(e) => setPolicy({ ...policy, autoCreateEnabled: e.target.checked })}
                />
              }
              label="Enable Auto-Creation"
            />

            <FormControlLabel
              control={
                <Switch
                  checked={policy.requireHumanApproval}
                  onChange={(e) => setPolicy({ ...policy, requireHumanApproval: e.target.checked })}
                />
              }
              label="Require Human Approval for Auto-Created Items"
            />

            <FormControlLabel
              control={
                <Switch
                  checked={policy.allowOllamaAutoCreate}
                  onChange={(e) => setPolicy({ ...policy, allowOllamaAutoCreate: e.target.checked })}
                />
              }
              label="Allow Auto-Creation from Ollama Models"
            />
            <Typography variant="caption" color="text.secondary">
              When enabled, work items can be auto-created based on recommendations from local Ollama models.
              This may reduce latency but could introduce inconsistencies compared to cloud models.
            </Typography>
          </Box>
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Severity Filters
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Only insights with these severities will be considered for auto-creation.
          </Typography>
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Allowed Severities</InputLabel>
            <Select
              multiple
              value={policy.allowedSeverities}
              onChange={(e) => setPolicy({ ...policy, allowedSeverities: e.target.value as ('Critical' | 'High')[] })}
              renderValue={(selected) => (
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                  {selected.map((value) => (
                    <Chip key={value} label={value} size="small" />
                  ))}
                </Box>
              )}
            >
              <MenuItem value="Critical">Critical</MenuItem>
              <MenuItem value="High">High</MenuItem>
            </Select>
          </FormControl>
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Confidence Threshold
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Minimum confidence score required for auto-creation (0.7 - 1.0).
            Higher values reduce false positives but may miss valid insights.
          </Typography>
          <Box px={2}>
            <Slider
              value={policy.minimumConfidence}
              onChange={(_, value) => setPolicy({ ...policy, minimumConfidence: value as number })}
              min={0.7}
              max={1.0}
              step={0.05}
              marks={[
                { value: 0.7, label: '0.7' },
                { value: 0.8, label: '0.8' },
                { value: 0.9, label: '0.9' },
                { value: 1.0, label: '1.0' },
              ]}
              valueLabelDisplay="auto"
            />
          </Box>
        </CardContent>
      </Card>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Rate Limiting
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Maximum number of work items that can be auto-created per day.
            This prevents overwhelming the system with automated actions.
          </Typography>
          <TextField
            label="Max Items Per Day"
            type="number"
            value={policy.maxItemsPerDay}
            onChange={(e) => setPolicy({ ...policy, maxItemsPerDay: parseInt(e.target.value) || 0 })}
            inputProps={{ min: 1, max: 100 }}
            fullWidth
          />
        </CardContent>
      </Card>

      <Box display="flex" justifyContent="flex-end" gap={2}>
        <Button onClick={loadPolicy} disabled={loading}>
          Reset
        </Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={saving}
        >
          {saving ? 'Saving...' : 'Save Policy'}
        </Button>
      </Box>
    </Box>
  );
};

export default RepoAutomationPolicyPage;