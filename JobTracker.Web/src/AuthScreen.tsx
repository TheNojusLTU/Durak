import { useState, type FormEvent } from 'react'
import { apiRequest, type AuthUser } from './api'

export type AuthMode = 'login' | 'register'

type AuthScreenProps = {
  mode: AuthMode
  initialError: string
  onModeChange: (mode: AuthMode) => void
  onAuthenticated: (user: AuthUser) => Promise<void>
}

export function AuthScreen({ mode, initialError, onModeChange, onAuthenticated }: AuthScreenProps) {
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
      const user = await apiRequest<AuthUser>(
        registering ? '/api/auth/register' : '/api/auth/login',
        {
          method: 'POST',
          body: registering
            ? { username, displayName, password, email: email || null }
            : { username, password },
        },
      )
      await onAuthenticated(user)
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
              {registering && <small>Use at least 8 characters with an uppercase letter, a lowercase letter, and a number.</small>}
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
