import { Avatar, Stack, Typography, alpha } from '@mui/material'

export function EmptyState({ icon, title, description }) {
  return (
    <Stack
      spacing={1.25}
      alignItems="center"
      justifyContent="center"
      sx={{
        px: 3,
        py: 5,
        textAlign: 'center',
        borderRadius: 4,
        border: `1px dashed ${alpha('#184038', 0.16)}`,
        backgroundColor: alpha('#f8fbfa', 0.9),
      }}
    >
      <Avatar sx={{ width: 56, height: 56, bgcolor: alpha('#1f7a65', 0.12), color: 'primary.main' }}>
        {icon}
      </Avatar>
      <Typography variant="h6">{title}</Typography>
      <Typography variant="body2" sx={{ maxWidth: 440, color: 'text.secondary' }}>
        {description}
      </Typography>
    </Stack>
  )
}
