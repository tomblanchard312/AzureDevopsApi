import React from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  Tooltip,
  Chip,
  useTheme,
} from '@mui/material';
import CodeIcon from '@mui/icons-material/Code';
import CopyIcon from '@mui/icons-material/ContentCopy';

interface DiffViewerProps {
  original: string;
  modified: string;
  title?: string;
  filePath?: string;
  language?: string;
}

export const DiffViewer: React.FC<DiffViewerProps> = ({
  original,
  modified,
  title,
  filePath,
  language,
}) => {
  const theme = useTheme();

  const handleCopyDiff = async () => {
    try {
      const diffContent = `--- Original\n${original}\n+++ Modified\n${modified}`;
      await navigator.clipboard.writeText(diffContent);
    } catch (error) {
      console.error('Failed to copy diff to clipboard:', error);
    }
  };

  const getLanguageLabel = (lang: string) => {
    const languageMap: Record<string, string> = {
      javascript: 'JavaScript',
      typescript: 'TypeScript',
      python: 'Python',
      java: 'Java',
      csharp: 'C#',
      cpp: 'C++',
      go: 'Go',
      rust: 'Rust',
      php: 'PHP',
      ruby: 'Ruby',
      swift: 'Swift',
      kotlin: 'Kotlin',
      scala: 'Scala',
      sql: 'SQL',
      html: 'HTML',
      css: 'CSS',
      json: 'JSON',
      xml: 'XML',
      yaml: 'YAML',
      markdown: 'Markdown',
    };
    return languageMap[lang.toLowerCase()] || lang;
  };

  return (
    <Paper
      elevation={1}
      sx={{
        backgroundColor: theme.palette.background.paper,
        border: `1px solid ${theme.palette.divider}`,
        borderRadius: 1,
        overflow: 'hidden',
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          p: 2,
          pb: 1,
          borderBottom: `1px solid ${theme.palette.divider}`,
          backgroundColor: theme.palette.mode === 'dark' ? '#161b22' : '#f6f8fa',
        }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <CodeIcon sx={{ color: theme.palette.text.secondary }} />
          <Typography variant="h6" component="h3">
            {title || 'Code Changes'}
          </Typography>
          {filePath && (
            <Chip
              label={filePath}
              size="small"
              variant="outlined"
              sx={{ ml: 1 }}
            />
          )}
          {language && language !== 'text' && (
            <Chip
              label={getLanguageLabel(language)}
              size="small"
              color="primary"
              variant="outlined"
            />
          )}
        </Box>

        <Box sx={{ display: 'flex', gap: 1 }}>
          <Tooltip title="Copy diff to clipboard">
            <IconButton onClick={handleCopyDiff} size="small">
              <CopyIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Diff Content - Simple Side-by-Side View */}
      <Box sx={{ display: 'flex', gap: 2, p: 2 }}>
        <Box flex={1}>
          <Typography variant="subtitle2" color="error.main" gutterBottom>
            Original
          </Typography>
          <Paper
            sx={{
              p: 2,
              bgcolor: theme.palette.mode === 'dark' ? '#161b22' : '#f6f8fa',
              fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
              fontSize: '14px',
              whiteSpace: 'pre-wrap',
              maxHeight: '400px',
              overflow: 'auto',
              border: `1px solid ${theme.palette.divider}`,
            }}
          >
            {original}
          </Paper>
        </Box>
        <Box flex={1}>
          <Typography variant="subtitle2" color="success.main" gutterBottom>
            Modified
          </Typography>
          <Paper
            sx={{
              p: 2,
              bgcolor: theme.palette.mode === 'dark' ? '#161b22' : '#f6f8fa',
              fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
              fontSize: '14px',
              whiteSpace: 'pre-wrap',
              maxHeight: '400px',
              overflow: 'auto',
              border: `1px solid ${theme.palette.divider}`,
            }}
          >
            {modified}
          </Paper>
        </Box>
      </Box>
    </Paper>
  );
};