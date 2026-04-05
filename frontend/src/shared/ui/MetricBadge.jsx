import { Chip } from '@mui/material'

export function MetricBadge({ icon, label }) {
  return (
    <Chip
      icon={icon}
      label={label}
      sx={{
        bgcolor: 'rgba(255,255,255,0.84)',
        color: '#16332d',
        borderRadius: 999,
        px: 1,
        '& .MuiChip-icon': {
          color: '#1f7a65',
        },
      }}
    />
  )
}
