import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    primary: { main: '#2f5bea' },
    background: { default: '#f5f7fb' },
  },
  shape: { borderRadius: 8 },
  typography: { fontFamily: 'Inter, "Segoe UI", Roboto, Arial, sans-serif' },
  components: {
    MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { textTransform: 'none' } } },
  },
})
