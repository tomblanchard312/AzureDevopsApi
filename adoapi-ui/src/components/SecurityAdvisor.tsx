import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Alert,
  Card,
  CardContent,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  FormLabel,
  FormControlLabel,
  Radio,
  CircularProgress,
  Snackbar,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Security as SecurityIcon,
  Code as CodeIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  ThumbUp as ThumbUpIcon,
  Comment as CommentIcon,
  Preview as PreviewIcon,
} from '@mui/icons-material';
import { DiffViewer } from './DiffViewer';
import type {
  SecurityFinding,
  SecurityRecommendation,
  DiffResponse,
  PullRequestCommentRequest,
  PullRequestCommentResponse,
} from '../types/api';

interface SecurityAdvisorProps {
  findings: SecurityFinding[];
  onGenerateRecommendation?: (findingId: string) => Promise<SecurityRecommendation>;
  onGenerateDiff?: (recommendationId: string) => Promise<DiffResponse>;
  onApproveRecommendation?: (recommendationId: string) => Promise<void>;
  onApplyRecommendation?: (recommendationId: string) => Promise<void>;
  onPostPullRequestComment?: (request: PullRequestCommentRequest) => Promise<PullRequestCommentResponse>;
  onPreviewPullRequestComment?: (request: PullRequestCommentRequest) => Promise<PullRequestCommentResponse>;
}

const SecurityAdvisor: React.FC<SecurityAdvisorProps> = ({
  findings,
  onGenerateRecommendation,
  onGenerateDiff,
  onApproveRecommendation,
  onApplyRecommendation,
  onPostPullRequestComment,
  onPreviewPullRequestComment,
}) => {
  const [expandedFinding, setExpandedFinding] = useState<string | null>(null);
  const [recommendations, setRecommendations] = useState<Record<string, SecurityRecommendation>>({});
  const [diffs, setDiffs] = useState<Record<string, DiffResponse>>({});
  const [loadingStates, setLoadingStates] = useState<Record<string, boolean>>({});
  const [selectedDiff, setSelectedDiff] = useState<{ recommendation: SecurityRecommendation; diff: DiffResponse } | null>(null);
  const [prCommentDialog, setPrCommentDialog] = useState(false);
  const [prCommentForm, setPrCommentForm] = useState({
    organization: '',
    project: '',
    pullRequestId: '',
    repositoryId: '',
    findingIds: [] as string[],
    previewOnly: true,
  });
  const [prCommentPreview, setPrCommentPreview] = useState<string>('');
  const [prCommentLoading, setPrCommentLoading] = useState(false);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical': return 'error';
      case 'high': return 'error';
      case 'medium': return 'warning';
      case 'low': return 'info';
      case 'info': return 'default';
      default: return 'default';
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'critical':
      case 'high': return <ErrorIcon />;
      case 'medium': return <WarningIcon />;
      case 'low':
      case 'info': return <InfoIcon />;
      default: return <SecurityIcon />;
    }
  };

  const getConfidenceColor = (confidence: string) => {
    switch (confidence.toLowerCase()) {
      case 'high': return 'success';
      case 'medium': return 'warning';
      case 'low': return 'error';
      default: return 'default';
    }
  };

  const handleGenerateRecommendation = async (findingId: string) => {
    if (!onGenerateRecommendation) return;

    setLoadingStates(prev => ({ ...prev, [`rec-${findingId}`]: true }));
    try {
      const recommendation = await onGenerateRecommendation(findingId);
      setRecommendations(prev => ({ ...prev, [findingId]: recommendation }));
    } catch (error) {
      console.error('Failed to generate recommendation:', error);
    } finally {
      setLoadingStates(prev => ({ ...prev, [`rec-${findingId}`]: false }));
    }
  };

  const handleGenerateDiff = async (recommendationId: string) => {
    if (!onGenerateDiff) return;

    setLoadingStates(prev => ({ ...prev, [`diff-${recommendationId}`]: true }));
    try {
      const diff = await onGenerateDiff(recommendationId);
      setDiffs(prev => ({ ...prev, [recommendationId]: diff }));
    } catch (error) {
      console.error('Failed to generate diff:', error);
    } finally {
      setLoadingStates(prev => ({ ...prev, [`diff-${recommendationId}`]: false }));
    }
  };

  const handleApproveRecommendation = async (recommendationId: string) => {
    if (!onApproveRecommendation) return;

    setLoadingStates(prev => ({ ...prev, [`approve-${recommendationId}`]: true }));
    try {
      await onApproveRecommendation(recommendationId);
      // Update local state
      setRecommendations(prev => {
        const updated = { ...prev };
        Object.keys(updated).forEach(findingId => {
          if (updated[findingId].id === recommendationId) {
            updated[findingId] = { ...updated[findingId], approved: true };
          }
        });
        return updated;
      });
    } catch (error) {
      console.error('Failed to approve recommendation:', error);
    } finally {
      setLoadingStates(prev => ({ ...prev, [`approve-${recommendationId}`]: false }));
    }
  };

  const handleApplyRecommendation = async (recommendationId: string) => {
    if (!onApplyRecommendation) return;

    setLoadingStates(prev => ({ ...prev, [`apply-${recommendationId}`]: true }));
    try {
      await onApplyRecommendation(recommendationId);
      // Update local state to reflect application
    } catch (error) {
      console.error('Failed to apply recommendation:', error);
    } finally {
      setLoadingStates(prev => ({ ...prev, [`apply-${recommendationId}`]: false }));
    }
  };

  const handleViewDiff = (recommendation: SecurityRecommendation, diff: DiffResponse) => {
    setSelectedDiff({ recommendation, diff });
  };

  const handleOpenPrCommentDialog = () => {
    setPrCommentDialog(true);
  };

  const handleClosePrCommentDialog = () => {
    setPrCommentDialog(false);
    setPrCommentPreview('');
    setPrCommentForm({
      organization: '',
      project: '',
      pullRequestId: '',
      repositoryId: '',
      findingIds: [],
      previewOnly: true,
    });
  };

  const handlePreviewPrComment = async () => {
    if (!onPreviewPullRequestComment) return;

    setPrCommentLoading(true);
    try {
      const request: PullRequestCommentRequest = {
        organization: prCommentForm.organization,
        project: prCommentForm.project,
        pullRequestId: parseInt(prCommentForm.pullRequestId),
        repositoryId: prCommentForm.repositoryId,
        findingIds: prCommentForm.findingIds.length > 0 ? prCommentForm.findingIds : undefined,
        previewOnly: true,
      };

      const response = await onPreviewPullRequestComment(request);
      if (response.success && response.commentMarkdown) {
        setPrCommentPreview(response.commentMarkdown);
      } else {
        setSnackbar({
          open: true,
          message: response.errorMessage || 'Failed to preview comment',
          severity: 'error',
        });
      }
    } catch (error) {
      console.error('Failed to preview PR comment:', error);
      setSnackbar({
        open: true,
        message: 'Failed to preview PR comment',
        severity: 'error',
      });
    } finally {
      setPrCommentLoading(false);
    }
  };

  const handlePostPrComment = async () => {
    if (!onPostPullRequestComment) return;

    setPrCommentLoading(true);
    try {
      const request: PullRequestCommentRequest = {
        organization: prCommentForm.organization,
        project: prCommentForm.project,
        pullRequestId: parseInt(prCommentForm.pullRequestId),
        repositoryId: prCommentForm.repositoryId,
        findingIds: prCommentForm.findingIds.length > 0 ? prCommentForm.findingIds : undefined,
        previewOnly: false,
      };

      const response = await onPostPullRequestComment(request);
      if (response.success) {
        setSnackbar({
          open: true,
          message: 'PR comment posted successfully',
          severity: 'success',
        });
        handleClosePrCommentDialog();
      } else {
        setSnackbar({
          open: true,
          message: response.errorMessage || 'Failed to post comment',
          severity: 'error',
        });
      }
    } catch (error) {
      console.error('Failed to post PR comment:', error);
      setSnackbar({
        open: true,
        message: 'Failed to post PR comment',
        severity: 'error',
      });
    } finally {
      setPrCommentLoading(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" component="h1" gutterBottom>
            Security Advisor
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Review security findings and recommendations. All changes require manual approval.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<CommentIcon />}
          onClick={handleOpenPrCommentDialog}
          disabled={findings.length === 0}
        >
          Post PR Comment
        </Button>
      </Box>

      {/* Summary Stats */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 3 }}>
        <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" color="error">
                {findings.filter(f => f.severity === 'Critical').length}
              </Typography>
              <Typography variant="body2">Critical</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" color="error">
                {findings.filter(f => f.severity === 'High').length}
              </Typography>
              <Typography variant="body2">High</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" color="warning.main">
                {findings.filter(f => f.severity === 'Medium').length}
              </Typography>
              <Typography variant="body2">Medium</Typography>
            </CardContent>
          </Card>
        </Box>
        <Box sx={{ flex: '1 1 200px', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6">
                {findings.length}
              </Typography>
              <Typography variant="body2">Total</Typography>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Findings List */}
      {findings.map((finding) => (
        <Accordion
          key={finding.id}
          expanded={expandedFinding === finding.id}
          onChange={() => setExpandedFinding(expandedFinding === finding.id ? null : finding.id)}
          sx={{ mb: 1 }}
        >
          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
            <Box sx={{ display: 'flex', alignItems: 'center', width: '100%', gap: 2 }}>
              {getSeverityIcon(finding.severity)}
              <Box sx={{ flexGrow: 1 }}>
                <Typography variant="h6">{finding.title}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {finding.filePath}{finding.lineNumber ? `:${finding.lineNumber}` : ''} • {finding.category}
                </Typography>
              </Box>
              <Chip
                label={finding.severity}
                color={getSeverityColor(finding.severity) as 'error' | 'warning' | 'info' | 'default'}
                size="small"
              />
              <Chip
                label={finding.status}
                variant="outlined"
                size="small"
              />
            </Box>
          </AccordionSummary>

          <AccordionDetails>
            <Box sx={{ mb: 2 }}>
              <Typography variant="body1" sx={{ mb: 2 }}>
                {finding.description}
              </Typography>

              {finding.metadata && Object.keys(finding.metadata).length > 0 && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>Metadata:</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {Object.entries(finding.metadata).map(([key, value]) => (
                      <Chip
                        key={key}
                        label={`${key}: ${value}`}
                        size="small"
                        variant="outlined"
                      />
                    ))}
                  </Box>
                </Box>
              )}
            </Box>

            {/* Recommendation Section */}
            {recommendations[finding.id] ? (
              <Box sx={{ mt: 2 }}>
                <Typography variant="h6" sx={{ mb: 2 }}>Recommendation</Typography>
                <Paper sx={{ p: 2, mb: 2 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                    <Typography variant="subtitle1">{recommendations[finding.id].title}</Typography>
                    <Chip
                      label={`${recommendations[finding.id].confidence} (${recommendations[finding.id].confidenceScore.toFixed(2)})`}
                      color={getConfidenceColor(recommendations[finding.id].confidence) as 'success' | 'warning' | 'error' | 'default'}
                      size="small"
                    />
                    {recommendations[finding.id].approved && (
                      <Chip
                        label="Approved"
                        color="success"
                        size="small"
                        icon={<CheckCircleIcon />}
                      />
                    )}
                  </Box>

                  <Typography variant="body2" sx={{ mb: 2 }}>
                    {recommendations[finding.id].confidenceExplanation}
                  </Typography>

                  <Typography variant="body2" sx={{ mb: 2, fontWeight: 'bold' }}>
                    Risk Assessment: {recommendations[finding.id].riskAssessment}
                  </Typography>

                  <Typography variant="body2" sx={{ mb: 2 }}>
                    <strong>Remediation Steps:</strong> {recommendations[finding.id].remediationSteps}
                  </Typography>

                  <Typography variant="body2" sx={{ mb: 2 }}>
                    <strong>Code Changes:</strong> {recommendations[finding.id].codeChanges}
                  </Typography>

                  <Typography variant="body2" sx={{ mb: 2 }}>
                    <strong>Justification:</strong> {recommendations[finding.id].justification}
                  </Typography>

                  {/* Why Not Fix Reasons */}
                  {recommendations[finding.id].whyNotFixReasons.length > 0 && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                      <Typography variant="subtitle2" sx={{ mb: 1 }}>Why Not Auto-Fix:</Typography>
                      <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
                        {recommendations[finding.id].whyNotFixReasons.map((reason, index) => (
                          <Box component="li" key={index} sx={{ mb: 0.5 }}>
                            {reason}
                          </Box>
                        ))}
                      </Box>
                    </Alert>
                  )}

                  {/* Action Buttons */}
                  <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                    {!diffs[recommendations[finding.id].id] && (
                      <Button
                        variant="outlined"
                        startIcon={<CodeIcon />}
                        onClick={() => handleGenerateDiff(recommendations[finding.id].id)}
                        disabled={loadingStates[`diff-${recommendations[finding.id].id}`]}
                      >
                        {loadingStates[`diff-${recommendations[finding.id].id}`] ? 'Generating...' : 'View Diff'}
                      </Button>
                    )}

                    {diffs[recommendations[finding.id].id] && (
                      <Button
                        variant="outlined"
                        startIcon={<CodeIcon />}
                        onClick={() => handleViewDiff(recommendations[finding.id], diffs[recommendations[finding.id].id])}
                      >
                        View Diff
                      </Button>
                    )}

                    {!recommendations[finding.id].approved && (
                      <Button
                        variant="contained"
                        color="success"
                        startIcon={<ThumbUpIcon />}
                        onClick={() => handleApproveRecommendation(recommendations[finding.id].id)}
                        disabled={loadingStates[`approve-${recommendations[finding.id].id}`]}
                      >
                        {loadingStates[`approve-${recommendations[finding.id].id}`] ? 'Approving...' : 'Approve'}
                      </Button>
                    )}

                    {recommendations[finding.id].approved && (
                      <Button
                        variant="contained"
                        color="primary"
                        onClick={() => handleApplyRecommendation(recommendations[finding.id].id)}
                        disabled={loadingStates[`apply-${recommendations[finding.id].id}`]}
                      >
                        {loadingStates[`apply-${recommendations[finding.id].id}`] ? 'Applying...' : 'Apply Changes'}
                      </Button>
                    )}
                  </Box>
                </Paper>
              </Box>
            ) : (
              <Button
                variant="contained"
                onClick={() => handleGenerateRecommendation(finding.id)}
                disabled={loadingStates[`rec-${finding.id}`]}
              >
                {loadingStates[`rec-${finding.id}`] ? 'Generating...' : 'Generate Recommendation'}
              </Button>
            )}
          </AccordionDetails>
        </Accordion>
      ))}

      {/* Diff Viewer Dialog */}
      <Dialog
        open={!!selectedDiff}
        onClose={() => setSelectedDiff(null)}
        maxWidth="lg"
        fullWidth
      >
        <DialogTitle>
          Code Changes - {selectedDiff?.recommendation.title}
        </DialogTitle>
        <DialogContent>
          {selectedDiff && (
            <DiffViewer
              original={selectedDiff.diff.hunks.length > 0 ? selectedDiff.diff.hunks[0].lines.join('\n') : ''}
              modified={selectedDiff.diff.hunks.length > 0 ? selectedDiff.diff.hunks[0].lines.join('\n') : ''}
              title="Proposed Changes"
            />
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelectedDiff(null)}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* PR Comment Dialog */}
      <Dialog
        open={prCommentDialog}
        onClose={handleClosePrCommentDialog}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          Post Security Review to Pull Request
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Organization"
              value={prCommentForm.organization}
              onChange={(e) => setPrCommentForm(prev => ({ ...prev, organization: e.target.value }))}
              placeholder="your-org"
              fullWidth
            />
            <TextField
              label="Project"
              value={prCommentForm.project}
              onChange={(e) => setPrCommentForm(prev => ({ ...prev, project: e.target.value }))}
              placeholder="your-project"
              fullWidth
            />
            <TextField
              label="Pull Request ID"
              type="number"
              value={prCommentForm.pullRequestId}
              onChange={(e) => setPrCommentForm(prev => ({ ...prev, pullRequestId: e.target.value }))}
              placeholder="123"
              fullWidth
            />
            <TextField
              label="Repository ID"
              value={prCommentForm.repositoryId}
              onChange={(e) => setPrCommentForm(prev => ({ ...prev, repositoryId: e.target.value }))}
              placeholder="repo-guid"
              fullWidth
            />

            <FormControl component="fieldset">
              <FormLabel component="legend">Findings to Include</FormLabel>
              <FormControlLabel
                control={
                  <Radio
                    checked={prCommentForm.findingIds.length === 0}
                    onChange={() => setPrCommentForm(prev => ({ ...prev, findingIds: [] }))}
                  />
                }
                label="All open findings"
              />
              <FormControlLabel
                control={
                  <Radio
                    checked={prCommentForm.findingIds.length > 0}
                    onChange={() => setPrCommentForm(prev => ({
                      ...prev,
                      findingIds: findings.filter(f => f.status === 'Open').map(f => f.id)
                    }))}
                  />
                }
                label="Selected findings only"
              />
            </FormControl>

            {prCommentPreview && (
              <Box>
                <Typography variant="h6" gutterBottom>
                  Preview
                </Typography>
                <Paper
                  sx={{
                    p: 2,
                    backgroundColor: 'grey.50',
                    fontFamily: 'monospace',
                    whiteSpace: 'pre-wrap',
                    maxHeight: '300px',
                    overflow: 'auto',
                  }}
                >
                  {prCommentPreview}
                </Paper>
              </Box>
            )}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClosePrCommentDialog}>
            Cancel
          </Button>
          <Button
            onClick={handlePreviewPrComment}
            disabled={prCommentLoading || !prCommentForm.organization || !prCommentForm.project || !prCommentForm.pullRequestId || !prCommentForm.repositoryId}
            startIcon={prCommentLoading ? <CircularProgress size={16} /> : <PreviewIcon />}
          >
            Preview
          </Button>
          <Button
            onClick={handlePostPrComment}
            disabled={prCommentLoading || !prCommentPreview || !prCommentForm.organization || !prCommentForm.project || !prCommentForm.pullRequestId || !prCommentForm.repositoryId}
            variant="contained"
            startIcon={prCommentLoading ? <CircularProgress size={16} /> : <CommentIcon />}
          >
            Post Comment
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
      >
        <Alert
          onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
          severity={snackbar.severity}
          sx={{ width: '100%' }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default SecurityAdvisor;