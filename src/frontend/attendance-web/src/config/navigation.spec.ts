import { describe, expect, it } from 'vitest'
import { filterNavigationItems } from './navigation'

describe('filterNavigationItems', () => {
  it('shows the expected sections for admin', () => {
    const routes = filterNavigationItems('admin').map((item) => item.route)

    expect(routes).toContain('/work-calendar')
    expect(routes).toContain('/reports')
    expect(routes).not.toContain('/system')
  })

  it('hides admin-only sections for user view', () => {
    const routes = filterNavigationItems('user').map((item) => item.route)

    expect(routes).toContain('/attendance')
    expect(routes).toContain('/absences')
    expect(routes).not.toContain('/work-assignments')
    expect(routes).not.toContain('/system')
  })

  it('shows technical sections for it view', () => {
    const routes = filterNavigationItems('it').map((item) => item.route)

    expect(routes).toContain('/system')
    expect(routes).toContain('/checkpoints')
    expect(routes).not.toContain('/work-calendar')
  })
})
