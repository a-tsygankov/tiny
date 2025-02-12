import React, { useEffect, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState, AppDispatch } from './store'
import {
  fetchAllShortUrls,
  createShortUrl,
  deleteShortUrl,
  UrlStatistics
} from './urlSlice.ts'

function App() {
  const dispatch = useDispatch<AppDispatch>()
  const { allStats, loading, error } = useSelector((state: RootState) => state.url)

  const [longUrl, setLongUrl] = useState('')
  const [createdBy, setCreatedBy] = useState('')
  const [customAlias, setCustomAlias] = useState('')

  useEffect(() => {
    // Load all stats on mount
    dispatch(fetchAllShortUrls())
  }, [dispatch])

  // Helper to convert date to local string or show "--" if null
  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString || dateString === '0001-01-01T00:00:00') {
      return '--'
    }
    const d = new Date(dateString)
    return d.toLocaleString() // local time format
  }

  // Handle creation form submit
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!longUrl || !createdBy) {
      alert('Long URL and CreatedBy are required')
      return
    }
    // Dispatch create
    await dispatch(createShortUrl({ longUrl, createdBy, customAlias }))
    // Clear form
    setLongUrl('')
    setCreatedBy('')
    setCustomAlias('')
  }

  // Handle delete button
  const handleDelete = (stats: UrlStatistics) => {
    const shortUrlId = stats.id.value // e.g. "gh" or "foobar"
    // In some backends, the actual short URL is "http://short.ly/foobar"
    // If you need the full short URL string, you might store it separately
    // or build it from the Id. For now, we assume the ID alone is sufficient:
    dispatch(deleteShortUrl(shortUrlId))
  }

  return (
    <div style={{ maxWidth: '800px', margin: 'auto', padding: '1rem' }}>
      <h1>My TinyUrl Service</h1>

      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      <section style={{ marginBottom: '2rem' }}>
        <h2>All Short URLs (Stats)</h2>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th>Short ID</th>
              <th>Click Count</th>
              <th>Last Accessed</th>
              <th>Created At</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {allStats.map((item) => (
              <tr key={item.id.value} style={{ borderBottom: '1px solid #ccc' }}>
                <td>{item.id.value}</td>
                <td>{item.clickCount}</td>
                <td>{formatDate(item.lastAccessed)}</td>
                <td>{formatDate(item.createdAt)}</td>
                <td>
                  <button onClick={() => handleDelete(item)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section>
        <h2>Create a New Short URL</h2>
        <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', maxWidth: '300px' }}>
          <label>
            Long URL:
            <input
              type="text"
              value={longUrl}
              onChange={(e) => setLongUrl(e.target.value)}
              required
            />
          </label>
          <label>
            Created By:
            <input
              type="text"
              value={createdBy}
              onChange={(e) => setCreatedBy(e.target.value)}
              required
            />
          </label>
          <label>
            Custom Alias (optional):
            <input
              type="text"
              value={customAlias}
              onChange={(e) => setCustomAlias(e.target.value)}
            />
          </label>
          <button type="submit" style={{ marginTop: '1rem' }}>Create</button>
        </form>
      </section>
    </div>
  )
}

export default App
