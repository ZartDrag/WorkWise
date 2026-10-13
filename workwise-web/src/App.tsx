import { WorkRequestsPage } from './features/workRequests/WorkRequestsPage'
import { AppBar, Box, Container, Toolbar, Typography } from '@mui/material'

export default function App() {
  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AppBar position="static" color="primary">
        <Toolbar>
          <Typography variant="h6" fontWeight={700}>WorkWise</Typography>
          <Typography variant="body2" sx={{ ml: 2, opacity: 0.8 }}>Work Request Tracker</Typography>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography variant="h5" fontWeight={700} gutterBottom>
          Work requests
        </Typography>
        <WorkRequestsPage />
      </Container>
    </Box>
  )
}
