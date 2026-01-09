import React from 'react';
import { Box, Typography, Paper } from '@mui/material';
import { DiffViewer } from '../components';

const DiffViewerDemo: React.FC = () => {
  const originalCode = `function helloWorld() {
  console.log("Hello, World!");
  return "greeting";
}`;

  const modifiedCode = `function helloWorld(name) {
  console.log(\`Hello, \${name}!\`);
  return "greeting";
}`;

  const originalMarkdown = `# Original Title

This is the original content.

## Section 1
Some text here.

## Section 2
More content.`;

  const modifiedMarkdown = `# Updated Title

This is the modified content with changes.

## Section 1
Some updated text here.

## New Section
Additional content added.

## Section 2
More content with modifications.`;

  return (
    <Box sx={{ p: 3, maxWidth: 1200, mx: 'auto' }}>
      <Typography variant="h4" component="h1" gutterBottom>
        DiffViewer Component Demo
      </Typography>

      <Typography variant="body1" sx={{ mb: 4 }}>
        This demonstrates the DiffViewer component with different types of content.
      </Typography>

      <Box sx={{ mb: 4 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Code Diff Example
        </Typography>
        <DiffViewer
          original={originalCode}
          modified={modifiedCode}
          title="JavaScript Function Changes"
        />
      </Box>

      <Box sx={{ mb: 4 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Markdown Diff Example
        </Typography>
        <DiffViewer
          original={originalMarkdown}
          modified={modifiedMarkdown}
          title="Documentation Changes"
        />
      </Box>

      <Paper sx={{ p: 2, backgroundColor: 'grey.50' }}>
        <Typography variant="h6" gutterBottom>
          Component Features:
        </Typography>
        <ul>
          <li>Unified diff view with syntax highlighting</li>
          <li>Additions shown in green, removals in red</li>
          <li>Monospaced font for code readability</li>
          <li>Copy-to-clipboard functionality</li>
          <li>MUI theme integration</li>
          <li>Responsive design</li>
        </ul>
      </Paper>
    </Box>
  );
};

export default DiffViewerDemo;