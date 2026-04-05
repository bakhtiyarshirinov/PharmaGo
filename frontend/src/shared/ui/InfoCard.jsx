import { Paper, Typography } from '@mui/material'

export function InfoCard({ title, value }) {
  return (
    <Paper
      elevation={0}
      sx={{
        p: 2,
        borderRadius: 4,
        border: '1px solid rgba(24,58,51,0.08)',
        backgroundColor: 'rgba(244,250,248,0.9)',
      }}
    >
      <Typography variant="overline" sx={{ color: 'text.secondary', letterSpacing: '0.12em' }}>
        {title}
      </Typography>
      <Typography variant="h6" sx={{ mt: 0.5 }}>
        {value}
      </Typography>
    </Paper>
  )
}
