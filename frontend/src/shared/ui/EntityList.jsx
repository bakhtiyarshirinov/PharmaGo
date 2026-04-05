import { Avatar, Box, Button, Chip, List, ListItem, ListItemAvatar, ListItemText, Paper, Stack, Typography } from '@mui/material'
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded'
import { EmptyState } from './EmptyState'

export function EntityList({
  items,
  primaryKey,
  titleKey,
  subtitleRenderer,
  trailingRenderer,
  metaRenderer,
  onItemClick,
  emptyLabel,
}) {
  if (!items.length) {
    return (
      <EmptyState
        icon={<AutoAwesomeRoundedIcon fontSize="large" />}
        title="Nothing here yet"
        description={emptyLabel}
      />
    )
  }

  return (
    <List sx={{ p: 0, display: 'flex', flexDirection: 'column', gap: 1.25 }}>
      {items.map((item) => (
        <ListItem
          key={item[primaryKey] || `${titleKey}-${subtitleRenderer?.(item)}`}
          disablePadding
          secondaryAction={trailingRenderer ? trailingRenderer(item) : null}
        >
          <Paper
            elevation={0}
            sx={{
              width: '100%',
              p: 1.5,
              borderRadius: 4,
              border: '1px solid rgba(22,53,47,0.08)',
              backgroundColor: 'rgba(255,255,255,0.76)',
              transition: 'transform 160ms ease, box-shadow 160ms ease',
              '&:hover': {
                transform: onItemClick ? 'translateY(-1px)' : 'none',
                boxShadow: '0 18px 36px rgba(16,48,41,0.08)',
              },
            }}
          >
            <Box
              onClick={onItemClick ? () => onItemClick(item) : undefined}
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.5,
                width: '100%',
                cursor: onItemClick ? 'pointer' : 'default',
                pr: trailingRenderer ? 8 : 0,
              }}
            >
              <ListItemAvatar>
                <Avatar sx={{ bgcolor: 'rgba(31,122,101,0.12)', color: 'primary.main' }}>
                  {(item[titleKey] || '?').slice(0, 1)}
                </Avatar>
              </ListItemAvatar>
              <ListItemText
                primary={
                  <Stack spacing={0.75}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                      {item[titleKey] || 'Untitled'}
                    </Typography>
                    {metaRenderer ? <Box>{metaRenderer(item)}</Box> : null}
                  </Stack>
                }
                secondary={
                  <Typography variant="body2" sx={{ mt: 0.35, color: 'text.secondary' }}>
                    {subtitleRenderer ? subtitleRenderer(item) : ''}
                  </Typography>
                }
              />
            </Box>
          </Paper>
        </ListItem>
      ))}
    </List>
  )
}

export function SmallAction({ children, onClick, color = 'primary' }) {
  return (
    <Button size="small" color={color} onClick={onClick}>
      {children}
    </Button>
  )
}

export function MetaChip({ label, color = 'default', variant = 'outlined' }) {
  return <Chip size="small" label={label} color={color} variant={variant} sx={{ mr: 0.75, mb: 0.5 }} />
}
