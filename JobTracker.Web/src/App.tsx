import { useEffect, useState, type FormEvent } from 'react'
import './App.css'

type AuthUser = {
  id: string
  username: string
  displayName: string
  email: string | null
  avatarKey: string
}

type AuthMode = 'login' | 'register'

type ApiProblem = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

type CsrfResponse = {
  requestToken: string
  headerName: string
}

let csrfToken: CsrfResponse | null = null

class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

const readModeFromPath = (): AuthMode =>
  window.location.pathname === '/register' ? 'register' : 'login'

async function readProblem(response: Response): Promise<never> {
  const problem = await response.json().catch(() => null) as ApiProblem | null
  const fieldMessages = problem?.errors
    ? Object.values(problem.errors).flat().join(' ')
    : ''
  throw new ApiError(
    fieldMessages || problem?.detail || problem?.title || `Request failed (${response.status}).`,
    response.status,
  )
}

async function getCsrfToken(): Promise<CsrfResponse> {
  if (csrfToken) return csrfToken

  let response: Response
  try {
    response = await fetch('/api/auth/csrf', { credentials: 'include', cache: 'no-store' })
  } catch {
    throw new Error('Unable to reach JobTracker. Check your connection and try again.')
  }

  if (!response.ok) return readProblem(response)
  csrfToken = await response.json() as CsrfResponse
  return csrfToken
}

async function refreshCsrfToken(): Promise<void> {
  csrfToken = null
  await getCsrfToken()
}

async function postAuth<T>(path: string, body?: object): Promise<T> {
  const csrf = await getCsrfToken()
  let response: Response
  try {
    response = await fetch(path, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        [csrf.headerName]: csrf.requestToken,
      },
      body: body ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new Error('Unable to reach JobTracker. Check your connection and try again.')
  }

  if (!response.ok) {
    if (response.status === 400) csrfToken = null
    return readProblem(response)
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

async function getCurrentUser(): Promise<AuthUser | null> {
  let response: Response
  try {
    response = await fetch('/api/auth/me', {
      credentials: 'include',
      cache: 'no-store',
    })
  } catch {
    throw new Error('Unable to reach JobTracker. Check your connection and try again.')
  }

  if (response.status === 401) return null
  if (!response.ok) return readProblem(response)
  return response.json() as Promise<AuthUser>
}

function App() {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [mode, setMode] = useState<AuthMode>(readModeFromPath)
  const [loading, setLoading] = useState(true)
  const [sessionError, setSessionError] = useState('')

  useEffect(() => {
    let active = true
    getCurrentUser()
      .then((currentUser) => {
        if (active) setUser(currentUser)
      })
      .catch((error: unknown) => {
        if (active) {
          setSessionError(error instanceof Error ? error.message : 'Unable to load your session.')
        }
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    const handlePopState = () => setMode(readModeFromPath())
    window.addEventListener('popstate', handlePopState)
    return () => {
      active = false
      window.removeEventListener('popstate', handlePopState)
    }
  }, [])

  const navigate = (nextMode: AuthMode | 'home') => {
    const path = nextMode === 'home' ? '/' : `/${nextMode}`
    window.history.pushState({}, '', path)
    setMode(nextMode === 'home' ? 'login' : nextMode)
  }

  const handleAuthenticated = async (authenticatedUser: AuthUser) => {
    setSessionError('')
    setUser(authenticatedUser)
    window.history.pushState({}, '', '/')
    try {
      await refreshCsrfToken()
    } catch {
      setSessionError('You are signed in, but a security token could not be refreshed. Try again before your next account action.')
    }
  }

  const handleLogout = async () => {
    setSessionError('')
    try {
      await postAuth<void>('/api/auth/logout')
      setUser(null)
      navigate('login')
      try {
        await refreshCsrfToken()
      } catch {
        setSessionError('You have signed out, but a security token could not be refreshed. Try again before signing in.')
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        setUser(null)
        navigate('login')
        setSessionError('Your session has expired. Please sign in again.')
        return
      }
      setSessionError(error instanceof Error ? error.message : 'Unable to sign out. Please try again.')
    }
  }

  if (loading) {
    return <main className="session-loading" aria-live="polite">Loading your workspace…</main>
  }

  if (!user) {
    return (
      <AuthScreen
        key={sessionError}
        mode={mode}
        initialError={sessionError}
        onModeChange={navigate}
        onAuthenticated={handleAuthenticated}
      />
    )
  }

  return (
    <div className="workspace">
      <aside className="sidebar" aria-label="Main navigation">
        <a className="brand" href="#home" aria-label="JobTracker home">
          <span className="brand-mark" aria-hidden="true">J</span>
          <span>JobTracker</span>
        </a>

        <p className="nav-label">WORKSPACE</p>
        <nav className="nav-links">
          <a className="nav-link is-active" href="#overview" aria-current="page">
            <span className="nav-dot" aria-hidden="true" />Overview
          </a>
          <a className="nav-link" href="#applications">
            <span className="nav-dot" aria-hidden="true" />Applications
          </a>
        </nav>

        <div className="sidebar-foot">
          <span className="connection-dot" aria-hidden="true" />
          <span>Signed in as {user.displayName}</span>
        </div>
      </aside>

      <main className="main-panel" id="home">
        <header className="topbar">
          <p>PERSONAL WORKSPACE</p>
          <div className="topbar-account">
            <span className="topbar-date">@{user.username}</span>
            <button className="text-button" type="button" onClick={handleLogout}>Sign out</button>
          </div>
        </header>

        <section className="page-content" id="overview" aria-labelledby="page-title">
          {sessionError && <p className="notice-error" role="alert">{sessionError}</p>}
          <div className="page-heading">
            <div>
              <p className="eyebrow">OVERVIEW</p>
              <h1 id="page-title">Your next move,<br /><em>well organized.</em></h1>
              <p className="page-intro">A clear view of your job search starts here, {user.displayName}.</p>
            </div>
            <span className="heading-index" aria-hidden="true">01</span>
          </div>

          <section className="summary-row" aria-label="Application summary">
            <article className="summary-item">
              <span className="summary-label">TOTAL APPLICATIONS</span>
              <strong>0</strong>
            </article>
            <article className="summary-item">
              <span className="summary-label">IN PROGRESS</span>
              <strong>0</strong>
            </article>
            <article className="summary-item">
              <span className="summary-label">UPCOMING INTERVIEWS</span>
              <strong>0</strong>
            </article>
          </section>

          <section className="applications-section" id="applications" aria-labelledby="applications-title">
            <div className="section-heading">
              <div>
                <p className="eyebrow">YOUR TRACKER</p>
                <h2 id="applications-title">Applications</h2>
              </div>
              <span className="section-count">0 entries</span>
            </div>

            <div className="empty-state">
              <span className="empty-mark" aria-hidden="true">+</span>
              <h3>Your list starts here</h3>
              <p>When application tracking is connected, your roles and next steps will appear here.</p>
            </div>
          </section>
        </section>
      </main>
    </div>
  )
}

type AuthScreenProps = {
  mode: AuthMode
  initialError: string
  onModeChange: (mode: AuthMode) => void
  onAuthenticated: (user: AuthUser) => Promise<void>
}

function AuthScreen({ mode, initialError, onModeChange, onAuthenticated }: AuthScreenProps) {
  const [username, setUsername] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(initialError)
  const [submitting, setSubmitting] = useState(false)
  const registering = mode === 'register'

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const authenticatedUser = await postAuth<AuthUser>(
        registering ? '/api/auth/register' : '/api/auth/login',
        registering
          ? { username, displayName, password, email: email || null }
          : { username, password },
      )
      await onAuthenticated(authenticatedUser)
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : 'Unable to authenticate. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  const switchMode = (nextMode: AuthMode) => {
    setError('')
    onModeChange(nextMode)
  }

  return (
    <main className="auth-page">
      <section className="auth-story" aria-label="JobTracker introduction">
        <a className="brand auth-brand" href="/">
          <span className="brand-mark" aria-hidden="true">J</span>
          <span>JobTracker</span>
        </a>
        <div className="story-copy">
          <p className="eyebrow">MAKE YOUR NEXT MOVE</p>
          <h1>A little more<br /><em>clarity.</em></h1>
          <p>Keep every opportunity, conversation, and next step in one thoughtful workspace.</p>
        </div>
        <p className="story-foot">A calmer way to move your career forward.</p>
      </section>

      <section className="auth-panel" aria-labelledby="auth-title">
        <div className="auth-form-wrap">
          <p className="eyebrow">{registering ? 'GET STARTED' : 'WELCOME BACK'}</p>
          <h2 id="auth-title">{registering ? 'Create your account' : 'Sign in to your account'}</h2>
          <p className="auth-subtitle">
            {registering ? 'Start organizing your job search today.' : 'Pick up where your job search left off.'}
          </p>

          <form className="auth-form" onSubmit={handleSubmit}>
            {registering && (
              <>
                <label className="form-field">
                  <span>Your name</span>
                  <input
                    autoComplete="name"
                    maxLength={20}
                    minLength={3}
                    name="displayName"
                    onChange={(event) => setDisplayName(event.target.value)}
                    placeholder="Jordan Lee"
                    required
                    value={displayName}
                  />
                </label>
                <label className="form-field">
                  <span>Username</span>
                  <input
                    autoComplete="username"
                    maxLength={20}
                    minLength={3}
                    name="username"
                    onChange={(event) => setUsername(event.target.value)}
                    placeholder="jordanlee"
                    required
                    value={username}
                  />
                </label>
                <label className="form-field">
                  <span>Email <span className="optional-label">OPTIONAL</span></span>
                  <input
                    autoComplete="email"
                    name="email"
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="you@example.com"
                    type="email"
                    value={email}
                  />
                </label>
              </>
            )}
            {!registering && (
              <label className="form-field">
                <span>Username</span>
                <input
                  autoComplete="username"
                  minLength={3}
                  name="username"
                  onChange={(event) => setUsername(event.target.value)}
                  placeholder="Your username"
                  required
                  value={username}
                />
              </label>
            )}
            <label className="form-field">
              <span>Password</span>
              <input
                autoComplete={registering ? 'new-password' : 'current-password'}
                minLength={registering ? 8 : undefined}
                name="password"
                onChange={(event) => setPassword(event.target.value)}
                placeholder={registering ? 'At least 8 characters' : 'Your password'}
                required
                type="password"
                value={password}
              />
              {registering && <small>Use at least 8 characters with an uppercase letter and a number.</small>}
            </label>

            {error && <p className="form-error" role="alert">{error}</p>}

            <button className="submit-button" disabled={submitting} type="submit">
              {submitting ? 'Please wait…' : registering ? 'Create account' : 'Sign in'}
              {!submitting && <span aria-hidden="true">→</span>}
            </button>
          </form>

          <p className="auth-switch">
            {registering ? 'Already have an account?' : 'New to JobTracker?'}{' '}
            <button
              className="text-button"
              onClick={() => switchMode(registering ? 'login' : 'register')}
              type="button"
            >
              {registering ? 'Sign in' : 'Create an account'}
            </button>
          </p>
        </div>
      </section>
    </main>
  )
}

export default App
