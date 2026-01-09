import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  TextField,
  Button,
  CircularProgress,
  Paper,
  Tabs,
  Tab,
  Checkbox,
  FormControlLabel,
  Alert,
  Divider,
} from '@mui/material';
import {
  PlayArrow as GenerateIcon,
  CheckCircle as ApplyIcon,
  Description as FileIcon,
} from '@mui/icons-material';
import { MarkdownPreview, DiffViewer } from '../components';
import { useSnackbar } from 'notistack';
import { useApiClient } from '../api';
import { useMsal } from '@azure/msal-react';
import { useAuth } from '../auth/useAuth';
import type { Project, Repository, DocsPreviewResponse } from '../types';

const DOC_FILES = [
  'README.md',
  'ARCHITECTURE.md',
  'DEV_GUIDE.md',
  'SECURITY.md',
  'DEPLOYMENT.md',
];

const STORAGE_KEYS = {
  selectedProject: 'docs_selectedProject',
  selectedRepository: 'docs_selectedRepository',
  branch: 'docs_branch',
};

const DocsPage: React.FC = () => {
  const api = useApiClient();
  const { accounts } = useMsal();
  const { hasRole } = useAuth();
  const isAuthenticated = accounts.length > 0;

  // State for repository selection
  const [projects, setProjects] = useState<Project[]>([]);
  const [repositories, setRepositories] = useState<Repository[]>([]);
  const [selectedProject, setSelectedProject] = useState<string>('');
  const [selectedRepository, setSelectedRepository] = useState<string>('');
  const [branch, setBranch] = useState<string>('main');

  // Loading and error states
  const [loadingProjects, setLoadingProjects] = useState(false);
  const [loadingRepositories, setLoadingRepositories] = useState(false);
  const [projectsError, setProjectsError] = useState<string | null>(null);
  const [repositoriesError, setRepositoriesError] = useState<string | null>(null);

  // State for preview generation
  const [isGenerating, setIsGenerating] = useState(false);
  const [previewData, setPreviewData] = useState<DocsPreviewResponse | null>(null);

  // State for review and apply
  const [selectedFile, setSelectedFile] = useState<string>('README.md');
  const [activeTab, setActiveTab] = useState<'preview' | 'diff'>('preview');
  const [reviewed, setReviewed] = useState(false);
  const [commitMessage, setCommitMessage] = useState('docs: add/update documentation');
  const [isApplying, setIsApplying] = useState(false);

  const { enqueueSnackbar } = useSnackbar();

  const loadPersistedSelections = () => {
    const savedProject = localStorage.getItem(STORAGE_KEYS.selectedProject) || '';
    const savedRepository = localStorage.getItem(STORAGE_KEYS.selectedRepository) || '';
    const savedBranch = localStorage.getItem(STORAGE_KEYS.branch) || 'main';

    setSelectedProject(savedProject);
    setSelectedRepository(savedRepository);
    setBranch(savedBranch);
  };

  const loadProjects = useCallback(async () => {
    if (!isAuthenticated) return;

    setLoadingProjects(true);
    setProjectsError(null);
    try {
      const result = await api.getProjects();
      setProjects(result);
      setProjectsError(null);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load projects';
      setProjectsError(errorMessage);
      enqueueSnackbar(errorMessage, { variant: 'error' });
    } finally {
      setLoadingProjects(false);
    }
  }, [api, enqueueSnackbar, isAuthenticated]);

  const loadRepositories = useCallback(async (projectName: string) => {
    if (!projectName) return;

    setLoadingRepositories(true);
    setRepositoriesError(null);
    try {
      const result = await api.getRepositories(projectName);
      setRepositories(result);
      setRepositoriesError(null);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load repositories';
      setRepositoriesError(errorMessage);
      enqueueSnackbar(errorMessage, { variant: 'error' });
    } finally {
      setLoadingRepositories(false);
    }
  }, [api, enqueueSnackbar]);

  // Load projects after authentication
  useEffect(() => {
    if (isAuthenticated) {
      loadProjects();
      loadPersistedSelections();
    }
  }, [isAuthenticated, loadProjects]);

  // Load repositories when project changes
  useEffect(() => {
    if (selectedProject) {
      loadRepositories(selectedProject);
    } else {
      setRepositories([]);
      setSelectedRepository('');
      setRepositoriesError(null);
    }
  }, [selectedProject, loadRepositories]);

  // Persist selections to localStorage
  useEffect(() => {
    localStorage.setItem(STORAGE_KEYS.selectedProject, selectedProject);
  }, [selectedProject]);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEYS.selectedRepository, selectedRepository);
  }, [selectedRepository]);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEYS.branch, branch);
  }, [branch]);

  const handleGeneratePreview = async () => {
    if (!selectedProject || !selectedRepository) {
      enqueueSnackbar('Please select a project and repository', { variant: 'warning' });
      return;
    }

    setIsGenerating(true);
    try {
      const request = {
        project: selectedProject,
        repositoryId: selectedRepository,
        branch: branch || 'main',
        options: {
          includeArchitecture: true,
          includeDevGuide: true,
          includeSecurity: true,
          includeDeployment: true,
        },
      };

      const result = await api.previewDocs(request);
      setPreviewData(result);
      enqueueSnackbar('Documentation preview generated successfully!', { variant: 'success' });
    } catch {
      enqueueSnackbar('Failed to generate documentation preview', { variant: 'error' });
    } finally {
      setIsGenerating(false);
    }
  };

  const handleApplyChanges = async () => {
    if (!previewData || !reviewed) {
      return;
    }

    setIsApplying(true);
    try {
      const request = {
        project: selectedProject,
        repositoryId: selectedRepository,
        branch: branch || 'main',
        commitMessage,
        changeSet: previewData.generatedFiles,
      };

      await api.applyDocs(request);
      enqueueSnackbar('Documentation changes applied successfully!', { variant: 'success' });

      // Reset state
      setPreviewData(null);
      setReviewed(false);
      setCommitMessage('docs: add/update documentation');
    } catch {
      enqueueSnackbar('Failed to apply documentation changes', { variant: 'error' });
    } finally {
      setIsApplying(false);
    }
  };

  const getCurrentFileContent = (): string => {
    if (!previewData) return '';
    const file = previewData.generatedFiles.find(f => f.fileName === selectedFile);
    return file?.content || '';
  };

  const getFileDiff = (): { oldValue: string; newValue: string } => {
    // For now, we'll diff against empty content since we don't have API to get existing files
    // In a real implementation, you'd call an API to get the current content
    return {
      oldValue: '', // Would be fetched from API
      newValue: getCurrentFileContent(),
    };
  };

  const canGeneratePreview = selectedProject && selectedRepository;
  const canApplyChanges = previewData && reviewed && commitMessage.trim() && hasRole('ADO.Contributor');

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Documentation Assistant
      </Typography>

      {/* Step 1: Select Repository */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Step 1: Select Repository
        </Typography>

        {!isAuthenticated ? (
          <Alert severity="info" sx={{ mb: 2 }}>
            Please sign in to load your projects and repositories.
          </Alert>
        ) : (
          <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          <Box sx={{ minWidth: 200, flex: '1 1 auto' }}>
            <FormControl fullWidth error={!!projectsError}>
              <InputLabel>Project</InputLabel>
              <Select
                value={selectedProject}
                label="Project"
                onChange={(e) => setSelectedProject(e.target.value)}
                disabled={loadingProjects || !isAuthenticated}
                endAdornment={
                  loadingProjects ? (
                    <CircularProgress size={20} sx={{ mr: 2 }} />
                  ) : null
                }
              >
                {projects.map((project) => (
                  <MenuItem key={project.id} value={project.name}>
                    {project.name}
                  </MenuItem>
                ))}
              </Select>
              {projectsError && (
                <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block' }}>
                  {projectsError}
                </Typography>
              )}
            </FormControl>
          </Box>

          <Box sx={{ minWidth: 200, flex: '1 1 auto' }}>
            <FormControl fullWidth disabled={!selectedProject || loadingRepositories} error={!!repositoriesError}>
              <InputLabel>Repository</InputLabel>
              <Select
                value={selectedRepository}
                label="Repository"
                onChange={(e) => setSelectedRepository(e.target.value)}
                endAdornment={
                  loadingRepositories ? (
                    <CircularProgress size={20} sx={{ mr: 2 }} />
                  ) : null
                }
              >
                {repositories.map((repo) => (
                  <MenuItem key={repo.id} value={repo.id}>
                    {repo.name}
                  </MenuItem>
                ))}
              </Select>
              {repositoriesError && (
                <Typography variant="caption" color="error" sx={{ mt: 1, display: 'block' }}>
                  {repositoriesError}
                </Typography>
              )}
            </FormControl>
          </Box>

          <Box sx={{ minWidth: 150, flex: '1 1 auto' }}>
            <TextField
              fullWidth
              label="Branch"
              value={branch}
              onChange={(e) => setBranch(e.target.value)}
              placeholder="main"
            />
          </Box>
        </Box>
        )}
      </Paper>

      {/* Step 2: Generate Preview */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Step 2: Generate Preview
        </Typography>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Button
            variant="contained"
            startIcon={isGenerating ? <CircularProgress size={20} /> : <GenerateIcon />}
            onClick={handleGeneratePreview}
            disabled={!canGeneratePreview || isGenerating}
          >
            {isGenerating ? 'Generating...' : 'Generate Preview'}
          </Button>

          {!canGeneratePreview && (
            <Typography variant="body2" color="text.secondary">
              Select a project and repository to continue
            </Typography>
          )}
        </Box>
      </Paper>

      {/* Step 3: Review and Apply */}
      {previewData && (
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Step 3: Review and Apply
          </Typography>

          <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
            {/* Left side: File tabs */}
            <Box sx={{ minWidth: 250, flex: '0 0 auto' }}>
              <Typography variant="subtitle2" gutterBottom>
                Generated Files
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                {DOC_FILES.map((fileName) => {
                  const hasContent = previewData.generatedFiles.some(f => f.fileName === fileName);
                  return (
                    <Button
                      key={fileName}
                      variant={selectedFile === fileName ? 'contained' : 'outlined'}
                      size="small"
                      startIcon={<FileIcon />}
                      onClick={() => setSelectedFile(fileName)}
                      disabled={!hasContent}
                      sx={{ justifyContent: 'flex-start' }}
                    >
                      {fileName}
                      {!hasContent && ' (empty)'}
                    </Button>
                  );
                })}
              </Box>
            </Box>

            {/* Right side: Content viewer */}
            <Box sx={{ flex: '1 1 auto', minWidth: 400 }}>
              <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1 }}>
                <Tabs
                  value={activeTab}
                  onChange={(_, newValue) => setActiveTab(newValue)}
                  sx={{ borderBottom: 1, borderColor: 'divider' }}
                >
                  <Tab label="Preview" value="preview" />
                  <Tab label="Diff" value="diff" />
                </Tabs>

                <Box sx={{ maxHeight: 600, overflow: 'auto' }}>
                  {activeTab === 'preview' ? (
                    <MarkdownPreview markdown={getCurrentFileContent()} />
                  ) : (
                    <DiffViewer
                      original={getFileDiff().oldValue}
                      modified={getFileDiff().newValue}
                      title={`Changes to ${selectedFile}`}
                    />
                  )}
                </Box>
              </Box>
            </Box>
          </Box>

          <Divider sx={{ my: 3 }} />

          {/* Bottom area: Apply controls */}
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={reviewed}
                  onChange={(e) => setReviewed(e.target.checked)}
                />
              }
              label="I have reviewed these changes and confirm they are ready to apply"
            />

            <TextField
              fullWidth
              label="Commit Message"
              value={commitMessage}
              onChange={(e) => setCommitMessage(e.target.value)}
              helperText="This message will be used for the commit when applying changes"
            />

            <Alert severity="info" sx={{ mb: 2 }}>
              <strong>Safety Notice:</strong> Applying these changes will modify files in the selected repository.
              Make sure you have reviewed all generated content carefully.
            </Alert>

            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="contained"
                color="primary"
                startIcon={isApplying ? <CircularProgress size={20} /> : <ApplyIcon />}
                onClick={handleApplyChanges}
                disabled={!canApplyChanges || isApplying}
              >
                {isApplying ? 'Applying...' : 'Apply Changes'}
              </Button>

              {!reviewed && (
                <Typography variant="body2" color="text.secondary" sx={{ alignSelf: 'center' }}>
                  Please review and check the confirmation box to enable applying changes
                </Typography>
              )}
              {reviewed && !hasRole('ADO.Contributor') && (
                <Typography variant="body2" color="error" sx={{ alignSelf: 'center' }}>
                  You need ADO.Contributor role to apply changes to repositories
                </Typography>
              )}
            </Box>
          </Box>
        </Paper>
      )}
    </Box>
  );
};

export default DocsPage;