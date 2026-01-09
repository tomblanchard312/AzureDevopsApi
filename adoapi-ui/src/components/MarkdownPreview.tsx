import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import {
  Typography,
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Divider,
  Link,
} from '@mui/material';
import { styled } from '@mui/material/styles';

interface MarkdownPreviewProps {
  markdown: string;
}

const StyledPaper = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2),
  backgroundColor: theme.palette.background.paper,
  '& pre': {
    backgroundColor: theme.palette.grey[100],
    padding: theme.spacing(1),
    borderRadius: theme.shape.borderRadius,
    overflow: 'auto',
    fontFamily: 'monospace',
    fontSize: '0.875rem',
    '& code': {
      backgroundColor: 'transparent',
      padding: 0,
      fontSize: 'inherit',
    },
  },
  '& code:not(pre code)': {
    backgroundColor: theme.palette.grey[100],
    padding: '2px 4px',
    borderRadius: '4px',
    fontFamily: 'monospace',
    fontSize: '0.875em',
  },
  '& blockquote': {
    borderLeft: `4px solid ${theme.palette.primary.main}`,
    paddingLeft: theme.spacing(2),
    marginLeft: 0,
    marginRight: 0,
    fontStyle: 'italic',
    color: theme.palette.text.secondary,
  },
  '& ul, & ol': {
    paddingLeft: theme.spacing(3),
  },
  '& li': {
    marginBottom: theme.spacing(0.5),
  },
  '& p': {
    marginBottom: theme.spacing(1),
    '&:last-child': {
      marginBottom: 0,
    },
  },
  '& hr': {
    margin: theme.spacing(2, 0),
    border: 'none',
    borderTop: `1px solid ${theme.palette.divider}`,
  },
}));

const StyledTableContainer = styled(TableContainer)(({ theme }) => ({
  margin: theme.spacing(2, 0),
  '& .MuiTableCell-root': {
    border: `1px solid ${theme.palette.divider}`,
    padding: theme.spacing(1),
  },
  '& .MuiTableHead-root .MuiTableCell-root': {
    backgroundColor: theme.palette.grey[50],
    fontWeight: 'bold',
  },
}));

const MarkdownPreview: React.FC<MarkdownPreviewProps> = ({ markdown }) => {
  return (
    <StyledPaper elevation={0}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          // Headings
          h1: ({ children }) => (
            <Typography variant="h4" component="h1" gutterBottom sx={{ mt: 3, mb: 2 }}>
              {children}
            </Typography>
          ),
          h2: ({ children }) => (
            <Typography variant="h5" component="h2" gutterBottom sx={{ mt: 3, mb: 2 }}>
              {children}
            </Typography>
          ),
          h3: ({ children }) => (
            <Typography variant="h6" component="h3" gutterBottom sx={{ mt: 2, mb: 1 }}>
              {children}
            </Typography>
          ),
          h4: ({ children }) => (
            <Typography variant="subtitle1" component="h4" gutterBottom sx={{ mt: 2, mb: 1, fontWeight: 'bold' }}>
              {children}
            </Typography>
          ),
          h5: ({ children }) => (
            <Typography variant="subtitle2" component="h5" gutterBottom sx={{ mt: 1, mb: 1, fontWeight: 'bold' }}>
              {children}
            </Typography>
          ),
          h6: ({ children }) => (
            <Typography variant="body1" component="h6" gutterBottom sx={{ mt: 1, mb: 1, fontWeight: 'bold' }}>
              {children}
            </Typography>
          ),

          // Paragraphs
          p: ({ children }) => (
            <Typography variant="body1" component="p" sx={{ mb: 2 }}>
              {children}
            </Typography>
          ),

          // Links
          a: ({ href, children }) => (
            <Link href={href} target="_blank" rel="noopener noreferrer" sx={{ wordBreak: 'break-word' }}>
              {children}
            </Link>
          ),

          // Code blocks
          pre: ({ children }) => (
            <Box component="pre" sx={{ my: 2, overflow: 'auto' }}>
              {children}
            </Box>
          ),

          // Inline code
          code: ({ children }) => (
            <Box
              component="code"
              sx={{
                backgroundColor: (theme) => theme.palette.grey[100],
                padding: '2px 4px',
                borderRadius: '4px',
                fontFamily: 'monospace',
                fontSize: '0.875em',
                wordBreak: 'break-word',
                display: 'inline',
                my: 0,
              }}
            >
              {children}
            </Box>
          ),

          // Blockquotes
          blockquote: ({ children }) => (
            <Box
              component="blockquote"
              sx={{
                borderLeft: (theme) => `4px solid ${theme.palette.primary.main}`,
                paddingLeft: 2,
                marginLeft: 0,
                marginRight: 0,
                fontStyle: 'italic',
                color: (theme) => theme.palette.text.secondary,
                my: 2,
              }}
            >
              {children}
            </Box>
          ),

          // Lists
          ul: ({ children }) => (
            <Box component="ul" sx={{ pl: 3, my: 1 }}>
              {children}
            </Box>
          ),
          ol: ({ children }) => (
            <Box component="ol" sx={{ pl: 3, my: 1 }}>
              {children}
            </Box>
          ),
          li: ({ children }) => (
            <Box component="li" sx={{ mb: 0.5 }}>
              {children}
            </Box>
          ),

          // Tables
          table: ({ children }) => (
            <StyledTableContainer>
              <Table size="small">
                {children}
              </Table>
            </StyledTableContainer>
          ),
          thead: ({ children }) => <TableHead>{children}</TableHead>,
          tbody: ({ children }) => <TableBody>{children}</TableBody>,
          tr: ({ children }) => <TableRow>{children}</TableRow>,
          th: ({ children }) => (
            <TableCell component="th" sx={{ fontWeight: 'bold', backgroundColor: (theme) => theme.palette.grey[50] }}>
              {children}
            </TableCell>
          ),
          td: ({ children }) => <TableCell>{children}</TableCell>,

          // Horizontal rule
          hr: () => <Divider sx={{ my: 2 }} />,

          // Emphasis
          strong: ({ children }) => (
            <Box component="strong" sx={{ fontWeight: 'bold' }}>
              {children}
            </Box>
          ),
          em: ({ children }) => (
            <Box component="em" sx={{ fontStyle: 'italic' }}>
              {children}
            </Box>
          ),
        }}
        // Disable HTML rendering for security
        skipHtml={true}
      >
        {markdown}
      </ReactMarkdown>
    </StyledPaper>
  );
};

export default MarkdownPreview;