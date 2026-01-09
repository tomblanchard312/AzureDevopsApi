import React, { useEffect, useState, useCallback } from 'react';
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
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Alert,
  CircularProgress,
  LinearProgress,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  Add as AddIcon,
} from '@mui/icons-material';
import { useApiClient } from '../api';
import { useAuth } from '../auth/useAuth';
import type { RepositoryMemory } from '../types/api';

interface MemoryRowProps {
  memory: RepositoryMemory;
  onEdit: (memory: RepositoryMemory) => void;
  onDeactivate: (id: string) => void;
  onRevalidate: (id: string) => void;
  canEdit: boolean;
  canAdmin: boolean;
}

const MemoryRow: React.FC<MemoryRowProps> = ({ memory, onEdit, onDeactivate, onRevalidate, canEdit, canAdmin }) => {
  const [expanded, setExpanded] = useState(false);

  return (
    <>
      <TableRow>
        <TableCell>{memory.memoryType}</TableCell>
        <TableCell>{memory.title}</TableCell>
        <TableCell>
          <LinearProgress
            variant="determinate"
            value={memory.confidence * 100}
            sx={{ width: 80 }}
          />
          <Typography variant="caption">{Math.round(memory.confidence * 100)}%</Typography>
        </TableCell>
        <TableCell>{memory.source}</TableCell>
        <TableCell>{new Date(memory.lastValidated).toLocaleDateString()}</TableCell>
        <TableCell>
          <Chip
            label={memory.isActive ? 'Active' : 'Inactive'}
            color={memory.isActive ? 'success' : 'default'}
            size="small"
          />
        </TableCell>
        <TableCell>
          <IconButton size="small" onClick={() => setExpanded(!expanded)}>
            {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
          {canEdit && (
            <IconButton size="small" onClick={() => onEdit(memory)}>
              <EditIcon />
            </IconButton>
          )}
          {canAdmin && (
            <>
              <IconButton size="small" onClick={() => onRevalidate(memory.id)}>
                <RefreshIcon />
              </IconButton>
              <IconButton size="small" onClick={() => onDeactivate(memory.id)}>
                <DeleteIcon />
              </IconButton>
            </>
          )}
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell colSpan={7} sx={{ py: 0 }}>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <Box sx={{ p: 2 }}>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                {memory.content}
              </Typography>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
};

const RepoMemoryPage: React.FC = () => {
  const { repoKey } = useParams<{ repoKey: string }>();
  const api = useApiClient();
  const { hasRole } = useAuth();
  const [memories, setMemories] = useState<RepositoryMemory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingMemory, setEditingMemory] = useState<RepositoryMemory | null>(null);
  const [formData, setFormData] = useState({
    memoryType: '',
    title: '',
    content: '',
    confidence: 0.5,
    source: '',
    sourceType: '',
  });

  const loadMemories = useCallback(async () => {
    if (!repoKey) return;
    try {
      setLoading(true);
      const data = await api.getRepoMemories(repoKey);
      setMemories(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load memories');
    } finally {
      setLoading(false);
    }
  }, [repoKey, api]);

  useEffect(() => {
    loadMemories();
  }, [loadMemories]);

  const handleCreate = () => {
    setEditingMemory(null);
    setFormData({
      memoryType: '',
      title: '',
      content: '',
      confidence: 0.5,
      source: '',
      sourceType: '',
    });
    setDialogOpen(true);
  };

  const handleEdit = (memory: RepositoryMemory) => {
    setEditingMemory(memory);
    setFormData({
      memoryType: memory.memoryType,
      title: memory.title,
      content: memory.content,
      confidence: memory.confidence,
      source: memory.source,
      sourceType: memory.sourceType,
    });
    setDialogOpen(true);
  };

  const handleSave = async () => {
    if (!repoKey) return;
    try {
      if (editingMemory) {
        await api.updateRepoMemory(repoKey, editingMemory.id, formData);
      } else {
        await api.createRepoMemory(repoKey, formData);
      }
      setDialogOpen(false);
      loadMemories();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save memory');
    }
  };

  const handleDeactivate = async (id: string) => {
    if (!repoKey) return;
    if (!confirm('Are you sure you want to deactivate this memory item?')) return;
    try {
      await api.deactivateRepoMemory(repoKey, id);
      loadMemories();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deactivate memory');
    }
  };

  const handleRevalidate = async (id: string) => {
    if (!repoKey) return;
    try {
      await api.revalidateRepoMemory(repoKey, id);
      loadMemories();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revalidate memory');
    }
  };

  const groupedMemories = memories.reduce((acc, memory) => {
    if (!acc[memory.memoryType]) acc[memory.memoryType] = [];
    acc[memory.memoryType].push(memory);
    return acc;
  }, {} as Record<string, RepositoryMemory[]>);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box p={3}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Repository Memory: {repoKey}</Typography>
        {hasRole('Contributor') && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={handleCreate}>
            Create Memory Item
          </Button>
        )}
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {Object.entries(groupedMemories).map(([type, typeMemories]) => (
        <Card key={type} sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              {type} ({typeMemories.length} items)
            </Typography>
            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Title</TableCell>
                    <TableCell>Confidence</TableCell>
                    <TableCell>Source</TableCell>
                    <TableCell>Last Validated</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {typeMemories.map((memory) => (
                    <MemoryRow
                      key={memory.id}
                      memory={memory}
                      onEdit={handleEdit}
                      onDeactivate={handleDeactivate}
                      onRevalidate={handleRevalidate}
                      canEdit={hasRole('Contributor')}
                      canAdmin={hasRole('Admin')}
                    />
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      ))}

      {/* Create/Edit Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>
          {editingMemory ? 'Edit Memory Item' : 'Create Memory Item'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Memory Type</InputLabel>
              <Select
                value={formData.memoryType}
                onChange={(e) => setFormData({ ...formData, memoryType: e.target.value })}
              >
                <MenuItem value="Pattern">Pattern</MenuItem>
                <MenuItem value="Decision">Decision</MenuItem>
                <MenuItem value="Context">Context</MenuItem>
                <MenuItem value="Rule">Rule</MenuItem>
              </Select>
            </FormControl>
            <TextField
              label="Title"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              fullWidth
            />
            <TextField
              label="Content"
              value={formData.content}
              onChange={(e) => setFormData({ ...formData, content: e.target.value })}
              multiline
              rows={4}
              fullWidth
            />
            <TextField
              label="Confidence"
              type="number"
              value={formData.confidence}
              onChange={(e) => setFormData({ ...formData, confidence: parseFloat(e.target.value) })}
              inputProps={{ min: 0, max: 1, step: 0.1 }}
              fullWidth
            />
            <TextField
              label="Source"
              value={formData.source}
              onChange={(e) => setFormData({ ...formData, source: e.target.value })}
              fullWidth
            />
            <TextField
              label="Source Type"
              value={formData.sourceType}
              onChange={(e) => setFormData({ ...formData, sourceType: e.target.value })}
              fullWidth
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleSave} variant="contained">
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default RepoMemoryPage;