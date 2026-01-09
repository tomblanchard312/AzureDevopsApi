import React from 'react';
import ReactDiffViewer, { DiffMethod } from 'react-diff-viewer-continued';
import { Box, Paper, IconButton, Tooltip, Typography, Chip } from '@mui/material';
import { ContentCopy as CopyIcon, Code as CodeIcon } from '@mui/icons-material';
import { useTheme } from '@mui/material/styles';

interface DiffViewerProps {
  original: string;
  modified: string;
  title?: string;
  language?: string;
  filePath?: string;
  readOnly?: boolean;
}

const DiffViewer: React.FC<DiffViewerProps> = ({
  original,
  modified,
  title,
  language = 'text',
  filePath,
  readOnly = true
}) => {
  const theme = useTheme();

  const handleCopyDiff = async () => {
    try {
      // Create a simple unified diff format for clipboard
      const diffLines = [];
      const originalLines = original.split('\n');
      const modifiedLines = modified.split('\n');

      // Simple line-by-line diff (could be enhanced with proper diff algorithm)
      const maxLines = Math.max(originalLines.length, modifiedLines.length);

      for (let i = 0; i < maxLines; i++) {
        const origLine = originalLines[i] || '';
        const modLine = modifiedLines[i] || '';

        if (origLine !== modLine) {
          if (origLine) diffLines.push(`- ${origLine}`);
          if (modLine) diffLines.push(`+ ${modLine}`);
        } else if (origLine) {
          diffLines.push(`  ${origLine}`);
        }
      }

      const diffText = diffLines.join('\n');
      await navigator.clipboard.writeText(diffText);
    } catch (error) {
      console.error('Failed to copy diff to clipboard:', error);
    }
  };

  // Enhanced styles for GitHub-style diff viewer
  const diffViewerStyles = {
    variables: {
      light: {
        diffViewerBackground: theme.palette.background.paper,
        diffViewerColor: theme.palette.text.primary,
        addedBackground: '#e6ffed',
        addedColor: '#24292e',
        removedBackground: '#ffeef0',
        removedColor: '#24292e',
        wordAddedBackground: '#acf2bd',
        wordRemovedBackground: '#fdb8c0',
        addedGutterBackground: '#cdffd8',
        removedGutterBackground: '#ffdce0',
        gutterBackground: '#f6f8fa',
        gutterBackgroundDark: '#f6f8fa',
        highlightBackground: '#fff5b4',
        highlightGutterBackground: '#ffea7f',
        codeFoldGutterBackground: '#f6f8fa',
        codeFoldBackground: '#f6f8fa',
        emptyLineBackground: '#fafbfc',
        gutterColor: '#586069',
        addedGutterColor: '#28a745',
        removedGutterColor: '#cb2431',
        codeFoldContentColor: '#586069',
        diffViewerTitleBackground: '#f6f8fa',
        diffViewerTitleColor: '#24292e',
        diffViewerTitleBorderColor: '#e1e4e8',
      },
      dark: {
        diffViewerBackground: theme.palette.background.paper,
        diffViewerColor: theme.palette.text.primary,
        addedBackground: '#0e4429',
        addedColor: '#e6edf3',
        removedBackground: '#67060c',
        removedColor: '#e6edf3',
        wordAddedBackground: '#055d20',
        wordRemovedBackground: '#a01115',
        addedGutterBackground: '#033a16',
        removedGutterBackground: '#490202',
        gutterBackground: '#161b22',
        gutterBackgroundDark: '#161b22',
        highlightBackground: '#bb8009',
        highlightGutterBackground: '#8a4600',
        codeFoldGutterBackground: '#161b22',
        codeFoldBackground: '#161b22',
        emptyLineBackground: '#0d1117',
        gutterColor: '#8b949e',
        addedGutterColor: '#56d364',
        removedGutterColor: '#f85149',
        codeFoldContentColor: '#8b949e',
        diffViewerTitleBackground: '#161b22',
        diffViewerTitleColor: '#e6edf3',
        diffViewerTitleBorderColor: '#30363d',
      },
    },
    line: {
      fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
      fontSize: '14px',
      lineHeight: '1.45',
      padding: '0 12px',
    },
    gutter: {
      fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
      fontSize: '14px',
      padding: '0 8px',
    },
    codeFold: {
      fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
    },
    wordDiff: {
      padding: '2px 0',
    },
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

      {/* Diff Content */}
      <Box
        sx={{
          fontFamily: '"SFMono-Regular", "Monaco", "Inconsolata", "Roboto Mono", monospace',
          '& .diff-viewer': {
            borderRadius: 0,
            overflow: 'hidden',
          },
          '& .diff-viewer .gutter': {
            backgroundColor: theme.palette.mode === 'dark' ? '#161b22' : '#f6f8fa',
          },
          '& .diff-viewer .line': {
            transition: 'background-color 0.2s ease',
          },
        }}
      >
        <ReactDiffViewer
          oldValue={original}
          newValue={modified}
          splitView={false} // Unified view like GitHub
          compareMethod={DiffMethod.LINES}
          styles={diffViewerStyles}
          useDarkTheme={theme.palette.mode === 'dark'}
          disableWordDiff={false}
          hideLineNumbers={false}
          showDiffOnly={true} // Only show changed lines
          renderContent={(str) => (
            <span
              style={{
                fontFamily: 'inherit',
                fontSize: 'inherit',
                lineHeight: 'inherit',
              }}
            >
              {str}
            </span>
          )}
        />
      </Box>

      {/* Footer with stats */}
      <Box
        sx={{
          p: 1,
          borderTop: `1px solid ${theme.palette.divider}`,
          backgroundColor: theme.palette.mode === 'dark' ? '#161b22' : '#f6f8fa',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        <Typography variant="caption" color="text.secondary">
          {readOnly ? 'Read-only view' : 'Editable'}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {original.split('\n').length} → {modified.split('\n').length} lines
        </Typography>
      </Box>
    </Paper>
  );
};

export default DiffViewer;