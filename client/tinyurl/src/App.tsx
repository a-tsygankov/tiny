// src/App.tsx
import React, { useEffect, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import type { RootState, AppDispatch } from './store'
import {
  fetchAllShortUrls,
  createShortUrl,
  deleteShortUrl,
  resolveShortUrl,
  UrlStatistics
} from './urlSlice.ts'

function App() {
  const dispatch = useDispatch<AppDispatch>()
  const { allStats, loading, error } = useSelector((state: RootState) => state.url)

  // Local states for the create form
  const [longUrl, setLongUrl] = useState('')
  const [createdBy, setCreatedBy] = useState('')
  const [customAlias, setCustomAlias] = useState('')

  useEffect(() => {
    // Always fetch the list on mount
    dispatch(fetchAllShortUrls())
  }, [dispatch])

  // Format date/time or show "--"
  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString || dateString === '0001-01-01T00:00:00') {
      return '--'
    }
    const d = new Date(dateString)
    return d.toLocaleString()
  }

  // Create short URL (CreatedBy is optional)
  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!longUrl) {
      alert('Long URL is required.')
      return
    }
    await dispatch(
      createShortUrl({
        longUrl,
        createdBy: createdBy.trim() === '' ? undefined : createdBy.trim(),
        customAlias
      })
    )
    // Clear fields
    setLongUrl('')
    setCreatedBy('')
    setCustomAlias('')
  }

  // Delete a row
  const handleDelete = (item: UrlStatistics) => {
    // If your back-end expects the full short URL (e.g. "http://short.ly/alias"), adjust:
    // dispatch(deleteShortUrl("http://short.ly/" + item.id.value))
    dispatch(deleteShortUrl(item.id.value))
  }

  // Resolve short URL to see the long URL
  const handleResolve = async (item: UrlStatistics) => {
    // Same idea: pass just the ID or the full short URL, depending on your back end
    const shortUrlId = item.id.value
    const resultAction = await dispatch(resolveShortUrl(shortUrlId))
    // If you store the resolved long URL in the slice, the table will re-render automatically.
    // Or you can parse the result to show an alert or update local state:
    if (resolveShortUrl.fulfilled.match(resultAction)) {
      const longUrl = resultAction.payload as string
      alert(`Long URL for ${shortUrlId}:\n${longUrl}`)
    } else {
      alert('Failed to resolve this short URL.')
    }
  }

  return (
    <div style={{ maxWidth: '800px', margin: 'auto', padding: '1rem' }}>
      <h1>My TinyUrl Service</h1>

      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      <section style={{ marginBottom: '2rem' }}>
        <h2>All Short URLs</h2>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th>Short ID</th>
              <th>Click Count</th>
              <th>Last Accessed</th>
              <th>Created At</th>
              <th>Actions</th>
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
                  <button onClick={() => handleResolve(item)}>Resolve</button>
                  {' '}
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
            Long URL (required):
            <input
              type="text"
              value={longUrl}
              onChange={(e) => setLongUrl(e.target.value)}
              required
            />
          </label>
          <label>
            Created By (optional):
            <input
              type="text"
              value={createdBy}
              onChange={(e) => setCreatedBy(e.target.value)}
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
