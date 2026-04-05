import { Paper, Typography } from '@mui/material'

export function MetricTile({ item }) {
  const colorMap = {
    success: '#dff7e9',
    warning: '#fff2da',
    error: '#ffe4df',
    default: '#eff7f4',
  }

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2.25,
        borderRadius: 5,
        minHeight: 132,
        backgroundColor: colorMap[item.tone] || colorMap.default,
        border: '1px solid rgba(17,51,44,0.08)',
      }}
    >
      <Typography variant="overline" sx={{ color: 'text.secondary', letterSpacing: '0.12em' }}>
        {item.label}
      </Typography>
      <Typography variant="h4" sx={{ mt: 1.25 }}>
        {item.value}
      </Typography>
    </Paper>
  )
}
