import './App.css'

function App() {
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
          <span>Frontend scaffold</span>
        </div>
      </aside>

      <main className="main-panel" id="home">
        <header className="topbar">
          <p>PERSONAL WORKSPACE</p>
          <span className="topbar-date">JOB SEARCH</span>
        </header>

        <section className="page-content" id="overview" aria-labelledby="page-title">
          <div className="page-heading">
            <div>
              <p className="eyebrow">OVERVIEW</p>
              <h1 id="page-title">Your next move,<br /><em>well organized.</em></h1>
              <p className="page-intro">A clear view of your job search starts here.</p>
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

export default App