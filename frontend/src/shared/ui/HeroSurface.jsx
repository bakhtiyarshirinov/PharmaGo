import { Box, Button, Grid, Paper, Stack, Typography } from '@mui/material'

export function HeroSurface({
  eyebrow,
  title,
  description,
  badges,
  metrics,
  actions,
}) {
  return (
    <Paper className="hero-banner" elevation={0}>
      <Grid container spacing={3} alignItems="stretch">
        <Grid size={{ xs: 12, md: 7 }}>
          <Stack spacing={2.5}>
            {eyebrow ? <Box>{eyebrow}</Box> : null}
            <Typography variant="h2">{title}</Typography>
            <Typography variant="body1" sx={{ maxWidth: 760, color: 'rgba(10, 28, 24, 0.72)' }}>
              {description}
            </Typography>
            {badges ? (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} flexWrap="wrap">
                {badges}
              </Stack>
            ) : null}
            {actions ? (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                {actions}
              </Stack>
            ) : null}
          </Stack>
        </Grid>
        <Grid size={{ xs: 12, md: 5 }}>
          <Grid container spacing={2}>
            {metrics}
          </Grid>
        </Grid>
      </Grid>
    </Paper>
  )
}

export function HeroAction({ children, ...props }) {
  return (
    <Button size="large" variant="contained" {...props}>
      {children}
    </Button>
  )
}
