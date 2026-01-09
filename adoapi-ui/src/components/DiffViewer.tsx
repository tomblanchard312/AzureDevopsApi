import React from 'react';
import ReactDiffViewer, { DiffMethod } from 'react-diff-viewer-continued';
import { Box, Paper, IconButton, Tooltip, Typography } from '@mui/material';
import { ContentCopy as CopyIcon } from '@mui/icons-material';
import { useTheme } from '@mui/material/styles';

interface DiffViewerProps {
  original: string;
  modified: string;
  title?: string;
}

const DiffViewer: React.FC<DiffViewerProps> = ({ original, modified, title }) => {
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

  // Custom styles for the diff viewer to match MUI theme
  const diffViewerStyles = {
    variables: {
      light: {
        diffViewerBackground: theme.palette.background.paper,
        diffViewerColor: theme.palette.text.primary,
        addedBackground: theme.palette.success.main + '20', // 20% opacity
        addedColor: theme.palette.success.main,
        removedBackground: theme.palette.error.main + '20',
        removedColor: theme.palette.error.main,
        wordAddedBackground: theme.palette.success.main + '40',
        wordRemovedBackground: theme.palette.error.main + '40',
        addedGutterBackground: theme.palette.success.main + '30',
        removedGutterBackground: theme.palette.error.main + '30',
        gutterBackground: theme.palette.background.default,
        gutterBackgroundDark: theme.palette.background.default,
        highlightBackground: theme.palette.warning.main + '20',
        highlightGutterBackground: theme.palette.warning.main + '30',
        codeFoldGutterBackground: theme.palette.background.default,
        codeFoldBackground: theme.palette.background.default,
        emptyLineBackground: theme.palette.background.default,
        gutterColor: theme.palette.text.secondary,
        addedGutterColor: theme.palette.success.main,
        removedGutterColor: theme.palette.error.main,
        codeFoldContentColor: theme.palette.text.secondary,
        diffViewerTitleBackground: theme.palette.background.default,
        diffViewerTitleColor: theme.palette.text.primary,
        diffViewerTitleBorderColor: theme.palette.divider,
      },
      dark: {
        diffViewerBackground: theme.palette.background.paper,
        diffViewerColor: theme.palette.text.primary,
        addedBackground: theme.palette.success.main + '20',
        addedColor: theme.palette.success.main,
        removedBackground: theme.palette.error.main + '20',
        removedColor: theme.palette.error.main,
        wordAddedBackground: theme.palette.success.main + '40',
        wordRemovedBackground: theme.palette.error.main + '40',
        addedGutterBackground: theme.palette.success.main + '30',
        removedGutterBackground: theme.palette.error.main + '30',
        gutterBackground: theme.palette.background.default,
        gutterBackgroundDark: theme.palette.background.default,
        highlightBackground: theme.palette.warning.main + '20',
        highlightGutterBackground: theme.palette.warning.main + '30',
        codeFoldGutterBackground: theme.palette.background.default,
        codeFoldBackground: theme.palette.background.default,
        emptyLineBackground: theme.palette.background.default,
        gutterColor: theme.palette.text.secondary,
        addedGutterColor: theme.palette.success.main,
        removedGutterColor: theme.palette.error.main,
        codeFoldContentColor: theme.palette.text.secondary,
        diffViewerTitleBackground: theme.palette.background.default,
        diffViewerTitleColor: theme.palette.text.primary,
        diffViewerTitleBorderColor: theme.palette.divider,
      },
    },
    line: {
      fontFamily: 'Monaco, Menlo, "Ubuntu Mono", monospace',
      fontSize: '14px',
      lineHeight: '1.4',
    },
    gutter: {
      fontFamily: 'Monaco, Menlo, "Ubuntu Mono", monospace',
      fontSize: '14px',
    },
    codeFold: {
      fontFamily: 'Monaco, Menlo, "Ubuntu Mono", monospace',
    },
  };

  return (
    <Paper
      elevation={1}
      sx={{
        p: 2,
        backgroundColor: theme.palette.background.paper,
        border: `1px solid ${theme.palette.divider}`,
      }}
    >
      {(title || true) && ( // Always show header for copy button
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            mb: 2,
            pb: 1,
            borderBottom: `1px solid ${theme.palette.divider}`,
          }}
        >
          <Typography variant="h6" component="h3">
            {title || 'Diff Viewer'}
          </Typography>
          <Tooltip title="Copy diff to clipboard">
            <IconButton onClick={handleCopyDiff} size="small">
              <CopyIcon />
            </IconButton>
          </Tooltip>
        </Box>
      )}

      <Box
        sx={{
          fontFamily: 'Monaco, Menlo, "Ubuntu Mono", monospace',
          '& .diff-viewer': {
            borderRadius: theme.shape.borderRadius,
            overflow: 'hidden',
          },
        }}
      >
        <ReactDiffViewer
          oldValue={original}
          newValue={modified}
          splitView={false} // Unified view
          compareMethod={DiffMethod.LINES}
          styles={diffViewerStyles}
          useDarkTheme={theme.palette.mode === 'dark'}
          disableWordDiff={false}
          hideLineNumbers={false}
          showDiffOnly={false}
        />
      </Box>
    </Paper>
  );
};

export default DiffViewer;