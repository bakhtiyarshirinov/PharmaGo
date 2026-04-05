import { Box, Card, CardContent, Stack, Typography, alpha } from '@mui/material'

export function SectionCard({ title, subtitle, children, accent = 'primary', actions, tone = 'default' }) {
  const glow =
    tone === 'warm'
      ? 'radial-gradient(circle at top right, rgba(239,108,63,0.12), transparent 32%)'
      : tone === 'cool'
        ? 'radial-gradient(circle at top right, rgba(31,122,101,0.12), transparent 32%)'
        : 'none'

  return (
    <Card
      elevation={0}
      sx={{
        border: `1px solid ${alpha('#15352f', 0.08)}`,
        background: `${glow}, ${alpha('#ffffff', 0.78)}`,
        backdropFilter: 'blur(18px)',
        overflow: 'hidden',
        boxShadow: `0 20px 50px ${alpha('#11312b', 0.06)}`,
      }}
    >
      <Box
        sx={{
          height: 5,
          background:
            accent === 'secondary'
              ? 'linear-gradient(90deg, #ef6c3f, #f8b37f)'
              : 'linear-gradient(90deg, #1f7a65, #6bc3aa)',
        }}
      />
      <CardContent sx={{ p: { xs: 2.25, md: 3 } }}>
        <Stack spacing={2.25}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'flex-start', flexWrap: 'wrap' }}>
            <Box>
              <Typography variant="h5">{title}</Typography>
              {subtitle ? (
                <Typography variant="body2" sx={{ mt: 0.75, color: 'text.secondary', maxWidth: 760 }}>
                  {subtitle}
                </Typography>
              ) : null}
            </Box>
            {actions ? <Box>{actions}</Box> : null}
          </Box>
          {children}
        </Stack>
      </CardContent>
    </Card>
  )
}
